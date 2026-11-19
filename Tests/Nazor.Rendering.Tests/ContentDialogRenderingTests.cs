using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class ContentDialogRenderingPage : ComponentBase
{
    [Parameter] public DialogButtonRole? DefaultButton { get; set; }
    [Parameter] public bool InitiallyOpen { get; set; } = true;
    [Parameter] public bool HasEditor { get; set; } = true;
    [Parameter] public bool Multiline { get; set; }
    [Parameter] public bool HandlesEnter { get; set; }
    [Parameter] public bool DisabledDefault { get; set; }
    [Parameter] public bool? PrimaryDefault { get; set; }
    [Parameter] public bool? SecondaryDefault { get; set; }
    [Parameter] public bool SingleAction { get; set; }
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Auto;
    [Parameter] public DialogPlacement Placement { get; set; }
    [Parameter] public float ContentWidth { get; set; }
    [Parameter] public float ContentHeight { get; set; }
    public bool Open { get; private set; }
    public Dialog Dialog { get; private set; } = null!;
    public DialogButton Secondary { get; private set; } = null!;
    public List<DialogButtonRole> Actions { get; } = [];
    public void Refresh() => StateHasChanged();
    protected override void OnInitialized() => Open = InitiallyOpen;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Backdrop");
        builder.AddAttribute(1, "style", "width: 100%; height: 100%; background: #d9bd9e;");
        builder.OpenComponent<Button>(10);
        builder.AddComponentParameter(11, nameof(Button.Id), "outside");
        builder.AddComponentParameter(12, nameof(Button.ChildContent), (RenderFragment)(label => label.AddContent(0, "Outside")));
        builder.CloseComponent();
        builder.OpenComponent<Dialog>(20);
        builder.AddComponentParameter(21, nameof(Dialog.IsOpen), Open);
        builder.AddComponentParameter(22, nameof(Dialog.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => Open = value));
        builder.AddComponentParameter(23, nameof(Dialog.Title), "Save report");
        builder.AddComponentParameter(24, nameof(Dialog.DefaultButton), DefaultButton);
        builder.AddComponentParameter(25, nameof(Dialog.Size), Size);
        builder.AddComponentParameter(26, nameof(Dialog.Placement), Placement);
        builder.AddComponentParameter(27, nameof(Dialog.OnAction), EventCallback.Factory.Create<DialogButtonRole>(this, role => Actions.Add(role)));
        builder.AddComponentParameter(28, nameof(Dialog.ChildContent), (RenderFragment)(body =>
        {
            if (HasEditor)
            {
                body.OpenComponent(0, Multiline ? typeof(TextArea) : typeof(TextField));
                body.AddComponentParameter(1, nameof(TextField.Id), "editor");
                if (HandlesEnter) body.AddAttribute(2, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => e.Handled = e.Key == "Enter"));
                body.CloseComponent();
            }
            if (ContentWidth > 0 || ContentHeight > 0)
            {
                body.OpenElement(10, "MeasuredContent");
                body.AddAttribute(11, "style", $"width: {ContentWidth}px; height: {ContentHeight}px; flex-shrink: 0; background: #d622ad;");
                body.CloseElement();
            }
        }));
        builder.AddComponentParameter(29, nameof(Dialog.Footer), (RenderFragment)(footer =>
        {
            if (!SingleAction)
            {
                Action(footer, 0, DialogButtonRole.Cancel, "Cancel", null);
                Action(footer, 20, DialogButtonRole.Secondary, "Discard", SecondaryDefault);
            }
            Action(footer, 40, DialogButtonRole.Primary, "Save", PrimaryDefault);
        }));
        builder.AddComponentReferenceCapture(30, value => Dialog = (Dialog)value);
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void Action(RenderTreeBuilder builder, int sequence, DialogButtonRole role, string label, bool? isDefault)
    {
        builder.OpenComponent<DialogButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(DialogButton.Role), role);
        builder.AddComponentParameter(sequence + 2, nameof(DialogButton.IsDefault), isDefault);
        builder.AddComponentParameter(sequence + 3, nameof(DialogButton.Disabled), DisabledDefault && DefaultButton == role);
        builder.AddComponentParameter(sequence + 4, nameof(DialogButton.ChildContent), (RenderFragment)(text => text.AddContent(0, label)));
        if (role == DialogButtonRole.Secondary) builder.AddComponentReferenceCapture(sequence + 5, value => Secondary = (DialogButton)value);
        builder.CloseComponent();
    }
}

public sealed class ContentDialogRenderingTests
{
    public static IEnumerable<object?[]> ThemesAndDefaultActions()
    {
        foreach (var theme in new[] { "basic", "fluent", "leaf" })
        foreach (var action in new DialogButtonRole?[] { null, DialogButtonRole.Primary, DialogButtonRole.Secondary, DialogButtonRole.Cancel })
            yield return [theme, action];
    }

    public static IEnumerable<object[]> ThemesAndScales()
    {
        foreach (var theme in new[] { "basic", "fluent", "leaf" })
        foreach (var scale in new[] { 1f, 1.5f, 2f })
            yield return [theme, scale];
    }

    [Theory]
    [MemberData(nameof(ThemesAndDefaultActions))]
    public void An_explicit_default_selects_the_enter_action_and_accent_without_changing_body_focus(string theme, DialogButtonRole? action)
    {
        using var context = new Context(theme, parameters: new() { [nameof(ContentDialogRenderingPage.DefaultButton)] = action });
        Assert.Same(context.Root.Single("TextFieldInput"), context.Input.Focused);
        foreach (var role in new[] { DialogButtonRole.Primary, DialogButtonRole.Secondary, DialogButtonRole.Cancel })
        {
            var button = context.Button(role);
            Assert.Equal(action == role, button.Attributes.GetValueOrDefault("default") is true);
            Assert.Equal(action == role ? "accent" : "standard", button.Attributes["appearance"]);
        }
        context.Enter();
        Assert.Equal(action is null, context.Page.Open);
        Assert.Equal(action.HasValue ? new[] { action.Value } : Array.Empty<DialogButtonRole>(), context.Page.Actions);
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Focused_actions_work_with_no_default_and_live_policy_changes_update_existing_actions(string theme)
    {
        using var context = new Context(theme);
        var primary = context.Button(DialogButtonRole.Primary);
        context.Page.DefaultButton = DialogButtonRole.Primary;
        context.Page.Refresh();
        context.Frame();
        Assert.Same(primary, context.Button(DialogButtonRole.Primary));
        Assert.Equal(true, primary.Attributes["default"]);
        context.Page.DefaultButton = DialogButtonRole.Secondary;
        context.Page.Refresh();
        context.Frame();
        Assert.False(primary.Attributes.GetValueOrDefault("default") is true);
        Assert.Equal(true, context.Button(DialogButtonRole.Secondary).Attributes["default"]);
        context.Page.DefaultButton = null;
        context.Page.Refresh();
        context.Frame();
        context.Input.Focus(primary, visible: true);
        context.Enter();
        Assert.Equal([DialogButtonRole.Primary], context.Page.Actions);
        Assert.False(context.Page.Open);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Content_that_uses_enter_prevents_the_default_action(bool multiline)
    {
        using var context = new Context("fluent", parameters: new()
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Primary,
            [nameof(ContentDialogRenderingPage.Multiline)] = multiline,
            [nameof(ContentDialogRenderingPage.HandlesEnter)] = !multiline,
        });
        context.Enter();
        Assert.True(context.Page.Open);
        Assert.Empty(context.Page.Actions);
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Without_body_controls_initial_focus_prefers_the_enabled_default_then_the_first_action(string theme)
    {
        using var selected = new Context(theme, parameters: new()
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Secondary,
            [nameof(ContentDialogRenderingPage.HasEditor)] = false,
        });
        Assert.Same(selected.Button(DialogButtonRole.Secondary), selected.Input.Focused);
        using var disabled = new Context(theme, parameters: new()
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Secondary,
            [nameof(ContentDialogRenderingPage.HasEditor)] = false,
            [nameof(ContentDialogRenderingPage.DisabledDefault)] = true,
        });
        Assert.Same(disabled.Button(DialogButtonRole.Cancel), disabled.Input.Focused);
    }

    [Fact]
    public void Disabled_default_actions_do_not_handle_enter_in_body_editors()
    {
        using var context = new Context("fluent", parameters: new()
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Primary,
            [nameof(ContentDialogRenderingPage.DisabledDefault)] = true,
        });
        context.Enter();
        Assert.True(context.Page.Open);
        Assert.Empty(context.Page.Actions);
    }

    [Fact]
    public void Explicit_button_defaults_override_their_own_policy()
    {
        using var context = new Context("fluent", parameters: new()
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Primary,
            [nameof(ContentDialogRenderingPage.PrimaryDefault)] = false,
            [nameof(ContentDialogRenderingPage.SecondaryDefault)] = true,
        });
        Assert.False(context.Button(DialogButtonRole.Primary).Attributes.GetValueOrDefault("default") is true);
        Assert.True(context.Button(DialogButtonRole.Secondary).Attributes.GetValueOrDefault("default") is true);
        context.Enter();
        Assert.Equal([DialogButtonRole.Secondary], context.Page.Actions);
    }

    [Fact]
    public void Ambiguous_default_actions_are_rejected_after_actions_register()
    {
        using var harness = new RenderHarness();
        Assert.Throws<InvalidOperationException>(() => harness.Host.Render<ContentDialogRenderingPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Primary,
            [nameof(ContentDialogRenderingPage.SecondaryDefault)] = true,
        })));
    }

    [Fact]
    public void Invalid_default_roles_are_rejected()
    {
        using var harness = new RenderHarness();
        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Host.Render<ContentDialogRenderingPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(ContentDialogRenderingPage.DefaultButton)] = (DialogButtonRole)999,
        })));
    }

    [Fact]
    public async Task Independently_updated_actions_cannot_introduce_an_ambiguous_default()
    {
        using var context = new Context("fluent", parameters: new() { [nameof(ContentDialogRenderingPage.DefaultButton)] = DialogButtonRole.Primary });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Page.Secondary.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(DialogButton.IsDefault)] = true,
        })));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void A_single_Fluent_auto_action_occupies_the_trailing_half_column(float scale)
    {
        using var context = new Context("fluent", scale, parameters: new()
        {
            [nameof(ContentDialogRenderingPage.SingleAction)] = true,
            [nameof(ContentDialogRenderingPage.HasEditor)] = false,
        });
        var area = context.Harness.Scene.GetContentBounds(context.Root.Single("DialogFooter"));
        var button = context.Harness.Scene.GetBounds(context.Button(DialogButtonRole.Primary));
        Assert.Equal((area.Width - 8) / 2, button.Width, 1 / scale);
        Assert.Equal(area.Right, button.Right, 1 / scale);
    }

    [Theory]
    [MemberData(nameof(ThemesAndScales))]
    public void Auto_uses_theme_sizing_and_explicit_sizes_remain_available(string theme, float scale)
    {
        using var context = new Context(theme, scale, parameters: new() { [nameof(ContentDialogRenderingPage.HasEditor)] = false });
        var bounds = context.Harness.Scene.GetBounds(context.Surface);
        Assert.Equal("auto", context.Surface.Attributes["size"]);
        Assert.InRange(bounds.Width, (theme == "fluent" ? 320 : theme == "leaf" ? 470 : 480) - 1 / scale, (theme == "fluent" ? 320 : theme == "leaf" ? 470 : 480) + 1 / scale);
        if (theme == "fluent")
        {
            Assert.InRange(bounds.Height, 184 - 1 / scale, 184 + 1 / scale);
            var actions = new[] { DialogButtonRole.Primary, DialogButtonRole.Secondary, DialogButtonRole.Cancel }.Select(role => context.Harness.Scene.GetBounds(context.Button(role))).ToArray();
            Assert.True(actions[0].X < actions[1].X && actions[1].X < actions[2].X);
            Assert.InRange(actions.Max(rect => rect.Width) - actions.Min(rect => rect.Width), 0, 1 / scale);
            Assert.Equal(8f, actions[1].X - actions[0].Right, 1 / scale);
        }
        foreach (var (size, width) in new[] { (DialogSize.Small, theme == "leaf" ? 360f : 320f), (DialogSize.Medium, theme == "leaf" ? 470f : 480f), (DialogSize.Large, 720f) })
        {
            context.Page.Size = size;
            context.Page.Refresh();
            context.Frame();
            Assert.InRange(context.Harness.Scene.GetBounds(context.Surface).Width, width - 1 / scale, width + 1 / scale);
        }
        context.Page.Size = DialogSize.FullScreen;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(new Rect(0, 0, 800, 600), context.Harness.Scene.GetBounds(context.Surface));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Fluent_auto_measures_content_clamps_to_work_area_and_scrolls_only_the_body(float scale)
    {
        using var context = new Context("fluent", scale, parameters: new()
        {
            [nameof(ContentDialogRenderingPage.HasEditor)] = false,
            [nameof(ContentDialogRenderingPage.ContentWidth)] = 420f,
            [nameof(ContentDialogRenderingPage.ContentHeight)] = 40f,
        });
        var sized = context.Harness.Scene.GetBounds(context.Surface);
        Assert.InRange(sized.Width, 468, 472);
        context.Page.ContentWidth = 1000;
        context.Page.ContentHeight = 1400;
        context.Page.Refresh();
        context.Frame();
        var clamped = context.Harness.Scene.GetBounds(context.Surface);
        Assert.InRange(clamped.Width, 548 - 1 / scale, 548 + 1 / scale);
        Assert.InRange(clamped.Height, 552 - 1 / scale, 552 + 1 / scale);
        var header = context.Harness.Scene.GetBounds(context.Root.Single("DialogHeader"));
        var footer = context.Harness.Scene.GetBounds(context.Root.Single("DialogFooter"));
        var scroll = context.Harness.Scene.GetScrollState(context.Root.Single("DialogBody"))!;
        Assert.True(scroll.CanScrollY);
        var body = context.Harness.Scene.GetBounds(context.Root.Single("DialogBody"));
        context.Input.PointerMoved(new Point(body.X + body.Width / 2, body.Y + body.Height / 2), KeyModifiers.None);
        context.Input.PointerWheel(new Point(body.X + body.Width / 2, body.Y + body.Height / 2), 0, -1, KeyModifiers.None);
        context.Frame();
        Assert.True(scroll.OffsetY > 0);
        Assert.Equal(header, context.Harness.Scene.GetBounds(context.Root.Single("DialogHeader")));
        Assert.Equal(footer, context.Harness.Scene.GetBounds(context.Root.Single("DialogFooter")));
        context.Width = 300;
        context.Height = 360;
        context.Frame();
        var narrow = context.Harness.Scene.GetBounds(context.Surface);
        Assert.InRange(narrow.X, 24 - 1 / scale, 24 + 1 / scale);
        Assert.InRange(narrow.Right, 276 - 1 / scale, 276 + 1 / scale);
        Assert.InRange(narrow.Bottom, 336 - 1 / scale, 336 + 1 / scale);
        foreach (var role in new[] { DialogButtonRole.Primary, DialogButtonRole.Secondary, DialogButtonRole.Cancel })
        {
            var action = context.Harness.Scene.GetBounds(context.Button(role));
            Assert.True(action.X >= narrow.X && action.Right <= narrow.Right + 1 / scale);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public async Task Fluent_entry_exit_and_reopen_use_separate_scale_and_opacity_timelines(float scale)
    {
        var clock = new ManualAnimationClock();
        using var context = new Context("fluent", scale, reducedMotion: false, clock: clock, parameters: new() { [nameof(ContentDialogRenderingPage.InitiallyOpen)] = false });
        var outside = context.Root.Elements("Button").Single(element => element.Id == "outside");
        context.Input.Focus(outside, visible: true);
        await context.Page.Dialog.OpenAsync();
        var initial = context.Frame();
        var surface = context.Surface;
        Assert.Equal(0d, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity));
        Assert.Equal(1.05, Assert.IsType<ScaleFunction>(context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Transform).Items.Single()).X, 0.001);
        clock.Advance(41.5);
        var halfOpacity = context.Frame();
        Assert.Equal(0.5, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity), 0.001);
        var inScale = Assert.IsType<ScaleFunction>(context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Transform).Items.Single()).X;
        Assert.InRange(inScale, 1.0001, 1.0499);
        Assert.Contains(context.Harness.Scene.BuildDisplayList().Commands.OfType<PushTransform>(), transform => Math.Abs(transform.Matrix.M11 - inScale) < 0.001);
        var point = new Point(10, 200);
        Assert.NotEqual(Pixel(initial, point, scale), Pixel(halfOpacity, point, scale));
        clock.Advance(41.5);
        context.Frame();
        Assert.Equal(1d, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity));
        Assert.InRange(Assert.IsType<ScaleFunction>(context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Transform).Items.Single()).X, 1.0001, 1.0499);
        clock.Advance(167);
        var opened = context.Frame();
        Assert.Equal(Matrix.Identity, Scene.Compose(context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Transform), context.Harness.Scene.GetLayoutNode(surface)!.RoundedFrame));
        await context.Page.Dialog.CloseAsync();
        context.Update();
        Assert.Same(surface, context.Surface);
        Assert.True(surface.Attributes.GetValueOrDefault("exiting") is true);
        Assert.True(surface.IsDisabled);
        clock.Advance(41.5);
        var halfExit = context.Frame();
        Assert.Equal(0.5, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity), 0.001);
        Assert.InRange(Assert.IsType<ScaleFunction>(context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Transform).Items.Single()).X, 1.0001, 1.0499);
        Assert.NotEqual(Pixel(opened, point, scale), Pixel(halfExit, point, scale));
        clock.Advance(41.5);
        context.Update();
        Assert.Equal(0d, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity));
        Assert.Same(surface, context.Surface);
        await context.Page.Dialog.OpenAsync();
        context.Update();
        Assert.Same(surface, context.Surface);
        Assert.False(surface.Attributes.GetValueOrDefault("exiting") is true);
        Assert.Same(context.Root.Single("TextFieldInput"), context.Input.Focused);
        clock.Advance(250);
        context.Update();
        Assert.Equal(1d, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity));
        await context.Page.Dialog.CloseAsync();
        context.Update();
        Assert.Same(surface, context.Surface);
        context.Harness.Host.RunUntil(() => !context.Root.Elements("Dialog").Any(), 2000);
        context.Update();
        Assert.Empty(context.Root.Elements("Dialog"));
        Assert.Same(outside, context.Input.Focused);
    }

    [Fact]
    public async Task Reduced_motion_removes_an_outgoing_dialog_immediately()
    {
        using var context = new Context("fluent");
        Assert.Equal(1d, context.Harness.Scene.GetEffectiveValue(context.Surface, StyleProperties.Opacity));
        Assert.Equal(Matrix.Identity, Scene.Compose(context.Harness.Scene.GetEffectiveValue(context.Surface, StyleProperties.Transform), context.Harness.Scene.GetLayoutNode(context.Surface)!.RoundedFrame));
        await context.Page.Dialog.CloseAsync();
        context.Frame();
        Assert.Empty(context.Root.Elements("Dialog"));
        Assert.Empty(context.Root.Elements("DialogScrim"));
    }

    private sealed class Context : IDisposable
    {
        public RenderHarness Harness { get; }
        public InputManager Input { get; }
        public RootComponent Root { get; }
        public ContentDialogRenderingPage Page { get; }
        public ElementNode Surface => Root.Single("Dialog");
        public float Width { get; set; } = 800;
        public float Height { get; set; } = 600;
        private readonly float scale;
        public Context(string theme, float scale = 1, bool reducedMotion = true, ManualAnimationClock? clock = null, Dictionary<string, object?>? parameters = null)
        {
            this.scale = scale;
            Harness = new RenderHarness(clock: clock);
            Input = new InputManager(Harness.Host.Renderer, Harness.Scene);
            Harness.Scene.Styles.Theme = theme switch { "fluent" => Nazor.Themes.Fluent.Themes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic };
            Harness.Scene.Transitions.ReducedMotion = reducedMotion;
            Harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reducedMotion);
            Root = Harness.Host.Render<ContentDialogRenderingPage>(ParameterView.FromDictionary(parameters ?? []));
            Page = (ContentDialogRenderingPage)Root.Component;
            Frame();
        }
        public RenderedImage Frame()
        {
            Update();
            return Harness.Graphics.RenderToImage(Harness.Scene.BuildDisplayList(), scale, Harness.Scene.Background);
        }
        public void Update()
        {
            Harness.Host.RunPending();
            Harness.Scene.Viewport = new Size(Width, Height);
            Harness.Scene.Scale = scale;
            Harness.Scene.Update();
            Harness.Host.RunPending();
            Harness.Scene.Update();
            Harness.Scene.BuildDisplayList();
        }
        public ElementNode Button(DialogButtonRole role) => Root.Elements("DialogButton").Single(element => Equals(element.Attributes["action-role"], role.ToString().ToLowerInvariant())).Elements().Single(element => element.Name == "Button");
        public void Enter()
        {
            Input.KeyDown("Enter", KeyModifiers.None, false);
            Input.KeyUp("Enter", KeyModifiers.None);
            Frame();
        }
        public void Dispose() { Input.Dispose(); Harness.Dispose(); }
    }
    private static Graphics.Color Pixel(RenderedImage image, Point point, float scale) => image.GetPixel((int)MathF.Floor(point.X * scale), (int)MathF.Floor(point.Y * scale));
}
