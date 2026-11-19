using Nazor.Graphics;
using Nazor.Graphics.Text;

namespace Nazor.Platform;

/// <summary>Image fitting modes for a Windows launcher badge.</summary>
public enum WindowsBadgeImageFit : byte
{
    /// <summary>Stretch to fill the content box.</summary>
    Fill,
    /// <summary>Fit the complete image, preserving its aspect ratio.</summary>
    Contain,
    /// <summary>Fill the content box, cropping excess image content.</summary>
    Cover,
    /// <summary>Use the prepared image's logical size.</summary>
    None,
    /// <summary>Contain the image without enlarging it.</summary>
    ScaleDown,
}

/// <summary>Resolved Windows launcher badge style in logical pixels. The shell reserves a fixed 16 by 16 logical pixel overlay.</summary>
public sealed record WindowsBadgeAppearance
{
    /// <summary>Borrowed application font provider returning Skia font faces, used on the UI thread; null uses installed system fonts. The owning window must keep it alive while publishing.</summary>
    public IFontProvider? Fonts { get; init; }
    /// <summary>Font provider version captured when this snapshot was created, so registrations invalidate retained badges.</summary>
    public int FontVersion { get; init; }
    /// <summary>Resolved decorative background layers. Images are borrowed from the owning renderer and must remain alive while publishing.</summary>
    public DisplayList? Background { get; init; }
    /// <summary>Whether background color and layers use the padding box inside the border; false uses the border box.</summary>
    public bool ClipBackgroundToPadding { get; init; }
    /// <summary>Optional border widths by side, overriding the uniform width. Hidden sides must have zero width.</summary>
    public Thickness? BorderWidths { get; init; }
    /// <summary>Optional top border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderTop { get; init; }
    /// <summary>Optional right border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderRight { get; init; }
    /// <summary>Optional bottom border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderBottom { get; init; }
    /// <summary>Optional left border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderLeft { get; init; }
    /// <summary>Transform of the complete badge, including shadows and mask, within the fixed overlay.</summary>
    public Matrix Transform { get; init; } = Matrix.Identity;
    /// <summary>Optional alpha mask drawn in overlay coordinates after badge composition and before transformation. Null disables masking; an empty list hides the badge. Images are borrowed from the owning renderer.</summary>
    public DisplayList? Mask { get; init; }
    /// <summary>Optional resolver called on the UI thread with free space after image fitting, which may be negative. Overrides ImagePosition and ImageOffset and must be deterministic for this snapshot.</summary>
    public Func<Size, Point>? ImagePositionResolver { get; init; }
    /// <summary>Container background; null uses red for counts and transparency for icons.</summary>
    public Color? BackgroundColor { get; init; }
    /// <summary>Count foreground.</summary>
    public Color Color { get; init; } = new(255, 255, 255, 255);
    /// <summary>Container width, constrained to the overlay area.</summary>
    public float Width { get; init; } = 16;
    /// <summary>Container height, constrained to the overlay area.</summary>
    public float Height { get; init; } = 16;
    /// <summary>Container offset from the overlay center.</summary>
    public Point Offset { get; init; }
    /// <summary>Content padding inside the border.</summary>
    public Thickness Padding { get; init; }
    /// <summary>Complete badge opacity from zero to one.</summary>
    public float Opacity { get; init; } = 1;
    /// <summary>Uniform border width; zero hides the border.</summary>
    public float BorderWidth { get; init; }
    /// <summary>Border color.</summary>
    public Color BorderColor { get; init; }
    /// <summary>Border pattern.</summary>
    public LineStyle BorderStyle { get; init; }
    /// <summary>Container corner radii.</summary>
    public CornerRadius Radius { get; init; } = new(8, 8);
    /// <summary>Count font families in fallback order. The caller must not mutate this collection.</summary>
    public IReadOnlyList<string> FontFamilies { get; init; } = ["Segoe UI"];
    /// <summary>Count font size before shrinking.</summary>
    public float FontSize { get; init; } = 11.2f;
    /// <summary>Count font weight from 1 to 1000.</summary>
    public int FontWeight { get; init; } = 700;
    /// <summary>Whether the count uses an italic font.</summary>
    public bool Italic { get; init; }
    /// <summary>Extra spacing between count characters.</summary>
    public float LetterSpacing { get; init; }
    /// <summary>Horizontal alignment: zero is left, one is right.</summary>
    public float TextAlign { get; init; } = 0.5f;
    /// <summary>Vertical alignment: zero is top, one is bottom.</summary>
    public float VerticalAlign { get; init; } = 0.5f;
    /// <summary>Whether the count shrinks to fit the content box.</summary>
    public bool TextFit { get; init; } = true;
    /// <summary>Count outline width.</summary>
    public float TextStrokeWidth { get; init; }
    /// <summary>Count outline color.</summary>
    public Color TextStrokeColor { get; init; }
    /// <summary>Optional image background override; null uses the container background.</summary>
    public Color? ImageBackgroundColor { get; init; }
    /// <summary>How the image fits the content box.</summary>
    public WindowsBadgeImageFit ImageFit { get; init; } = WindowsBadgeImageFit.Contain;
    /// <summary>Image alignment as a fraction of the space left after fitting.</summary>
    public Point ImagePosition { get; init; } = new(0.5f, 0.5f);
    /// <summary>Additional image position in logical pixels.</summary>
    public Point ImageOffset { get; init; }
    /// <summary>Image opacity from zero to one, excluding the container.</summary>
    public float ImageOpacity { get; init; } = 1;
    /// <summary>Whether the image is clipped to the rounded container.</summary>
    public bool ClipImage { get; init; }
    /// <summary>Container shadows in painting order, first on top. The caller must not mutate this collection.</summary>
    public IReadOnlyList<DrawShadow> Shadows { get; init; } = [];
    /// <summary>Count shadows; inset and spread are unsupported. The caller must not mutate this collection.</summary>
    public IReadOnlyList<DrawShadow> TextShadows { get; init; } = [];
}
