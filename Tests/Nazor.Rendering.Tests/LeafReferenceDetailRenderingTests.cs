using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafReferenceDisclosurePage : ComponentBase
{
    public bool Open { get; set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.AddAttribute(1, "style", "padding: 24px; width: 360px; display: flex; flex-direction: column; gap: 16px;");
        builder.OpenComponent<Expander>(10);
        builder.AddComponentParameter(11, nameof(Expander.Header), "Expander");
        builder.AddComponentParameter(12, nameof(Expander.IsExpanded), Open);
        builder.AddComponentParameter(13, nameof(Expander.ChildContent), (RenderFragment)(content => content.AddContent(0, "Expanded body")));
        builder.CloseComponent();
        builder.OpenComponent<Accordion>(20);
        builder.AddComponentParameter(21, nameof(Accordion.ExpandedValues), Open ? new[] { "one" } : Array.Empty<string>());
        builder.AddComponentParameter(22, nameof(Accordion.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<AccordionItem>(0);
            content.AddComponentParameter(1, nameof(AccordionItem.Header), "Accordion");
            content.AddComponentParameter(2, nameof(AccordionItem.Value), "one");
            content.AddComponentParameter(3, nameof(AccordionItem.ChildContent), (RenderFragment)(body => body.AddContent(0, "Accordion body")));
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class LeafReferenceDetailRenderingTests
{
    public static IEnumerable<object[]> Modes =>
        from scale in new[] { 1f, 1.5f, 2f }
        from reduced in new[] { false, true }
        from forcedColors in new[] { false, true }
        select new object[] { scale, reduced, forcedColors };

    [Theory]
    [MemberData(nameof(Modes))]
    public void Disclosure_chevrons_paint_the_fourteen_pixel_SVG_path_and_scaled_stroke_in_every_state(float scale, bool reduced, bool forcedColors)
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, reduced, forcedColors);
        harness.Scene.Transitions.ReducedMotion = reduced;
        var root = harness.Host.Render<LeafReferenceDisclosurePage>();
        var page = (LeafReferenceDisclosurePage)root.Component;
        Frame();
        AssertStrokes();
        foreach (var open in new[] { true, false })
        {
            page.Open = open;
            page.Refresh();
            Frame();
            AssertStrokes();
            clock.Advance(225);
            Frame();
            AssertStrokes();
            clock.Advance(225);
            Frame();
            AssertStrokes();
        }

        void Frame()
        {
            for (var frame = 0; frame < 4; frame++) { harness.Host.RunPending(); harness.Render(420, 520, scale); }
        }

        void AssertStrokes()
        {
            var strokes = harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Where(line => line.Points.Count == 3).ToArray();
            Assert.Equal(2, strokes.Length);
            foreach (var name in new[] { "ExpanderChevron", "AccordionItemChevron" })
            {
                var box = harness.Scene.GetContentBox(root.Single(name));
                Assert.Equal(14, box.Width);
                Assert.Equal(14, box.Height);
                Point[] expected = [new(box.X + box.Width * 6 / 24, box.Y + box.Height * 9 / 24),
                    new(box.X + box.Width * 12 / 24, box.Y + box.Height * 15 / 24),
                    new(box.X + box.Width * 18 / 24, box.Y + box.Height * 9 / 24)];
                var stroke = Assert.Single(strokes, line => line.Points.Zip(expected).All(pair =>
                    Math.Abs(pair.First.X - pair.Second.X) < .0001f && Math.Abs(pair.First.Y - pair.Second.Y) < .0001f));
                Assert.Equal(1.7f * 14 / 24, stroke.Thickness, 6);
                Assert.Equal(255, stroke.Color.A);
            }
        }
    }
}
