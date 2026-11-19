using Nazor.Rendering;

namespace Nazor.Controls;

internal interface IButtonGroupMember
{
    ElementNode? Node { get; }

    void Place(GroupPosition? position);
}

/// <summary>
/// Joins related buttons into one visual unit, such as the alignment buttons of an editor: every
/// <see cref="Button" />, <see cref="ToggleButton" />, <see cref="MenuButton" /> and <see cref="SplitButton" /> inside
/// it gets the <c>position</c> attribute (<c>first</c>, <c>middle</c>, <c>last</c> or <c>only</c>) in the order they
/// appear, so themes can join their borders, and takes the group's <see cref="Appearance" /> and <see cref="Size" />
/// when it leaves its own at the default. The element has the <c>group</c> role and the <c>orientation</c> attribute;
/// <see cref="Disabled" /> disables every member. Each button stays a Tab stop; use a <see cref="Toolbar" /> for a
/// single Tab stop with arrow keys.
/// </summary>
public sealed class ButtonGroup : ContentControl
{
    private readonly List<IButtonGroupMember> members = [];

    /// <summary>Gets or sets the direction in which the buttons are joined.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets the appearance of the members that keep the standard appearance.</summary>
    [Parameter] public ControlAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size of the members that keep the medium size.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether every member rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the NSS element name emitted by ButtonGroup.</summary>
    protected override string ElementName => "ButtonGroup";

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    /// <summary>Adds the group role, the orientation, the shared look and the disabled state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(12, "appearance", Vocabulary.Name(Appearance));
        builder.AddAttribute(13, "size", Vocabulary.Name(Size));
        builder.AddAttribute(14, "disabled", Disabled);
    }

    /// <summary>Supplies the group to its buttons.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ButtonGroup>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Places the members after every render, as buttons may have been added, removed or moved.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender) => Arrange();

    internal void Join(IButtonGroupMember member)
    {
        if (!members.Contains(member)) members.Add(member);
    }

    internal void Leave(IButtonGroupMember member)
    {
        if (members.Remove(member)) Arrange();
    }

    internal void Arrange()
    {
        if (Element.Element is not { IsRemoved: false } root) return;
        var order = new List<ElementNode>();
        Collect(root, order);
        var placed = members.Select(member => (member, index: member.Node is { } node ? order.IndexOf(node) : -1)).ToList();
        var ordered = placed.Where(entry => entry.index >= 0).OrderBy(entry => entry.index).Select(entry => entry.member).ToList();
        foreach (var (member, index) in placed)
            member.Place(index < 0 ? null : Vocabulary.Position(ordered.IndexOf(member), ordered.Count));
    }

    private static void Collect(ElementNode element, List<ElementNode> order)
    {
        foreach (var child in element.Children.OfType<ElementNode>())
        {
            if (child.Attributes.GetValueOrDefault("overlay") is true) continue;
            order.Add(child);
            Collect(child, order);
        }
    }
}
