using System.Net;
using System.Text;
using TextMateSharp.Grammars;

namespace Nazor.Tooling.Tests;

public sealed class GrammarPreviewTests
{
    private static readonly string RepositoryRoot = Nazor.Testing.RepositoryPaths.Root;

    [Fact]
    public void Preview_of_the_highlighting_is_written()
    {
        string[] files = ["Samples/Nazor.Samples.Counter/Counter.nazor", "Tests/Fixtures/Projects/DeviceRegistry/App.nazor", "Tests/Fixtures/Projects/DeviceRegistry/Components/DeviceCard.nazor.nss"];
        var html = new StringBuilder("""
            <!doctype html><html><head><meta charset="utf-8"><title>Nazor grammars</title>
            <style>body{font-family:Segoe UI,sans-serif;margin:16px} .pair{display:flex;gap:16px;flex-wrap:wrap}
            pre{font:13px/1.45 Cascadia Mono,Consolas,monospace;padding:12px;border-radius:6px;overflow:auto;flex:1;min-width:420px}</style></head><body>
            <h1>Nazor grammars</h1>
            """);
        foreach (var file in files)
        {
            var source = File.ReadAllText(Path.Combine(RepositoryRoot, file));
            var scope = file.EndsWith(".nss", StringComparison.Ordinal) ? "source.nss" : "text.nazor";
            html.Append("<h2>").Append(WebUtility.HtmlEncode(file)).Append("</h2><div class=\"pair\">");
            foreach (var (theme, background, text) in new[] { (ThemeName.VisualStudioLight, "#ffffff", "#000000"), (ThemeName.VisualStudioDark, "#1e1e1e", "#d4d4d4") })
            {
                html.Append("<pre style=\"background:").Append(background).Append(";color:").Append(text).Append("\">");
                foreach (var line in new GrammarHarness(theme).Colorize(scope, source))
                {
                    foreach (var (run, color, bold, italic) in line)
                    {
                        html.Append("<span style=\"color:").Append(color ?? "inherit");
                        if (bold) html.Append(";font-weight:bold");
                        if (italic) html.Append(";font-style:italic");
                        html.Append("\">").Append(WebUtility.HtmlEncode(run)).Append("</span>");
                    }
                    html.Append('\n');
                }
                html.Append("</pre>");
            }
            html.Append("</div>");
        }
        html.Append("</body></html>");
        var output = Path.Combine(RepositoryRoot, "artifacts", "grammar-preview");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "index.html"), html.ToString());
        Assert.Contains("<span", html.ToString());
    }
}
