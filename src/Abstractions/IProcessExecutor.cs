using System.Diagnostics;

namespace codecrafters_shell.Interfaces;

public interface IProcessExecutor
{
    Process Start(
        string filePath,
        IReadOnlyList<string> args,
        IShellContext context,
        bool redirectInput,
        bool redirectOutput,
        bool redirectError
    );
    
    // int Execute(
    //     string filePath,
    //     IReadOnlyList<string> args,
    //     IShellContext context,
    //     bool redirectInput,
    //     bool redirectOutput,
    //     bool redirectError
    // );
}