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

    // public int Execute(string filePath, IReadOnlyList<string> args,
    //     IShellContext context,
    //     bool redirectInput,
    //     bool redirectOutput,
    //     bool redirectError)
    // {
    //     var psi = new ProcessStartInfo
    //     {
    //         FileName = Path.GetFileName(filePath),
    //         WorkingDirectory = context.CurrentDirectory,
    //         UseShellExecute = false,
    //         CreateNoWindow = true,
    //         RedirectStandardInput = redirectInput,
    //         RedirectStandardOutput = redirectOutput,
    //         RedirectStandardError = redirectError
    //     };
    //
    //     foreach (var arg in args) psi.ArgumentList.Add(arg);
    //
    //     try
    //     {
    //         var process = Process.Start(psi);
    //         if (process == null) return 1;
    //
    //         _currentProcess = process;
    //
    //         if (psi.RedirectStandardInput)
    //         {
    //             Task.Run(() =>
    //             {
    //                 process.StandardInput.Write(context.StdIn.ReadToEnd());
    //                 process.StandardInput.Close();
    //             });
    //         }
    //
    //         process.OutputDataReceived += (_, e) =>
    //         {
    //             if (e.Data != null) context.StdOut.WriteLine(e.Data);
    //         };
    //
    //         process.ErrorDataReceived += (_, e) =>
    //         {
    //             if (e.Data != null) context.StdErr.WriteLine(e.Data);
    //         };
    //
    //         process.BeginOutputReadLine();
    //         process.BeginErrorReadLine();
    //
    //         process.WaitForExit();
    //         return process.ExitCode;
    //     }
    //     catch (Exception ex)
    //     {
    //         context.StdErr.WriteLine("Exception occured during execution of {0}:\n{1}", filePath, ex.Message);
    //         return 1;
    //     }
    //     finally
    //     {
    //         _currentProcess = null;
    //     }
    // }
}