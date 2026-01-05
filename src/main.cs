using codecrafters_shell.Autocomplete;
using codecrafters_shell.Core.Context;
using codecrafters_shell.Core;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Core.Lexing;
using codecrafters_shell.Core.Registry;
using codecrafters_shell.LineEditing;
using codecrafters_shell.PathResolving;
using codecrafters_shell.Utilities.LineEditing;

namespace codecrafters_shell;

internal static class Program
{
    private static void Main()
    {
        var builtins = BuiltinRegistry.Create();
        var shellCtx = new ShellContext(builtins, Console.Out, Console.Error);
        var lexer = new CommandLexer();
        var resolver = new PathResolver(shellCtx);
        var executor = new ProcessExecutor();
        var builtinSource = new BuiltinCompletionSource(shellCtx.Builtins.Keys.ToList());
        var provider = new AutocompletionProvider([builtinSource]);
        // var keyHandler = new KeyHandler(shellCtx, provider);
        // var readLine = new ReadLine(keyHandler);
        var console = new SystemConsole();
        var editor = new LineEditor(provider);
        var renderer = new SimpleLineRenderer(console);
        var readLine = new ReadLine(editor, new SimpleLineRenderer(console));
        var shell = new Shell(shellCtx, readLine, resolver, executor, lexer);
        shell.Start();
    }
}