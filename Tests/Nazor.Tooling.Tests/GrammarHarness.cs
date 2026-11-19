using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Nazor.Tooling.Tests;

internal sealed record Token(int Line, string Text, IReadOnlyList<string> Scopes)
{
    public bool Has(string scope) => Scopes.Any(s => s == scope || s.StartsWith(scope + ".", StringComparison.Ordinal));

    public override string ToString() => $"'{Text}' [{string.Join(", ", Scopes)}]";
}

internal sealed class GrammarHarness : IRegistryOptions
{
    public static readonly string GrammarDirectory = Path.Combine(AppContext.BaseDirectory, "Grammars");
    public static readonly GrammarHarness Instance = new(ThemeName.Light);

    private readonly RegistryOptions builtIn;
    private readonly Registry registry;
    private readonly Dictionary<string, IGrammar> grammars = [];
    private readonly Lock gate = new();

    public GrammarHarness(ThemeName theme)
    {
        builtIn = new RegistryOptions(theme);
        registry = new Registry(this);
    }

    public IRawGrammar GetGrammar(string scopeName) => scopeName switch
    {
        "text.nazor" => Read("nazor.tmLanguage.json"),
        "source.nss" => Read("nss.tmLanguage.json"),
        "source.cs" => Read("csharp.tmLanguage.json"),
        _ => builtIn.GetGrammar(scopeName),
    };

    public ICollection<string> GetInjections(string scopeName) => builtIn.GetInjections(scopeName);

    public IRawTheme GetTheme(string scopeName) => builtIn.GetTheme(scopeName);

    public IRawTheme GetDefaultTheme() => builtIn.GetDefaultTheme();

    public (List<Token> Tokens, int FinalDepth) Tokenize(string scopeName, string source)
    {
        lock (gate)
        {
            if (!grammars.TryGetValue(scopeName, out var grammar)) grammars[scopeName] = grammar = registry.LoadGrammar(scopeName);
            var tokens = new List<Token>();
            IStateStack? stack = null;
            var lines = source.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var result = grammar.TokenizeLine(lines[i], stack, TimeSpan.FromSeconds(5));
                stack = result.RuleStack;
                foreach (var token in result.Tokens)
                {
                    var end = Math.Min(token.EndIndex, lines[i].Length);
                    if (end > token.StartIndex) tokens.Add(new Token(i + 1, lines[i][token.StartIndex..end], token.Scopes.ToList()));
                }
            }
            return (tokens, stack?.Depth ?? 0);
        }
    }

    public List<List<(string Text, string? Color, bool Bold, bool Italic)>> Colorize(string scopeName, string source)
    {
        lock (gate)
        {
            if (!grammars.TryGetValue(scopeName, out var grammar)) grammars[scopeName] = grammar = registry.LoadGrammar(scopeName);
            var colors = registry.GetColorMap().ToList();
            var result = new List<List<(string, string?, bool, bool)>>();
            IStateStack? stack = null;
            foreach (var line in source.Replace("\r\n", "\n").Split('\n'))
            {
                var tokens = grammar.TokenizeLine2(line, stack, TimeSpan.FromSeconds(5));
                stack = tokens.RuleStack;
                var runs = new List<(string, string?, bool, bool)>();
                for (var i = 0; i < tokens.Tokens.Length; i += 2)
                {
                    var start = tokens.Tokens[i];
                    var end = i + 2 < tokens.Tokens.Length ? tokens.Tokens[i + 2] : line.Length;
                    if (end <= start) continue;
                    var metadata = (uint)tokens.Tokens[i + 1];
                    var fontStyle = (int)((metadata >> 11) & 0xF);
                    var foreground = (int)((metadata >> 15) & 0x1FF);
                    var color = foreground > 1 && foreground - 1 < colors.Count ? colors[foreground - 1] : null;
                    runs.Add((line[start..end], color, (fontStyle & 2) != 0, (fontStyle & 1) != 0));
                }
                result.Add(runs);
            }
            return result;
        }
    }

    private static IRawGrammar Read(string file)
    {
        using var reader = new StreamReader(Path.Combine(GrammarDirectory, file));
        return GrammarReader.ReadGrammarSync(reader);
    }
}

internal static class TokenAssertions
{
    public static Token At(this List<Token> tokens, string source, string text, int occurrence = 1)
    {
        var lines = source.Replace("\r\n", "\n").Split('\n');
        var found = 0;
        for (var line = 0; line < lines.Length; line++)
        {
            for (var index = lines[line].IndexOf(text, StringComparison.Ordinal); index >= 0; index = lines[line].IndexOf(text, index + 1, StringComparison.Ordinal))
            {
                if (++found != occurrence) continue;
                var offset = 0;
                foreach (var token in tokens.Where(t => t.Line == line + 1))
                {
                    if (index >= offset && index < offset + token.Text.Length) return token;
                    offset += token.Text.Length;
                }
            }
        }
        throw new InvalidOperationException($"'{text}' (occurrence {occurrence}) not found.");
    }

    public static void Scoped(this List<Token> tokens, string source, string text, string scope, int occurrence = 1)
    {
        var token = tokens.At(source, text, occurrence);
        Assert.True(token.Has(scope), $"{token} does not have {scope}.");
    }

    public static void NotScoped(this List<Token> tokens, string source, string text, string scope, int occurrence = 1)
    {
        var token = tokens.At(source, text, occurrence);
        Assert.False(token.Has(scope), $"{token} should not have {scope}.");
    }
}
