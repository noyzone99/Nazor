using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor.Themes.NazorLeaf;

/// <summary>
/// The four-leaf mark of Nazor: four leaves in a two-by-two grid, in two tones of the seed color. It is the brand of
/// an application (in the title bar, an about page or a splash screen) and, with <see cref="IsLoading" />, the loading
/// indicator of the Leaf theme, whose leaves light up in turn while the mark slowly turns.
/// </summary>
/// <remarks>
/// The element <c>LeafMark</c> contains four <c>LeafMarkPetal</c> parts with the <c>petal</c> attribute (1 to 4, in
/// reading order) and carries the <c>loading</c> attribute. The mark is decorative unless it has an
/// <see cref="Control.AccessibleName" />; while loading it is a <c>progressbar</c> named "Loading". Its size comes from
/// <see cref="Size" />, or from the <c>--leaf-mark-size</c> custom property (28px by default).
/// </remarks>
public sealed class LeafMark : Control
{
    /// <summary>Gets or sets whether the mark is a loading indicator.</summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>Gets or sets the side of the mark in logical pixels; 0 uses the theme size.</summary>
    [Parameter] public double Size { get; set; }

    /// <summary>Gets the NSS element name emitted by LeafMark.</summary>
    protected override string ElementName => "LeafMark";

    /// <summary>Gets the inline declarations, with the explicit size when one is set.</summary>
    protected override string? ElementStyle =>
        Size > 0 ? FormattableString.Invariant($"--leaf-mark-size: {Size}px; {base.ElementStyle}") : base.ElementStyle;

    /// <summary>Validates the size.</summary>
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Size) || Size < 0) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    /// <summary>Adds the role, the loading state and the accessible name.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "loading", IsLoading);
        if (IsLoading)
        {
            builder.AddAttribute(11, "role", "progressbar");
            builder.AddAttribute(12, "indeterminate", true);
            if (AccessibleName is null) builder.AddAttribute(13, "accessible-name", LeafStrings.Get("Loading"));
        }
        else if (AccessibleName is null) builder.AddAttribute(14, "semantic-hidden", true);
        else builder.AddAttribute(15, "role", "image");
    }

    /// <summary>Adds the four leaves.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        for (var petal = 1; petal <= 4; petal++)
        {
            builder.OpenElement(1000, "LeafMarkPetal");
            builder.SetKey(petal);
            builder.AddAttribute(1001, "petal", petal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.AddAttribute(1002, "semantic-hidden", true);
            builder.CloseElement();
        }
    }
}
