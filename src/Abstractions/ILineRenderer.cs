namespace codecrafters_shell.Abstractions;

public interface ILineRenderer
{
    void Render(string prompt, string text = "");
    void Clear();
}