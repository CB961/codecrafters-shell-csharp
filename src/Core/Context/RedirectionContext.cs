using codecrafters_shell.Core.Registry;
using codecrafters_shell.Interfaces;
using codecrafters_shell.PathResolving;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Core.Context;

public sealed class RedirectionContext : IShellContext
{
    public IReadOnlyDictionary<string, BuiltinRegistry.BuiltinHandler> Builtins { get; }
    public string CurrentDirectory { get; set; }
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
        CurrentDirectory = mainCtx.CurrentDirectory;
        StdOut = redirectOutput ?? mainCtx.StdOut;
        StdErr = redirectError ?? mainCtx.StdErr;
        Env = mainCtx.Env;
        PathResolver = new PathResolver(this);
    }
}