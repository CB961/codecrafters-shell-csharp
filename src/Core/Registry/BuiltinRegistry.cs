using codecrafters_shell.Helpers;
using codecrafters_shell.Interfaces;

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
            ["pwd"] = Pwd,
            ["cd"] = Cd,
            ["echo"] = Echo,
            ["type"] = Type
        };
    }

    private static int Exit(IReadOnlyList<string> args, IShellContext context)
    {
        var code = args.Count > 0 && int.TryParse(args[0], out var result)
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