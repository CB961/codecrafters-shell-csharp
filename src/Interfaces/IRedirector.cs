namespace codecrafters_shell.Interfaces;

public interface IRedirector
{
    IShellContext CreateRedirectedContext(IShellContext context);
    void Cleanup();
}