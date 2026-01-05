using System.Text;
using codecrafters_shell.Autocomplete;
using codecrafters_shell.Enums;

namespace codecrafters_shell.Utilities.LineEditing;

public class LineEditor(AutocompletionProvider provider)
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

        public void Replace(WordBoundaries boundaries, string suggestion)
        {
            Text.Remove(boundaries.Start, boundaries.Length);
            Text.Insert(boundaries.Start, suggestion + " ");
            Cursor = Text.Length;
        }
    }

    #endregion

    #region Dependencies

    private readonly LineBuffer _buffer = new();

    #endregion

    #region fields

    private WordBoundaries? _wordBoundaries;
    private string _prefix = string.Empty;

    #endregion

    #region Methods

    public EditorAction HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Tab:
                return HandleTab();
            case ConsoleKey.Backspace:
                return HandleBackspace();
            case ConsoleKey.Enter:
                return EditorAction.AcceptLine;
            default:
                return char.IsControl(key.KeyChar) ? EditorAction.None : WriteChar(key.KeyChar);
        }
    }
    
    #region Handlers

    private EditorAction HandleTab()
    {
        return Autocomplete() ? EditorAction.None : EditorAction.RingBell;
    }
    
    private EditorAction HandleBackspace()
    {
        ResetAutocomplete();
        _buffer.RemoveBeforeCursor();
        return EditorAction.None;
    }
    
    private EditorAction WriteChar(char value)
    {
        ResetAutocomplete();
        _buffer.Insert(value);
        return EditorAction.None;
    }

    #endregion

    #region Autocomplete

    private bool Autocomplete()
    {
        _wordBoundaries = GetWordBoundaries();
        _prefix = _wordBoundaries != null ? GetWord(_wordBoundaries) : string.Empty;
        
        return IsContinuingAutocomplete(_prefix) ? NextAutocomplete() : StartAutocomplete();
    }

    private bool IsContinuingAutocomplete(string prefix)
    {
        return !provider.IsNewPrefix(prefix);
    }

    private bool StartAutocomplete()
    {
        provider.ProvideSuggestions(_prefix);
        var suggestion = provider.GetCurrentSuggestion();
        
        if (_wordBoundaries == null || string.IsNullOrEmpty(suggestion)) return false;
        
        AutocompleteWord(_wordBoundaries, suggestion);
        return true;
    }

    private bool NextAutocomplete()
    {
        var suggestion = provider.GetNextSuggestion();
        if (_wordBoundaries != null && !string.IsNullOrEmpty(suggestion))
        {
            AutocompleteWord(_wordBoundaries, suggestion);
            return true;
        }

        return false;
    }
    
    private void AutocompleteWord(WordBoundaries boundaries, string suggestion)
    {
        _buffer.Replace(boundaries, suggestion);
    }

    private void ResetAutocomplete()
    {
        provider.Reset();
        _wordBoundaries = null;
        _prefix = string.Empty;
    }

    #endregion

    #region Helpers

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