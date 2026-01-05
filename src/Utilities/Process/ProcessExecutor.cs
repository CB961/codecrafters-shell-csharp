using System.Diagnostics;

namespace codecrafters_shell.Interfaces;

public class ProcessExecutor : IProcessExecutor
{
    private Process? _currentProcess;

    public ProcessExecutor()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cancel();
        };
    }

    public int Execute(string filePath, IReadOnlyList<string> args, IShellContext context)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.GetFileName(filePath),
            WorkingDirectory = context.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in args) psi.ArgumentList.Add(arg);

        try
        {
            var process = Process.Start(psi);
            if (process == null) return 1;

            _currentProcess = process;
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) context.StdOut.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) context.StdErr.WriteLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            context.StdErr.WriteLine("Exception occured during execution of {0}:\n{1}", filePath, ex.Message);
            return 1;
        }
        finally
        {
            _currentProcess = null;
        }
    }

    public void Cancel()
    {
        try
        {
            _currentProcess?.Kill(true);
            _currentProcess = null;
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch
        {
        }
    }
}