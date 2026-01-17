using System.Text;
using codecrafters_shell.Core.Autocomplete;
using codecrafters_shell.Core.History;
using codecrafters_shell.Enums;

namespace codecrafters_shell.Core.LineEditing;

public class LineEditor(AutocompletionProvider provider, CommandHistory history)
{
    #region Nested Types

    private record WordBoundaries(int Start, int Length);

    private sealed class LineBuffer
    {
        public StringBuilder Text { get; } = new();
        public int Cursor { get; private set; }

        public void Insert(char value)
        {
            Text.Insert(Cursor, value);
            Cursor++;
        }

        public void Insert(int start, string value)
        {
            Text.Insert(start, value);
            Cursor = start + value.Length;
        }

        private void MoveLeft() => Cursor = Math.Max(0, Cursor - 1);
        private void MoveRight() => Cursor = Math.Min(Text.Length, Cursor + 1);

        public void Clear()
        {
            Text.Clear();
            Cursor = 0;
        }

        public void RemoveBeforeCursor()
        {
            if (Cursor == 0) return;
            MoveLeft();
            Text.Remove(Cursor, 1);
        }

        public void Replace(WordBoundaries boundaries, string suggestion, bool trailingSpace)
        {
            Text.Remove(boundaries.Start, boundaries.Length);
            Text.Insert(boundaries.Start, suggestion + (trailingSpace ? " " : ""));
            Cursor = Text.Length;
        }
    }

    private sealed class AutocompletionState
    {
        public enum CompletionPhase
        {
            None,
            AwaitingSecondTab,
            ShowingCompletions,
            CyclingCompletions
        }

        public bool HasActiveSession { get; private set; }
        public CompletionPhase Phase { get; private set; } = CompletionPhase.None;
        public WordBoundaries? Boundaries { get; private set; }
        public string Prefix { get; private set; } = string.Empty;
        public string LastCompletion { get; private set; } = string.Empty;
        public string CompletionList { get; private set; } = string.Empty;

        public void Start(WordBoundaries boundaries, string prefix)
        {
            HasActiveSession = true;
            Boundaries = boundaries;
            Prefix = prefix;
            Phase = CompletionPhase.None;
            LastCompletion = string.Empty;
            CompletionList = string.Empty;
        }

        public void AdvancePhase(CompletionPhase phase) => Phase = phase;

        public void RecordSuggestion(string suggestion) => LastCompletion = suggestion;

        public void SetCompletionList(string[] suggestions) => CompletionList = string.Join("  ", suggestions);

        public void Reset()
        {
            HasActiveSession = false;
            Boundaries = null;
            Prefix = string.Empty;
            Phase = CompletionPhase.None;
            LastCompletion = string.Empty;
            CompletionList = string.Empty;
        }

        public override string ToString()
        {
            return $$"""
                     CompletionState
                     {
                          HasActiveSession: {{HasActiveSession}}
                          Phase: {{Phase}}
                          Boundaries: {{Boundaries?.Start ?? -1}}, {{Boundaries?.Length ?? -1}}
                          Prefix: {{Prefix}}
                          LastCompletion: {{LastCompletion}}
                          CompletionList: {{CompletionList}}
                     }
                     """;
        }
    }

    #endregion

    #region Dependencies

    private readonly LineBuffer _buffer = new();
    private readonly AutocompletionState _completionState = new();

    #endregion

    #region fields

    // private WordBoundaries? _wordBoundaries;

    #endregion

    #region Methods

    public EditorAction HandleKey(ConsoleKeyInfo key)
    {
        return key.Key switch
        {
            ConsoleKey.UpArrow => PrevHistory(),
            ConsoleKey.DownArrow => NextHistory(),
            ConsoleKey.Tab => HandleTab(),
            ConsoleKey.Backspace => HandleBackspace(),
            ConsoleKey.Enter => EditorAction.AcceptLine,
            _ => char.IsControl(key.KeyChar) ? EditorAction.Continue : WriteChar(key.KeyChar)
        };
    }

    #region Handlers
    
    private EditorAction PrevHistory()
    {
        if (history is { HasBeenBrowsed: true, IsAtFirstPos: true })
            return EditorAction.RingBell;

        var prev = history.GetPrevious();
        ReplaceCurrentInput(prev);

        return EditorAction.Continue;
    }

    private EditorAction NextHistory()
    {
        if (history is { HasBeenBrowsed: true, IsAtLastPos: true })
            return EditorAction.RingBell;
            
        var next = history.GetNext();
        ReplaceCurrentInput(next);

        return EditorAction.Continue;
    }

    private void ReplaceCurrentInput(string current)
    {
        _buffer.Clear();
        _buffer.Insert(0, current);
    }

    private EditorAction HandleTab()
    {
        var boundaries = GetWordBoundaries();
        if (boundaries == null)
            return EditorAction.RingBell;

        var prefix = GetPrefix(boundaries);

        if (!_completionState.HasActiveSession)
        {
            provider.PrepareSuggestions(prefix);
            var suggestionsCount = provider.GetSuggestionCount();
            
            if (suggestionsCount == 0)
                return EditorAction.RingBell;

            _completionState.Start(boundaries, prefix);
        }
        
        var suggestionCount = provider.GetSuggestionCount();

        return suggestionCount switch
        {
            1 => CompleteSingle(),
            _ => CompleteMultiple()
        };
    }

    private EditorAction HandleBackspace()
    {
        ResetAutocomplete();
        _buffer.RemoveBeforeCursor();
        return EditorAction.Continue;
    }

    private EditorAction WriteChar(char value)
    {
        ResetAutocomplete();
        _buffer.Insert(value);
        return EditorAction.Continue;
    }

    #endregion

    #region Autocomplete

    private string GetPrefix(WordBoundaries wordBoundaries) => GetWord(wordBoundaries);

    private EditorAction CompleteSingle()
    {
        var suggestion = provider.GetCurrentSuggestion();
        AutocompleteWord(_completionState.Boundaries!, suggestion);

        _completionState.Reset();
        return EditorAction.Continue;
    }

    private EditorAction CompleteMultiple()
    {
        var suggestions = provider.GetSuggestions();
        
        if (suggestions.Count > 1)
        {
            var lcp = GetLongestCommonPrefix(suggestions);

            if (lcp.Length > _completionState.Prefix.Length)
            {
                PartialComplete(_completionState.Boundaries!, lcp);
                
                provider.RefinePrefix(lcp);
                _completionState.Start(_completionState.Boundaries!, lcp);
                
                return EditorAction.Continue;
            }
        }
        
        switch (_completionState.Phase)
        {
            case AutocompletionState.CompletionPhase.None:
                _completionState.AdvancePhase(AutocompletionState.CompletionPhase.AwaitingSecondTab);
                return EditorAction.RingBell;
            case AutocompletionState.CompletionPhase.AwaitingSecondTab:
                _completionState.SetCompletionList([..provider.GetSuggestions()]);
                _completionState.AdvancePhase(AutocompletionState.CompletionPhase.ShowingCompletions);
                return EditorAction.ShowCompletions;
            case AutocompletionState.CompletionPhase.ShowingCompletions:
            case AutocompletionState.CompletionPhase.CyclingCompletions:
                _completionState.AdvancePhase(AutocompletionState.CompletionPhase.CyclingCompletions);
                CycleSuggestion();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return EditorAction.Continue;
    }

    private void CycleSuggestion()
    {
        var completion = string.IsNullOrEmpty(_completionState.LastCompletion)
            ? provider.GetCurrentSuggestion()
            : provider.GetNextSuggestion();
        _completionState.RecordSuggestion(completion);
        AutocompleteWord(_completionState.Boundaries!, completion);
    }

    public string GetCompletionList()
    {
        return _completionState.CompletionList;
    }

    private void AutocompleteWord(WordBoundaries boundaries, string suggestion)
    {
        _buffer.Replace(boundaries, suggestion, true);
    }

    private void PartialComplete(WordBoundaries boundaries, string commonPrefix)
    {
        _buffer.Replace(boundaries, commonPrefix, false);
    }

    private void ResetAutocomplete()
    {
        _completionState.Reset();
        provider.Reset();
    }

    #endregion

    #region Helpers

    private static string GetLongestCommonPrefix(IReadOnlyList<string> strings)
    {
        if (strings.Count == 0)
            return string.Empty;

        var copy = strings.ToList();
        
        Array.Sort([copy]);
        
        var first = strings[0];
        var last = strings[^1];
        var minLength = Math.Min(first.Length, last.Length);

        var i = 0;

        while (i < minLength && first[i] == last[i])
            i++;

        return first[..i];
    }
    
    public string GetText() => _buffer.Text.ToString();

    public void ClearBuffer() => _buffer.Clear();

    private string GetWord(WordBoundaries wordBoundaries) =>
        _buffer.Text.ToString(wordBoundaries.Start, wordBoundaries.Length);

    private WordBoundaries? GetWordBoundaries()
    {
        if (_buffer.Text.Length == 0)
            return null;

        var pos = _buffer.Cursor == _buffer.Text.Length ? _buffer.Cursor - 1 : _buffer.Cursor;
        var buffer = _buffer.Text;

        if (char.IsWhiteSpace(buffer[pos]))
            return null;

        var start = pos;
        var end = pos;

        while (start > 0 && !char.IsWhiteSpace(buffer[start - 1]))
            start--;

        while (end < buffer.Length - 1 && !char.IsWhiteSpace(buffer[end + 1]))
            end++;

        return new WordBoundaries(start, end - start + 1);
    }

    #endregion

    #endregion
}