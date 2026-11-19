namespace Nazor.Controls.Tests.Data;

internal static class DataTestSupport
{
    // Writes a rendering next to the test binaries, for visual checks of the Basic theme.
    public static void Snapshot(this ControlHarness harness, string name)
    {
        var folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "snapshots"));
        File.WriteAllBytes(Path.Combine(folder.FullName, name + ".png"), harness.Graphics.EncodePng(harness.Image()));
    }

    public static ElementNode Cell(this ControlHarness harness, int row, int column) =>
        harness.Root.Elements().Single(e => e.Attributes.GetValueOrDefault("row-index") is int r && r == row + 2
            && e.Attributes.GetValueOrDefault("column-index") is int c && c == column + 1 && e.Attributes.GetValueOrDefault("focusable") is true);

    public static string Text(ElementNode element) => string.Concat(Descend(element).OfType<TextNode>().Select(t => t.Text));

    private static IEnumerable<Node> Descend(Node node)
    {
        yield return node;
        if (node is ElementNode element)
            foreach (var child in element.Children)
                foreach (var nested in Descend(child)) yield return nested;
    }
}
