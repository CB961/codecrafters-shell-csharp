namespace codecrafters_shell.Abstractions;

public interface IConsole
{
    int BufferWidth { get; }
    int BufferHeight { get; }
    int CursorLeft { get; }
    int CursorTop { get; }
    void SetCursorPosition(int left, int top);
    void SetBufferSize(int width, int height);
    void Write(string value);
    void WriteLine(string value);
}