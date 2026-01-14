using System.Diagnostics;

namespace codecrafters_shell.Interfaces;

public class ProcessExecutor : IProcessExecutor
{
    public Process Start(string filePath, IReadOnlyList<string> args, IShellContext context, bool redirectInput, bool redirectOutput,
        bool redirectError)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.GetFileName(filePath),
            WorkingDirectory = context.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectError
        };
        
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            context.StdErr.WriteLine("Exception occured during execution of {0}:\n{1}", filePath, ex.Message);
        }

        return process;
    }
}