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
            if (Match(TokenType.Redirect))
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
        var redirectToken = Consume(TokenType.Redirect);
        var target = Consume(TokenType.Word);
        var redirectType = ProduceRedirectType(redirectToken.Value);
        
        return new RedirectNode(redirectType, new LiteralArgument(target.Value));
    }

    #region Helpers

    private static RedirectType ProduceRedirectType(string tokenValue)
    {
        return tokenValue switch
        {
            ">>" or "1>>" => RedirectType.Append,
            "2>>" => RedirectType.AppendError,
            ">" or "1>" => RedirectType.Out,
            "2>" => RedirectType.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenValue), tokenValue, null)
        };
    }

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