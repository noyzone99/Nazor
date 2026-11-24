using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Nazor.Website.Shared;

namespace Nazor.Website.Generator;

/// <summary>The HTML fragment and metadata produced from one Markdown page.</summary>
internal sealed record ConvertedPage(string Title, string Summary, string Html, List<TocEntry> Toc);

/// <summary>
/// Converts documentation Markdown into website HTML: highlighted code, wrapped tables,
/// heading anchors, and links rewritten to website routes or to the GitHub repository.
/// </summary>
internal sealed partial class MarkdownConverter(string repository, string output, string repositoryUrl, IReadOnlyDictionary<string, string> routes)
{
    private static readonly string[] ImageExtensions = [".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico"];

    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UsePipeTables()
        .UseGridTables()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseFootnotes()
        .UseAutoLinks()
        .UseListExtras()
        .Build();

    public ConvertedPage Convert(string markdown, string source, string route)
    {
        var document = Markdown.Parse(markdown, pipeline);

        // The page title is shown in the page header, so the first H1 is removed from the body.
        var title = Path.GetFileNameWithoutExtension(source);
        if (document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1) is { } h1)
        {
            title = PlainText(h1.Inline);
            h1.Parent?.Remove(h1);
        }

        var summary = document.OfType<ParagraphBlock>().Select(p => PlainText(p.Inline)).FirstOrDefault(text => text.Length > 0) ?? string.Empty;
        summary = Shorten(summary, 220);

        var toc = document.Descendants<HeadingBlock>()
            .Where(h => h.Level is 2 or 3 && h.GetAttributes().Id is not null)
            .Select(h => new TocEntry { Id = h.GetAttributes().Id!, Text = PlainText(h.Inline), Level = h.Level })
            .ToList();

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new HighlightedCodeRenderer());
        renderer.Render(document);
        writer.Flush();

        var html = writer.ToString();
        html = RewriteUrls(html, source, route);
        html = ExternalLinkPattern().Replace(html, "<a href=\"$1\" target=\"_blank\" rel=\"noopener\"");
        html = TableOpenPattern().Replace(html, "<div class=\"table-wrap\"><table>");
        html = html.Replace("</table>", "</table></div>", StringComparison.Ordinal);
        html = HeadingPattern().Replace(html, match =>
        {
            var level = match.Groups[1].Value;
            var id = match.Groups[2].Value;
            return $"<h{level} id=\"{id}\"><a class=\"hash\" href=\"{route}#{id}\" aria-hidden=\"true\" tabindex=\"-1\"></a>";
        });

        return new ConvertedPage(title, summary, html, toc);
    }

    private string RewriteUrls(string html, string source, string route) =>
        UrlAttributePattern().Replace(html, match =>
        {
            var attribute = match.Groups[1].Value;
            var url = WebUtility.HtmlDecode(match.Groups[2].Value);
            var rewritten = ResolveUrl(url, source, route, attribute == "src");
            return $" {attribute}=\"{WebUtility.HtmlEncode(rewritten)}\"";
        });

    private string ResolveUrl(string url, string source, string route, bool isImage)
    {
        if (url.Length == 0 || url.Contains("://", StringComparison.Ordinal) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.StartsWith('#'))
        {
            return route + url;
        }

        var hashIndex = url.IndexOf('#', StringComparison.Ordinal);
        var path = hashIndex >= 0 ? url[..hashIndex] : url;
        var fragment = hashIndex >= 0 ? url[hashIndex..] : string.Empty;
        path = Uri.UnescapeDataString(path);

        var directory = Path.GetDirectoryName(source)?.Replace('\\', '/') ?? string.Empty;
        var resolved = Normalize(directory.Length > 0 ? $"{directory}/{path}" : path);

        if (routes.TryGetValue(resolved, out var target))
        {
            return target + fragment;
        }

        var trimmed = resolved.TrimEnd('/');
        if (trimmed.Equals("Docs/ApiReferences", StringComparison.OrdinalIgnoreCase))
        {
            return "api";
        }

        if (trimmed.Equals("Docs/Guide", StringComparison.OrdinalIgnoreCase))
        {
            return "guide";
        }

        var fullPath = Path.Combine(repository, trimmed);
        if (File.Exists(fullPath) && (isImage || ImageExtensions.Contains(Path.GetExtension(fullPath), StringComparer.OrdinalIgnoreCase)))
        {
            var media = Path.Combine(output, "media", trimmed);
            Directory.CreateDirectory(Path.GetDirectoryName(media)!);
            File.Copy(fullPath, media, overwrite: true);
            return $"content/media/{trimmed}";
        }

        if (trimmed.Length == 0)
        {
            return repositoryUrl + fragment;
        }

        var kind = Directory.Exists(fullPath) ? "tree" : "blob";
        return $"{repositoryUrl}/{kind}/main/{Uri.EscapeDataString(trimmed).Replace("%2F", "/", StringComparison.Ordinal)}{fragment}";
    }

    private static string Normalize(string path)
    {
        var parts = new List<string>();
        foreach (var part in path.Replace('\\', '/').Split('/'))
        {
            if (part is "" or ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                }

                continue;
            }

            parts.Add(part);
        }

        return string.Join('/', parts);
    }

    private static string PlainText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        Append(inline);
        return Regex.Replace(text.ToString(), @"\s+", " ").Trim();

        void Append(Inline node)
        {
            switch (node)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case HtmlEntityInline entity:
                    text.Append(entity.Transcoded.ToString());
                    break;
                case LineBreakInline:
                    text.Append(' ');
                    break;
                case ContainerInline container:
                    foreach (var child in container)
                    {
                        Append(child);
                    }

                    break;
            }
        }
    }

    private static string Shorten(string text, int length)
    {
        if (text.Length <= length)
        {
            return text;
        }

        var sentence = text.LastIndexOf(". ", length, StringComparison.Ordinal);
        if (sentence > length / 2)
        {
            return text[..(sentence + 1)];
        }

        var space = text.LastIndexOf(' ', length);
        return text[..(space > 0 ? space : length)].TrimEnd(',', ';', ':') + "…";
    }

    [GeneratedRegex("\\s(href|src)=\"([^\"]*)\"")]
    private static partial Regex UrlAttributePattern();

    [GeneratedRegex("<a href=\"(https?://[^\"]*)\"")]
    private static partial Regex ExternalLinkPattern();

    [GeneratedRegex("<table>")]
    private static partial Regex TableOpenPattern();

    [GeneratedRegex("<h([2-4]) id=\"([^\"]+)\">")]
    private static partial Regex HeadingPattern();

    /// <summary>Renders fenced code as a framed, highlighted block with a language label and a copy button.</summary>
    private sealed class HighlightedCodeRenderer : HtmlObjectRenderer<CodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock block)
        {
            var language = (block as FencedCodeBlock)?.Info;
            var code = block.Lines.ToString().TrimEnd('\n', '\r');
            renderer.EnsureLine();

            if (string.Equals(language, "mermaid", StringComparison.OrdinalIgnoreCase))
            {
                renderer.Write("<div class=\"diagram\"><pre class=\"mermaid\">").Write(WebUtility.HtmlEncode(code)).WriteLine("</pre></div>");
                return;
            }

            var normalized = CodeHighlighter.Normalize(language);
            renderer.Write("<div class=\"code\" data-lang=\"").Write(normalized).Write("\">");
            renderer.Write("<div class=\"code-head\"><span class=\"code-lang\">").Write(WebUtility.HtmlEncode(CodeHighlighter.Label(language))).Write("</span>");
            renderer.Write("<button type=\"button\" class=\"code-copy\" aria-label=\"Copy code\">Copy</button></div>");
            renderer.Write("<pre><code>").Write(CodeHighlighter.Highlight(code, language)).WriteLine("</code></pre></div>");
        }
    }
}
