using codecrafters_shell.PathResolving;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Interfaces;

public interface IShellContext
{
    IReadOnlyDictionary<string, BuiltinRegistry.BuiltinHandler> Builtins { get; }
    string CurrentDirectory { get; set; }
    TextWriter StdOut { get; }
    TextWriter StdErr { get; }
    IDictionary<string, string> Env { get; }
    IPathResolver PathResolver { get; }
}