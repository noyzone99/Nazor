namespace Nazor.Controls.Tests.TextLayout;

internal static class Nodes
{
    public static string Text(ElementNode element)
    {
        var text = new System.Text.StringBuilder();
        Append(element, text);
        return text.ToString().Trim();
    }

    public static ElementNode Part(ElementNode element, string name) =>
        Descendants(element).First(e => e.Name == name);

    public static IEnumerable<ElementNode> Descendants(ElementNode element)
    {
        foreach (var child in element.Children.OfType<ElementNode>())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void Append(Node node, System.Text.StringBuilder text)
    {
        if (node is TextNode leaf) text.Append(leaf.Text);
        else if (node is ElementNode element)
            foreach (var child in element.Children) Append(child, text);
    }
}
