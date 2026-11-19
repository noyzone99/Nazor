using Nazor.Style;
using Nazor.Style.Syntax;

namespace Nazor.Nss;

internal static class DeclarationCompiler
{
    public static void Compile(StyleToken name, List<StyleToken> value, List<StyleDeclaration> output, NssCompiler.Context context)
    {
        var propertyName = name.Value;
        var bang = value.FindIndex(t => t.IsDelim('!'));
        if (bang >= 0)
        {
            context.Error("NZS105", value[bang], "!important is not supported by NSS: use specificity or a more specific stylesheet level.");
            return;
        }
        if (SystemColors.MayName(propertyName)) value = [.. SystemColors.Substitute(value)];
        if (StyleProperties.IsCustomProperty(propertyName))
        {
            output.Add(new CustomPropertyDeclaration(propertyName, Strip(value)));
            return;
        }
        var isShorthand = StyleProperties.TryGetShorthand(propertyName, out var shorthand);
        if (!isShorthand && !StyleProperties.TryGet(propertyName, out _))
        {
            var suggestion = Suggest(propertyName);
            context.Error("NZS101", name, $"Unknown property '{propertyName}'." + (suggestion is null ? "" : $" Did you mean '{suggestion}'?"));
            return;
        }
        if (value.Count == 0)
        {
            context.Error("NZS102", name, $"Missing value for '{propertyName}'.");
            return;
        }
        var longhands = isShorthand ? shorthand.Longhands : [Longhand(propertyName)];
        if (value.Any(t => t.Kind == StyleTokenKind.Function && t.Value.Equals("var", StringComparison.OrdinalIgnoreCase)))
        {
            if (ValidateVar(value, context)) output.Add(new PendingDeclaration(isShorthand ? shorthand.Name : longhands[0].Name, Strip(value)));
            return;
        }
        if (value is [{ Kind: StyleTokenKind.Ident } keyword] && Keyword(keyword.Value) is { } global)
        {
            output.AddRange(longhands.Select(l => new KeywordDeclaration(l, global)));
            return;
        }
        try
        {
            var reader = new TokenReader(value);
            if (isShorthand) output.AddRange(shorthand.Expand(reader).Select(v => new ValueDeclaration(v.Property, v.Value)));
            else output.Add(new ValueDeclaration(longhands[0], longhands[0].Parse(reader)));
        }
        catch (StyleValueException exception)
        {
            var token = exception.Token.Kind == StyleTokenKind.EndOfFile ? value[^1] : exception.Token;
            context.Error("NZS102", token, $"Invalid value for '{propertyName}': {exception.Message}");
        }
    }

    private static StyleProperty Longhand(string name)
    {
        StyleProperties.TryGet(name, out var property);
        return property;
    }

    private static GlobalKeyword? Keyword(string value) => value.ToLowerInvariant() switch
    {
        "initial" => GlobalKeyword.Initial,
        "inherit" => GlobalKeyword.Inherit,
        "unset" => GlobalKeyword.Unset,
        _ => null,
    };

    private static bool ValidateVar(List<StyleToken> value, NssCompiler.Context context)
    {
        for (var i = 0; i < value.Count; i++)
        {
            if (value[i].Kind != StyleTokenKind.Function || !value[i].Value.Equals("var", StringComparison.OrdinalIgnoreCase)) continue;
            var j = i + 1;
            while (j < value.Count && value[j].Kind == StyleTokenKind.Whitespace) j++;
            if (j >= value.Count || value[j].Kind != StyleTokenKind.Ident || !StyleProperties.IsCustomProperty(value[j].Value))
            {
                context.Error("NZS102", j < value.Count ? value[j] : value[i], "var() expects a custom property name such as --surface.");
                return false;
            }
        }
        var depth = 0;
        foreach (var token in value)
        {
            if (token.Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
            else if (token.Kind == StyleTokenKind.RightParen) depth--;
        }
        if (depth != 0)
        {
            context.Error("NZS102", value[^1], "Unbalanced parentheses in the value.");
            return false;
        }
        return true;
    }

    private static StyleToken[] Strip(List<StyleToken> tokens) => tokens.Select(t => t with { Start = 0, Length = 0 }).ToArray();

    private static string? Suggest(string name)
    {
        var candidates = StyleProperties.All.Select(p => p.Name).Concat(StyleProperties.AllShorthands.Select(s => s.Name));
        string? best = null;
        var bestDistance = int.MaxValue;
        foreach (var candidate in candidates)
        {
            var distance = Distance(name.ToLowerInvariant(), candidate);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        return bestDistance <= Math.Max(2, name.Length / 4) ? best : null;
    }

    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
