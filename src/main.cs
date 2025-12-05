class Program
{
    internal enum ShellCommands
    {
        EXIT,
        HELP,
        ECHO,
        CD,
        TYPE
    }

    static void Main()
    {
        ReadUserInput();
    }

    private static ShellCommands? TryGetCommand(string command)
    {
        ShellCommands result;

        return Enum.TryParse<ShellCommands>(command, true, out result) ? result : null;
    }

    private static void ReadUserInput()
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

                if (TryGetCommand(cmd) != null)
                {
                    switch (TryGetCommand(s[0]))
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
                    Console.WriteLine("{0}: command not found", userInput);
                }
            }
        } while (true);
    }

    private static void HandleType(string args)
    {
        //var cmd = TryGetCommand(args);
        //var path = Environment.GetEnvironmentVariable("path");

        //if (cmd != null)
        //{
        //    Console.WriteLine($"{cmd?.ToString().ToLower()} is a shell builtin");
        //}
        //else
        //{
        //    if (!CheckEachDirectoryInPath(args, path!))
        //    {
        //        Console.WriteLine($"{args}: not found");
        //    }
        //}
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

    private static bool IsBuiltinCommand(string command, out ShellCommands? builtin)
    {
        builtin = TryGetCommand(command);
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
