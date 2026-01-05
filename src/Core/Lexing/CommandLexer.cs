using System.Text;

namespace codecrafters_shell.Core.Lexing;

public class CommandLexer
{
    public List<Token> Tokens { get; } = [];

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
                    if (c is not ('"' or '\\'))
                        current.Append('\\');

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
                switch (c)
                {
                    // 1> or 2>
                    case '1' or '2' when i + 1 < input.Length && input[i + 1] == '>':
                        FlushWord();
                        if (i + 2 < input.Length && input[i + 2] == '>')
                        {
                            Tokens.Add(new Token(TokenType.Redirect, $"{c}>>"));
                            i++;
                        }
                        else
                        {
                            Tokens.Add(new Token(TokenType.Redirect, $"{c}>"));
                        }

                        i++;
                        continue;
                    case '>':
                        if (i + 1 < input.Length && input[i + 1] == '>')
                        {
                            FlushWord();
                            Tokens.Add(new Token(TokenType.Redirect, ">>"));
                            i++;
                            continue;
                        }

                        FlushWord();
                        Tokens.Add(new Token(TokenType.Redirect, $"{c}"));
                        i++;
                        continue;
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