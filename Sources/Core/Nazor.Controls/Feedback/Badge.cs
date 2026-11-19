using System.Globalization;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The corner of the decorated content at which a <see cref="Badge" /> indicator sits.</summary>
public enum BadgePosition
{
    /// <summary>The top corner at the end of the reading direction (top right in left-to-right text).</summary>
    TopEnd,
    /// <summary>The top corner at the start of the reading direction.</summary>
    TopStart,
    /// <summary>The bottom corner at the end of the reading direction.</summary>
    BottomEnd,
    /// <summary>The bottom corner at the start of the reading direction.</summary>
    BottomStart,
}

/// <summary>
/// A small indicator: a <see cref="Count" /> (shown as "99+" above <see cref="Max" />), a <see cref="Dot" /> or a short
/// <see cref="Text" /> such as "New", a typed <see cref="Icon" /> or decorative <see cref="IndicatorContent" />.
/// Precedence is Dot, Count, IndicatorContent, Icon, then Text. With <see cref="ContentControl.ChildContent" /> the badge decorates that content,
/// drawn inside the <c>BadgeContent</c> part, and the <c>BadgeIndicator</c> part sits at the <see cref="Position" />
/// corner (the <c>corner</c> attribute, and <c>decorating</c> on the element); without it the indicator stands alone
/// (<c>standalone</c>). The indicator has the <c>dot</c> attribute for a dot and is announced as "5 new" or as
/// <see cref="Control.AccessibleName" />. The indicator is the single semantic owner; icons/custom content default to
/// localized new-activity text, so supply a descriptive AccessibleName. A count of zero hides the indicator unless
/// <see cref="ShowZero" /> is set. Count values use keyed BadgeValue parts; themes can animate changed values without
/// remounting the decorated content. Accessible count text always exposes the actual count, independent of the visual cap.
/// </summary>
public sealed class Badge : ContentControl
{
    private int? previousCount;
    private bool countChanged;
    /// <summary>Gets or sets the number shown, or null for a dot or a text badge.</summary>
    [Parameter] public int? Count { get; set; }

    /// <summary>Gets or sets the largest number shown as is; larger counts show as "{Max}+". 99 by default.</summary>
    [Parameter] public int Max { get; set; } = 99;

    /// <summary>Gets or sets whether a zero count still shows the indicator.</summary>
    [Parameter] public bool ShowZero { get; set; }

    /// <summary>Gets or sets whether the indicator is a dot without text, such as an unread mark.</summary>
    [Parameter] public bool Dot { get; set; }

    /// <summary>Gets or sets a short text shown instead of a count, such as "New" or "Beta".</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Gets or sets a typed decorative icon shown in BadgeIcon when Dot, Count and IndicatorContent are absent. It takes precedence over Text; supply AccessibleName to describe its meaning.</summary>
    [Parameter] public IFontSymbol? Icon { get; set; }

    /// <summary>Gets or sets decorative indicator content shown in BadgeIndicatorContent when Dot and Count are absent. It takes precedence over Icon and Text. Content is hidden from accessibility and interaction; supply AccessibleName for the single indicator owner.</summary>
    [Parameter] public RenderFragment? IndicatorContent { get; set; }

    /// <summary>Gets or sets the meaning of the badge's color; accent by default.</summary>
    [Parameter] public Intent Intent { get; set; } = Intent.Accent;

    /// <summary>Gets or sets the corner of the decorated content at which the indicator sits.</summary>
    [Parameter] public BadgePosition Position { get; set; }

    /// <summary>Gets or sets the size step of the indicator.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets the text drawn in the indicator, or null for a dot, typed icon or custom indicator content.</summary>
    public string? DisplayText => Dot ? null : Count is { } count ? count > Max ? Max.ToString(CultureInfo.CurrentCulture) + "+" : count.ToString(CultureInfo.CurrentCulture) : IndicatorContent is not null || Icon is not null ? null : Text;

    /// <summary>Gets whether the indicator is shown.</summary>
    public bool IsIndicatorVisible => Dot || (Count is { } count ? count != 0 || ShowZero : IndicatorContent is not null || Icon is not null || Text is not null);

    private string IndicatorKind => Dot ? "dot" : Count is not null ? "count" : IndicatorContent is not null ? "content" : Icon is not null ? "icon" : "text";

    /// <summary>Gets the NSS element name emitted by Badge.</summary>
    protected override string ElementName => "Badge";

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (Max < 0) throw new ArgumentOutOfRangeException(nameof(Max), "The maximum count cannot be negative.");
        if (Count < 0) throw new ArgumentOutOfRangeException(nameof(Count), "A count cannot be negative.");
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (!Enum.IsDefined(Position)) throw new ArgumentOutOfRangeException(nameof(Position));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (previousCount != Count)
        {
            countChanged = previousCount is not null && Count is not null;
            previousCount = Count;
        }
    }

    /// <summary>Adds the look and placement attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "decorating", ChildContent is not null);
        builder.AddAttribute(13, "standalone", ChildContent is null);
        builder.AddAttribute(14, "corner", CornerName(Position));
        builder.AddAttribute(15, "indicator-kind", IndicatorKind);
    }

    /// <summary>Adds the decorated content and the indicator.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.OpenElement(1000, "BadgeContent");
            builder.AddContent(1001, ChildContent);
            builder.CloseElement();
        }
        if (!IsIndicatorVisible) return;
        builder.OpenElement(1010, "BadgeIndicator");
        builder.AddAttribute(1011, "role", "text");
        builder.AddAttribute(1012, "accessible-name", AccessibleName ?? AccessibleText());
        builder.AddAttribute(1013, "accessible-description", AccessibleDescription);
        builder.AddAttribute(1014, "dot", Dot);
        builder.AddAttribute(1015, "corner", CornerName(Position));
        builder.AddAttribute(1016, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(1017, "labeled-by", LabeledBy);
        builder.AddAttribute(1018, "described-by", DescribedBy);
        builder.AddAttribute(1019, "content-kind", IndicatorKind);
        builder.AddAttribute(1020, "icon", IndicatorKind == "icon");
        if (!Dot)
        {
            if (Count is { } count)
            {
                builder.OpenElement(1030, "BadgeValue");
                builder.SetKey(count);
                builder.AddAttribute(1031, "changed", countChanged);
                builder.AddAttribute(1032, "semantic-hidden", true);
                builder.AddContent(1033, DisplayText);
                builder.CloseElement();
            }
            else if (IndicatorContent is not null)
            {
                builder.OpenElement(1040, "BadgeIndicatorContent");
                builder.AddAttribute(1041, "semantic-hidden", true);
                builder.AddAttribute(1042, "disabled", true);
                builder.AddAttribute(1043, "style", "pointer-events: none");
                builder.AddContent(1044, IndicatorContent);
                builder.CloseElement();
            }
            else if (Icon is not null)
            {
                builder.OpenElement(1050, "BadgeIcon");
                builder.AddAttribute(1051, "semantic-hidden", true);
                builder.OpenComponent<global::Nazor.Controls.Icon>(1052);
                builder.AddComponentParameter(1053, nameof(global::Nazor.Controls.Icon.Symbol), Icon);
                builder.CloseComponent();
                builder.CloseElement();
            }
            else builder.AddContent(1060, Text);
        }
        builder.CloseElement();
    }

    private string? AccessibleText() =>
        Dot ? ControlStrings.Get("NewActivity") : Count is { } count ? ControlStrings.Format("BadgeCount", count.ToString(CultureInfo.CurrentCulture)) :
            IndicatorContent is not null || Icon is not null ? ControlStrings.Get("NewActivity") : null;

    private static string CornerName(BadgePosition position) => position switch
    {
        BadgePosition.TopStart => "top-start", BadgePosition.BottomEnd => "bottom-end", BadgePosition.BottomStart => "bottom-start", _ => "top-end",
    };
}
