using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A strip of commands along the edge of a pane, such as Cancel and Save at the bottom of a settings page or the
/// selection commands under a list: <see cref="Start" />, <see cref="Center" /> and <see cref="End" /> slots
/// (<c>ActionBarStart</c>, <c>ActionBarCenter</c>, <c>ActionBarEnd</c>) keep their content at the leading edge, in the
/// middle and at the trailing edge; content between the tags is added to the end slot. Like a <see cref="Toolbar" /> it
/// has the <c>toolbar</c> role, so it is one Tab stop with arrow navigation between its controls.
/// </summary>
public sealed class ActionBar : ContentControl
{
    /// <summary>Gets or sets the content at the leading edge.</summary>
    [Parameter] public RenderFragment? Start { get; set; }

    /// <summary>Gets or sets the content in the middle.</summary>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary>Gets or sets the content at the trailing edge, before the content between the tags.</summary>
    [Parameter] public RenderFragment? End { get; set; }

    /// <summary>Gets the NSS element name emitted by ActionBar.</summary>
    protected override string ElementName => "ActionBar";

    /// <summary>Adds the toolbar role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "toolbar");

    /// <summary>Adds the three slots.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "ActionBarStart");
        builder.AddContent(1001, Start);
        builder.CloseElement();
        builder.OpenElement(1010, "ActionBarCenter");
        builder.AddContent(1011, Center);
        builder.CloseElement();
        builder.OpenElement(1020, "ActionBarEnd");
        builder.AddContent(1021, End);
        builder.AddContent(1022, ChildContent);
        builder.CloseElement();
    }
}
