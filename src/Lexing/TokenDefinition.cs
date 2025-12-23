// using System.Text.RegularExpressions;
//
// namespace codecrafters_shell;
//
// public static class TokenDefinition
// {
//     public static readonly (TokenType type, Regex Pattern)[] TokenSpecs =
//     [
//         (TokenType.RedirectOut, new Regex(">|1>", RegexOptions.Compiled)),
//         (TokenType.StringLiteral, new Regex("\"(?:\\\\.|[^\"\\\\])*\"", RegexOptions.Compiled)),
//         (TokenType.StringLiteral, new Regex("'[^']*'", RegexOptions.Compiled)),
//         (TokenType.Word, new Regex(@"[^\s|<>]+", RegexOptions.Compiled))
//     ];
// }