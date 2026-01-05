/*using System.Text;
using codecrafters_shell.Abstractions;
using codecrafters_shell.Autocomplete;
using codecrafters_shell.Interfaces;

namespace codecrafters_shell.LineEditing;

public record WordBoundaries(int Start, int Length);

public class KeyHandler
{
    #region Dependencies

    private readonly AutocompletionProvider _autocompletionProvider;
    private readonly Console2 _console = new();
    private readonly Dictionary<string, Action> _inputHandles = [];
    private readonly IShellContext _context;
    private readonly ILineRenderer _renderer = new SimpleLineRenderer();

    #endregion

    #region Fields

    private int _cursorPos;
    private int _cursorLimit;
    private ConsoleKeyInfo _keyInfo;
    private WordBoundaries? _wordBoundaries;
    private string _prefix = string.Empty;

    #endregion

    #region Properties

    public StringBuilder TextBuffer { get; } = new();

    #endregion

    #region Constructors

    public KeyHandler(IShellContext context, AutocompletionProvider autocompletionProvider)
    {
        _inputHandles.Add("Tab", Autocomplete);
        _inputHandles.Add("Backspace", Backspace);
        _inputHandles.Add("LeftArrow", MoveCursorLeft);
        _inputHandles.Add("RightArrow", MoveCursorRight);
        _inputHandles.Add("UpArrow", PrevHistory);
        _inputHandles.Add("DownArrow", NextHistory);
        _inputHandles.Add("Delete", Delete);
        _context = context;
        _autocompletionProvider = autocompletionProvider;
    }

    #endregion

    #region Methods

    public void Handle(ConsoleKeyInfo keyInfo)
    {
        _keyInfo = keyInfo;
        var keyInput = ConstructKeyInput();

        // Check for key commands
        _inputHandles.TryGetValue(keyInput, out var keyAction);
        keyAction ??= WriteCharacter;
        keyAction.Invoke();
    }

    private string ConstructKeyInput()
    {
        var keyString = _keyInfo.Key.ToString();
        return _keyInfo.Modifiers is ConsoleModifiers.Control or ConsoleModifiers.Alt
            ? $"{keyString}+" + _keyInfo.Key
            : keyString;
    }

    private void Autocomplete()
    {
        _wordBoundaries = GetWordBoundaries();
        _prefix = _wordBoundaries != null ? GetWord(_wordBoundaries) : string.Empty;
        var builtinSource = new BuiltinCompletionSource(_context.Builtins.Keys.ToList());

        if (IsInAutocompleteMode(_prefix))
        {
            NextAutocomplete();
            return;
        }

        StartAutocomplete([builtinSource]);
    }

    private bool IsInAutocompleteMode(string prefix)
    {
        return !_autocompletionProvider.IsNewPrefix(prefix);
    }

    private void StartAutocomplete(List<ICompletionSource> sources)
    {
        _autocompletionProvider.ProvideSuggestions(_prefix, sources);
        var suggestion = _autocompletionProvider.GetCurrentSuggestion();
        if (_wordBoundaries != null && !string.IsNullOrEmpty(suggestion))
            ReplaceWord(_wordBoundaries, suggestion);
    }

    private void NextAutocomplete()
    {
        var suggestion = _autocompletionProvider.GetCurrentSuggestion();
        if (_wordBoundaries != null && !string.IsNullOrEmpty(suggestion))
            ReplaceWord(_wordBoundaries, suggestion);
    }

    private void NextHistory()
    {
        throw new NotImplementedException();
    }

    private void PrevHistory()
    {
        throw new NotImplementedException();
    }

    private (int left, int top) GetCursorCoordinates(int index)
    {
        var actualIndex = 2 + index;
        var width = _console.BufferWidth;
        var left = actualIndex % width;
        var topOffset = actualIndex / width;

        return (left, _console.CursorTop + topOffset);
    }

    private WordBoundaries? GetWordBoundaries()
    {
        if (TextBuffer.Length == 0)
            return null;

        var pos = _cursorPos == 0 ? 0 : Math.Min(_cursorPos, TextBuffer.Length - 1);

        if (char.IsWhiteSpace(TextBuffer[pos]))
            return null;

        var start = pos;
        var end = pos;

        while (start > 0 && !char.IsWhiteSpace(TextBuffer[start - 1]))
            start--;

        while (end < TextBuffer.Length - 1 && !char.IsWhiteSpace(TextBuffer[end + 1]))
            end++;

        return new WordBoundaries(start, end - start + 1);
    }

    private string GetWord(WordBoundaries wordBoundaries)
    {
        return TextBuffer.ToString(wordBoundaries.Start, wordBoundaries.Length);
    }

    public void Reset()
    {
        TextBuffer.Clear();
        _cursorPos = 0;
        _cursorLimit = 0;
        _keyInfo = new ConsoleKeyInfo();
        _wordBoundaries = null;
        _prefix = string.Empty;
    }

    #region Helpers

    private void ReplaceWord(WordBoundaries boundaries, string suggestion)
    {
        TextBuffer.Remove(boundaries.Start, boundaries.Length);
        TextBuffer.Insert(boundaries.Start, suggestion + " ");

        _console.Write("\r");
        _console.Write("$ ");
        _console.Write($"{TextBuffer}");
        // var redraw = TextBuffer.ToString(boundaries.Start, TextBuffer.Length - boundaries.Start);
        //
        // var (left, top) = GetCursorCoordinates(boundaries.Start);
        // _console.SetCursorPosition(left, top);
        //
        // _console.Write($"{redraw}");
        //
        // var (newLeft, newTop) = GetCursorCoordinates(boundaries.Start + suggestion.Length);
        // _console.SetCursorPosition(newLeft, newTop);

        _cursorPos = boundaries.Start + suggestion.Length;
        _cursorLimit = TextBuffer.Length;
    }

    private void IncrementCursorPos()
    {
        if (_cursorPos < TextBuffer.Length)
            _cursorPos++;
    }

    private void IncrementCursorLimit()
    {
        _cursorLimit++;
    }

    private void DecrementCursorPos()
    {
        if (_cursorPos > 0)
            _cursorPos--;
    }

    private void DecrementCursorLimit()
    {
        if (_cursorLimit > 0)
            _cursorLimit--;
    }

    private bool CursorIsAtStartOfLine()
    {
        return _cursorPos == 0;
    }

    private bool CursorIsAtEndOfLine()
    {
        return _cursorPos == _cursorLimit;
    }

    private bool CursorIsAtStartOfBuffer()
    {
        return _console.CursorLeft == 0;
    }

    private bool CursorIsAtEndOfBuffer()
    {
        return _console.CursorLeft == _console.BufferWidth;
    }

    #endregion

    #endregion

    #region KeyActions

    private void Backspace()
    {
        if (CursorIsAtStartOfLine())
            return;

        MoveCursorLeft();
        var index = _cursorPos;
        TextBuffer.Remove(index, 1);
        var rewrite = TextBuffer.ToString()[index..];
        var left = _console.CursorLeft;
        var top = _console.CursorTop;
        _console.Write($"{rewrite} ");
        _console.SetCursorPosition(left, top);
        DecrementCursorLimit();
    }

    private void Delete()
    {
        throw new NotImplementedException();
    }

    private void MoveCursorLeft()
    {
        if (CursorIsAtStartOfLine())
            return;

        if (CursorIsAtStartOfBuffer() && _console.CursorTop != 0)
            _console.SetCursorPosition(_console.BufferWidth - 1, _console.CursorTop - 1);
        else
            _console.SetCursorPosition(_console.CursorLeft - 1, _console.CursorTop);

        DecrementCursorPos();
    }

    private void MoveCursorRight()
    {
        if (CursorIsAtEndOfLine())
            return;

        if (CursorIsAtEndOfBuffer() && _console.CursorTop < _console.BufferHeight)
            _console.SetCursorPosition(0, _console.BufferHeight + 1);
        else
            _console.SetCursorPosition(_console.CursorLeft + 1, _console.CursorTop);

        IncrementCursorPos();
    }

    private void WriteCharacter()
    {
        WriteCharacter(_keyInfo.KeyChar);
    }

    private void WriteCharacter(char value)
    {
        if (char.IsControl(value))
            return;

        // Appending
        if (CursorIsAtEndOfLine())
        {
            TextBuffer.Append(value);
            _console.Write(value.ToString());
            IncrementCursorPos();
        }
        else
        {
            var left = _console.CursorLeft;
            var top = _console.CursorTop;
            var toAppend = TextBuffer.ToString()[_cursorPos..];
            TextBuffer.Insert(_cursorPos, value);
            _console.Write($"{value}{toAppend} ");
            _console.SetCursorPosition(left, top);
            MoveCursorRight();
        }

        IncrementCursorLimit();
    }

    #endregion
}*/