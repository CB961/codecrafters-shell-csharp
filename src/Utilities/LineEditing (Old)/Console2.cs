/*using codecrafters_shell.Abstractions;

namespace codecrafters_shell.LineEditing;

public class Console2 : IConsole
{
    public int BufferWidth => Console.BufferWidth;
    public int BufferHeight => Console.BufferHeight;
    public int CursorLeft => Console.CursorLeft;
    public int CursorTop => Console.CursorTop;

    public void SetCursorPosition(int left, int top)
    {
        Console.SetCursorPosition(left, top);
    }

    public void SetBufferSize(int width, int height)
    {
        if (OperatingSystem.IsWindows()) Console.SetBufferSize(width, height);
    }

    public void Write(string value)
    {
        Console.Write(value);
    }

    public void WriteLine(string value)
    {
        Console.WriteLine(value);
    }
}*/