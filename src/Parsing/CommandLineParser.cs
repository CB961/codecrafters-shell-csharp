using System.Collections.Immutable;
using codecrafters_shell.Parsing.Ast;

namespace codecrafters_shell.Parsing;

public class CommandLineParser(IEnumerable<Token> tokens)
{
    private readonly List<Token> _tokens = tokens.ToList();
    private int _pos;

    public SimpleCommand ParseCommand()
    {
        var nameToken = Consume(TokenType.Word);
        var name = nameToken.Value;
        var args = new List<ArgumentNode>();
        var redirects = new List<RedirectNode>();

        while (!AtEnd())
        {
            if (Match(TokenType.RedirectOut))
            {
                redirects.Add(ParseRedirect());
            }
            else if (Match(TokenType.Word))
            {
                args.Add(new LiteralArgument(Advance().Value));
            }
            else
            {
                throw new ParseException($"Unexpected token: {Peek().Type}");
            }
        }

        return new SimpleCommand(new CommandName(name), args.ToImmutableList(), redirects.ToImmutableList());
    }
    
    private RedirectNode ParseRedirect()
    {
        Consume(TokenType.RedirectOut);
        var target = Consume(TokenType.Word);
        return new RedirectNode(RedirectType.Out, new LiteralArgument(target.Value));
    }

    #region Helpers

    private Token Consume(params TokenType[] types)
    {
        if (AtEnd())
            throw new ParseException("Unexpected end of input.");
        
        var token = Peek();

        if (!types.Contains(token.Type))
        {
            var expected = string.Join(", ", types);
            throw new ParseException($"Expected one of [{expected}], but found {token.Type}");
        }

        _pos++;
        return token;
    }

    private Token Peek() => 
        _pos >= _tokens.Count ? throw new ParseException("Unexpected end of input.") : _tokens[_pos];

    private Token Advance()
    {
        var token = Peek();
        _pos++;
        return token;
    }

    private bool Match(params TokenType[] types) => _pos < _tokens.Count && types.Contains(_tokens[_pos].Type);

    private bool AtEnd() => _pos >= _tokens.Count;

    #endregion
}