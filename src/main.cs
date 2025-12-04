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
                if (GetCommand(userInput) == null)
                {
                    Console.WriteLine("{0}: command not found", userInput);
                }

                if (GetCommand(userInput) == ShellCommands.EXIT)
                {
                    break;
                }
            }
            Console.Write("$ ");
        } while (true);
    }
}
