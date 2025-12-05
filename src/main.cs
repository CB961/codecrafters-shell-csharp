using Microsoft.VisualBasic;
using System.Diagnostics;

class Program
{
    private static Process? _currentProcess = null;

    internal enum ShellCommands
    {
        EXIT,
        HELP,
        ECHO,
        CD,
        TYPE,
        PWD
    }

    static void Main()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        Start();
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            e.Cancel = true;

            try
            {
                var processId = _currentProcess.Id;
                _currentProcess.Kill(entireProcessTree: true);
                Console.WriteLine("\nProcess {0} terminated.", processId);
            }
            catch
            { }
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
            string? userInput;
            Console.Write("$ ");
            userInput = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(userInput))
            {
                var s = userInput.Split(' ', 2);
                string cmd = s[0];
                string args = s.Length > 1 ? s[1] : "";

                if (TryGetBuiltin(cmd) != null)
                {
                    switch (TryGetBuiltin(s[0]))
                    {
                        case ShellCommands.EXIT:
                            HandleExit(args);
                            break;
                        case ShellCommands.ECHO:
                            HandleEcho(args);
                            break;
                        case ShellCommands.TYPE:
                            HandleType(args);
                            break;
                        default:
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
            }
        } while (true);
    }

    private static bool TryExecuteAsProcess(string program, string args)
    {
        string? filePath = FindExecutableInPath(program);

        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        return StartProcess(program, args);
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

        string? fullPath = FindExecutableInPath(args);

        PrintTypeResult(args, fullPath);
    }

    private static void HandleEcho(string args)
    {
        Console.WriteLine(args);
    }

    private static void HandleExit(string args)
    {
        if (int.TryParse(args, out int statusCode))
        {
            Environment.Exit(statusCode);
        }
        else
        {
            Environment.Exit(0);
        }
    }

    private static string? FindExecutableInPath(string command)
    {
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar == null)
        {
            return null;
        }

        string[] directories = pathVar.Split(Path.PathSeparator);

        string[] pathExt = Environment.GetEnvironmentVariable("PATHEXT")?
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            ?? [];

        foreach (string dir in directories)
        {
            string filePath = Path.Combine(dir, command);
            if (File.Exists(filePath) && HasExecutePermission(filePath))
                return filePath;

            foreach (string ext in pathExt)
            {
                string fullPath = Path.Combine(dir, command + ext);
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
        ShellCommands result;

        return Enum.TryParse<ShellCommands>(command, true, out result) ? result : null;
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
