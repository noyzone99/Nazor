namespace Nazor.Controls;

/// <summary>
/// How strongly a control is filled, the shared vocabulary of buttons and similar controls. Themes draw each value in
/// their own style; a theme without a matching style uses the nearest one. The value is written as the
/// <c>appearance</c> attribute, for example <c>Button[appearance="accent"]</c>.
/// </summary>
public enum ControlAppearance
{
    /// <summary>The ordinary look of the control.</summary>
    Standard,
    /// <summary>Filled with the accent color, for the main action of a view.</summary>
    Accent,
    /// <summary>A softer tint of the accent color, for important but secondary actions.</summary>
    Tonal,
    /// <summary>Only an outline, for alternative actions.</summary>
    Outline,
    /// <summary>No fill or border until hovered, for actions in toolbars, lists and dense views.</summary>
    Subtle,
    /// <summary>Drawn as a hyperlink.</summary>
    Link,
}

/// <summary>
/// The meaning of a control's color, combined with <see cref="ControlAppearance" />. It is written as the <c>intent</c>
/// attribute, for example <c>Button[intent="danger"]</c>.
/// </summary>
public enum Intent
{
    /// <summary>No particular meaning.</summary>
    Neutral,
    /// <summary>The accent color of the application.</summary>
    Accent,
    /// <summary>A successful or positive outcome.</summary>
    Success,
    /// <summary>Something that needs attention.</summary>
    Warning,
    /// <summary>A destructive or failed action.</summary>
    Danger,
}

/// <summary>The size step of a control, written as the <c>size</c> attribute.</summary>
public enum ControlSize
{
    /// <summary>The compact size, for dense views.</summary>
    Small,
    /// <summary>The ordinary size.</summary>
    Medium,
    /// <summary>The large size, for prominent actions and touch.</summary>
    Large,
}

/// <summary>The outline of a control, written as the <c>shape</c> attribute.</summary>
public enum Shape
{
    /// <summary>The theme's usual outline.</summary>
    Default,
    /// <summary>Fully rounded ends.</summary>
    Pill,
    /// <summary>A circle, for controls that show only an icon.</summary>
    Circle,
    /// <summary>A square, for controls that show only an icon.</summary>
    Square,
}

/// <summary>The importance of a message, written as the <c>severity</c> attribute.</summary>
public enum Severity
{
    /// <summary>Neutral information.</summary>
    Info,
    /// <summary>A successful outcome.</summary>
    Success,
    /// <summary>A condition that needs attention.</summary>
    Warning,
    /// <summary>An error or a failure.</summary>
    Error,
}

/// <summary>The direction in which a control lays out or moves, written as the <c>orientation</c> attribute.</summary>
public enum Orientation
{
    /// <summary>Left to right.</summary>
    Horizontal,
    /// <summary>Top to bottom.</summary>
    Vertical,
}

/// <summary>The position of a control within a joined group, written as the <c>position</c> attribute.</summary>
public enum GroupPosition
{
    /// <summary>The only member of the group.</summary>
    Only,
    /// <summary>The first member.</summary>
    First,
    /// <summary>A member between the first and the last.</summary>
    Middle,
    /// <summary>The last member.</summary>
    Last,
}

internal static class Vocabulary
{
    internal static string Name(ControlAppearance value) => value switch
    {
        ControlAppearance.Accent => "accent", ControlAppearance.Tonal => "tonal", ControlAppearance.Outline => "outline",
        ControlAppearance.Subtle => "subtle", ControlAppearance.Link => "link", _ => "standard",
    };

    internal static string Name(Intent value) => value switch
    {
        Intent.Accent => "accent", Intent.Success => "success", Intent.Warning => "warning", Intent.Danger => "danger", _ => "neutral",
    };

    internal static string Name(ControlSize value) => value switch { ControlSize.Small => "small", ControlSize.Large => "large", _ => "medium" };

    internal static string Name(Shape value) => value switch { Shape.Pill => "pill", Shape.Circle => "circle", Shape.Square => "square", _ => "default" };

    internal static string Name(Severity value) => value switch { Severity.Success => "success", Severity.Warning => "warning", Severity.Error => "error", _ => "info" };

    internal static string Name(GroupPosition value) => value switch
    {
        GroupPosition.First => "first", GroupPosition.Middle => "middle", GroupPosition.Last => "last", _ => "only",
    };

    internal static string Name(Orientation value) => value == Orientation.Vertical ? "vertical" : "horizontal";

    internal static GroupPosition Position(int index, int count) =>
        count <= 1 ? GroupPosition.Only : index == 0 ? GroupPosition.First : index == count - 1 ? GroupPosition.Last : GroupPosition.Middle;
}
