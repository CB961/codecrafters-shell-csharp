using codecrafters_shell.Interfaces;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.PathResolving;

public class PathResolver(IShellContext context) : IPathResolver
{
    public string? FindExecutableInPath(string command)
    {
        var path = OperatingSystem.IsWindows() ? context.Env["Path"] : context.Env["PATH"];

        if (string.IsNullOrEmpty(path)) return null;

        var dirs = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (dirs.Length <= 0) return null;

        var candidates = OperatingSystem.IsWindows() ? BuildWindowsExecutableCandidates(command) : [command];

        return dirs.SelectMany(dir => candidates, Path.Combine)
            .FirstOrDefault(fullPath => File.Exists(fullPath) && IsExecutable(fullPath));
    }

    private string[] BuildWindowsExecutableCandidates(string exeName)
    {
        if (!string.IsNullOrEmpty(Path.GetExtension(exeName))) return [exeName];

        var pathExt = context.Env["PATHEXT"];
        var exts = pathExt.Split(Path.PathSeparator);
        var result = new List<string>();

        try
        {
            result.AddRange(exts.Select(ext => exeName + ext));
        }
        catch (Exception ex)
        {
            context.StdErr.WriteLine(ex.Message);
        }

        return [.. result];
    }

    private bool IsExecutable(string filePath)
    {
        if (OperatingSystem.IsWindows()) return true;

        try
        {
            var mode = File.GetUnixFileMode(filePath);
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
        catch (Exception ex)
        {
            context.StdErr.WriteLine(ex.Message);
            return false;
        }
    }
}