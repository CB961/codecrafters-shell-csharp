namespace codecrafters_shell.Core.History;

public sealed class CommandHistory
{
    private int _currentIndex;

    private List<string> UsedCommands { get; } = [];
    public bool IsEmpty => UsedCommands.Count == 0;
    public bool IsAtFirstPos => !IsEmpty && _currentIndex == 0;
    public bool IsAtLastPos => !IsEmpty && _currentIndex == UsedCommands.Count - 1;
    public bool HasBeenBrowsed { get; private set; }

    public void Record(string command)
    {
        UsedCommands.Add(command);
        HasBeenBrowsed = false;
        _currentIndex = UsedCommands.Count - 1;
    }
    
    public string GetCurrent() => !IsEmpty ? UsedCommands[_currentIndex] : string.Empty;
    
    public string GetNext()
    {
        if (IsEmpty)
            return string.Empty;
        
        ToNext();
        
        if (!HasBeenBrowsed)
            HasBeenBrowsed = true;
        
        return GetCurrent();
    }

    public string GetPrevious()
    {
        if (IsEmpty)
            return string.Empty;
        
        if (!HasBeenBrowsed)
        {
            HasBeenBrowsed = true;
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