using System.Text;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class SliderCutoutPage : ComponentBase
{
    [Parameter] public bool Vertical { get; set; }
    [Parameter] public string Background { get; set; } = "#9b253f";
    [Parameter] public bool UseCard { get; set; }
    [Parameter] public ValueLabelMode Labels { get; set; }
    private bool hidden;
    private bool reversed;
    private double value = 40;
    private double lower = 20;
    private double upper = 80;

    public void Hide(bool value) { hidden = value; StateHasChanged(); }
    public void Configure(bool reverse, double single, double start, double end)
    {
        reversed = reverse;
        value = single;
        lower = start;
        upper = end;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", $"padding: 20px; background: {Background};");
        if (UseCard)
        {
            builder.OpenComponent<Card>(10);
            builder.AddComponentParameter(11, nameof(Card.Style), "background: #173a50;");
            builder.AddComponentParameter(12, nameof(Card.ChildContent), (RenderFragment)Controls);
            builder.CloseComponent();
        }
        else Controls(builder);
        builder.CloseElement();
    }

    private void Controls(RenderTreeBuilder builder)
    {
        builder.OpenElement(100, "Tracks");
        builder.AddAttribute(101, "style", $"display: flex; flex-direction: {(Vertical ? "row" : "column")}; align-items: flex-start; gap: 32px;");
        var size = Vertical ? "width: 22px; height: 160px;" : "width: 200px; height: 22px;";
        var style = size + (hidden ? " visibility: hidden;" : "");
        builder.OpenComponent<Slider>(110);
        builder.AddComponentParameter(111, nameof(Slider.Value), value);
        builder.AddComponentParameter(112, nameof(Slider.Orientation), Vertical ? Orientation.Vertical : Orientation.Horizontal);
        builder.AddComponentParameter(113, nameof(Slider.IsDirectionReversed), reversed);
        builder.AddComponentParameter(114, nameof(Slider.Style), style);
        builder.AddComponentParameter(115, nameof(Slider.ValueLabel), Labels);
        builder.CloseComponent();
        builder.OpenComponent<RangeSlider>(120);
        builder.AddComponentParameter(121, nameof(RangeSlider.LowerValue), lower);
        builder.AddComponentParameter(122, nameof(RangeSlider.UpperValue), upper);
        builder.AddComponentParameter(123, nameof(RangeSlider.Orientation), Vertical ? Orientation.Vertical : Orientation.Horizontal);
        builder.AddComponentParameter(124, nameof(RangeSlider.IsDirectionReversed), reversed);
        builder.AddComponentParameter(125, nameof(RangeSlider.Style), style);
        builder.AddComponentParameter(126, nameof(RangeSlider.ValueLabel), Labels);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class SliderCutoutRenderingTests
{
    [Theory]
    [InlineData(false, 1f, ValueLabelMode.None)]
    [InlineData(false, 1.5f, ValueLabelMode.None)]
    [InlineData(false, 2f, ValueLabelMode.None)]
    [InlineData(true, 1f, ValueLabelMode.None)]
    [InlineData(true, 1.5f, ValueLabelMode.None)]
    [InlineData(true, 2f, ValueLabelMode.None)]
    [InlineData(false, 1f, ValueLabelMode.OnFocus)]
    [InlineData(false, 1.5f, ValueLabelMode.OnFocus)]
    [InlineData(false, 2f, ValueLabelMode.OnFocus)]
    [InlineData(true, 1f, ValueLabelMode.OnFocus)]
    [InlineData(true, 1.5f, ValueLabelMode.OnFocus)]
    [InlineData(true, 2f, ValueLabelMode.OnFocus)]
    [InlineData(false, 1f, ValueLabelMode.Always)]
    [InlineData(false, 1.5f, ValueLabelMode.Always)]
    [InlineData(false, 2f, ValueLabelMode.Always)]
    [InlineData(true, 1f, ValueLabelMode.Always)]
    [InlineData(true, 1.5f, ValueLabelMode.Always)]
    [InlineData(true, 2f, ValueLabelMode.Always)]
    public void Leaf_thumb_gaps_show_the_actual_surface_in_every_range_state(bool vertical, float scale, ValueLabelMode labels)
    {
        var svg = Convert.ToBase64String(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><path fill='#964135' d='M0 0h32v32H0z'/><path fill='#287f82' d='M0 0h16v16H0zM16 16h16v16H16z'/></svg>"));
        foreach (var (background, card) in new[]
        {
            ("#9b253f", false),
            ("linear-gradient(to right, #671c34, #153f70)", false),
            ($"url(\"data:image/svg+xml;base64,{svg}\")", false),
            ("#9b253f", true),
        })
        {
            using var harness = new RenderHarness();
            harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
            harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
            var root = harness.Host.Render<SliderCutoutPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(SliderCutoutPage.Vertical)] = vertical,
                [nameof(SliderCutoutPage.Background)] = background,
                [nameof(SliderCutoutPage.UseCard)] = card,
                [nameof(SliderCutoutPage.Labels)] = labels,
            }));
            var page = (SliderCutoutPage)root.Component;
            page.Hide(true);
            harness.Host.RunPending();
            harness.Render(320, 260, scale);
            var loading = harness.Scene.WhenImagesLoaded();
            harness.Host.RunUntil(() => loading.IsCompleted, 10000);
            Assert.True(loading.IsCompletedSuccessfully, loading.Exception?.ToString());
            harness.Host.RunPending();
            var surface = harness.Render(320, 260, scale);
            page.Hide(false);
            using var input = new InputManager(harness.Host.Renderer, harness.Scene);

            foreach (var (reverse, single, lower, upper) in new[]
            {
                (false, 40d, 20d, 80d),
                (true, 40d, 20d, 80d),
                (false, 40d, 49d, 51d),
                (false, 0d, 0d, 100d),
                (true, 100d, 0d, 100d),
            })
            {
                page.Configure(reverse, single, lower, upper);
                harness.Host.RunPending();
                AssertGaps(harness.Render(320, 260, scale));
                foreach (var thumb in root.Elements().Where(element => element.Name is "SliderThumb" or "RangeSliderThumb"))
                {
                    var bounds = harness.Scene.GetBounds(thumb);
                    var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                    input.PointerMoved(center, KeyModifiers.None);
                    AssertGaps(harness.Render(320, 260, scale));
                    input.Focus(thumb.ParentElement, visible: true);
                    AssertGaps(harness.Render(320, 260, scale));
                    input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None);
                    AssertGaps(harness.Render(320, 260, scale));
                    input.PointerCanceled();
                    harness.Render(320, 260, scale);
                }
            }

            void AssertGaps(RenderedImage image)
            {
                var thumbs = root.Elements().Where(element => element.Name is "SliderThumb" or "RangeSliderThumb").ToArray();
                var boxes = thumbs.Select(harness.Scene.GetBounds).ToArray();
                foreach (var box in boxes)
                foreach (var point in vertical
                    ? new[] { new Point(box.X + box.Width / 2, box.Y - 2), new Point(box.X + box.Width / 2, box.Bottom + 2) }
                    : new[] { new Point(box.X - 2, box.Y + box.Height / 2), new Point(box.Right + 2, box.Y + box.Height / 2) })
                {
                    var x = (int)MathF.Floor(point.X * scale);
                    var y = (int)MathF.Floor(point.Y * scale);
                    var pixel = new Rect(x / scale, y / scale, 1 / scale, 1 / scale);
                    if (boxes.Any(other => other != box && !other.Inflate(6).Intersect(pixel).IsEmpty)) continue;
                    if (surface.GetPixel(x, y) != image.GetPixel(x, y))
                    {
                        var folder = Path.Combine(AppContext.BaseDirectory, "artifacts", "slider-gap");
                        Directory.CreateDirectory(folder);
                        var detail = string.Join("\n", thumbs.Select(thumb => $"{thumb.Name}: {harness.Scene.GetBounds(thumb)} parent={thumb.ParentElement?.Name} states={thumb.ParentElement?.States} outline={harness.Scene.GetStyle(thumb).Get(StyleProperties.OutlineOffset)}"));
                        File.WriteAllText(Path.Combine(folder, $"{vertical}-{scale}-{labels}.txt"), $"background={background} card={card} point={point} box={box} expected={surface.GetPixel(x, y)} actual={image.GetPixel(x, y)}\n{detail}\n{harness.Scene.DescribeLayout()}");
                        File.WriteAllBytes(Path.Combine(folder, $"{vertical}-{scale}-{labels}.png"), harness.Graphics.EncodePng(image));
                    }
                    Assert.Equal(surface.GetPixel(x, y), image.GetPixel(x, y));
                }
                foreach (var track in root.Elements().Where(element => element.Name is "SliderTrack" or "RangeSliderTrack"))
                {
                    var bounds = harness.Scene.GetBounds(track);
                    var samples = 0;
                    for (var fraction = 0.05f; fraction < 1; fraction += 0.1f)
                    {
                        var point = vertical
                            ? new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height * fraction)
                            : new Point(bounds.X + bounds.Width * fraction, bounds.Y + bounds.Height / 2);
                        if (boxes.Any(box => box.Inflate(7).Contains(point))) continue;
                        var x = (int)MathF.Floor(point.X * scale);
                        var y = (int)MathF.Floor(point.Y * scale);
                        Assert.NotEqual(surface.GetPixel(x, y), image.GetPixel(x, y));
                        samples++;
                    }
                    Assert.True(samples > 0);
                }
            }
        }
    }
}
