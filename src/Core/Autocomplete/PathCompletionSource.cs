using System.Collections.Immutable;
using codecrafters_shell.Abstractions;
using codecrafters_shell.PathResolving;

namespace codecrafters_shell.Core.Autocomplete;

public class PathCompletionSource(PathResolver resolver) : ICompletionSource
{
    public IEnumerable<string> ProvideSuggestions(string prefix)
    {
        var executables = GetExecutablesFromPath();
        var suggestions = executables.Where(exe => exe.StartsWith(prefix, StringComparison.Ordinal));

        return suggestions;
    }

    private ImmutableArray<string> GetExecutablesFromPath()
    {
        return resolver.GetExecutablesFromPath();
    }
}