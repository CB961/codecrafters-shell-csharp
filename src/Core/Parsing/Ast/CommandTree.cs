using System.Collections.Immutable;

namespace codecrafters_shell.Core.Parsing.Ast;

/*
 * Grammar rules:
 * command_line -> command (argument | redirect)*
 * command -> WORD
 * argument -> WORD | STRING_LITERAL
 * redirect -> REDIRECT_OUT (WORD | STRING_LITERAL)
 */

public sealed class CommandTree(CommandTreeNode root)
{
    public CommandTreeNode Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
};

public abstract record CommandTreeNode;

public record CommandName(string Value);

public record SimpleCommand(
    CommandName Name,
    ImmutableList<ArgumentNode> Arguments,
    ImmutableList<RedirectNode> Redirects
) : CommandTreeNode;

public record PipelineCommand(
    ImmutableList<CommandTreeNode> Commands
) : CommandTreeNode;

public record RedirectNode(
    RedirectType Type,
    ArgumentNode Target
) : CommandTreeNode;

public abstract record ArgumentNode;

public record LiteralArgument(string Value) : ArgumentNode;

public record VariableArgument(string Name) : ArgumentNode;