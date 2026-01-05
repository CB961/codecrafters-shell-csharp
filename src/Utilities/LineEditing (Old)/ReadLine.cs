/*namespace codecrafters_shell.LineEditing;

public class ReadLine(KeyHandler keyHandler)
{
    private ConsoleKeyInfo _readKey;

    public string Read(string prompt)
    {
        Console.Write(prompt);
        return GetText();
    }

    private string GetText()
    {
        do
        {
            _readKey = Console.ReadKey(true);
            keyHandler.Handle(_readKey);
        } while (_readKey.Key != ConsoleKey.Enter);

        var text = keyHandler.TextBuffer.ToString();
        keyHandler.Reset();
        Console.WriteLine();
        return text;
    }
}*/