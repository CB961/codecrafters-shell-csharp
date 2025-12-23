using codecrafters_shell.Interfaces;

namespace codecrafters_shell.Helpers;

public static class PathHelper
{
    public static string ExpandPath(string path, IShellContext context)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        var home = ResolveHome(context);
        
        if (path == "~") return home;

        return path.StartsWith($"~{Path.DirectorySeparatorChar}") 
            ? Path.Combine(home, path[2..]) 
            : Path.GetFullPath(path, context.CurrentDirectory);
    }

    private static string ResolveHome(IShellContext context)
    {
        if (context.Env.TryGetValue("HOME", out var home) && !string.IsNullOrWhiteSpace(home))
            return home;

        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("HOME is not set");
        
        if (context.Env.TryGetValue("USERPROFILE", out var profile))
            return profile;

        if (context.Env.TryGetValue("HOMEDRIVE", out var drive) &&
            context.Env.TryGetValue("HOMEPATH", out var path))
            return drive + path;

        throw new InvalidOperationException("HOME is not set");
    }
}