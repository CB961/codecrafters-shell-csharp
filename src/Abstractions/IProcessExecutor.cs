namespace codecrafters_shell.Interfaces;

public interface IProcessExecutor
{
    int Execute(
        string filePath,
        IReadOnlyList<string> args,
        IShellContext context
    );

    void Cancel();
}