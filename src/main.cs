class Program
{
    internal enum ShellCommands
    {
        EXIT,
        HELP,
        ECHO,
        CD
    }

    static void Main()
    {
        // TODO: Uncomment the code below to pass the first stage
        Console.Write("$ ");

        ReadUserInput();
    }

    private static ShellCommands? GetCommand(string command)
    {
        ShellCommands result;

        return Enum.TryParse<ShellCommands>(command, true, out result) ? result : null;
    }

    private static void ReadUserInput()
    {
        string? userInput;

        do
        {
            userInput = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(userInput))
            {
                var s = userInput.Split(' ');
                var cmd = s[0];
                string args = string.Empty;

                if (GetCommand(cmd) != null)
                {
                    for (int i = 1; i < s.Length; i++)
                    {
                        args += $"{s[i]} ";
                    }

                    args = args.Trim();

                    switch (GetCommand(s[0]))
                    {
                        case ShellCommands.EXIT:
                            return;
                        case ShellCommands.ECHO:
                            Console.WriteLine(args);
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
            Console.Write("$ ");
        } while (true);
    }
}
