using System.Collections.Immutable;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Core.Parsing.Ast;
using codecrafters_shell.src.Interfaces;

namespace codecrafters_shell.Core.Evaluating;

public sealed class CommandEvaluator
{
    private readonly ArgumentEvaluator _argEvaluator;
    private readonly IShellContext _context;
    private readonly IPathResolver _resolver;
    private readonly IProcessExecutor _executor;

    public CommandEvaluator(
        IShellContext context,
        IPathResolver resolver,
        IProcessExecutor executor
    )
    {
        _context = context;
        _resolver = resolver;
        _argEvaluator = new ArgumentEvaluator(_context);
        _executor = executor;
    }

    public int Evaluate(CommandTree commandTree)
    {
        return commandTree.Root switch
        {
            SimpleCommand sc => EvaluateSimple(sc),
            // PipelineCommand plc => EvaluatePipeline(plc),
            _ => throw new NotSupportedException()
        };
    }

    private int EvaluateSimple(SimpleCommand sc)
    {
        var cmd = sc.Name.Value;
        var args = _argEvaluator.EvaluateAll(sc.Arguments);

        // ReSharper disable once InvertIf
        if (HasRedirects(sc.Redirects))
        {
            using var redirectScope = new RedirectScope(sc.Redirects, _context, _argEvaluator);
            return ExecuteCommand(cmd, args, redirectScope.Context);
        }

        return ExecuteCommand(cmd, args, _context);
    }

    private int ExecuteCommand(string command, IReadOnlyList<string> arguments, IShellContext context)
    {
        const int errorCommandNotFound = 0x16;

        if (IsBuiltin(command, context)) return ExecuteBuiltin(command, arguments.ToArray(), context);

        var fullPath = _resolver.FindExecutableInPath(command);

        if (!string.IsNullOrEmpty(fullPath))
            return _executor.Execute(fullPath, arguments.ToArray(), context);

        context.StdErr.WriteLine($"{command}: command not found");
        return errorCommandNotFound;
    }

    private static bool HasRedirects(ImmutableList<RedirectNode> redirects)
    {
        return redirects.Count > 0;
    }

    private static bool IsBuiltin(string command, IShellContext context)
    {
        return context.Builtins.ContainsKey(command);
    }

    private static int ExecuteBuiltin(string command, string[] arguments, IShellContext context)
    {
        var result = context.Builtins.TryGetValue(command, out var handler);

        return result ? handler!.Invoke(arguments, context) : 1;
    }
}