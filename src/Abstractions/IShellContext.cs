using codecrafters_shell.Core.History;
using codecrafters_shell.Core.Registry;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Interfaces;

public interface IShellContext
{
    IReadOnlyDictionary<string, BuiltinRegistry.BuiltinHandler> Builtins { get; }
    CommandHistory History { get; }
    string CurrentDirectory { get; set; }
    TextReader StdIn { get; }
    TextWriter StdOut { get; }
    TextWriter StdErr { get; }
    IDictionary<string, string> Env { get; }
    IPathResolver PathResolver { get; }
}