using codecrafters_shell.Abstractions;
using codecrafters_shell.Enums;

namespace codecrafters_shell.Utilities.LineEditing;

public sealed class ReadLine(LineEditor editor, ILineRenderer renderer)
{
    public string Read(string prompt)
    {
        renderer.Render(prompt);
        var text = GetText(prompt);
        
        return text;
    }

    private string GetText(string prompt)
    {
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            var action = editor.HandleKey(key);

            var text = editor.GetText();
            renderer.Render(prompt, text);

            if (action == EditorAction.AcceptLine) break;
        }

        Console.WriteLine();
        var result = editor.GetText();
        editor.ClearBuffer();
        return result;
    }
}