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
        var cmd = TryGetCommand(args);

        if (cmd != null)
        {
            Console.WriteLine($"{cmd?.ToString().ToLower()} is a shell builtin");
        }
        else
        {
            Console.WriteLine($"{args}: not found");
        }
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
}
