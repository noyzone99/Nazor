using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The outline of a <see cref="Skeleton" /> placeholder, written as the <c>shape</c> attribute.</summary>
public enum SkeletonShape
{
    /// <summary>Lines of text (<c>text</c>); the last of several lines is shorter.</summary>
    Text,
    /// <summary>A circle, for an avatar or an icon (<c>circle</c>).</summary>
    Circle,
    /// <summary>A rectangle, for an image, a card or a control (<c>rectangle</c>).</summary>
    Rectangle,
}

/// <summary>
/// A placeholder in the shape of content that is still loading, such as text lines, an avatar or a picture. It is hidden
/// from assistive technologies (announce the loading elsewhere, for example with a <see cref="Primitives.LoadingBoundary" />)
/// and static in Basic. A text skeleton renders <see cref="Lines" /> <c>SkeletonLine</c> parts, the last with the
/// <c>last</c> attribute. <see cref="Width" /> and <see cref="Height" /> are NSS lengths, such as "120px" or "40%".
/// </summary>
public sealed class Skeleton : Control
{
    /// <summary>Gets or sets the outline of the placeholder.</summary>
    [Parameter] public SkeletonShape Shape { get; set; }

    /// <summary>Gets or sets the number of lines of a text placeholder; 1 by default.</summary>
    [Parameter] public int Lines { get; set; } = 1;

    /// <summary>Gets or sets the width as an NSS length, or null for the theme's width.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Gets or sets the height as an NSS length, or null for the theme's height.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Gets the NSS element name emitted by Skeleton.</summary>
    protected override string ElementName => "Skeleton";

    /// <summary>Gets the size declarations and the caller's declarations.</summary>
    protected override string? ElementStyle
    {
        get
        {
            var own = string.Join("; ", new[] { Width is null ? null : "width: " + Width, Height is null ? null : "height: " + Height }.Where(s => s is not null));
            return Css.Join(own.Length == 0 ? null : own, Style);
        }
    }

    /// <summary>Validates the shape and the number of lines.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Shape)) throw new ArgumentOutOfRangeException(nameof(Shape));
        if (Lines < 1) throw new ArgumentOutOfRangeException(nameof(Lines), "A text skeleton has at least one line.");
    }

    /// <summary>Adds the shape and hides the placeholder from assistive technologies.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "shape", Shape switch { SkeletonShape.Circle => "circle", SkeletonShape.Rectangle => "rectangle", _ => "text" });
        builder.AddAttribute(11, "semantic-hidden", true);
    }

    /// <summary>Adds the lines of a text placeholder.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Shape != SkeletonShape.Text) return;
        for (var i = 0; i < Lines; i++)
        {
            builder.OpenElement(1000, "SkeletonLine");
            builder.SetKey(i);
            builder.AddAttribute(1001, "last", Lines > 1 && i == Lines - 1);
            builder.CloseElement();
        }
    }
}
