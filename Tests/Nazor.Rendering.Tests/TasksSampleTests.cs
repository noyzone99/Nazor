using Microsoft.Extensions.DependencyInjection;
using Nazor.Input;
using Nazor.Samples.Tasks.Services;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class TasksSampleTests
{
    [Fact]
    public async Task Component_styles_work_without_an_application_sheet_and_tasks_survive_navigation()
    {
        using var services = new ServiceCollection()
            .AddSingleton<ITaskStore, InMemoryTaskStore>()
            .AddSingleton<KeyBindings>()
            .BuildServiceProvider();
        using var harness = new RenderHarness(services);
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Nazor.Controls.StyleSheets.Default;
        var root = harness.Host.Render<Nazor.Samples.Tasks.App>();
        var image = harness.Render(1080, 760, 1);
        Assert.Null(harness.Scene.Styles.Application);
        Assert.Equal("Nazor — Tasks", root.Single("Window").Attributes["title"]);
        var heading = root.Elements("TextBlock").Single(e => e.Class == "title");
        Assert.Equal(28, harness.Scene.GetStyle(heading).FontSize);
        var card = root.Elements("Container").First(e => e.Class == "task-card row");
        Assert.Equal(Length.Px(12), harness.Scene.GetStyle(card).Get(StyleProperties.PaddingLeft));
        var bounds = harness.Scene.GetBounds(card);
        var check = harness.Scene.GetBounds(card.Elements().Single(e => e.Name == "CheckBox"));
        var remove = harness.Scene.GetBounds(card.Elements().Single(e => e.Name == "Button" && e.Text() == "Delete"));
        Assert.True(remove.X > check.Right);
        Assert.True(harness.Scene.GetBounds(root.Elements("Container").Single(e => e.Class == "item-list scroll-fill")).Height > bounds.Height);
        ImageAssert.MatchesBaseline(harness.Graphics, image, "tasks-1080x760@1x");

        var store = services.GetRequiredService<ITaskStore>();
        store.Add("Task to keep");
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "About"));
        ImageAssert.MatchesBaseline(harness.Graphics, harness.Render(1080, 760, 1), "tasks-about-1080x760@1x");
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "Tasks"));
        harness.Render(1080, 760, 1);
        Assert.Contains("Task to keep", root.Text());
    }
}
