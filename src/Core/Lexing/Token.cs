namespace codecrafters_shell;

public record Token(TokenType Type)
{
    public TokenType Type { get; } = Type;
    public string Value { get; } = "";

    public Token(TokenType type, string value) : this(type)
    {
        Value = value;
    }

    public override string ToString()
    {
        return "Token" +
               "{\n" +
               $"Type: {Type}\n" +
               $"Value: {Value}\n" +
               "}";
    }
}