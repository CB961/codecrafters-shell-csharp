using System.Collections;
using codecrafters_shell.Core.History;
using codecrafters_shell.Core.Registry;
using codecrafters_shell.Interfaces;
using codecrafters_shell.PathResolving;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Core.Context;

public sealed class ShellContext : IShellContext
{
    public IReadOnlyDictionary<string, BuiltinRegistry.BuiltinHandler> Builtins { get; }
    public CommandHistory History { get; }
    public string CurrentDirectory { get; set; } = Environment.CurrentDirectory;
    public TextReader StdIn { get; }
    public TextWriter StdOut { get; }
    public TextWriter StdErr { get; }

    public IDictionary<string, string> Env { get; } =
        Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string)e.Value!);

    public IPathResolver PathResolver { get; }

    public ShellContext(
        IReadOnlyDictionary<string, BuiltinRegistry.BuiltinHandler> builtins,
        CommandHistory history,
        TextReader input,
        TextWriter output,
        TextWriter error
    )
    {
        Builtins = builtins;
        StdIn = input;
        StdOut = output;
        StdErr = error;
        PathResolver = new PathResolver(this);
        History = history;
    }
}