class Program
{
    internal enum ShellCommands
    { 
        help,
        echo,
        cd
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
        string userInput;

        do
        {
            userInput = Console.ReadLine();

            if (!string.IsNullOrEmpty(userInput))
            {
                if (GetCommand(userInput) == null)
                {
                    Console.WriteLine("{0}: command not found", userInput);
                }
            }
            Console.Write("$ ");
        } while (userInput != null);
    }
}
