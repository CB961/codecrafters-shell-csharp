using codecrafters_shell.Helpers;
using codecrafters_shell.Interfaces;
using static System.Int32;

namespace codecrafters_shell.Core.Registry;

public static class BuiltinRegistry
{
    public delegate int BuiltinHandler(
        IReadOnlyList<string> args,
        IShellContext context
    );

    public static IReadOnlyDictionary<string, BuiltinHandler> Create()
    {
        return new Dictionary<string, BuiltinHandler>(StringComparer.OrdinalIgnoreCase)
        {
            ["exit"] = Exit,
            ["cd"] = Cd,
            ["echo"] = Echo,
            ["history"] = History,
            ["pwd"] = Pwd,
            ["type"] = Type
        };
    }

    private static int Exit(IReadOnlyList<string> args, IShellContext context)
    {
        var code = args.Count > 0 && TryParse(args[0], out var result)
            ? result
            : 0;

        Environment.Exit(code);
        return code;
    }

    private static int Pwd(IReadOnlyList<string> args, IShellContext context)
    {
        context.StdOut.WriteLine(Path.TrimEndingDirectorySeparator(context.CurrentDirectory));
        return 0;
    }

    private static int Cd(IReadOnlyList<string> args, IShellContext context)
    {
        var targetPath = args.Count == 0
            ? "~"
            : args[0];

        var fullPath = PathHelper.ExpandPath(targetPath, context);

        if (!Directory.Exists(fullPath))
        {
            context.StdErr.WriteLine($"cd: {targetPath}: No such file or directory");
            return 1;
        }

        context.CurrentDirectory = fullPath;
        return 0;
    }

    private static int Echo(IReadOnlyList<string> args, IShellContext context)
    {
        context.StdOut.WriteLine(string.Join(' ', args));

        return 0;
    }

    private static int History(IReadOnlyList<string> args, IShellContext context)
    {
        var historyProvider = context.History;
        var history = historyProvider.Commands;
        var count = history.Count;
        var limit = count;

        switch (args)
        {
            case ["-r", var filePath]:
            {
                var fullPath = Path.GetFullPath(filePath);

                if (!File.Exists(fullPath))
                {
                    context.StdErr.WriteLine($"history: {filePath}: File doesn't exist!");
                    return 1;
                }
            
                historyProvider.LoadFromFile(fullPath);
                return 0;
            }
            case ["-w", var filePath]:
            {
                var fullPath = Path.GetFullPath(filePath);
                historyProvider.WriteToFile(fullPath);
                return 0;
            }
        }

        if (args.Count == 1)
        {
            var result = TryParse(args[0], out var parsed);

            if (!result)
            {
                context.StdErr.WriteLine($"history: {args[0]}: numeric argument required");
                return 1;
            }
                
            limit = Math.Clamp(parsed, 0, count);
        }
        
        var start = count - limit;
        
        for (var i = start; i < history.Count; i++)
        {
            context.StdOut.WriteLine($"    {i + 1}  {history[i]}");
        }

        return 0;
    }
    
    private static int Type(IReadOnlyList<string> args, IShellContext context)
    {
        var arg = args.Count > 0 ? args[0] : null;

        if (string.IsNullOrEmpty(arg)) return 1;

        var isBuiltin = context.Builtins.ContainsKey(arg);

        if (isBuiltin)
        {
            context.StdOut.WriteLine($"{arg.ToLower()} is a shell builtin");
            return 0;
        }

        var executablePath = context.PathResolver.FindExecutableInPath(arg);

        if (string.IsNullOrEmpty(executablePath))
        {
            context.StdErr.WriteLine($"{arg}: not found");
            return 1;
        }

        context.StdOut.WriteLine($"{arg} is {Path.TrimEndingDirectorySeparator(executablePath)}");
        return 0;
    }
}