using codecrafters_shell.Evaluating;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Lexing;
using codecrafters_shell.LineEditing;
using codecrafters_shell.Parsing;
using codecrafters_shell.Parsing.Ast;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell;

public class Shell(
    IShellContext context,
    ReadLine readLine,
    IPathResolver resolver,
    IProcessExecutor executor,
    CommandLexer lexer
    )
{
    public void Start()
    {
        do
        {
            var userInput = readLine.Read("$ ");

            lexer.Tokenize(userInput);
            var tokens = lexer.Tokens;

            var parser = new CommandLineParser(tokens);
            var commandLine = parser.ParseCommand();

            var cmdTree = new CommandTree(commandLine);
            var evaluator = new CommandEvaluator(context, resolver, executor);
            evaluator.Evaluate(cmdTree);
        } while (true);
        // ReSharper disable once FunctionNeverReturns
    }
}