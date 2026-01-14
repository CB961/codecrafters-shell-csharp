using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipelines;
using codecrafters_shell.Core.Context;
using codecrafters_shell.Core.Parsing.Ast;
using codecrafters_shell.Enums;
using codecrafters_shell.Interfaces;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Core.Evaluating;

public sealed class CommandEvaluator
{
    #region Nested Types
    
        private sealed class PipelineStage(string name, List<string> args, StageKind kind)
        {
            public string Name { get; } = name;
            public List<string> Args { get; } = args;
    
            public Pipe? InputPipe { get; set; }
            public Pipe? OutputPipe { get; set; }
    
            public Process? Process { get; set; }
            public Task<int>? BuiltinTask { get; set; }
    
            public StageKind Kind { get; } = kind;
        }
    
        #endregion
    
    #region Dependencies
    
    private readonly ArgumentEvaluator _argEvaluator;
    private readonly IShellContext _context;
    private readonly IPathResolver _resolver;
    private readonly IProcessExecutor _executor;

    #endregion
    
    #region Constructors

    public CommandEvaluator(
        IShellContext context,
        IPathResolver resolver,
        IProcessExecutor executor
    )
    {
        _context = context;
        _resolver = resolver;
        _argEvaluator = new ArgumentEvaluator(_context);
        _executor = executor;
    }

    #endregion

    #region Methods

    public int Evaluate(CommandTree commandTree)
    {
        return EvaluatePipeline((commandTree.Root as PipelineCommand)!);
    }

    private int EvaluatePipeline(PipelineCommand plc)
    {
        return plc.Commands.Count switch
        {
            0 => -1,
            1 => ExecuteSingleCommand((plc.Commands[0] as SimpleCommand)!, _context),
            _ => ExecutePipeline(plc)
        };
    }

    private int ExecutePipeline(PipelineCommand plc)
    {
        if (plc.Commands.Count == 0)
            return -1;
        
        // Staging phase
        var stages = plc.Commands
            .Cast<SimpleCommand>()
            .Select(cmd =>
            {
                var name = cmd.Name.Value;
                var args = _argEvaluator.EvaluateAll(cmd.Arguments).ToList();
                var kind = IsBuiltin(name, _context) ? StageKind.Builtin : StageKind.External;
                return new PipelineStage(name, args, kind);
            })
            .ToList();

        // Creating pipes between stages
        CreatePipes(stages);

        var processes = new List<Process>();
        var tasks = new List<Task>();

        try
        {
            var wireResult = WireStages(stages, processes, tasks);

            if (wireResult != 0)
                return wireResult;

            var lastStage = stages[^1];
            CreateForwardingProcessOutputTasks(lastStage, ref tasks);
            
            Task.WaitAll(tasks.ToArray());

            foreach (var stage in stages.Where(stage => stage.Kind == StageKind.External && !stage.Process!.HasExited))
            {
                stage.Process!.WaitForExit();
            }

            foreach (var stage in stages)
            {
                stage.BuiltinTask?.Wait();
            }
            
            return lastStage.Kind switch
            {
                StageKind.External => lastStage.Process!.ExitCode,
                StageKind.Builtin => lastStage.BuiltinTask!.Result,
                _ => -1
            };
        }
        catch (Exception ex)
        {
            _context.StdErr.WriteLine($"Pipeline error: {ex.Message}");

            foreach (var p in processes)
            {
                try
                {
                    if (!p.HasExited)
                        p.Kill();
                }
                catch
                {
                    // Ignore
                }
            }

            return -1;
        }
        finally
        {
            foreach (var p in processes)
                p.Dispose();
        }
    }

    #region Pipeline helpers

    private static void CreatePipes(List<PipelineStage> stages)
    {
        for (var i = 0; i < stages.Count - 1; i++)
        {
            var pipe = new Pipe();

            stages[i].OutputPipe = pipe;
            stages[i + 1].InputPipe = pipe;
        }
    }

    private void CreateForwardingProcessOutputTasks(PipelineStage lastStage, ref List<Task> tasks)
    {
        var lastProcess = lastStage.Process!;

        if (lastStage.Kind != StageKind.External) return;
        tasks.Add(ForwardAsync(lastProcess.StandardOutput, _context.StdOut));
        tasks.Add(ForwardAsync(lastProcess.StandardError, _context.StdErr));
    }

    private int WireStages(List<PipelineStage> stages, List<Process> processes, List<Task> tasks)
    {
        foreach (var stage in stages)
        {
            if (stage.Kind == StageKind.Builtin)
            {
                stage.BuiltinTask = Task.Run(async () =>
                {
                    var inputReader =
                        stage.InputPipe != null
                            ? new StreamReader(stage.InputPipe.Reader.AsStream())
                            : _context.StdIn;

                    var outputWriter =
                        stage.OutputPipe != null
                            ? new StreamWriter(stage.OutputPipe.Writer.AsStream()) { AutoFlush = true }
                            : _context.StdOut;

                    var ctx = new ShellContext(
                        _context.Builtins,
                        inputReader,
                        outputWriter,
                        _context.StdErr
                    );

                    ExecuteBuiltin(stage.Name, stage.Args.ToArray(), ctx);

                    if (stage.OutputPipe != null)
                        await stage.OutputPipe.Writer.CompleteAsync();

                    return 0;
                });
            }
            else
            {
                if (!ExecutableIsInPath(stage.Name, out var fullPath))
                {
                    _context.StdErr.WriteLine($"{stage.Name}: command not found");
                    return 127;
                }

                var isFirst = stage.InputPipe == null;

                var process = _executor.Start(
                    fullPath,
                    stage.Args,
                    _context,
                    redirectInput: !isFirst,
                    redirectOutput: true,
                    redirectError: true
                );

                stage.Process = process;
                processes.Add(process);

                if (stage.InputPipe != null)
                {
                    tasks.Add(
                        PipeToStreamAsync(
                            stage.InputPipe.Reader,
                            process.StandardInput.BaseStream
                        )
                    );
                }

                if (stage.OutputPipe != null)
                {
                    tasks.Add(
                        StreamToPipeAsync(
                            process.StandardOutput.BaseStream,
                            stage.OutputPipe.Writer
                        )
                    );
                }
            }
        }

        return 0;
    }
    
    private static async Task StreamToPipeAsync(
        Stream source,
        PipeWriter writer,
        CancellationToken ct = default
    )
    {
        try
        {
            while (true)
            {
                var memory = writer.GetMemory(4096);
                var bytesRead = await source.ReadAsync(memory, ct);

                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);

                var result = await writer.FlushAsync(ct);
                if (result.IsCompleted)
                    break;
            }
        }
        catch
        {
            // Ignore errors
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }
    
    private static async Task PipeToStreamAsync(
        PipeReader reader,
        Stream destination,
        CancellationToken ct = default
    )
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(ct);
                var buffer = result.Buffer;

                foreach (var segment in buffer)
                {
                    await destination.WriteAsync(segment, ct);
                }

                reader.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                    break;
            }
        }
        catch
        {
            // Ignore errors
        }
        finally
        {
            await reader.CompleteAsync();
            await destination.FlushAsync(ct);
            destination.Close();
        }
    }

    private static async Task ForwardAsync(
        StreamReader reader,
        TextWriter writer)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }
    }

    #endregion
    
    /*private int ExecutePipeline(PipelineCommand plc)
    {
        var commands = plc.Commands;
        var cmdCount = commands.Count;

        var tasks = new List<Task>();
        var processes = new List<Process>();

        Pipe? previousPipe = null;
        Process? previousProcess = null;

        try
        {
            for (var i = 0; i < cmdCount; i++)
            {
                var cmd = (SimpleCommand)commands[i];
                var name = cmd.Name.Value;
                var args = _argEvaluator.EvaluateAll(cmd.Arguments);

                var isFirst = i == 0;
                var isLast = i == cmdCount - 1;

                var currentPipe = isLast ? null : new Pipe();

                var stdin =
                    i == 0
                        ? ReaderToStream(_context.StdIn)
                        : previousPipe!.Reader.AsStream();

                var stdout =
                    isLast
                        ? previousPipe!.Writer.AsStream()
                        : currentPipe!.Writer.AsStream();

                if (IsBuiltin(name, _context))
                {
                    Task.Run(() =>
                    {
                        using var reader = new StreamReader(stdin);
                        using var writer = new StreamWriter(stdout);
                        writer.AutoFlush = true;

                        var ctx = new ShellContext(
                            _context.Builtins,
                            reader,
                            writer,
                            _context.StdErr
                        );

                        ExecuteBuiltin(name, args.ToArray(), ctx);
                        writer.Flush();
                        stdout.Close();
                    });

                    previousPipe = currentPipe;
                    // _context.StdErr.WriteLine("Builtins are currently not supported in pipelines");
                    // return 1;
                }
                else
                {
                    if (!ExecutableIsInPath(name, out var fullPath))
                    {
                        _context.StdErr.WriteLine($"{name}: command not found");
                        return 127;
                    }

                    var process = _executor.Start(
                        fullPath,
                        args,
                        _context,
                        !isFirst,
                        true,
                        true
                    );
                    processes.Add(process);

                    if (previousProcess != null && previousPipe != null)
                    {
                        tasks.Add(StreamToPipeAsync(
                            previousProcess.StandardOutput.BaseStream,
                            previousPipe.Writer)
                        );

                        tasks.Add(PipeToStreamAsync(
                            previousPipe.Reader,
                            process.StandardInput.BaseStream)
                        );
                    }

                    if (isLast)
                    {
                        tasks.Add(ForwardAsync(process.StandardOutput, _context.StdOut));
                        tasks.Add(ForwardAsync(process.StandardError, _context.StdErr));
                    }

                    previousProcess = process;
                    previousPipe = currentPipe;
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var p in processes.Where(p => !p.HasExited))
            {
                p.WaitForExit();
            }

            return processes.Last().ExitCode;
        }
        catch (Exception ex)
        {
            _context.StdErr.WriteLine($"Pipeline error: {ex.Message}");

            foreach (var p in processes)
            {
                try
                {
                    if (!p.HasExited)
                        p.Kill();
                }
                catch
                {
                    // Ignore
                }
            }

            return -1;
        }
        finally
        {
            foreach (var p in processes)
                p.Dispose();
        }
    }*/

    private int ExecuteSingleCommand(SimpleCommand sc,
        IShellContext context,
        bool redirectInput = false,
        bool redirectOutput = true,
        bool redirectError = true
    )
    {
        var cmd = sc.Name.Value;
        var args = _argEvaluator.EvaluateAll(sc.Arguments);

        // ReSharper disable once InvertIf
        if (HasRedirects(sc.Redirects))
        {
            using var redirectScope = new RedirectScope(sc.Redirects, context, _argEvaluator);
            return ExecuteCommand(cmd, args, redirectScope.Context, redirectInput, redirectOutput, redirectError);
        }

        return ExecuteCommand(cmd, args, context, redirectInput, redirectOutput, redirectOutput);
    }

    private int ExecuteCommand(string command, IReadOnlyList<string> arguments, IShellContext context,
        bool redirectInput, bool redirectOutput, bool redirectError)
    {
        const int errorCommandNotFound = 0x16;

        if (IsBuiltin(command, context)) return ExecuteBuiltin(command, arguments.ToArray(), context);

        if (ExecutableIsInPath(command, out var fullPath))
        {
            var process = StartProcess(fullPath, arguments.ToArray(), context, redirectInput, redirectOutput,
                redirectError);

            if (process.StartInfo.RedirectStandardInput)
            {
                process.StandardInput.Write(context.StdIn.ReadToEnd());
                process.StandardInput.Close();
            }

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) context.StdOut.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) context.StdErr.WriteLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            return process.ExitCode;
        }

        context.StdErr.WriteLine($"{command}: command not found");
        return errorCommandNotFound;
    }


    private static bool HasRedirects(ImmutableList<RedirectNode> redirects)
    {
        return redirects.Count > 0;
    }

    private static bool IsBuiltin(string command, IShellContext context)
    {
        return context.Builtins.ContainsKey(command);
    }

    private static int ExecuteBuiltin(string command, string[] arguments, IShellContext context)
    {
        var result = context.Builtins.TryGetValue(command, out var handler);

        return result ? handler!.Invoke(arguments, context) : 1;
    }

    private bool ExecutableIsInPath(string executableName, out string fullPath)
    {
        fullPath = _resolver.FindExecutableInPath(executableName) ?? string.Empty;
        return !string.IsNullOrEmpty(fullPath);
    }

    private Process StartProcess(string fullPath, string[] args, IShellContext context, bool redirectInput,
        bool redirectOutput, bool redirectError)
        => _executor.Start(fullPath, args, context, redirectInput, redirectOutput, redirectError);

    #endregion
}