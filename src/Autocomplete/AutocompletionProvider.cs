using codecrafters_shell.Abstractions;

namespace codecrafters_shell.Autocomplete;

public sealed class AutocompletionProvider
{
    private string _lastAutocompleteWord = string.Empty;

    private int _currentSuggestionIndex; 
    private List<string> Suggestions { get; } = [];
    
    public void ProvideSuggestions(string prefix, List<ICompletionSource> sources)
    {
        _lastAutocompleteWord = IsNewPrefix(prefix) ? prefix : _lastAutocompleteWord;
        foreach (var source in sources)
        {
            Suggestions.AddRange(source.ProvideSuggestions(prefix).ToList());
        }
    }

    private void ToNext()
    {
        if (_currentSuggestionIndex < Suggestions.Count - 1)
            _currentSuggestionIndex++;
    }

    private void ToPrevious()
    {
        if (_currentSuggestionIndex > 0)
            _currentSuggestionIndex--;
    }
    
    public string GetCurrentSuggestion() => 
        Suggestions.Count > 0 && _currentSuggestionIndex < Suggestions.Count
            ? Suggestions[_currentSuggestionIndex] 
            : string.Empty;

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

    public bool IsNewPrefix(string prefix)
    {
        if (!IsCached())
            return true;

        return IsCached() && prefix != _lastAutocompleteWord;
    }
    
    private bool IsCached()
    {
        return !string.IsNullOrEmpty(_lastAutocompleteWord);
    }
}