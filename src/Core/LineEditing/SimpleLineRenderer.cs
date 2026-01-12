using codecrafters_shell.Abstractions;

namespace codecrafters_shell.LineEditing;

public class SimpleLineRenderer(IConsole console) : ILineRenderer
{
    private int _lastRenderLength;
    
    public void Render(string prompt, string text = "")
    {
        console.Write('\r');

        var line = prompt + text;
        console.Write(line);

        var trailingChars = _lastRenderLength - line.Length;
        if (trailingChars > 0)
        {
            console.Write(new string(' ', trailingChars));
            console.Write('\r');
            console.Write(line);
        }

        _lastRenderLength = line.Length;
    }

    public void Clear()
    {
        console.Write('\r');
        console.Write(new string(' ', _lastRenderLength));
        console.Write('\r');
        _lastRenderLength = 0;
    }
}