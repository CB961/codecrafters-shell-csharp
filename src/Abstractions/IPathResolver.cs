namespace codecrafters_shell.src.Interfaces;

public interface IPathResolver
{
    string? FindExecutableInPath(string command);
}