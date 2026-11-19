using System.Globalization;

namespace Nazor.Style.Syntax;

/// <summary>Raised when a value does not match the grammar of a property.</summary>
/// <param name="message">The message (string).</param>
/// <param name="token">The NSS token identifying the value or error location.</param>
public sealed class StyleValueException(string message, StyleToken token) : Exception(message)
{
    /// <summary>Gets the source token associated with the invalid value.</summary>
    public StyleToken Token { get; } = token;
}

/// <summary>Reads the component values of a declaration, skipping whitespace on request.</summary>
/// <param name="tokens">The NSS lexical token sequence.</param>
public sealed class TokenReader(IReadOnlyList<StyleToken> tokens)
{
    private int position;

    /// <summary>Gets the NSS token sequence represented by this declaration or reader.</summary>
    public IReadOnlyList<StyleToken> Tokens { get; } = tokens;

    /// <summary>Gets whether only whitespace or an end-of-input token remains.</summary>
    public bool AtEnd
    {
        get
        {
            SkipWhitespace();
            return position >= Tokens.Count || Tokens[position].Kind == StyleTokenKind.EndOfFile;
        }
    }

    /// <summary>Returns the next token without consuming it, skipping whitespace.</summary>
    /// <returns>The next non-whitespace token, or the end-of-input token.</returns>
    public StyleToken Peek()
    {
        SkipWhitespace();
        return position < Tokens.Count ? Tokens[position] : End;
    }

    /// <summary>Consumes and returns the next token, skipping surrounding whitespace.</summary>
    /// <returns>The next non-whitespace token, or the end-of-input token.</returns>
    public StyleToken Next()
    {
        var token = Peek();
        if (position < Tokens.Count) position++;
        return token;
    }

    /// <summary>Whitespace comes before the next token (as <c>+</c> and <c>-</c> require in <c>calc()</c>).</summary>
    public bool WhitespaceBefore
    {
        get
        {
            SkipWhitespace();
            return position > 0 && Tokens[position - 1].Kind == StyleTokenKind.Whitespace;
        }
    }

    /// <summary>Whitespace follows the last token read.</summary>
    public bool WhitespaceAfter => position < Tokens.Count && Tokens[position].Kind == StyleTokenKind.Whitespace;

    /// <summary>Consumes the next token if its kind matches the supplied kind.</summary>
    /// <param name="kind">The style token kind represented by this value.</param>
    /// <returns>true if a matching token was consumed; otherwise, false.</returns>
    public bool TryConsume(StyleTokenKind kind)
    {
        if (Peek().Kind != kind) return false;
        position++;
        return true;
    }

    /// <summary>Consumes the next token if it is the specified case-insensitive identifier.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>true if the specified identifier was consumed; otherwise, false.</returns>
    public bool TryConsumeIdent(string name)
    {
        if (!Peek().Is(StyleTokenKind.Ident, name)) return false;
        position++;
        return true;
    }

    /// <summary>Consumes a token of the required kind or throws a style-value exception.</summary>
    /// <param name="kind">The style token kind represented by this value.</param>
    /// <param name="description">The expected syntax displayed in an error message.</param>
    public void Expect(StyleTokenKind kind, string description)
    {
        var token = Next();
        if (token.Kind != kind) throw Error($"Expected {description}.", token);
    }

    /// <summary>Throws a style-value exception if unconsumed tokens remain.</summary>
    public void ExpectEnd()
    {
        if (!AtEnd) throw Error($"Unexpected '{Peek()}'.", Peek());
    }

    /// <summary>Splits the remaining tokens at top-level commas.</summary>
    /// <returns>The resulting List&lt;TokenReader&gt; value.</returns>
    public List<TokenReader> SplitByComma()
    {
        var parts = new List<TokenReader>();
        var current = new List<StyleToken>();
        var depth = 0;
        for (; position < Tokens.Count && Tokens[position].Kind != StyleTokenKind.EndOfFile; position++)
        {
            var token = Tokens[position];
            if (token.Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
            else if (token.Kind == StyleTokenKind.RightParen) depth--;
            if (token.Kind == StyleTokenKind.Comma && depth == 0)
            {
                parts.Add(new TokenReader(current));
                current = [];
                continue;
            }
            current.Add(token);
        }
        parts.Add(new TokenReader(current));
        return parts;
    }

    /// <summary>
    /// The arguments of the function whose name was just read, split at its top-level commas, up to its closing
    /// parenthesis. Whitespace stays: <c>calc()</c> needs it around <c>+</c> and <c>-</c>.
    /// </summary>
    /// <param name="function">The function (StyleToken).</param>
    /// <returns>The resulting List&lt;TokenReader&gt; value.</returns>
    public List<TokenReader> FunctionArguments(StyleToken function)
    {
        var parts = new List<TokenReader>();
        var current = new List<StyleToken>();
        var depth = 0;
        while (true)
        {
            if (position >= Tokens.Count || Tokens[position].Kind == StyleTokenKind.EndOfFile)
                throw Error($"Missing ')' after {function.Value}(.", function);
            var token = Tokens[position++];
            if (token.Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
            else if (token.Kind == StyleTokenKind.RightParen)
            {
                if (depth == 0) break;
                depth--;
            }
            else if (token.Kind == StyleTokenKind.Comma && depth == 0)
            {
                parts.Add(new TokenReader(current));
                current = [];
                continue;
            }
            current.Add(token);
        }
        parts.Add(new TokenReader(current));
        return parts;
    }

    /// <summary>Gets the end-of-input token returned when no tokens remain.</summary>
    public StyleToken End => position > 0 && position <= Tokens.Count
        ? new StyleToken(StyleTokenKind.EndOfFile, "", 0, Tokens[^1].Start + Tokens[^1].Length, 0)
        : new StyleToken(StyleTokenKind.EndOfFile, "", 0, 0, 0);

    /// <summary>Creates a style-value exception associated with the supplied token.</summary>
    /// <param name="message">The message (string).</param>
    /// <param name="token">The NSS token identifying the value or error location.</param>
    /// <returns>The exception carrying the message and source token.</returns>
    public static StyleValueException Error(string message, StyleToken token) => new(message, token);

    private void SkipWhitespace()
    {
        while (position < Tokens.Count && Tokens[position].Kind == StyleTokenKind.Whitespace) position++;
    }
}

/// <summary>Grammars of the value types shared by properties.</summary>
public static class ValueParsers
{
    /// <summary>Starts a length: a number, a dimension, a percentage or a math function.</summary>
    /// <param name="token">The NSS token identifying the value or error location.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool IsLengthStart(StyleToken token) =>
        token.Kind is StyleTokenKind.Number or StyleTokenKind.Dimension or StyleTokenKind.Percentage || IsMathFunction(token);

    /// <summary>Determines whether the token begins a supported NSS mathematical function.</summary>
    /// <param name="token">The NSS token identifying the value or error location.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool IsMathFunction(StyleToken token) =>
        token.Kind == StyleTokenKind.Function && token.Value.ToLowerInvariant() is "calc" or "min" or "max" or "clamp";

    /// <summary>Parses length from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <param name="allowPercent">Whether percentage lengths are accepted.</param>
    /// <param name="allowNegative">Whether negative length values are accepted.</param>
    /// <returns>The resulting Length value.</returns>
    public static Length ParseLength(TokenReader reader, bool allowPercent = true, bool allowNegative = false)
    {
        var token = reader.Next();
        Length length;
        switch (token.Kind)
        {
            case StyleTokenKind.Number when token.Number == 0:
                length = Length.Zero;
                break;
            case StyleTokenKind.Dimension when Unit(token.Value) is { } unit:
                length = new Length(token.Number, unit);
                break;
            case StyleTokenKind.Dimension:
                throw TokenReader.Error($"Unit '{token.Value}' is not supported: use px, em, rem, ch, ex, vw, vh, vmin, vmax or %.", token);
            case StyleTokenKind.Percentage when allowPercent:
                length = Length.Percent(token.Number);
                break;
            case StyleTokenKind.Function when IsMathFunction(token):

                return new CalcParser(reader, allowPercent).Parse(token, nonNegative: !allowNegative);
            case StyleTokenKind.Number:
                throw TokenReader.Error($"A length needs a unit: '{token}px'.", token);
            default:
                throw TokenReader.Error(allowPercent ? "Expected a length (px) or a percentage." : "Expected a length (px).", token);
        }
        if (!allowNegative && length.Value < 0) throw TokenReader.Error("Negative values are not allowed here.", token);
        return length;
    }

    private static LengthUnit? Unit(string unit) => unit.ToLowerInvariant() switch
    {
        "px" => LengthUnit.Px,
        "em" => LengthUnit.Em,
        "rem" => LengthUnit.Rem,
        "ch" => LengthUnit.Ch,
        "ex" => LengthUnit.Ex,
        "vw" => LengthUnit.Vw,
        "vh" => LengthUnit.Vh,
        "vmin" => LengthUnit.Vmin,
        "vmax" => LengthUnit.Vmax,
        _ => null,
    };

    private sealed class CalcParser(TokenReader reader, bool allowPercent)
    {
        public Length Parse(StyleToken function, bool nonNegative)
        {
            var (root, isNumber) = Function(function);
            if (isNumber) throw TokenReader.Error($"{function.Value}() must give a length, not a number.", function);
            return CalcExpression.Simplify(root, nonNegative);
        }

        public double ParseNumber(StyleToken function)
        {
            var (root, isNumber) = Function(function);
            if (!isNumber) throw TokenReader.Error($"{function.Value}() must give a number, not a length.", function);
            return root.Evaluate(0);
        }

        private (CalcNode Node, bool IsNumber) Function(StyleToken function)
        {
            var name = function.Value.ToLowerInvariant();
            if (name == "calc")
            {
                var sum = Sum();
                reader.Expect(StyleTokenKind.RightParen, "')'");
                return sum;
            }
            var arguments = new List<(CalcNode Node, bool IsNumber)> { Sum() };
            while (reader.TryConsume(StyleTokenKind.Comma)) arguments.Add(Sum());
            var close = reader.Peek();
            reader.Expect(StyleTokenKind.RightParen, "',' or ')'");
            if (name == "clamp" && arguments.Count != 3) throw TokenReader.Error("clamp() takes three values: minimum, preferred and maximum.", close);
            if (arguments.Any(a => a.IsNumber != arguments[0].IsNumber))
                throw TokenReader.Error($"The values of {name}() must all be lengths or all numbers.", function);
            var kind = name switch
            {
                "min" => CalcFunctionKind.Min,
                "max" => CalcFunctionKind.Max,
                _ => CalcFunctionKind.Clamp,
            };
            return (new CalcFunction(kind, new ValueList<CalcNode>(arguments.Select(a => a.Node))), arguments[0].IsNumber);
        }

        private (CalcNode Node, bool IsNumber) Sum()
        {
            var left = Product();
            while (true)
            {
                var token = reader.Peek();

                if (token.Kind is StyleTokenKind.Number or StyleTokenKind.Dimension or StyleTokenKind.Percentage)
                    throw TokenReader.Error("Missing an operator: + and - need a space on both sides, like 100% - 10px.", token);
                if (!token.IsDelim('+') && !token.IsDelim('-')) return left;
                if (!reader.WhitespaceBefore) throw TokenReader.Error($"'{token.Value}' needs a space on both sides in calc().", token);
                reader.Next();
                if (!reader.WhitespaceAfter) throw TokenReader.Error($"'{token.Value}' needs a space on both sides in calc().", token);
                var right = Product();
                if (left.IsNumber != right.IsNumber) throw TokenReader.Error("Cannot add a number and a length: write the unit, like 0px.", token);
                left = (new CalcOperation(token.IsDelim('+') ? CalcOperator.Add : CalcOperator.Subtract, left.Node, right.Node), left.IsNumber);
            }
        }

        private (CalcNode Node, bool IsNumber) Product()
        {
            var left = Value();
            while (true)
            {
                var token = reader.Peek();
                if (!token.IsDelim('*') && !token.IsDelim('/')) return left;
                reader.Next();
                var right = Value();
                if (token.IsDelim('*'))
                {
                    if (!left.IsNumber && !right.IsNumber) throw TokenReader.Error("Cannot multiply two lengths: one side of '*' must be a number.", token);
                    left = (new CalcOperation(CalcOperator.Multiply, left.Node, right.Node), left.IsNumber && right.IsNumber);
                }
                else
                {
                    if (!right.IsNumber) throw TokenReader.Error("Cannot divide by a length: the right side of '/' must be a number.", token);
                    if (right.Node.Reduce() is { Number: 0 }) throw TokenReader.Error("Division by zero.", token);
                    left = (new CalcOperation(CalcOperator.Divide, left.Node, right.Node), left.IsNumber);
                }
            }
        }

        private (CalcNode Node, bool IsNumber) Value()
        {
            var token = reader.Next();
            switch (token.Kind)
            {
                case StyleTokenKind.Number:
                    return (new CalcNumber(token.Number), true);
                case StyleTokenKind.Dimension when Unit(token.Value) is { } unit:
                    return (new CalcLength(new Length(token.Number, unit)), false);
                case StyleTokenKind.Percentage when allowPercent:
                    return (new CalcLength(Length.Percent(token.Number)), false);
                case StyleTokenKind.Percentage:
                    throw TokenReader.Error("Percentages are not allowed here.", token);
                case StyleTokenKind.LeftParen:
                    var sum = Sum();
                    reader.Expect(StyleTokenKind.RightParen, "')'");
                    return sum;
                case StyleTokenKind.Function when IsMathFunction(token):
                    return Function(token);
                case StyleTokenKind.Dimension:
                    throw TokenReader.Error($"Unit '{token.Value}' is not supported: use px, em, rem, vw, vh, vmin, vmax or %.", token);
                default:
                    throw TokenReader.Error($"Expected a number, a length or '(' in calc(), found '{token}'.", token);
            }
        }
    }

    /// <summary>Parses a number or a unitless calc, min, max or clamp expression from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <param name="min">The smallest accepted value.</param>
    /// <param name="max">The largest accepted value.</param>
    /// <returns>The resulting double value.</returns>
    public static double ParseNumber(TokenReader reader, double min = double.MinValue, double max = double.MaxValue)
    {
        var token = reader.Next();
        var number = token.Kind == StyleTokenKind.Function && IsMathFunction(token)
            ? new CalcParser(reader, allowPercent: false).ParseNumber(token)
            : token.Kind == StyleTokenKind.Number ? token.Number : throw TokenReader.Error("Expected a number.", token);
        if (!double.IsFinite(number) || number < min || number > max)
            throw TokenReader.Error(string.Create(CultureInfo.InvariantCulture, $"The value must be between {min} and {max}."), token);
        return number;
    }

    /// <summary>Parses integer from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The resulting int value.</returns>
    public static int ParseInteger(TokenReader reader) => ParseInteger(reader, int.MinValue);

    /// <summary>Parses integer from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <param name="min">The smallest accepted value.</param>
    /// <returns>The resulting int value.</returns>
    public static int ParseInteger(TokenReader reader, int min)
    {
        var token = reader.Next();
        if (token.Kind != StyleTokenKind.Number || token.Number != Math.Floor(token.Number) || Math.Abs(token.Number) > int.MaxValue)
            throw TokenReader.Error("Expected an integer.", token);
        if (token.Number < min) throw TokenReader.Error(string.Create(CultureInfo.InvariantCulture, $"The value must be at least {min}."), token);
        return (int)token.Number;
    }

    /// <summary>Parses time from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The resulting TimeSpan value.</returns>
    public static TimeSpan ParseTime(TokenReader reader)
    {
        var token = reader.Next();
        if (token.Kind == StyleTokenKind.Dimension && token.Number >= 0)
        {
            if (token.Value == "ms") return TimeSpan.FromMilliseconds(token.Number);
            if (token.Value == "s") return TimeSpan.FromSeconds(token.Number);
        }
        if (token.Kind == StyleTokenKind.Number && token.Number == 0) return TimeSpan.Zero;
        throw TokenReader.Error("Expected a non-negative time (ms or s).", token);
    }

    /// <summary>Parses an NSS color, including premultiplied sRGB or Oklab color mixing, into straight-alpha 8-bit sRGB.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The resulting Color value.</returns>
    public static Color ParseColor(TokenReader reader)
    {
        var token = reader.Next();
        switch (token.Kind)
        {
            case StyleTokenKind.Hash:
                return ParseHex(token);
            case StyleTokenKind.Ident when token.Value.Equals("currentcolor", StringComparison.OrdinalIgnoreCase):
                return Color.CurrentColor;
            case StyleTokenKind.Ident when NamedColors.TryGet(token.Value, out var named):
                return named;
            case StyleTokenKind.Function when token.Value.ToLowerInvariant() is "rgb" or "rgba":
                return ParseRgb(reader, token);
            case StyleTokenKind.Function when token.Value.ToLowerInvariant() is "hsl" or "hsla":
                return ParseHsl(reader, token);
            case StyleTokenKind.Function when token.Value.Equals("color-mix", StringComparison.OrdinalIgnoreCase):
                return ParseColorMix(reader, token);
            default:
                throw TokenReader.Error($"Expected a color, found '{token}'.", token);
        }
    }

    /// <summary>Parses keyword from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <param name="keywords">The case-insensitive map from NSS keywords to their values.</param>
    /// <typeparam name="TEnum">The enumeration type represented by the accepted keywords.</typeparam>
    /// <returns>The resulting TEnum value.</returns>
    public static TEnum ParseKeyword<TEnum>(TokenReader reader, IReadOnlyDictionary<string, TEnum> keywords) where TEnum : struct
    {
        var token = reader.Next();
        if (token.Kind == StyleTokenKind.Ident && keywords.TryGetValue(token.Value.ToLowerInvariant(), out var value)) return value;
        throw TokenReader.Error($"Expected one of: {string.Join(", ", keywords.Keys)}.", token);
    }

    /// <summary>Parses easing from the current token position.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The resulting EasingFunction value.</returns>
    public static EasingFunction ParseEasing(TokenReader reader)
    {
        var token = reader.Next();
        if (token.Kind == StyleTokenKind.Ident)
        {
            switch (token.Value.ToLowerInvariant())
            {
                case "linear": return EasingFunction.Linear;
                case "ease": return EasingFunction.Ease;
                case "ease-in": return EasingFunction.EaseIn;
                case "ease-out": return EasingFunction.EaseOut;
                case "ease-in-out": return EasingFunction.EaseInOut;
            }
        }
        if (token.Kind == StyleTokenKind.Function && token.Value.Equals("cubic-bezier", StringComparison.OrdinalIgnoreCase))
        {
            var values = new double[4];
            for (var i = 0; i < 4; i++)
            {
                if (i > 0) reader.Expect(StyleTokenKind.Comma, "','");
                values[i] = ParseNumber(reader, i % 2 == 0 ? 0 : double.MinValue, i % 2 == 0 ? 1 : double.MaxValue);
            }
            reader.Expect(StyleTokenKind.RightParen, "')'");
            return new EasingFunction(values[0], values[1], values[2], values[3]);
        }
        throw TokenReader.Error("Expected a timing function (linear, ease, ease-in, ease-out, ease-in-out, cubic-bezier()).", token);
    }

    private static Color ParseHex(StyleToken token)
    {
        var hex = token.Value;
        if (hex.Any(c => !char.IsAsciiHexDigit(c)) || hex.Length is not (3 or 4 or 6 or 8))
            throw TokenReader.Error($"Invalid hexadecimal color '#{hex}'.", token);
        if (hex.Length <= 4) hex = string.Concat(hex.Select(c => new string(c, 2)));
        if (hex.Length == 6) hex += "ff";

        return Color.FromRgba(Convert.ToUInt32(hex, 16));
    }

    private static Color ParseRgb(TokenReader reader, StyleToken function)
    {
        var channels = ParseChannels(reader, function, out var alpha);
        return new Color(Channel(channels[0], 255), Channel(channels[1], 255), Channel(channels[2], 255), alpha);
    }

    private static Color ParseHsl(TokenReader reader, StyleToken function)
    {
        var channels = ParseChannels(reader, function, out var alpha);
        var hue = ((channels[0].Number % 360) + 360) % 360 / 360;
        var saturation = Math.Clamp(channels[1].Number / 100, 0, 1);
        var lightness = Math.Clamp(channels[2].Number / 100, 0, 1);
        double r = lightness, g = lightness, b = lightness;
        if (saturation > 0)
        {
            var q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
            var p = 2 * lightness - q;
            r = Hue(p, q, hue + 1.0 / 3);
            g = Hue(p, q, hue);
            b = Hue(p, q, hue - 1.0 / 3);
        }
        return new Color((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255), alpha);
    }

    private static Color ParseColorMix(TokenReader reader, StyleToken function) => ParseColorMixValue(reader, function).ToColor();

    private static ColorMixValue ParseColorMixValue(TokenReader reader, StyleToken function)
    {
        if (!reader.TryConsumeIdent("in")) throw TokenReader.Error("Expected 'in srgb' or 'in oklab'.", reader.Peek());
        var space = reader.Next();
        var oklab = space.Is(StyleTokenKind.Ident, "oklab");
        if (!oklab && !space.Is(StyleTokenKind.Ident, "srgb")) throw TokenReader.Error("color-mix() mixes in srgb or oklab.", space);
        reader.Expect(StyleTokenKind.Comma, "','");
        var (first, p1) = ParseMixComponent(reader, oklab);
        reader.Expect(StyleTokenKind.Comma, "','");
        var (second, p2) = ParseMixComponent(reader, oklab);
        reader.Expect(StyleTokenKind.RightParen, "')'");

        var (w1, w2) = (p1, p2) switch
        {
            (null, null) => (50.0, 50.0),
            (_, null) => (p1.Value, 100 - p1.Value),
            (null, _) => (100 - p2.Value, p2.Value),
            _ => (p1.Value, p2.Value),
        };
        var sum = w1 + w2;
        if (sum == 0) throw TokenReader.Error("The percentages of color-mix() cannot both be zero.", function);
        if (oklab) return OklabColorMix.Mix(first, second, w1, w2);

        var firstColor = first.ToColor();
        var secondColor = second.ToColor();
        double a1 = firstColor.A / 255.0, a2 = secondColor.A / 255.0, weight = a1 * w1 + a2 * w2;
        byte Mix(byte c1, byte c2) => weight == 0 ? (byte)0 : (byte)Math.Round((c1 * a1 * w1 + c2 * a2 * w2) / weight, MidpointRounding.AwayFromZero);
        return ColorMixValue.FromColor(new Color(Mix(firstColor.R, secondColor.R), Mix(firstColor.G, secondColor.G), Mix(firstColor.B, secondColor.B),
            (byte)Math.Round(weight / sum * Math.Min(sum, 100) / 100 * 255, MidpointRounding.AwayFromZero)));
    }

    private static (ColorMixValue Color, double? Percentage) ParseMixComponent(TokenReader reader, bool preservePrecision)
    {
        var percentage = ParseMixPercentage(reader);
        var start = reader.Peek();
        ColorMixValue value;
        if (preservePrecision && start.Is(StyleTokenKind.Function, "color-mix"))
        {
            reader.Next();
            value = ParseColorMixValue(reader, start);
        }
        else
        {
            var color = ParseColor(reader);
            if (color.IsCurrentColor) throw TokenReader.Error("color-mix() cannot mix currentcolor.", start);
            value = ColorMixValue.FromColor(color);
        }
        return (value, percentage ?? ParseMixPercentage(reader));
    }

    private static double? ParseMixPercentage(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind != StyleTokenKind.Percentage) return null;
        reader.Next();
        if (!double.IsFinite(token.Number) || token.Number is < 0 or > 100) throw TokenReader.Error("The percentage must be between 0% and 100%.", token);
        return token.Number;
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 0.5) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    private static StyleToken[] ParseChannels(TokenReader reader, StyleToken function, out byte alpha)
    {
        var channels = new StyleToken[3];
        var commas = false;
        for (var i = 0; i < 3; i++)
        {
            if (i == 1) commas = reader.TryConsume(StyleTokenKind.Comma);
            else if (i > 1 && commas) reader.Expect(StyleTokenKind.Comma, "','");
            var token = reader.Next();
            if (token.Kind is not (StyleTokenKind.Number or StyleTokenKind.Percentage) &&
                !(i == 0 && token.Kind == StyleTokenKind.Dimension && token.Value == "deg"))
                throw TokenReader.Error($"Invalid {function.Value}() channel '{token}'.", token);
            channels[i] = token;
        }
        alpha = 255;
        var separator = commas ? reader.TryConsume(StyleTokenKind.Comma) : reader.Peek().IsDelim('/') && reader.Next().IsDelim('/');
        if (separator)
        {
            var token = reader.Next();
            var value = token.Kind switch
            {
                StyleTokenKind.Number => token.Number,
                StyleTokenKind.Percentage => token.Number / 100,
                _ => throw TokenReader.Error("Invalid alpha value.", token),
            };
            alpha = (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
        }
        reader.Expect(StyleTokenKind.RightParen, "')'");
        return channels;
    }

    private static byte Channel(StyleToken token, double max) =>
        (byte)Math.Round(Math.Clamp(token.Kind == StyleTokenKind.Percentage ? token.Number / 100 * max : token.Number, 0, max));
}
