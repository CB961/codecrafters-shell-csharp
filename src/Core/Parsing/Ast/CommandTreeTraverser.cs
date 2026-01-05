namespace codecrafters_shell.Core.Parsing.Ast;

public abstract class CommandTreeTraverser
{
    protected int Depth { get; private set; }

    public void Traverse(CommandTreeNode node)
    {
        Enter(node);
        Depth++;

        switch (node)
        {
            case SimpleCommand sc:
                TraverseSimple(sc);
                break;
            case PipelineCommand pc:
                TraversePipeline(pc);
                break;
        }

        Depth--;
        Exit(node);
    }

    private void TraversePipeline(PipelineCommand pc)
    {
        foreach (var cmd in pc.Commands) Traverse(cmd);
    }

    protected virtual void TraverseSimple(SimpleCommand sc)
    {
        foreach (var arg in sc.Arguments) TraverseArgument(arg);

        foreach (var redirectNode in sc.Redirects) TraverseRedirect(redirectNode);
    }

    protected virtual void TraverseRedirect(RedirectNode redirectNode)
    {
        TraverseArgument(redirectNode.Target);
    }

    protected virtual void TraverseArgument(ArgumentNode argumentNode)
    {
    }

    protected virtual void Enter(CommandTreeNode node)
    {
    }

    protected virtual void Exit(CommandTreeNode node)
    {
    }
}