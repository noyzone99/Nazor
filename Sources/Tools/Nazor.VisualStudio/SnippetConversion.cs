#nullable disable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Nazor.VisualStudio
{
    internal static class SnippetConversion
    {
        private static readonly XNamespace Snippets = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";

        public static string ToVisualStudio(string snippet)
        {
            snippet = snippet.Replace("\r\n", "\n");
            var code = new StringBuilder();
            var literals = new Dictionary<string, string>();
            var ended = false;
            for (var i = 0; i < snippet.Length; i++)
            {
                var c = snippet[i];
                if (c == '\\' && i + 1 < snippet.Length && (snippet[i + 1] == '$' || snippet[i + 1] == '}' || snippet[i + 1] == '\\'))
                {
                    Text(code, snippet[++i]);
                    continue;
                }
                if (c == '$' && i + 1 < snippet.Length && char.IsDigit(snippet[i + 1]))
                {
                    var end = i + 1;
                    while (end < snippet.Length && char.IsDigit(snippet[end])) end++;
                    Field(snippet.Substring(i + 1, end - i - 1), null);
                    i = end - 1;
                    continue;
                }
                if (c == '$' && i + 2 < snippet.Length && snippet[i + 1] == '{' && char.IsDigit(snippet[i + 2]))
                {
                    var end = i + 2;
                    while (end < snippet.Length && char.IsDigit(snippet[end])) end++;
                    var number = snippet.Substring(i + 2, end - i - 2);
                    string placeholder = null;
                    if (end < snippet.Length && snippet[end] == ':')
                    {
                        var value = new StringBuilder();
                        for (end++; end < snippet.Length && snippet[end] != '}'; end++)
                            value.Append(snippet[end] == '\\' && end + 1 < snippet.Length ? snippet[++end] : snippet[end]);
                        placeholder = value.ToString();
                    }
                    Field(number, placeholder);
                    i = end;
                    continue;
                }
                Text(code, c);
            }
            if (!ended) code.Append("$end$");
            return new XDocument(new XElement(Snippets + "CodeSnippet", new XAttribute("Format", "1.0.0"),
                    new XElement(Snippets + "Header", new XElement(Snippets + "Title", "Nazor")),
                    new XElement(Snippets + "Snippet",
                        new XElement(Snippets + "Declarations", literals.Select(l =>
                            new XElement(Snippets + "Literal", new XElement(Snippets + "ID", l.Key), new XElement(Snippets + "Default", l.Value)))),
                        new XElement(Snippets + "Code", new XAttribute("Language", "CSharp"), new XCData(code.ToString()))))).ToString();

            void Field(string number, string placeholder)
            {
                if (number == "0")
                {
                    if (!ended) code.Append("$end$");
                    ended = true;
                    return;
                }
                var id = "field" + number;
                code.Append('$').Append(id).Append('$');
                if (!literals.TryGetValue(id, out var existing) || existing.Length == 0)
                    literals[id] = placeholder ?? (existing ?? number.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void Text(StringBuilder code, char c) => code.Append(c == '$' ? "$$" : c.ToString());
    }
}
