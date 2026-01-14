using System.Collections.Immutable;
using codecrafters_shell.Interfaces;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.PathResolving;

public class PathResolver(IShellContext context) : IPathResolver
{
    private string[] _cachedPaths = [];

    private ImmutableArray<string> CachedExecutables { get; set; } = [];

    private string[] GetPathDirectories()
    {
        var path = OperatingSystem.IsWindows() ? context.Env["Path"] : context.Env["PATH"];
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    public ImmutableArray<string> GetExecutablesFromPath()
    {
        var paths = GetPathDirectories();

        if (CachedExecutables.Length > 0 && paths == _cachedPaths)
            return CachedExecutables;

        _cachedPaths = paths;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<string>();


        foreach (var dir in paths)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                files.AddRange(from filePath in Directory.EnumerateFiles(dir)
                    where IsExecutable(filePath)
                    select OperatingSystem.IsWindows()
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : Path.GetFileName(filePath)
                    into fileName
                    where !string.IsNullOrEmpty(fileName)
                    where seen.Add(fileName)
                    select fileName
                );
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        CachedExecutables = [..files.OrderBy(x => x, StringComparer.Ordinal)];

        return CachedExecutables;
    }

    public string? FindExecutableInPath(string command)
    {
        var paths = GetPathDirectories();

        if (paths.Length == 0) return null;

        var candidates = OperatingSystem.IsWindows() ? BuildWindowsExecutableCandidates(command) : [command];

        return paths.SelectMany(path => candidates, Path.Combine)
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

    private static bool IsExecutable(string filePath)
    {
        if (OperatingSystem.IsWindows()) return true;

        try
        {
            var mode = File.GetUnixFileMode(filePath);
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
        catch
        {
            return false;
        }
    }
}