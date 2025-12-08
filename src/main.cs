using System.Diagnostics;
using System.Security;
using System.Text;

internal static class Program
{
    private static Process? _currentProcess = null;

    private enum ShellCommands
    {
        Cd,
        Echo,
        Exit,
        Help,
        Pwd,
        Type
    }

    private static void Main()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        Start();
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (_currentProcess is { HasExited: false })
        {
            e.Cancel = true;

            try
            {
                var processId = _currentProcess.Id;
                _currentProcess.Kill(entireProcessTree: true);
                Console.WriteLine("\nProcess {0} terminated.", processId);
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
        else
        {
            e.Cancel = false;
        }
    }

    private static void Start()
    {
        do
        {
            Console.Write("$ ");
            var userInput = Console.ReadLine()?.Trim() ?? string.Empty;

            var tokenizedInput = Tokenize(userInput);
            if (tokenizedInput.Count == 0) continue;

            var cmd = tokenizedInput[0];
            var args = tokenizedInput.Count > 1 ? tokenizedInput.Skip(1).ToArray() : [];

            if (TryGetBuiltin(cmd) != null)
            {
                switch (TryGetBuiltin(cmd))
                {
                    case ShellCommands.Exit:
                        HandleExit(args);
                        break;
                    case ShellCommands.Pwd:
                        PrintCurrentWorkingDirectory();
                        break;
                    case ShellCommands.Cd:
                        HandleCd(args);
                        break;
                    case ShellCommands.Echo:
                        HandleEcho(args);
                        break;
                    case ShellCommands.Type:
                        HandleType(args);
                        break;
                }
            }
            else
            {
                var result = TryExecuteAsProcess(cmd, args);

                if (!result)
                {
                    Console.WriteLine("{0}: command not found", cmd);
                }
            }
        } while (true);
        // ReSharper disable once FunctionNeverReturns
    }

    private static List<string> Tokenize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [];
        }

        var tokens = new List<string>();
        var currentToken = new StringBuilder();
        var inSingleQuotes = false;
        var inDoubleQuotes = false;
        var isEscaping = false;

        foreach (var c in input)
        {
            switch (c)
            {
                case '\\' when !inSingleQuotes && !inDoubleQuotes && !isEscaping:
                    isEscaping = !isEscaping;
                    continue;
                case '\'' when !inDoubleQuotes && !isEscaping:
                    inSingleQuotes = !inSingleQuotes;
                    continue;
                case '\"' when !inSingleQuotes && !isEscaping:
                    inDoubleQuotes = !inDoubleQuotes;
                    continue;
            }

            if (isEscaping)
            {
                currentToken.Append(c);
                isEscaping = !isEscaping;
                continue;
            }

            if (!inSingleQuotes && !inDoubleQuotes && char.IsWhiteSpace(c))
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                continue;
            }

            currentToken.Append(c);
        }

        if (inDoubleQuotes || inSingleQuotes)
        {
            throw new Exception("Unmatched quotes in input");
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }

    private static void HandleCd(string[] args)
    {
        var prevPath = Environment.CurrentDirectory;
        var path = args.Length > 0 ? args[0] : "~";

        if (path == "~")
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
                    var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
                    path = Path.Combine(homeDrive + homePath ?? prevPath);
                }
                else
                {
                    path = Environment.GetEnvironmentVariable("HOME") ?? prevPath;
                }
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("Could not find variable referring to home directory of the user");
                path = prevPath;
            }
            catch (SecurityException ex)
            {
                Console.WriteLine("Caller does not have the required permissions: \n{0}", ex.Message);
                path = prevPath;
            }
        }

        var exists = DirectoryExists(path!);

        if (!exists)
        {
            Console.WriteLine($"{path}: No such file or directory");
            return;
        }

        Environment.CurrentDirectory = path!;
    }

    private static bool DirectoryExists(string path)
    {
        return Path.Exists(path);
    }

    private static void PrintCurrentWorkingDirectory()
    {
        Console.WriteLine(Environment.CurrentDirectory);
    }

    private static bool TryExecuteAsProcess(string program, string[] args)
    {
        var filePath = FindExecutableInPath(program);
        return !string.IsNullOrEmpty(filePath) && StartProcess(program, args);
    }

    private static bool StartProcess(string fileName, string[] args)
    {
        try
        {
            using Process process = new();

            _currentProcess = process;

            process.StartInfo.FileName = fileName;
            process.StartInfo.ArgumentList.Clear();
            foreach (var arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occured: {0}", ex.Message);
            return false;
        }
        finally
        {
            _currentProcess = null;
        }
    }

    private static void HandleType(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        foreach (var arg in args)
        {
            if (IsBuiltinCommand(arg, out var builtin))
            {
                Console.WriteLine($"{arg} is a shell builtin");
                return;
            }

            var fullPath = FindExecutableInPath(arg);
            PrintTypeResult(arg, fullPath);
        }
    }

    private static void HandleEcho(string[] args)
    {
        Console.WriteLine(string.Join(' ', args));
    }

    private static void HandleExit(string[] args)
    {
        var code = 0;

        if (args.Length > 0)
        {
            // ReSharper disable once UnusedVariable
            var succeeded = int.TryParse(args[0], out code);
        }

        Environment.Exit(code);
    }

    private static string? FindExecutableInPath(string command)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar == null)
        {
            return null;
        }

        var directories = pathVar.Split(Path.PathSeparator);

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT")?
                          .Split(';', StringSplitOptions.RemoveEmptyEntries)
                      ?? [];

        foreach (var dir in directories)
        {
            var filePath = Path.Combine(dir, command);
            if (File.Exists(filePath) && HasExecutePermission(filePath))
                return filePath;

            foreach (var ext in pathExt)
            {
                var fullPath = Path.Combine(dir, command + ext);
                if (File.Exists(fullPath) && HasExecutePermission(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static ShellCommands? TryGetBuiltin(string command)
    {
        return Enum.TryParse<ShellCommands>(command, true, out var result) ? result : null;
    }

    private static bool IsBuiltinCommand(string command, out ShellCommands? builtin)
    {
        builtin = TryGetBuiltin(command);
        return builtin != null;
    }

    private static void PrintTypeResult(string command, string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine($"{command}: not found");
            return;
        }

        Console.WriteLine($"{command} is {filePath}");
    }

    private static bool HasExecutePermission(string? filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "test";
            process.StartInfo.Arguments = $"-x \"{filePath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}