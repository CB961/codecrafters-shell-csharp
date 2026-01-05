using System.Collections.Immutable;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Core.Parsing.Ast;

namespace codecrafters_shell.Core.Evaluating;

public sealed class ArgumentEvaluator(IShellContext context)
{
    private readonly IShellContext _context = context;

    public string Evaluate(ArgumentNode arg)
    {
        return arg switch
        {
            LiteralArgument lit => lit.Value,
            VariableArgument var => ResolveVariable(var),
            _ => throw new NotSupportedException(arg.GetType().Name)
        };
    }

    public ImmutableList<string> EvaluateAll(
        ImmutableList<ArgumentNode> args)
    {
        return args.Select(Evaluate).ToImmutableList();
    }

    private string ResolveVariable(VariableArgument var)
    {
        return _context.Env.TryGetValue(var.Name, out var value)
            ? value
            : string.Empty;
    }
}