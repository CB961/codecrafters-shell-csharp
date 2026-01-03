namespace codecrafters_shell.Abstractions;

public interface ICompletionSource
{
    IEnumerable<string> ProvideSuggestions(string prefix);
}