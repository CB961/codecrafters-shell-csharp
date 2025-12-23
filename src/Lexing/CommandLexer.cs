using System.Text;

namespace codecrafters_shell.Lexing;

public class CommandLexer
{
    public List<Token> Tokens { get; } = [];

    // public void Lex(string input)
    // {
    //     var index = 0;
    //     if (string.IsNullOrEmpty(input)) return;
    //
    //     while (index < input.Length)
    //     {
    //         if (char.IsWhiteSpace(input[index]))
    //         {
    //             index++;
    //             continue;
    //         }
    //
    //         var matchFound = false;
    //
    //         foreach (var (type, pattern) in TokenDefinition.TokenSpecs)
    //         {
    //             var match = pattern.Match(input, index);
    //
    //             if (!match.Success || match.Index != index)
    //             {
    //                 continue;
    //             }
    //
    //             var value = match.Value;
    //
    //             if (type == TokenType.StringLiteral)
    //             {
    //                 value = UnquoteString(value);
    //             }
    //             
    //             Tokens.Add(new Token(type, value));
    //             index += match.Length;
    //             matchFound = true;
    //             break;
    //         }
    //
    //         if (!matchFound)
    //         {
    //             throw new LexerException($"Unexpected character '{input[index]}' at position {index}");
    //         }
    //     }
    // }

    private static string UnquoteString(string value)
    {
        var inner = value.Substring(1, value.Length - 2);
        var sb = new StringBuilder(inner.Length);

        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '\\' && i + 1 < inner.Length)
            {
                var next = inner[i + 1];

                if (next is '"' or '\\')
                {
                    sb.Append(next);
                    i++;
                    continue;
                }
            }

            sb.Append(inner[i]);
        }

        return sb.ToString();
    }

    public void ClearTokens()
    {
        Tokens.Clear();
    }

    public void Tokenize(string input)
    {
        Tokens.Clear();
        if (string.IsNullOrEmpty(input)) return;

        var current = new StringBuilder();
        var inSingleQuotes = false;
        var inDoubleQuotes = false;
        var escaping = false;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (escaping)
            {
                if (inDoubleQuotes)
                {
                    if (c is not ('"' or '\\'))
                    {
                        current.Append('\\');
                    }
                }

                current.Append(c);
                escaping = false;
                continue;
            }

            switch (c)
            {
                case '\\' when !inSingleQuotes:
                    escaping = true;
                    continue;
                case '\'' when !inDoubleQuotes:
                    inSingleQuotes = !inSingleQuotes;
                    continue;
                case '"' when !inSingleQuotes:
                    inDoubleQuotes = !inDoubleQuotes;
                    continue;
            }

            if (!inSingleQuotes && !inDoubleQuotes)
            {
                switch (c)
                {
                    // 1>
                    case '1' when i + 1 < input.Length && input[i + 1] == '>':
                        FlushWord();
                        Tokens.Add(new Token(TokenType.RedirectOut, "1>"));
                        i++;
                        continue;
                    case '>':
                        FlushWord();
                        Tokens.Add(new Token(TokenType.RedirectOut, ">"));
                        continue;
                }
            }

            if (!inSingleQuotes && !inDoubleQuotes && char.IsWhiteSpace(c))
            {
                FlushWord();
                continue;
            }

            current.Append(c);
        }

        if (escaping)
            throw new LexerException("Trailing escape character");

        if (inSingleQuotes || inDoubleQuotes)
            throw new LexerException("Unmatched quotes in input");

        FlushWord();
        return;

        void FlushWord()
        {
            if (current.Length == 0) return;
            Tokens.Add(new Token(TokenType.Word, current.ToString()));
            current.Clear();
        }
    }
}