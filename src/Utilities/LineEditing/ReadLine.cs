using codecrafters_shell.Abstractions;
using codecrafters_shell.Enums;

namespace codecrafters_shell.Utilities.LineEditing;

public sealed class ReadLine(IConsole console, LineEditor editor, ILineRenderer renderer)
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
            var key = console.ReadKey(intercept: true);
            var action = editor.HandleKey(key);

            if (action == EditorAction.RingBell)
            {
                console.Write('\a');
                continue;
            }
            
            renderer.Render(prompt, editor.GetText());
            
            if (action == EditorAction.AcceptLine) 
                break;
        }

        console.WriteLine();
        var result = editor.GetText();
        editor.ClearBuffer();
        return result;
    }
}