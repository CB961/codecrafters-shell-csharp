using codecrafters_shell.Abstractions;

namespace codecrafters_shell.Autocomplete;

public class BuiltinCompletionSource(IReadOnlyList<string> builtins) : ICompletionSource
{
    public IEnumerable<string> ProvideSuggestions(string prefix)
    {
        var suggestions = builtins
            .Where(s => s.StartsWith(prefix, StringComparison.Ordinal));

        return suggestions;
    }
}