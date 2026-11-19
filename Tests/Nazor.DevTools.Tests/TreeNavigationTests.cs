using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

public sealed class TreeNavigationTests
{
    [Fact]
    public void Framework_filter_promotes_user_instances_without_rewriting_the_captured_tree()
    {
        var tree = CapturedData.LinkedTree();
        DevToolsJson.Write(tree);
        var user = new TreeNavigation(tree);
        Assert.Equal(["child", "twin", "empty"], user.ComponentChildren("c"));
        Assert.False(user.Contains("component", "framework"));
        Assert.Equal(["c", "framework", "child"], user.Breadcrumbs("component", "child"));
        Assert.Equal(["framework", "twin", "empty"], tree.Components[0].Children);
        var all = new TreeNavigation(tree, showFramework: true);
        Assert.Equal(["framework", "twin", "empty"], all.ComponentChildren("c"));
        Assert.Equal(["child"], all.ComponentChildren("framework"));
        Assert.NotEqual(user.ComponentLabel("child"), user.ComponentLabel("twin"));
        Assert.Contains("no output", user.ComponentLabel("empty"));
        Assert.Contains("2 outputs", user.ComponentLabel("child"));
    }

    [Fact]
    public void Search_keeps_ancestors_and_matches_keys_instance_ids_attributes_and_physical_text()
    {
        var tree = CapturedData.LinkedTree();
        Assert.Equal(["c", "child"], new TreeNavigation(tree, "#child").ComponentIds.Order());
        Assert.Equal(["twin"], new TreeNavigation(tree, "other-card").ComponentChildren("c"));
        var attributes = new TreeNavigation(tree, "CAPTURED ATTRIBUTE");
        Assert.Equal(["e"], attributes.ElementRoots);
        Assert.Empty(attributes.ElementChildren("e"));
        var text = new TreeNavigation(tree, "Captured content");
        Assert.Equal(["text"], text.ElementChildren("e"));
        Assert.Equal(["e", "text"], text.Breadcrumbs("element", "text"));
        Assert.Empty(new TreeNavigation(tree, "no match").ComponentRoots);
    }
}
