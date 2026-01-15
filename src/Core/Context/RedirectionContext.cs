using codecrafters_shell.Core.Registry;
using codecrafters_shell.Interfaces;
using codecrafters_shell.PathResolving;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Core.Context;

public sealed class RedirectionContext : IShellContext
{
    public IReadOnlyDictionary<string, BuiltinRegistry.BuiltinHandler> Builtins { get; }
    public List<string> History { get; set; }
    public string CurrentDirectory { get; set; }
    public TextReader StdIn { get; }
    public TextWriter StdOut { get; }
    public TextWriter StdErr { get; }

    public IDictionary<string, string> Env { get; }
    public IPathResolver PathResolver { get; }

    public RedirectionContext(
        IShellContext mainCtx,
        TextWriter? redirectOutput,
        TextWriter? redirectError
    )
    {
        Builtins = mainCtx.Builtins;
        History = mainCtx.History;
        CurrentDirectory = mainCtx.CurrentDirectory;
        StdIn = mainCtx.StdIn;
        StdOut = redirectOutput ?? mainCtx.StdOut;
        StdErr = redirectError ?? mainCtx.StdErr;
        Env = mainCtx.Env;
        PathResolver = new PathResolver(this);
    }
}