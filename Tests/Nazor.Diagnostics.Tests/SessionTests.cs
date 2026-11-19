using Nazor.Controls;
using Nazor.Rendering;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Diagnostics.Tests;

public sealed class Toggler : ComponentBase
{
    public bool Show { get; set; } = true;

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, "Class", "box");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(content =>
        {
            if (Show)
            {
                content.OpenComponent<TextBlock>(0);
                content.AddComponentParameter(1, "Id", "note");
                content.AddComponentParameter(2, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Nota")));
                content.CloseComponent();
            }
            content.OpenElement(10, "Panel");
            content.AddAttribute(11, "style", "display: none");
            content.AddAttribute(12, "count", 3);
            content.AddAttribute(13, "ratio", 0.5);
            content.AddAttribute(14, "enabled", true);
            content.SetKey("panel");
            content.CloseElement();
        }));
        builder.CloseComponent();
    }
}

public sealed class SessionTests : IDisposable
{
    private readonly DiagnosticsHarness harness = new();

    public void Dispose() => harness.Dispose();

    private DevToolsSession Session => harness.Session;

    private NodeInfo Info(int id) => Session.Get(id) ?? throw new InvalidOperationException($"No node {id}.");

    [Fact]
    public void The_tree_of_the_counter_is_data_with_owners_scopes_and_events()
    {
        harness.Host.Render<Counter>();
        var root = Info(Assert.Single(Session.Roots));
        Assert.Equal((NodeKind.Element, "Container", "counter"), (root.Kind, root.Name, Assert.Single(root.Classes)));
        Assert.Equal(("Container", "Counter", (int?)null), (root.Owner, root.Scope, root.ParentId));

        var (text, button) = (Info(root.Children[0]), Info(root.Children[1]));
        Assert.Equal(("TextBlock", "Button"), (text.Name, button.Name));
        Assert.All(new[] { text, button }, child => Assert.Equal(root.Id, child.ParentId));
        Assert.Equal("Count: 0", string.Concat(text.Children.Select(id => Info(id)).Select(n => n.Kind == NodeKind.Text ? n.Text : "")));
        Assert.Equal("text", text.Attributes["role"]);
        Assert.Contains("__default_onclick", button.Events);
        Assert.DoesNotContain("onclick", button.Attributes.Keys);
        Assert.Same(harness.Host.Renderer.RootComponents.Single().Nodes[0], Session.ElementOf(root.Id));
    }

    [Fact]
    public void Ids_stay_while_the_nodes_live_and_changes_are_reported()
    {
        harness.Host.Render<Counter>();
        var root = Info(Session.Roots[0]);
        var textId = Info(root.Children[0]).Children.Last();
        var changes = new List<TreeChange>();
        Session.TreeChanged += changes.Add;

        Assert.True(harness.Input.Activate(Session.ElementOf(root.Children[1])!));
        harness.Host.RunPending();
        Assert.Equal(root.Children, Info(Session.Roots[0]).Children);
        Assert.Equal("1", Info(textId).Text);
        Assert.Contains(changes, change => change.Updated.Contains(textId));
    }

    [Fact]
    public void A_removed_node_loses_its_id_and_ids_are_never_reused()
    {
        var root = harness.Host.Render<Toggler>();
        var toggler = (Toggler)root.Component;
        var box = Info(Session.Roots[0]);
        var noteId = box.Children[0];
        Assert.Equal("note", Info(noteId).ElementId);
        var changes = new List<TreeChange>();
        Session.TreeChanged += changes.Add;

        toggler.Show = false;
        toggler.Refresh();
        harness.Host.RunPending();
        Assert.Null(Session.Get(noteId));
        Assert.Null(Session.NodeOf(noteId));
        Assert.Contains(changes, change => change.Removed.Contains(noteId) && change.ChildrenChanged.Contains(box.Id));

        toggler.Show = true;
        toggler.Refresh();
        harness.Host.RunPending();
        var again = Info(box.Id).Children[0];
        Assert.True(again > noteId);
        Assert.Contains(changes, change => change.Added.Contains(again));
    }

    [Fact]
    public void Attributes_are_text_and_hidden_elements_are_marked()
    {
        harness.Host.Render<Toggler>();
        var panel = Info(Info(Session.Roots[0]).Children[^1]);
        Assert.Equal(("Panel", "panel"), (panel.Name, panel.Key));
        Assert.Equal(("3", "0.5", "true", "display: none"), (panel.Attributes["count"], panel.Attributes["ratio"], panel.Attributes["enabled"], panel.Attributes["style"]));
        Assert.False(panel.IsRendered);
        Assert.True(Info(Session.Roots[0]).IsRendered);
        Assert.Throws<ArgumentException>(() => Session.IdOf(RemovedNode()));
    }

    [Fact]
    public void Types_are_named_as_code_writes_them() =>
        Assert.Equal("Dictionary<String, List<Int32>>", DevToolsSession.TypeName(typeof(Dictionary<string, List<int>>)));

    private Node RemovedNode()
    {
        var root = harness.Host.Render<Toggler>();
        var toggler = (Toggler)root.Component;
        var note = root.Nodes.OfType<ElementNode>().Single().Children.OfType<ElementNode>().First();
        toggler.Show = false;
        toggler.Refresh();
        harness.Host.RunPending();
        return note;
    }
}
