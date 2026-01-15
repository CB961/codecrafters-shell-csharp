using codecrafters_shell.Abstractions;
using codecrafters_shell.Core.Context;
using codecrafters_shell.Enums;

namespace codecrafters_shell.Core.LineEditing;

public sealed class ReadLine(IConsole console, LineEditor editor, ILineRenderer renderer, List<string> history)
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

            if (action == EditorAction.ShowCompletions)
            {
                console.WriteLine();
                console.WriteLine(editor.GetCompletionList());
                renderer.Render(prompt, editor.GetText());
            }
            
            if (action == EditorAction.AcceptLine) 
                break;
        }

        console.WriteLine();
        var result = editor.GetText();
        editor.ClearBuffer();
        history.Add(result);
        return result;
    }
}