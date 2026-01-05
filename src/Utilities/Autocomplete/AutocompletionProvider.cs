using codecrafters_shell.Abstractions;

namespace codecrafters_shell.Autocomplete;

public sealed class AutocompletionProvider(List<ICompletionSource> sources)
{
    private string _lastPrefix = string.Empty;
    private int _currentSuggestionIndex;

    private List<string> Suggestions { get; set; } = [];

    public void ProvideSuggestions(string prefix)
    {
        _lastPrefix = IsNewPrefix(prefix) ? prefix : _lastPrefix;
        foreach (var source in sources) Suggestions.AddRange(source.ProvideSuggestions(prefix).ToList());
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

    public string GetCurrentSuggestion()
    {
        return Suggestions.Count > 0 && _currentSuggestionIndex < Suggestions.Count
            ? Suggestions[_currentSuggestionIndex]
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

    public bool IsNewPrefix(string prefix)
    {
        if (!IsCached())
            return true;

        return IsCached() && prefix != _lastPrefix;
    }

    private bool IsCached()
    {
        return !string.IsNullOrEmpty(_lastPrefix);
    }

    public void Reset()
    {
        Suggestions = [];
        _currentSuggestionIndex = 0;
        _lastPrefix = "";
    }
}