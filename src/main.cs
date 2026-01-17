using codecrafters_shell.Core.Autocomplete;
using codecrafters_shell.Core.Context;
using codecrafters_shell.Core;
using codecrafters_shell.Core.History;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Core.Lexing;
using codecrafters_shell.Core.Registry;
using codecrafters_shell.LineEditing;
using codecrafters_shell.PathResolving;
using codecrafters_shell.Core.LineEditing;

namespace codecrafters_shell;

internal static class Program
{
    private static void Main(string[] args)
    {
        #region Core setup

        if (args.Length == 1 && args[0].Contains("HISTFILE="))
        {
            var arg = args[0].Split("HISTFILE=", StringSplitOptions.RemoveEmptyEntries);
            Environment.SetEnvironmentVariable("HISTFILE", arg[1]);
        }

        var builtins = BuiltinRegistry.Create();
        var history = new CommandHistory();
        var shellCtx = new ShellContext(builtins, history, Console.In, Console.Out, Console.Error);

        var result = shellCtx.Env.TryGetValue("HISTFILE", out var histFile);

        if (result && !string.IsNullOrEmpty(histFile))
        {
            history.InitHistoryFromFile(histFile);
        }

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