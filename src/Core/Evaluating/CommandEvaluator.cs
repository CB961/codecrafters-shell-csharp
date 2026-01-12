using System.Collections.Immutable;
using System.Diagnostics;
using codecrafters_shell.Core.Context;
using codecrafters_shell.Core.Parsing.Ast;
using codecrafters_shell.Interfaces;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Core.Evaluating;

public sealed class CommandEvaluator
{
    private readonly ArgumentEvaluator _argEvaluator;
    private readonly IShellContext _context;
    private readonly IPathResolver _resolver;
    private readonly IProcessExecutor _executor;

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
        var commands = plc.Commands;
        var cmdCount = plc.Commands.Count;

        var processes = new List<Process>();
        var tasks = new List<Task>();

        Process? previousProcess = null;
        StringWriter? previousOutput = null;

        try
        {
            for (var i = 0; i < cmdCount; i++)
            {
                var cmd = (SimpleCommand)commands[i];
                var name = cmd.Name.Value;
                var args = _argEvaluator.EvaluateAll(cmd.Arguments);

                var isFirst = i == 0;
                var isLast = i == cmdCount - 1;

                if (IsBuiltin(name, _context))
                {
                    _context.StdErr.WriteLine("Builtins are currently not supported in pipelines");
                    return 1;
                }
                else
                {
                    if (!ExecutableIsInPath(name, out var fullPath))
                    {
                        _context.StdErr.WriteLine($"{name}: command not found");
                        return 127;
                    }

                    var process = _executor.Start(fullPath, args, _context, !isFirst, true, true);
                    processes.Add(process);

                    if (!isFirst)
                    {
                        if (previousProcess != null)
                        {
                            var source = previousProcess.StandardOutput;
                            var dest = process.StandardInput;

                            var task = CopyStreamAsync(source, dest);
                            tasks.Add(task);
                        }
                        else if (previousOutput != null)
                        {
                            
                        }
                    }

                    if (isLast)
                    {
                        var task1 = ForwardAsync(process.StandardOutput, _context.StdOut);
                        var task2 = ForwardAsync(process.StandardError, _context.StdErr);
                        tasks.AddRange([task1, task2]);
                    }

                    previousProcess = process;
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var p in processes.Where(p => !p.HasExited))
            {
                p.WaitForExit();
            }

            return processes.Last().ExitCode;
        }
        // Pipeline command to test on Windows:
        // powershell -command "1..20 | ForEach-Object { Start-Sleep 1; \"Line $_\" } | Select-Object -First 5" 
        catch (Exception ex)
        {
            _context.StdErr.WriteLine($"Pipeline error: {ex.Message}");
            foreach (var p in processes)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                    }
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
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static async Task CopyStreamAsync(StreamReader source, StreamWriter dest)
    {
        try
        {
            await source.BaseStream.CopyToAsync(dest.BaseStream);
        }
        catch
        {
            // Ignore pipe errors
        }
        finally
        {
            dest.Close();
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
}