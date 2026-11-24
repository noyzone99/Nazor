using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Nazor.Website.Shared;

/// <summary>
/// Produces lightweight syntax-highlighted HTML for the languages used by the Nazor documentation.
/// The generator uses it at build time and the website uses it for the snippets on the home page.
/// </summary>
public static partial class CodeHighlighter
{
    private const string CSharpKeywords =
        "abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|" +
        "else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|get|goto|if|implicit|in|init|int|interface|internal|is|" +
        "lock|long|nameof|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|record|ref|" +
        "required|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|" +
        "unchecked|unsafe|ushort|using|value|var|virtual|void|volatile|when|where|while|with|yield|global|file|scoped|notnull|unmanaged|" +
        "and|or|not|add|remove|dynamic|let|from|select|orderby|group|into|join|on|equals|by|ascending|descending";

    private static readonly Regex CSharp = new(
        @"(?<c>//[^\n]*|/\*[\s\S]*?\*/)" +
        @"|(?<s>\$?@""(?:[^""]|"""")*""|""""""[\s\S]*?""""""|\$?""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)'?)" +
        @"|(?<p>^[ \t]*\#[a-z]+[^\n]*)" +
        @"|(?<a>(?<=\[)[A-Z][A-Za-z0-9_]*(?=[\](]))" +
        @"|(?<k>\b(?:" + CSharpKeywords + @")\b)" +
        @"|(?<n>\b(?:0x[0-9A-Fa-f_]+|\d[\d_]*(?:\.\d+)?(?:[eE][+-]?\d+)?[fFdDmMuUlL]*)\b)" +
        @"|(?<f>\b[A-Za-z_][A-Za-z0-9_]*(?=\s*(?:<[A-Za-z0-9_,\s<>?]*>)?\())" +
        @"|(?<t>\b[A-Z][A-Za-z0-9_]*\b)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex Razor = new(
        @"(?<c>@\*[\s\S]*?\*@|<!--[\s\S]*?-->|//[^\n]*|/\*[\s\S]*?\*/)" +
        @"|(?<s>""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)*')" +
        @"|(?<g></?[A-Za-z][A-Za-z0-9_.:-]*|/?>)" +
        @"|(?<k>@(?:[a-z][A-Za-z0-9_-]*)|\b(?:" + CSharpKeywords + @")\b)" +
        @"|(?<a>(?<=\s)[@A-Za-z][A-Za-z0-9_:.@-]*(?==))" +
        @"|(?<n>\b\d+(?:\.\d+)?(?:px|em|rem|ms|s|%)?\b)" +
        @"|(?<f>\b[A-Za-z_][A-Za-z0-9_]*(?=\())" +
        @"|(?<t>\b[A-Z][A-Za-z0-9_]*\b)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex Css = new(
        @"(?<c>/\*[\s\S]*?\*/|(?<=^|\s)//[^\n]*)" +
        @"|(?<s>""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)*')" +
        @"|(?<k>@[A-Za-z-]+|!important\b)" +
        @"|(?<v>--[A-Za-z0-9_-]+)" +
        @"|(?<p>(?<=(?:^|[{;])[ \t]*)[A-Za-z-]+(?=\s*:(?!:|[a-z-]+\s*[{,(]|[a-z-]+\s*$)))" +
        @"|(?<n>\#[0-9A-Fa-f]{3,8}\b|-?\b\d*\.?\d+(?:px|em|rem|ms|s|deg|turn|fr|vw|vh|%|x)?)" +
        @"|(?<f>\b[A-Za-z-]+(?=\())" +
        @"|(?<t>(?<![A-Za-z0-9_-])[.#:]{1,2}[A-Za-z_][A-Za-z0-9_-]*|\b[A-Z][A-Za-z0-9]*\b)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex Xml = new(
        @"(?<c><!--[\s\S]*?-->)" +
        @"|(?<s>""[^""]*""|'[^']*')" +
        @"|(?<g></?[A-Za-z_][A-Za-z0-9_.:-]*|/?>|<\?[A-Za-z]+|\?>)" +
        @"|(?<a>(?<=\s)[A-Za-z_][A-Za-z0-9_.:-]*(?==))",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex Json = new(
        @"(?<a>""(?:[^""\\\n]|\\.)*""(?=\s*:))" +
        @"|(?<s>""(?:[^""\\\n]|\\.)*"")" +
        @"|(?<k>\b(?:true|false|null)\b)" +
        @"|(?<n>-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b)",
        RegexOptions.CultureInvariant);

    private static readonly Regex Shell = new(
        @"(?<c>(?<=^|\s)\#[^\n]*)" +
        @"|(?<s>""(?:[^""\\\n`]|[\\`].)*""|'[^'\n]*')" +
        @"|(?<v>\$(?:env:)?[A-Za-z_][A-Za-z0-9_]*|\$\{[^}]+\}|\$\()" +
        @"|(?<f>(?<=^[ \t]*|[|;&(][ \t]*)[A-Za-z][A-Za-z0-9._-]*(?:-[A-Za-z]+)*)" +
        @"|(?<a>(?<=\s)--?[A-Za-z][A-Za-z0-9-]*)" +
        @"|(?<n>(?<=[\s=:@])\d+(?:\.\d+)*(?:-[a-z]+)?\b)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex Ini = new(
        @"(?<c>^[ \t]*[#;][^\n]*)" +
        @"|(?<g>^[ \t]*\[[^\]\n]+\])" +
        @"|(?<a>^[ \t]*[A-Za-z0-9_.-]+(?=\s*=))" +
        @"|(?<s>""[^""\n]*"")",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly string[] GroupNames = ["c", "s", "p", "a", "k", "n", "f", "t", "g", "v"];

    /// <summary>Returns the display label for a fenced-code language identifier.</summary>
    public static string Label(string? language) => Normalize(language) switch
    {
        "csharp" => "C#",
        "razor" => "Razor",
        "nazor" => "Nazor",
        "css" => "NSS",
        "nss" => "NSS",
        "powershell" => "PowerShell",
        "bash" => "Bash",
        "xml" => "XML",
        "json" => "JSON",
        "ini" => "INI",
        "mermaid" => "Diagram",
        "" => "Text",
        var other => other,
    };

    /// <summary>Normalizes the aliases used in fenced code blocks.</summary>
    public static string Normalize(string? language) => (language ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "c#" or "cs" or "csharp" => "csharp",
        "razor" or "cshtml" => "razor",
        "nazor" => "nazor",
        "css" => "css",
        "nss" => "nss",
        "ps" or "ps1" or "pwsh" or "powershell" => "powershell",
        "sh" or "shell" or "bash" or "console" => "bash",
        "xml" or "csproj" or "props" or "targets" or "html" or "svg" => "xml",
        "json" or "jsonc" => "json",
        "ini" or "toml" or "editorconfig" or "desktop" => "ini",
        "text" or "txt" or "plaintext" => string.Empty,
        var other => other,
    };

    /// <summary>Returns HTML-encoded code with token spans for the given language.</summary>
    public static string Highlight(string code, string? language)
    {
        var regex = Normalize(language) switch
        {
            "csharp" => CSharp,
            "razor" or "nazor" => Razor,
            "css" or "nss" => Css,
            "xml" => Xml,
            "json" => Json,
            "powershell" or "bash" => Shell,
            "ini" => Ini,
            _ => null,
        };

        if (regex is null)
        {
            return WebUtility.HtmlEncode(code);
        }

        var html = new StringBuilder(code.Length * 2);
        var position = 0;
        foreach (Match match in regex.Matches(code))
        {
            if (match.Length == 0)
            {
                continue;
            }

            var group = GroupNames.FirstOrDefault(name => match.Groups[name].Success);
            if (group is null)
            {
                continue;
            }

            html.Append(WebUtility.HtmlEncode(code[position..match.Index]));
            html.Append("<span class=\"tk-").Append(group).Append("\">");
            html.Append(WebUtility.HtmlEncode(match.Value));
            html.Append("</span>");
            position = match.Index + match.Length;
        }

        html.Append(WebUtility.HtmlEncode(code[position..]));
        return html.ToString();
    }
}
