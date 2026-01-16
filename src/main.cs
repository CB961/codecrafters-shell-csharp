using codecrafters_shell.Core.Autocomplete;
using codecrafters_shell.Core.Context;
using codecrafters_shell.Core;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Core.Lexing;
using codecrafters_shell.Core.Registry;
using codecrafters_shell.LineEditing;
using codecrafters_shell.PathResolving;
using codecrafters_shell.Core.LineEditing;

namespace codecrafters_shell;

internal static class Program
{
    private static void Main()
    {
        #region Core setup

        var builtins = BuiltinRegistry.Create();
        var shellCtx = new ShellContext(builtins, Console.In, Console.Out, Console.Error);
        var lexer = new CommandLexer();
        var resolver = new PathResolver(shellCtx);
        var executor = new ProcessExecutor();

        #endregion

        #region Autocompletion setup

        var builtinSource = new BuiltinCompletionSource(shellCtx.Builtins.Keys.ToList());
        var pathExesSource = new PathCompletionSource(resolver);  
        var provider = new AutocompletionProvider([builtinSource, pathExesSource]);

        #endregion

        #region Line editor setup
        
        var console = new SystemConsole();
        var editor = new LineEditor(provider, shellCtx.History);
        var renderer = new SimpleLineRenderer(console);
        var readLine = new ReadLine(console, editor, renderer, shellCtx.History);

        #endregion
        
        var shell = new Shell(shellCtx, readLine, resolver, executor, lexer);
        shell.Start();
    }
}