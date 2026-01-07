using System.Collections.Immutable;
using codecrafters_shell.Abstractions;

namespace codecrafters_shell.Autocomplete;

public sealed class AutocompletionProvider(List<ICompletionSource> sources)
{
    private string _lastPrefix = string.Empty;
    private int _currentSuggestionIdx;

    private ImmutableList<string> Suggestions { get; set; } = [];

    public void PrepareSuggestions(string prefix)
    {
        if (!IsNewPrefix(prefix))
            return;

        Reset();
        _lastPrefix = prefix;

        var uniqueSuggestions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var suggestion in sources.SelectMany(source => source.ProvideSuggestions(_lastPrefix)))
        {
            uniqueSuggestions.Add(suggestion);
        }
        
        Suggestions = [..uniqueSuggestions.OrderBy(s => s, StringComparer.Ordinal)];
    }

    private void ToNext()
    {
        if (Suggestions.Count == 0) 
            return; 
        
        _currentSuggestionIdx = (_currentSuggestionIdx + 1) % Suggestions.Count; // Cycling suggestions is okay
    }

    private void ToPrevious()
    {
        if (_currentSuggestionIdx > 0)
            _currentSuggestionIdx--;
    }

    public int GetSuggestionCount()
    {
        return Suggestions.Count;
    }

    public IReadOnlyList<string> GetSuggestions()
    {
        return Suggestions;
    }

    public string GetCurrentSuggestion()
    {
        return Suggestions.Count > 0 && _currentSuggestionIdx < Suggestions.Count
            ? Suggestions[_currentSuggestionIdx]
            : string.Empty;
    }

    public string GetNextSuggestion()
    {
        ToNext();
        return GetCurrentSuggestion();
    }

    public string GetPreviousSuggestion()
    {
        ToPrevious();
        return GetCurrentSuggestion();
    }

    private bool IsNewPrefix(string prefix)
    {
        return prefix != _lastPrefix;
    }
    
    public void Reset()
    {
        Suggestions = [];
        _currentSuggestionIdx = 0;
        _lastPrefix = "";
    }
}