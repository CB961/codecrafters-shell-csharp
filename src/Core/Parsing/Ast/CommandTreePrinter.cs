namespace codecrafters_shell.Core.Parsing.Ast;

public class CommandTreePrinter : CommandTreeTraverser
{
    protected override void Enter(CommandTreeNode node)
    {
        var pad = new string(' ', Depth * 2);

        Console.WriteLine(node switch
        {
            SimpleCommand simpleCommand => 
                $"{pad}SimpleCommand\n" +
                $"{pad}Name: {simpleCommand.Name}\n" +
                $"{pad}Redirects: {simpleCommand.Redirects.Count > 0}"
            ,
            PipelineCommand pipelineCommand => $"{pad}Pipeline",
            _ => $"{pad}{node.GetType().Name}"
        });
    }

    protected override void TraverseArgument(ArgumentNode arg)
    {
        var pad = new string(' ', (Depth + 1) * 2);

        Console.WriteLine(arg switch
        {
            LiteralArgument l => $"{pad}LiteralArg \"{l.Value}\"",
            VariableArgument v => $"{pad}VariableArg ${v.Name}",
            _ => $"{pad}{arg.GetType().Name}"
        });
    }
}