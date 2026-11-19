using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Examples.Actions;
using Nazor.Samples.Gallery.Examples.Feedback;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class GalleryVariantSampleTests : IDisposable
{
    private readonly ServiceProvider services = new ServiceCollection().AddSingleton<KeyBindings>().BuildServiceProvider();
    private readonly TestHost host;
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly Scene scene;
    private readonly InputManager input;
    private RootComponent root = null!;
    public GalleryVariantSampleTests()
    {
        host = new TestHost(services, captureExceptions: true);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, images: graphics.Images) { Viewport = new Size(1200, 1500) };
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        scene.Transitions.ReducedMotion = true;
        input = new InputManager(host.Renderer, scene);
    }
    public void Dispose() { input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); services.Dispose(); }
    private void Show<T>(string theme) where T : IComponent
    {
        scene.Styles.Theme = theme switch { "fluent" => Nazor.Themes.Fluent.Themes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic };
        root = host.Render<T>(); Frame();
    }
    private void Frame()
    {
        for (var frame = 0; frame < 6; frame++) { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        Assert.Empty(host.Exceptions);
    }
    private ElementNode ById(string id) => root.Elements().Single(node => node.Id == id);
    private void Activate(ElementNode node) { Assert.True(input.Activate(node)); Frame(); }
    private void Press(string key) { input.KeyDown(key, KeyModifiers.None, false); input.KeyUp(key, KeyModifiers.None); Frame(); }
    private void Choose(string id, string label)
    {
        Activate(ById(id));
        Activate(root.Elements("SelectOption").Single(node => node.Text() == label));
    }
    private void Overflow() => Activate(ById("sample-command-toolbar").Elements().Single(node => node.Name == "MenuButton"));
    private ElementNode MenuItem(string name) => root.Elements().Single(node => node.Name is "MenuItem" or "MenuCheckItem" && node.Elements().Any(part => part.Name == "MenuItemLabel" && part.Text() == name));

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Toggle_split_keeps_its_checked_state_when_a_different_menu_option_is_chosen(string theme)
    {
        Show<MenuButtonsSample>(theme);
        var split = ById("sample-toggle-split"); Activate(split);
        Assert.True(split.Attributes.GetValueOrDefault("pressed") is true);
        input.Focus(split, visible: true); input.KeyDown("F4", KeyModifiers.None, false); input.KeyUp("F4", KeyModifiers.None); Frame();
        Activate(MenuItem("Green"));
        Assert.Contains("Highlight: on; color: Green", ById("sample-highlight-status").Text());
        Assert.True(split.Attributes.GetValueOrDefault("pressed") is true);
        Activate(split);
        Assert.Contains("Highlight: off; color: Green", ById("sample-highlight-status").Text());
        Assert.False(input.Activate(root.Elements("SplitButton").Single(node => node.IsDisabled)));
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Priority_command_overflow_keeps_toggle_and_disabled_state_when_resized_back_into_the_bar(string theme)
    {
        Show<ToolbarSample>(theme);
        Assert.Contains(ById("sample-command-toolbar").Elements(), node => node.Name == "Button" && node.Attributes.GetValueOrDefault("accessible-name") is "Save");
        Overflow();
        Assert.False(input.Activate(MenuItem("Delete")));
        Activate(MenuItem("Wrap"));
        Assert.Equal("Wrap: on", ById("sample-toolbar-result").Text());
        Press("Escape");
        Activate(ById("sample-toolbar-delete-enabled"));
        Overflow(); Activate(MenuItem("Delete"));
        Assert.Equal("Executed Delete", ById("sample-toolbar-result").Text());
        Press("Escape");
        input.Focus(ById("sample-toolbar-width"), visible: true); Press("End");
        var wrap = ById("sample-command-toolbar").Elements().Single(node => node.Name == "ToggleButton");
        Assert.True(wrap.Attributes.GetValueOrDefault("pressed") is true);
        Activate(wrap);
        Assert.Equal("Wrap: off", ById("sample-toolbar-result").Text());
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Avatar_presence_and_badge_zero_dot_and_cap_modes_are_interactive(string theme)
    {
        Show<AvatarsBadgesSample>(theme);
        Choose("sample-avatar-presence", "Offline");
        Assert.Equal("offline", ById("sample-avatar-live").Attributes["presence"]);
        var badge = ById("sample-configurable-badge");
        Assert.Equal("99+", badge.Elements().Single(node => node.Name == "BadgeIndicator").Text());
        Activate(ById("sample-badge-clear"));
        Assert.DoesNotContain(badge.Elements(), node => node.Name == "BadgeIndicator");
        Activate(ById("sample-badge-show-zero"));
        Assert.Equal("0", badge.Elements().Single(node => node.Name == "BadgeIndicator").Text());
        Activate(ById("sample-badge-dot"));
        Assert.True(badge.Elements().Single(node => node.Name == "BadgeIndicator").Attributes.GetValueOrDefault("dot") is true);
        Assert.Contains(root.Elements("Avatar"), node => node.Elements().Any(part => part.Name == "AvatarFallback"));
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Message_variants_publish_live_feedback_and_reopen_after_dismissal(string theme)
    {
        Show<MessagesSample>(theme);
        Choose("sample-message-severity", "Error"); Choose("sample-message-layout", "Expanded");
        var message = ById("sample-configurable-message");
        Assert.Equal("assertive", message.Attributes["live"]); Assert.Equal("expanded", message.Attributes["layout"]);
        Activate(ById("sample-message-publish"));
        Assert.Equal("success", message.Attributes["severity"]); Assert.Equal("polite", message.Attributes["live"]);
        Assert.Contains("Changes published.", message.Text());
        Activate(message.Elements().Single(node => node.Name == "InlineMessageClose"));
        Assert.DoesNotContain(root.Elements(), node => node.Id == "sample-configurable-message");
        Activate(root.Elements("Button").Single(node => node.Text() == "Reset messages"));
        Assert.Contains("Changes have not been published.", ById("sample-configurable-message").Text());
        Activate(ById("sample-message-closable"));
        Assert.DoesNotContain(ById("sample-configurable-message").Elements(), node => node.Name == "InlineMessageClose");
    }
}
