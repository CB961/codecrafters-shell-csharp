using codecrafters_shell.Autocomplete;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Lexing;
using codecrafters_shell.LineEditing;
using codecrafters_shell.PathResolving;

namespace codecrafters_shell
{
    internal static class Program
    {
        private static void Main()
        {
            var builtins = BuiltinRegistry.Create();
            var shellCtx = new ShellContext(builtins, Console.Out, Console.Error);
            var lexer = new CommandLexer();
            var resolver = new PathResolver(shellCtx);
            var executor = new ProcessExecutor();
            var provider = new AutocompletionProvider();
            var keyHandler = new KeyHandler(shellCtx, provider);
            var readLine = new ReadLine(keyHandler);
            var shell = new Shell(shellCtx, readLine, resolver, executor, lexer);
            shell.Start();
        }
    }
}