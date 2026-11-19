using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The alignment of the content of a <see cref="StatusBarSection" />, written as the <c>align</c> attribute.</summary>
public enum StatusBarSectionAlignment
{
    /// <summary>At the start (left).</summary>
    Start,
    /// <summary>In the middle.</summary>
    Center,
    /// <summary>At the end (right).</summary>
    End,
}

/// <summary>
/// The strip at the bottom of a window that shows the state of the application in <see cref="StatusBarSection" />
/// children, such as the cursor position, the encoding or the progress of a task. It is a group named "Status bar"
/// unless <see cref="Control.AccessibleName" /> says otherwise; with <see cref="Live" /> it becomes a status region whose changes
/// assistive technologies announce. <see cref="ShowSizeGrip" /> adds a decorative <c>StatusBarSizeGrip</c> part at the
/// end. The element has the <c>live</c> attribute when live.
/// </summary>
public sealed class StatusBar : ContentControl
{
    /// <summary>Gets or sets how changes are announced, or null for a bar that is not announced.</summary>
    [Parameter] public LivePoliteness? Live { get; set; }

    /// <summary>Gets or sets whether a size grip is drawn at the end of the bar, as a reminder that the window resizes.</summary>
    [Parameter] public bool ShowSizeGrip { get; set; }

    /// <summary>Gets the NSS element name emitted by StatusBar.</summary>
    protected override string ElementName => "StatusBar";

    /// <summary>Validates the politeness.</summary>
    protected override void OnParametersSet()
    {
        if (Live is { } live && !Enum.IsDefined(live)) throw new ArgumentOutOfRangeException(nameof(Live));
    }

    /// <summary>Adds the group or status role, the default name and the live politeness.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Live is null ? "group" : "status");
        if (AccessibleName is null && LabeledBy is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Get("StatusBar"));
        builder.AddAttribute(12, "live", Live switch { LivePoliteness.Assertive => "assertive", LivePoliteness.Polite => "polite", _ => null });
    }

    /// <summary>Adds the sections and the size grip.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.AddContent(1000, ChildContent);
        if (!ShowSizeGrip) return;
        builder.OpenElement(1010, "StatusBarSizeGrip");
        builder.AddAttribute(1011, "semantic-hidden", true);
        builder.CloseElement();
    }
}

/// <summary>
/// A section of a <see cref="StatusBar" />: <see cref="Width" /> wide, as wide as its content, or sharing the free space
/// with the other flexible sections (<see cref="IsFlexible" />). Text inside it is read as text; a named section
/// (<see cref="Control.AccessibleName" />) is a group for content such as buttons. The element has the
/// <c>flexible</c> and <c>align</c> attributes.
/// </summary>
public sealed class StatusBarSection : ContentControl
{
    /// <summary>Gets or sets the fixed width, or null to fit the content or share the free space.</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>Gets or sets whether the section grows to share the free space of the bar.</summary>
    [Parameter] public bool IsFlexible { get; set; }

    /// <summary>Gets or sets the alignment of the content inside the section.</summary>
    [Parameter] public StatusBarSectionAlignment Alignment { get; set; }

    /// <summary>Gets the NSS element name emitted by StatusBarSection.</summary>
    protected override string ElementName => "StatusBarSection";

    /// <summary>Gets the fixed width and the caller's declarations.</summary>
    protected override string? ElementStyle => Width is { } width ? Css.Join("width: " + Css.Px(width), Style) : Style;

    /// <summary>Validates the width and the alignment.</summary>
    protected override void OnParametersSet()
    {
        if (Width is { } width && (!double.IsFinite(width) || width < 0)) throw new ArgumentOutOfRangeException(nameof(Width));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
    }

    /// <summary>Adds the role, the flexibility and the alignment.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", AccessibleName is null && LabeledBy is null ? "text" : "group");
        builder.AddAttribute(11, "flexible", IsFlexible && Width is null);
        builder.AddAttribute(12, "align", Alignment switch { StatusBarSectionAlignment.Center => "center", StatusBarSectionAlignment.End => "end", _ => "start" });
    }
}
