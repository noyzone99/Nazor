namespace Nazor.Controls;

/// <summary>The preferred vertical position of a choice list relative to its field.</summary>
public enum DropDownPlacement
{
    /// <summary>Uses the space available above and below the field, preferring below when the list fits.</summary>
    Auto,

    /// <summary>Prefers opening above the field.</summary>
    Top,

    /// <summary>Prefers opening below the field.</summary>
    Bottom,

    /// <summary>Overlays the field, aligning its selected option when possible or centering the list otherwise. Available window/work-area space can shift and constrain the list.</summary>
    Center,
}

internal static class DropDownOptions
{
    public const int DefaultMaximum = 10;

    public static void Validate(DropDownPlacement dropDownPlacement, int? preferredMinVisibleItems, int? preferredMaxVisibleItems)
    {
        if (!Enum.IsDefined(dropDownPlacement)) throw new ArgumentOutOfRangeException(nameof(dropDownPlacement));
        if (preferredMaxVisibleItems is <= 0) throw new ArgumentOutOfRangeException(nameof(preferredMaxVisibleItems));
        if (preferredMinVisibleItems is <= 0 || preferredMinVisibleItems > preferredMaxVisibleItems)
            throw new ArgumentOutOfRangeException(nameof(preferredMinVisibleItems));
    }

    public static IReadOnlyDictionary<string, object?> Attributes(DropDownPlacement placement, int? minimum, int? maximum) =>
        new Dictionary<string, object?>
        {
            ["drop-down"] = placement switch { DropDownPlacement.Top => "top", DropDownPlacement.Bottom => "bottom", DropDownPlacement.Center => "center", _ => "auto" },
            ["preferred-min-visible-items"] = minimum,
            ["preferred-max-visible-items"] = maximum,
        };
}
