namespace codecrafters_shell.Core.History;

public sealed class CommandHistory
{
    private int _currentIndex;

    private List<string> UsedCommandsCurrentSession { get; } = [];
    private List<string> UsedCommands { get; } = [];
    public IReadOnlyList<string> Commands => UsedCommands;
    
    public bool IsEmpty => UsedCommands.Count == 0;
    public bool IsAtFirstPos => !IsEmpty && _currentIndex == 0;
    public bool IsAtLastPos => !IsEmpty && _currentIndex == UsedCommands.Count - 1;
    public bool HasBeenBrowsed { get; private set; }

    public void Record(string command)
    {
        UsedCommands.Add(command);
        UsedCommandsCurrentSession.Add(command);
        HasBeenBrowsed = false;
        _currentIndex = UsedCommands.Count - 1;
    }

    public void InitHistoryFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;
        
        LoadFromFile(filePath);
    }

    public void LoadFromFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
            return;

        try
        {
            foreach (var line in File.ReadLines(fullPath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    UsedCommands.Add(line);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return;
        }
        
        _currentIndex = UsedCommands.Count - 1;
        HasBeenBrowsed = false;
    }

    public void WriteToFile(string filePath, bool append = false)
    {
        var fullPath = Path.GetFullPath(filePath);

        if (UsedCommandsCurrentSession.Count == 0)
            return;

        using var outputFile = new StreamWriter(fullPath, append: append);
        try
        {
            foreach (var cmd in UsedCommandsCurrentSession)
            {
                outputFile.WriteLine(cmd);
            }
            UsedCommandsCurrentSession.Clear();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
        }
    }

    private string GetCurrent() => !IsEmpty ? UsedCommands[_currentIndex] : string.Empty;
    
    public string GetNext()
    {
        if (IsEmpty || !HasBeenBrowsed)
            return string.Empty;
        
        ToNext();
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
        if (IsEmpty) return string.Empty;
        
        _currentIndex = UsedCommands.Count - 1;
        return UsedCommands.Last();
    }
    
    private void ToPrevious()
    {
        if (_currentIndex > 0) 
            _currentIndex--;
    }
    
    private void ToNext()
    {
        if (_currentIndex < UsedCommands.Count - 1)
            _currentIndex++;
    }

    public void SaveSessionToHistFile(string histFile)
    {
        WriteToFile(histFile, append: true);
    }
}