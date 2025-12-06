using System.Diagnostics;
using System.Security;

internal static class Program
{
    private static Process? _currentProcess = null;

    private enum ShellCommands
    {
        EXIT,
        HELP,
        ECHO,
        CD,
        TYPE,
        PWD
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
            var userInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(userInput)) continue;
            var s = userInput.Split(' ', 2);
            var cmd = s[0];
            var args = s.Length > 1 ? s[1] : "";

            if (TryGetBuiltin(cmd) != null)
            {
                switch (TryGetBuiltin(s[0]))
                {
                    case ShellCommands.EXIT:
                        HandleExit(args);
                        break;
                    case ShellCommands.PWD:
                        PrintCurrentWorkingDirectory();
                        break;
                    case ShellCommands.CD:
                        HandleCd(args);
                        break;
                    case ShellCommands.ECHO:
                        HandleEcho(args);
                        break;
                    case ShellCommands.TYPE:
                        HandleType(args);
                        break;
                }
            }
            else
            {
                var result = TryExecuteAsProcess(cmd, args);

                if (!result)
                {
                    Console.WriteLine("{0}: command not found", userInput);
                }
            }
        } while (true);
    }

    private static void HandleCd(string path)
    {
        var prevPath = Environment.CurrentDirectory;
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

    private static bool TryExecuteAsProcess(string program, string args)
    {
        var filePath = FindExecutableInPath(program);

        return !string.IsNullOrEmpty(filePath) && StartProcess(program, args);
    }

    private static bool StartProcess(string fileName, string args)
    {
        try
        {
            using Process process = new();

            _currentProcess = process;

            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = args;

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
            Console.WriteLine("Exception occured: {0}", ex.Message.ToString());
            return false;
        }
        finally
        {
            _currentProcess = null;
        }
    }

    private static void HandleType(string args)
    {
        if (IsBuiltinCommand(args, out var builtin))
        {
            Console.WriteLine($"{args} is a shell builtin");
            return;
        }

        var fullPath = FindExecutableInPath(args);

        PrintTypeResult(args, fullPath);
    }

    private static void HandleEcho(string args)
    {
        Console.WriteLine(args);
    }

    private static void HandleExit(string args)
    {
        Environment.Exit(int.TryParse(args, out var statusCode) ? statusCode : 0);
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
            using var process = new System.Diagnostics.Process();
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
