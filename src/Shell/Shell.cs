using codecrafters_shell.Evaluating;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Lexing;
using codecrafters_shell.Parsing;
using codecrafters_shell.Parsing.Ast;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell;

public class Shell
{
    private readonly IShellContext _ctx;
    private readonly IPathResolver _resolver;
    private readonly IProcessExecutor _executor;
    private readonly CommandLexer _lexer;

    public Shell(IShellContext context, IPathResolver resolver, IProcessExecutor executor, CommandLexer lexer)
    {
        _ctx = context;
        _resolver = resolver;
        _executor = executor;
        _lexer = lexer;
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _executor.Cancel();
        };
    }
    
    public void Start()
    {
        do
        {
            Console.Write("$ ");
            var userInput = Console.ReadLine() ?? string.Empty;
            _lexer.Tokenize(userInput);
            var tokens = _lexer.Tokens;

            var parser = new CommandLineParser(tokens);
            var commandLine = parser.ParseCommand();

            var cmdTree = new CommandTree(commandLine);
            var evaluator = new CommandEvaluator(_ctx, _resolver, _executor);
            evaluator.Evaluate(cmdTree);
            
            _lexer.ClearTokens();
        } while (true);
        // ReSharper disable once FunctionNeverReturns
    }
}