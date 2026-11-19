using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The window edge a <see cref="Drawer" /> is attached to, written as the <c>side</c> attribute.</summary>
public enum DrawerPlacement
{
    /// <summary>The leading edge, as a navigation drawer.</summary>
    Start,
    /// <summary>The trailing edge, as a details or settings panel.</summary>
    End,
    /// <summary>The top edge.</summary>
    Top,
    /// <summary>The bottom edge, as a bottom sheet.</summary>
    Bottom,
}

/// <summary>
/// A panel that slides over the window from one edge: a side drawer (<see cref="DrawerPlacement.Start" />,
/// <see cref="DrawerPlacement.End" />) or a sheet (<see cref="DrawerPlacement.Top" />, <see cref="DrawerPlacement.Bottom" />)
/// with a header (<see cref="Title" /> or <see cref="Header" /> and a close button), a scrolling body and a
/// <see cref="Footer" />. A bottom sheet shows a grab <c>DrawerHandle</c> by default. <see cref="Size" /> sets the width of a
/// side drawer or the height of a sheet. Modal by default with a scrim that dismisses it when pressed. Parts:
/// <c>DrawerScrim</c>, <c>Drawer</c> (<c>side</c>, <c>dialog-placement</c>), <c>DrawerHandle</c>, <c>DrawerHeader</c>,
/// <c>DrawerTitle</c>, <c>DrawerClose</c>, <c>DrawerBody</c> and <c>DrawerFooter</c>.
/// </summary>
public sealed class Drawer : DialogBase
{
    private ElementReference body;
    private ElementReference footer;

    /// <summary>Gets or sets the edge the drawer is attached to.</summary>
    [Parameter] public DrawerPlacement Placement { get; set; } = DrawerPlacement.End;

    /// <summary>Gets or sets the title, which names the drawer.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets rich header content, used instead of <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>Gets or sets the body of the drawer.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the footer, such as <see cref="DialogButton" /> actions.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Gets or sets the width of a side drawer or the height of a sheet, in logical units; null uses the theme's size.</summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>Gets or sets whether the drawer confines interaction and the focus and shows a scrim.</summary>
    [Parameter] public bool Modal { get; set; } = true;

    /// <summary>Gets or sets whether a press outside the drawer dismisses it.</summary>
    [Parameter] public bool CloseOnOutside { get; set; } = true;

    /// <summary>Gets or sets whether the header shows a close button.</summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>Gets or sets whether a grab handle is shown; null shows it on bottom sheets.</summary>
    [Parameter] public bool? ShowHandle { get; set; }

    /// <summary>Gets the NSS element name emitted by Drawer.</summary>
    protected override string ElementName => "Drawer";

    /// <summary>Gets the dialog placement of the edge.</summary>
    protected override DialogPlacement SurfacePlacement => Placement switch
    {
        DrawerPlacement.Start => DialogPlacement.Start, DrawerPlacement.Top => DialogPlacement.Top,
        DrawerPlacement.Bottom => DialogPlacement.Bottom, _ => DialogPlacement.End,
    };

    /// <summary>Gets whether the drawer is modal.</summary>
    protected override bool IsModal => Modal;

    /// <summary>Gets whether a press outside dismisses the drawer.</summary>
    protected override bool DismissOnOutside => CloseOnOutside;

    /// <summary>Gets whether the drawer has a title part.</summary>
    protected override bool HasTitle => Title is not null || Header is not null;

    /// <summary>Gets the size declaration and the caller's declarations.</summary>
    protected override string? SurfaceStyle => Size is { } size
        ? Css.Join((Placement is DrawerPlacement.Start or DrawerPlacement.End ? "width: " : "height: ") + Css.Px(size), Style)
        : Style;

    /// <summary>Validates the placement and the size.</summary>
    protected override void Validate()
    {
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (Size is { } size && (!double.IsFinite(size) || size <= 0)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    /// <summary>Adds the side attribute.</summary>
    /// <param name="attributes">The attributes forwarded to the surface.</param>
    protected override void AddSurfaceAttributes(IDictionary<string, object?> attributes) =>
        attributes["side"] = Placement.ToString().ToLowerInvariant();

    /// <summary>Focuses the first control of the body, then of the footer, before the close button.</summary>
    /// <returns>The element to focus, or null.</returns>
    protected override ElementReference? DefaultInitialFocus() => FirstFocusable(body, footer);

    /// <summary>Renders the handle, the header, the body and the footer.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildContent(RenderTreeBuilder builder)
    {
        if (ShowHandle ?? Placement == DrawerPlacement.Bottom)
        {
            builder.OpenElement(0, "DrawerHandle");
            builder.AddAttribute(1, "semantic-hidden", true);
            builder.CloseElement();
        }
        if (HasTitle || ShowCloseButton)
        {
            builder.OpenElement(10, "DrawerHeader");
            if (HasTitle)
            {
                builder.OpenElement(11, "DrawerTitle");
                builder.AddAttribute(12, "id", TitleId);
                builder.AddAttribute(13, "role", "heading");
                builder.AddAttribute(14, "level", 2);
                if (Header is not null) builder.AddContent(15, Header);
                else builder.AddContent(16, Title);
                builder.CloseElement();
            }
            if (ShowCloseButton) OverlayParts.CloseButton(builder, 20, "DrawerClose", this, DismissAsync);
            builder.CloseElement();
        }
        builder.OpenElement(40, "DrawerBody");
        builder.AddElementReferenceCapture(41, reference => body = reference);
        builder.AddContent(42, ChildContent);
        builder.CloseElement();
        if (Footer is null) return;
        builder.OpenElement(50, "DrawerFooter");
        builder.AddElementReferenceCapture(51, reference => footer = reference);
        builder.AddContent(52, Footer);
        builder.CloseElement();
    }
}
