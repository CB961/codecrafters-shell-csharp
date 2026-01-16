namespace codecrafters_shell.Core.History;

public sealed class CommandHistory
{
    private int _currentIndex;
    private bool _historyWasBrowsed;

    private List<string> UsedCommands { get; } = [];

    
    public bool IsEmpty => UsedCommands.Count == 0;
    public bool BrowserAtFirstPos() => !IsEmpty && _currentIndex == 0;
    public bool BrowserAtLastPos() => !IsEmpty && _currentIndex == UsedCommands.Count - 1;
    public bool WasHistoryBrowsed() => _historyWasBrowsed;
    
    public void Record(string command)
    {
        UsedCommands.Add(command);
        _historyWasBrowsed = false;
        _currentIndex = UsedCommands.Count - 1;
    }
    
    public string GetCurrent() => !IsEmpty ? UsedCommands[_currentIndex] : string.Empty;
    
    public string GetNext()
    {
        if (IsEmpty)
            return string.Empty;
        
        ToNext();
        
        if (!_historyWasBrowsed)
            _historyWasBrowsed = true;
        
        return GetCurrent();
    }

    public string GetPrevious()
    {
        if (IsEmpty)
            return string.Empty;
        
        if (!_historyWasBrowsed)
        {
            _historyWasBrowsed = true;
            return GetLast();
        }

        ToPrevious();
        return GetCurrent();
    }

    private string GetLast()
    {
        return !IsEmpty ? UsedCommands.Last() : string.Empty;
    }

    public IReadOnlyList<string> GetCommandHistory()
    {
        return [..UsedCommands];
    }
    
    private void ToPrevious()
    {
        if (_currentIndex - 1 > 0) 
            _currentIndex--;
    }
    
    private void ToNext()
    {
        if (_currentIndex < UsedCommands.Count - 1)
            _currentIndex++;
    }
}