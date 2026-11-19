using System.Globalization;
using System.Text;

namespace Nazor.Style.Syntax;

/// <summary>Identifies the lexical category of an NSS token.</summary>
public enum StyleTokenKind : byte
{
    /// <summary>Represents an identifier.</summary>
    Ident,

    /// <summary>Represents a function name and opening parenthesis.</summary>
    Function,

    /// <summary>Represents an at-rule keyword.</summary>
    AtKeyword,

    /// <summary>Represents a hash-prefixed value.</summary>
    Hash,

    /// <summary>Represents a quoted string.</summary>
    String,

    /// <summary>Represents a unitless number.</summary>
    Number,

    /// <summary>Represents a percentage.</summary>
    Percentage,

    /// <summary>Represents a number with a unit.</summary>
    Dimension,

    /// <summary>Represents whitespace.</summary>
    Whitespace,

    /// <summary>Represents a colon.</summary>
    Colon,

    /// <summary>Represents a semicolon.</summary>
    Semicolon,

    /// <summary>Represents a comma.</summary>
    Comma,

    /// <summary>Represents an opening square bracket.</summary>
    LeftBracket,

    /// <summary>Represents a closing square bracket.</summary>
    RightBracket,

    /// <summary>Represents an opening parenthesis.</summary>
    LeftParen,

    /// <summary>Represents a closing parenthesis.</summary>
    RightParen,

    /// <summary>Represents an opening brace.</summary>
    LeftBrace,

    /// <summary>Represents a closing brace.</summary>
    RightBrace,

    /// <summary>Represents a delimiter character.</summary>
    Delim,

    /// <summary>Represents an unterminated or invalid string.</summary>
    BadString,

    /// <summary>Represents the end of the source text.</summary>
    EndOfFile,
}

/// <summary>
/// A token of NSS syntax, including CSS escapes in quoted strings but not escaped identifiers or full URL tokens. <see cref="Value" /> is the
/// identifier, function or at-keyword name, string contents, hash name, unit or delimiter; <see cref="Start" /> is
/// the offset in the tokenized text.
/// </summary>
/// <param name="Kind">The style token kind represented by this value.</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="Number">The number (double).</param>
/// <param name="Start">The start offset in the UTF-16 source text.</param>
/// <param name="Length">The length of the token in the original UTF-16 source text.</param>
public readonly record struct StyleToken(StyleTokenKind Kind, string Value, double Number, int Start, int Length)
{
    /// <summary>Tests the token kind and value, comparing the value without regard to case.</summary>
    /// <param name="kind">The style token kind represented by this value.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>true if both the kind and case-insensitive value match; otherwise, false.</returns>
    public bool Is(StyleTokenKind kind, string value) => Kind == kind && string.Equals(Value, value, StringComparison.OrdinalIgnoreCase);

    /// <summary>Tests whether this token is the specified delimiter character.</summary>
    /// <param name="c">The c (char).</param>
    /// <returns>true if the token represents the specified delimiter; otherwise, false.</returns>
    public bool IsDelim(char c) => Kind == StyleTokenKind.Delim && Value.Length == 1 && Value[0] == c;

    /// <summary>Serializes the token back to NSS text, safely quoting string backslashes, quotes and control characters for diagnostics and substituted values.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind switch
    {
        StyleTokenKind.Ident => Value,
        StyleTokenKind.Function => Value + "(",
        StyleTokenKind.AtKeyword => "@" + Value,
        StyleTokenKind.Hash => "#" + Value,
        StyleTokenKind.String => QuoteString(Value),
        StyleTokenKind.Number => Format(Number),
        StyleTokenKind.Percentage => Format(Number) + "%",
        StyleTokenKind.Dimension => Format(Number) + Value,
        StyleTokenKind.Whitespace => " ",
        StyleTokenKind.Colon => ":",
        StyleTokenKind.Semicolon => ";",
        StyleTokenKind.Comma => ",",
        StyleTokenKind.LeftBracket => "[",
        StyleTokenKind.RightBracket => "]",
        StyleTokenKind.LeftParen => "(",
        StyleTokenKind.RightParen => ")",
        StyleTokenKind.LeftBrace => "{",
        StyleTokenKind.RightBrace => "}",
        StyleTokenKind.Delim => Value,
        _ => "",
    };

    /// <summary>Serializes a token sequence back to NSS text.</summary>
    /// <param name="tokens">The NSS lexical token sequence.</param>
    /// <returns>The NSS text reconstructed from the token sequence.</returns>
    public static string Serialize(IEnumerable<StyleToken> tokens)
    {
        var builder = new StringBuilder();
        foreach (var token in tokens) builder.Append(token.ToString());
        return builder.ToString().Trim();
    }

    private static string Format(double number) => number.ToString("0.############", CultureInfo.InvariantCulture);

    private static string QuoteString(string value)
    {
        var text = new StringBuilder().Append('"');
        foreach (var scalar in value.EnumerateRunes())
        {
            if (scalar.Value is '"' or '\\') text.Append('\\').Append((char)scalar.Value);
            else if (scalar.Value < 32 || scalar.Value == 127) text.Append('\\').Append(scalar.Value.ToString("X", CultureInfo.InvariantCulture)).Append(' ');
            else text.Append(scalar.ToString());
        }
        return text.Append('"').ToString();
    }
}

/// <summary>Converts CSS text into tokens. Comments are dropped; whitespace runs become one token.</summary>
public static class StyleTokenizer
{
    /// <summary>Converts NSS text into tokens with original UTF-16 source positions, decoding quoted CSS hex escapes and line continuations. Unterminated strings remain invalid NSS.</summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The tokens produced from the source text.</returns>
    public static List<StyleToken> Tokenize(string text)
    {
        var tokens = new List<StyleToken>();
        var i = 0;
        while (i < text.Length)
        {
            var start = i;
            var c = text[i];
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 2;
                continue;
            }
            if (IsWhitespace(c))
            {
                while (i < text.Length && IsWhitespace(text[i])) i++;
                tokens.Add(new(StyleTokenKind.Whitespace, " ", 0, start, i - start));
                continue;
            }
            if (c is '"' or '\'')
            {
                tokens.Add(ReadString(text, ref i));
                continue;
            }
            if (StartsNumber(text, i))
            {
                tokens.Add(ReadNumeric(text, ref i));
                continue;
            }
            if (StartsIdentifier(text, i))
            {
                var name = ReadName(text, ref i);
                if (i < text.Length && text[i] == '(')
                {
                    i++;
                    tokens.Add(new(StyleTokenKind.Function, name, 0, start, i - start));

                    if (name.Equals("url", StringComparison.OrdinalIgnoreCase) && ReadUnquotedUrl(text, ref i) is { } url) tokens.AddRange(url);
                }
                else tokens.Add(new(StyleTokenKind.Ident, name, 0, start, i - start));
                continue;
            }
            if (c == '@' && StartsIdentifier(text, i + 1))
            {
                i++;
                var name = ReadName(text, ref i);
                tokens.Add(new(StyleTokenKind.AtKeyword, name, 0, start, i - start));
                continue;
            }
            if (c == '#' && i + 1 < text.Length && IsNameCharacter(text[i + 1]))
            {
                i++;
                var name = ReadName(text, ref i);
                tokens.Add(new(StyleTokenKind.Hash, name, 0, start, i - start));
                continue;
            }
            i++;
            var kind = c switch
            {
                ':' => StyleTokenKind.Colon,
                ';' => StyleTokenKind.Semicolon,
                ',' => StyleTokenKind.Comma,
                '[' => StyleTokenKind.LeftBracket,
                ']' => StyleTokenKind.RightBracket,
                '(' => StyleTokenKind.LeftParen,
                ')' => StyleTokenKind.RightParen,
                '{' => StyleTokenKind.LeftBrace,
                '}' => StyleTokenKind.RightBrace,
                _ => StyleTokenKind.Delim,
            };
            tokens.Add(new(kind, c.ToString(), 0, start, 1));
        }
        tokens.Add(new(StyleTokenKind.EndOfFile, "", 0, text.Length, 0));
        return tokens;
    }

    private static StyleToken[]? ReadUnquotedUrl(string text, ref int i)
    {
        var j = i;
        while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
        if (j >= text.Length || text[j] is '"' or '\'' or ')') return null;
        var start = j;
        while (j < text.Length && text[j] != ')' && !char.IsWhiteSpace(text[j])) j++;
        var value = text[start..j];
        var end = j;
        while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
        if (j >= text.Length || text[j] != ')') return null;
        i = j + 1;
        return [new(StyleTokenKind.String, value, 0, start, end - start), new(StyleTokenKind.RightParen, ")", 0, j, 1)];
    }

    private static StyleToken ReadString(string text, ref int i)
    {
        var start = i;
        var quote = text[i++];
        var value = new StringBuilder();
        while (i < text.Length)
        {
            var c = text[i];
            if (c == quote)
            {
                i++;
                return new(StyleTokenKind.String, value.ToString(), 0, start, i - start);
            }
            if (c is '\n' or '\r' or '\f') return new(StyleTokenKind.BadString, value.ToString(), 0, start, i - start);
            if (c == '\\')
            {
                i++;
                if (i == text.Length) break;
                if (text[i] is '\n' or '\r' or '\f')
                {
                    if (text[i++] == '\r' && i < text.Length && text[i] == '\n') i++;
                    continue;
                }
                if (HexValue(text[i]) >= 0)
                {
                    var scalar = 0;
                    var digits = 0;
                    while (i < text.Length && digits < 6 && HexValue(text[i]) is var digit && digit >= 0)
                    {
                        scalar = scalar * 16 + digit;
                        digits++; i++;
                    }
                    if (i < text.Length && IsWhitespace(text[i]))
                    {
                        if (text[i++] == '\r' && i < text.Length && text[i] == '\n') i++;
                    }
                    value.Append(scalar == 0 || !Rune.IsValid(scalar) ? "\uFFFD" : char.ConvertFromUtf32(scalar));
                }
                else AppendScalar(text, ref i, value);
                continue;
            }
            AppendScalar(text, ref i, value);
        }
        return new(StyleTokenKind.BadString, value.ToString(), 0, start, i - start);
    }

    private static int HexValue(char value) => value switch
    {
        >= '0' and <= '9' => value - '0',
        >= 'a' and <= 'f' => value - 'a' + 10,
        >= 'A' and <= 'F' => value - 'A' + 10,
        _ => -1,
    };

    private static void AppendScalar(string text, ref int index, StringBuilder value)
    {
        var current = text[index++];
        if (char.IsHighSurrogate(current) && index < text.Length && char.IsLowSurrogate(text[index]))
            value.Append(current).Append(text[index++]);
        else value.Append(current == '\0' || char.IsSurrogate(current) ? '\uFFFD' : current);
    }

    private static StyleToken ReadNumeric(string text, ref int i)
    {
        var start = i;
        if (text[i] is '+' or '-') i++;
        while (i < text.Length && char.IsAsciiDigit(text[i])) i++;
        if (i + 1 < text.Length && text[i] == '.' && char.IsAsciiDigit(text[i + 1]))
        {
            i++;
            while (i < text.Length && char.IsAsciiDigit(text[i])) i++;
        }
        if (i + 1 < text.Length && text[i] is 'e' or 'E' &&
            (char.IsAsciiDigit(text[i + 1]) || (text[i + 1] is '+' or '-' && i + 2 < text.Length && char.IsAsciiDigit(text[i + 2]))))
        {
            i += 2;
            while (i < text.Length && char.IsAsciiDigit(text[i])) i++;
        }
        var number = double.Parse(text.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        if (i < text.Length && text[i] == '%')
        {
            i++;
            return new(StyleTokenKind.Percentage, "%", number, start, i - start);
        }
        if (StartsIdentifier(text, i))
        {
            var unit = ReadName(text, ref i);
            return new(StyleTokenKind.Dimension, unit.ToLowerInvariant(), number, start, i - start);
        }
        return new(StyleTokenKind.Number, "", number, start, i - start);
    }

    private static string ReadName(string text, ref int i)
    {
        var start = i;
        while (i < text.Length && IsNameCharacter(text[i])) i++;
        return text[start..i];
    }

    private static bool StartsNumber(string text, int i)
    {
        if (i >= text.Length) return false;
        var c = text[i];
        if (char.IsAsciiDigit(c)) return true;
        if (c == '.') return i + 1 < text.Length && char.IsAsciiDigit(text[i + 1]);
        if (c is '+' or '-')
            return i + 1 < text.Length && (char.IsAsciiDigit(text[i + 1]) || (text[i + 1] == '.' && i + 2 < text.Length && char.IsAsciiDigit(text[i + 2])));
        return false;
    }

    private static bool StartsIdentifier(string text, int i)
    {
        if (i >= text.Length) return false;
        var c = text[i];
        if (c == '-') return i + 1 < text.Length && (IsNameStart(text[i + 1]) || text[i + 1] == '-');
        return IsNameStart(c);
    }

    private static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_' || c >= 0x80;

    private static bool IsNameCharacter(char c) => IsNameStart(c) || char.IsAsciiDigit(c) || c == '-';

    private static bool IsWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
}
