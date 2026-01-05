using codecrafters_shell.Abstractions;

namespace codecrafters_shell.Utilities.LineEditing;

public class SystemConsole : IConsole
{
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public void Write(char value) => Console.Write(value);

    public void Write(string value) => Console.Write(value);

    public void WriteLine(string value = "") => Console.WriteLine(value);
}