using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class CheckBoxMotionPage : ComponentBase
{
    public int State { get; private set; }
    public bool Disabled { get; set; }
    public List<bool> Changes { get; } = [];
    public void Set(int state) { State = state; StateHasChanged(); }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "padding: 24px; background: Canvas;");
        builder.OpenComponent<CheckBox>(10);
        builder.AddComponentParameter(11, nameof(CheckBox.IsChecked), State == 1);
        builder.AddComponentParameter(12, nameof(CheckBox.IsIndeterminate), State == 2);
        builder.AddComponentParameter(13, nameof(CheckBox.Disabled), Disabled);
        builder.AddComponentParameter(14, nameof(CheckBox.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, Changes.Add));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class CheckBoxMotionRenderingTests
{
    public static IEnumerable<object[]> ThemesAndScales()
    {
        foreach (var theme in new[] { "basic", "fluent", "leaf" })
        foreach (var scheme in new[] { ColorScheme.Light, ColorScheme.Dark })
        foreach (var scale in new[] { 1f, 1.5f, 2f })
            yield return [theme, scheme, scale];
    }

    [Theory]
    [MemberData(nameof(ThemesAndScales))]
    public void Persistent_vectors_animate_real_stroke_paint_and_reverse_without_delaying_semantic_state(string theme, ColorScheme scheme, float scale)
    {
        using var context = new Context(theme, scheme, scale);
        var off = context.Frame();
        Assert.Equal(0, Stroke(off, check: true));
        Assert.Equal(0, Stroke(off, check: false));
        Assert.Equal(0, Ink(off.Image, check: true));
        context.Set(1);
        context.AssertState(1);
        var partial = context.Advance(70);
        var partialCheck = Stroke(partial, check: true);
        Assert.True(partialCheck > 0);
        Assert.True(Ink(partial.Image, check: true) > 0);
        var effective = context.Harness.Scene.GetEffectiveValue(context.Check, StyleProperties.StrokeTrimEnd);
        if (theme == "basic") Assert.Equal(1, effective);
        else Assert.InRange(effective, 0.001, 0.999);

        var reversed = context.Set(0);
        context.AssertState(0);
        Assert.Equal(theme == "basic" ? 0 : partialCheck, Stroke(reversed, check: true), 0.001f);
        var receding = context.Advance(70);
        if (theme != "basic") Assert.InRange(Stroke(receding, check: true), 0, partialCheck - 0.0001f);
        context.Set(2);
        context.AssertState(2);
        var mixedPartial = context.Advance(100);
        Assert.True(Stroke(mixedPartial, check: false) > 0);
        Assert.True(Ink(mixedPartial.Image, check: false) > 0);
        var mixed = context.Advance(1000);
        Assert.Equal(0, Stroke(mixed, check: true));
        var fullMixed = Stroke(mixed, check: false);
        Assert.True(fullMixed > 0);
        if (theme != "basic") Assert.True(Stroke(mixedPartial, check: false) < fullMixed);

        context.Set(1);
        context.AssertState(1);
        var crossing = context.Advance(100);
        if (theme != "basic")
        {
            Assert.True(Stroke(crossing, check: true) > 0);
            Assert.True(Stroke(crossing, check: false) > 0);
            Assert.True(Ink(crossing.Image, check: true) > 0);
            Assert.True(Ink(crossing.Image, check: false) > 0);
        }
        var on = context.Advance(1000);
        var fullCheck = Stroke(on, check: true);
        Assert.True(fullCheck > 0);
        Assert.Equal(0, Stroke(on, check: false));
        Assert.Equal(effective, partialCheck / fullCheck, 0.001);
        if (theme != "basic") Assert.True(Ink(partial.Image, check: true) < Ink(on.Image, check: true));
        Assert.Same(context.Check, context.Root.Single("CheckBoxCheck"));
        Assert.Same(context.Mixed, context.Root.Single("CheckBoxMixed"));
        var content = context.Harness.Scene.GetContentBounds(context.Mark);
        var glyph = context.Harness.Scene.GetBounds(context.Check);
        var rounding = 0.5f / scale + 0.0001f;
        Assert.InRange(glyph.X + glyph.Width / 2 - content.X - content.Width / 2, -rounding, rounding);
        Assert.InRange(glyph.Y + glyph.Height / 2 - content.Y - content.Height / 2, -rounding, rounding);
        foreach (var edge in new[] { glyph.X - content.X, glyph.Y - content.Y, glyph.Right - content.Right, glyph.Bottom - content.Bottom })
            Assert.InRange(edge, -rounding, rounding);

        context.Page.Disabled = true;
        context.Page.Refresh();
        context.Frame();
        var box = context.Harness.Scene.GetBounds(context.Mark);
        var center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
        context.Input.PointerMoved(center, KeyModifiers.None);
        context.Input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None);
        context.Input.PointerReleased(center, PointerButton.Primary, KeyModifiers.None);
        var disabled = context.Advance(1000);
        context.AssertState(1);
        Assert.Equal(fullCheck, Stroke(disabled, check: true), 0.001f);
        Assert.Empty(context.Page.Changes);
    }

    [Theory]
    [MemberData(nameof(ThemesAndScales))]
    public void Reduced_motion_paints_each_destination_in_the_same_frame(string theme, ColorScheme scheme, float scale)
    {
        using var context = new Context(theme, scheme, scale, reducedMotion: true);
        var on = context.Set(1);
        Assert.True(Stroke(on, check: true) > 0);
        Assert.True(Ink(on.Image, check: true) > 0);
        Assert.Equal(0, Stroke(on, check: false));
        var mixed = context.Set(2);
        Assert.Equal(0, Stroke(mixed, check: true));
        Assert.True(Stroke(mixed, check: false) > 0);
        Assert.True(Ink(mixed.Image, check: false) > 0);
        var off = context.Set(0);
        Assert.Equal(0, Stroke(off, check: true));
        Assert.Equal(0, Stroke(off, check: false));
        Assert.Equal(0, Ink(off.Image, check: true));
        Assert.Equal(0, Ink(off.Image, check: false));
        Assert.False(context.Harness.Scene.Transitions.IsRunning);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Fluent_hover_and_press_sample_transform_motion_in_the_display_list_and_pixels(float scale)
    {
        using var context = new Context("fluent", ColorScheme.Light, scale);
        context.Set(1);
        var rest = context.Advance(1000);
        var mark = context.Harness.Scene.GetBounds(context.Mark);
        var restGlyph = context.Harness.Scene.GetBounds(context.Check);
        var center = new Point(mark.X + mark.Width / 2, mark.Y + mark.Height / 2);
        context.Input.PointerMoved(center, KeyModifiers.None);
        context.Frame();
        var hovering = context.Advance(30);
        var intermediate = context.Harness.Scene.GetBounds(context.Check).Width;
        Assert.InRange(intermediate, restGlyph.Width + 0.001f, restGlyph.Width * 1.166667f - 0.001f);
        Assert.Contains(hovering.Commands.OfType<PushTransform>(),
            transform => Math.Abs(transform.Matrix.M11 - intermediate / restGlyph.Width) < 0.001);
        var hover = context.Advance(1000);
        Assert.Equal(restGlyph.Width * 1.166667f, context.Harness.Scene.GetBounds(context.Check).Width, 0.001f);
        Assert.True(Ink(hover.Image, check: true) > Ink(rest.Image, check: true));
        Assert.Equal(mark, context.Harness.Scene.GetBounds(context.Mark));

        context.Input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None);
        context.Frame();
        var pressing = context.Advance(20);
        var pressedWidth = context.Harness.Scene.GetBounds(context.Check).Width;
        Assert.InRange(pressedWidth, restGlyph.Width * 0.833333f + 0.001f, restGlyph.Width * 1.166667f - 0.001f);
        Assert.Contains(pressing.Commands.OfType<PushTransform>(),
            transform => Math.Abs(transform.Matrix.M11 - pressedWidth / restGlyph.Width) < 0.001);
        context.Input.PointerCanceled(clearHover: false);
        context.Frame();
        Assert.Equal(pressedWidth, context.Harness.Scene.GetBounds(context.Check).Width, 0.001f);
        context.Advance(30);
        Assert.True(context.Harness.Scene.GetBounds(context.Check).Width > pressedWidth);
        context.Page.Disabled = true;
        context.Page.Refresh();
        context.Frame();
        Assert.Equal(restGlyph.Width, context.Harness.Scene.GetBounds(context.Check).Width, 0.001f);
        Assert.Empty(context.Page.Changes);
    }

    private sealed record Snapshot(RenderedImage Image, DrawCommand[] Commands);

    private sealed class Context : IDisposable
    {
        private readonly ManualAnimationClock clock = new();
        private readonly float scale;
        public RenderHarness Harness { get; }
        public InputManager Input { get; }
        public RootComponent Root { get; }
        public CheckBoxMotionPage Page { get; }
        public ElementNode Check { get; }
        public ElementNode Mixed { get; }
        public ElementNode Mark { get; }

        public Context(string theme, ColorScheme scheme, float scale, bool reducedMotion = false)
        {
            this.scale = scale;
            Harness = new RenderHarness(clock: clock);
            Input = new InputManager(Harness.Host.Renderer, Harness.Scene);
            Harness.Scene.Styles.Theme = theme switch
            {
                "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
                "leaf" => NazorLeafTheme.Theme,
                _ => Nazor.Themes.Themes.Basic,
            };
            Harness.Scene.Transitions.ReducedMotion = reducedMotion;
            Harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: reducedMotion);
            var application = NssCompiler.Compile("CheckBoxMotion", "CheckBoxCheck { color: #ff00ff; } CheckBoxMixed { color: #00ff00; }");
            Assert.True(application.Success, string.Join("\n", application.Diagnostics));
            Harness.Scene.Styles.Application = application.StyleSheet;
            Root = Harness.Host.Render<CheckBoxMotionPage>();
            Page = (CheckBoxMotionPage)Root.Component;
            Frame();
            Check = Root.Single("CheckBoxCheck");
            Mixed = Root.Single("CheckBoxMixed");
            Mark = Root.Single("CheckBoxMark");
        }

        public Snapshot Frame()
        {
            Harness.Host.RunPending();
            var image = Harness.Render(240, 120, scale);
            return new(image, Harness.Scene.BuildDisplayList().Commands.ToArray());
        }

        public Snapshot Set(int state) { Page.Set(state); return Frame(); }
        public Snapshot Advance(double milliseconds) { clock.Advance(milliseconds); return Frame(); }
        public void AssertState(int state)
        {
            Assert.Equal(state == 1, Root.Single("CheckBox").Attributes["checked"]);
            Assert.Equal(state == 2, Root.Single("CheckBox").Attributes["indeterminate"]);
        }
        public void Dispose() { Input.Dispose(); Harness.Dispose(); }
    }

    private static float Stroke(Snapshot snapshot, bool check)
    {
        var length = 0f;
        foreach (var line in snapshot.Commands.OfType<DrawPolyline>().Where(line => check ? line.Color.R > 200 && line.Color.B > 200 && line.Color.G < 50 : line.Color.G > 200 && line.Color.R < 50 && line.Color.B < 50))
        for (var index = 1; index < line.Points.Count; index++)
        {
            var dx = line.Points[index].X - line.Points[index - 1].X;
            var dy = line.Points[index].Y - line.Points[index - 1].Y;
            length += MathF.Sqrt(dx * dx + dy * dy);
        }
        return length;
    }

    private static long Ink(RenderedImage image, bool check)
    {
        long ink = 0;
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            var pixel = image.GetPixel(x, y);
            ink += check ? Math.Max(0, Math.Min(pixel.R - pixel.G, pixel.B - pixel.G)) : Math.Max(0, Math.Min(pixel.G - pixel.R, pixel.G - pixel.B));
        }
        return ink;
    }
}
