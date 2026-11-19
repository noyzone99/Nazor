using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Where the items of a <see cref="Toolbar" /> show their label relative to their icon.</summary>
public enum ToolbarLabelPosition
{
    /// <summary>The label follows the icon, written as <c>beside</c>.</summary>
    Beside,
    /// <summary>The label is under the icon, written as <c>below</c>.</summary>
    Below,
    /// <summary>Only the icon is shown; the label stays the accessible name and the tooltip. Written as <c>none</c>.</summary>
    None,
}

/// <summary>
/// A bar of commands, such as the formatting buttons of an editor. It is one Tab stop: the arrow keys (Up and Down when
/// vertical), Home and End move between its items. Fill it freely with buttons and fields and organize them with
/// <see cref="ToolbarGroup" />, <see cref="ToolbarSeparator" /> and <see cref="ToolbarSpacer" />, or give it
/// <see cref="Commands" />: it then renders one button per command (a toggle button for commands with a checked state)
/// in a <c>ToolbarCommands</c> row and moves the commands that do not fit into a "More" <see cref="MenuButton" />
/// (<c>ToolbarCommandsOverflow</c>); free content can follow the commands. Commands are not split into an overflow
/// menu in a vertical toolbar. The element
/// has the <c>toolbar</c> role and the <c>orientation</c>, <c>vertical</c>, <c>size</c> and <c>label-position</c>
/// attributes, which themes use to style the items.
/// </summary>
public sealed class Toolbar : ContentControl, IDisposable
{
    private readonly List<Command> observed = [];
    private IReadOnlyList<Command> commands = [];

    /// <summary>Gets or sets the direction of the bar.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets where the items show their labels.</summary>
    [Parameter] public ToolbarLabelPosition LabelPosition { get; set; }

    /// <summary>Gets or sets the size of the command buttons, also written as the <c>size</c> attribute.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the commands rendered as buttons, in order; null for free content only.</summary>
    [Parameter] public IEnumerable<Command>? Commands { get; set; }

    /// <summary>Gets or sets the icon of a command's button, shown before its label; null for none.</summary>
    [Parameter] public RenderFragment<Command>? CommandIcon { get; set; }

    /// <summary>Gets or sets the priority of a command; lower priorities move to the overflow menu first.</summary>
    [Parameter] public Func<Command, int>? CommandPriority { get; set; }

    /// <summary>Gets or sets the name of the overflow button; "More options" in the language of the interface by default.</summary>
    [Parameter] public string? OverflowLabel { get; set; }

    /// <summary>Gets the NSS element name emitted by Toolbar.</summary>
    protected override string ElementName => "Toolbar";

    /// <summary>Validates the parameters and follows the commands.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(LabelPosition)) throw new ArgumentOutOfRangeException(nameof(LabelPosition));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        commands = Commands is null ? [] : [.. Commands];
        if (commands.Any(command => command is null)) throw new ArgumentException("Commands cannot contain null.", nameof(Commands));
        if (observed.SequenceEqual(commands)) return;
        Unobserve();
        foreach (var command in commands.Distinct())
        {
            command.Changed += Refresh;
            observed.Add(command);
        }
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    /// <summary>Adds the toolbar role and the attributes that style the items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "toolbar");
        builder.AddAttribute(11, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(12, "vertical", Orientation == Orientation.Vertical);
        builder.AddAttribute(13, "label-position", LabelPosition.ToString().ToLowerInvariant());
        builder.AddAttribute(14, "size", Vocabulary.Name(Size));
    }

    /// <summary>Adds the command buttons and the free content.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<Toolbar>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "ChildContent", (RenderFragment)AddItems);
        builder.CloseComponent();
    }

    private void AddItems(RenderTreeBuilder builder)
    {
        if (Commands is not null)
        {
            if (Orientation == Orientation.Vertical)
            {
                builder.OpenElement(0, "ToolbarCommands");
                foreach (var command in commands) AddCommandButton(builder, command);
                builder.CloseElement();
            }
            else
            {
                builder.OpenComponent<OverflowContainer<Command>>(2);
                builder.AddComponentParameter(3, nameof(OverflowContainer<Command>.As), "ToolbarCommands");
                builder.AddComponentParameter(4, nameof(OverflowContainer<Command>.Items), commands);
                builder.AddComponentParameter(5, nameof(OverflowContainer<Command>.Priority), CommandPriority);
                builder.AddComponentParameter(6, nameof(OverflowContainer<Command>.ItemTemplate), (RenderFragment<Command>)(command => b => AddCommandButton(b, command)));
                builder.AddComponentParameter(7, nameof(OverflowContainer<Command>.OverflowTemplate), (RenderFragment<IReadOnlyList<Command>>)(hidden => b => AddOverflow(b, hidden)));
                builder.CloseComponent();
            }
        }
        builder.AddContent(10, ChildContent);
    }

    private void AddCommandButton(RenderTreeBuilder builder, Command command)
    {
        var iconOnly = LabelPosition == ToolbarLabelPosition.None && CommandIcon is not null;
        var tooltip = iconOnly ? string.Join(" ", new[] { command.Description ?? command.Text, command.Gesture is { } g ? "(" + g.ToDisplayString() + ")" : null }.Where(s => s is not null)) : null;
        if (command.IsChecked is { } isChecked)
        {
            builder.OpenComponent<ToggleButton>(0);
            builder.SetKey(command);
            builder.AddComponentParameter(1, nameof(ToggleButton.IsPressed), isChecked);
            builder.AddComponentParameter(2, nameof(ToggleButton.IsPressedChanged), EventCallback.Factory.Create<bool>(this, value => command.IsChecked = value));
        }
        else
        {
            builder.OpenComponent<Button>(0);
            builder.SetKey(command);
        }
        builder.AddComponentParameter(3, nameof(ButtonBase.Command), command);
        builder.AddComponentParameter(4, nameof(ButtonBase.Appearance), ControlAppearance.Subtle);
        builder.AddComponentParameter(5, nameof(ButtonBase.Size), Size);
        if (CommandIcon is not null) builder.AddComponentParameter(6, nameof(ButtonBase.Leading), CommandIcon(command));
        if (tooltip is not null) builder.AddComponentParameter(7, nameof(Control.Tooltip), tooltip);
        builder.CloseComponent();
    }

    private void AddOverflow(RenderTreeBuilder builder, IReadOnlyList<Command> hidden)
    {
        builder.OpenComponent<MenuButton>(0);
        builder.AddComponentParameter(1, nameof(MenuButton.Appearance), ControlAppearance.Subtle);
        builder.AddComponentParameter(2, nameof(MenuButton.Size), Size);
        builder.AddComponentParameter(3, nameof(MenuButton.ShowChevron), false);
        builder.AddComponentParameter(4, nameof(MenuButton.Alignment), OverlayAlignment.End);
        builder.AddComponentParameter(5, nameof(Control.AccessibleName), OverflowLabel ?? ControlStrings.Get("More"));
        builder.AddComponentParameter(6, nameof(Control.Tooltip), OverflowLabel ?? ControlStrings.Get("More"));
        builder.AddComponentParameter(7, nameof(Control.Class), "ToolbarOverflowButton");
        builder.AddComponentParameter(8, nameof(ButtonBase.Leading), (RenderFragment)(b =>
        {
            b.OpenElement(0, "ToolbarOverflowGlyph");
            b.AddAttribute(1, "semantic-hidden", true);
            b.AddContent(2, "…");
            b.CloseElement();
        }));
        builder.AddComponentParameter(9, nameof(MenuButton.Menu), (RenderFragment)(menu =>
        {
            foreach (var command in hidden)
            {
                if (command.IsChecked is { } isChecked)
                {
                    menu.OpenComponent<MenuCheckItem>(0);
                    menu.SetKey(command);
                    menu.AddComponentParameter(1, nameof(MenuCheckItem.IsChecked), isChecked);
                    menu.AddComponentParameter(2, nameof(MenuCheckItem.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, value => command.IsChecked = value));
                    menu.AddComponentParameter(3, nameof(MenuItemBase.Command), command);
                    menu.CloseComponent();
                }
                else
                {
                    menu.OpenComponent<MenuItem>(4);
                    menu.SetKey(command);
                    menu.AddComponentParameter(5, nameof(MenuItemBase.Command), command);
                    menu.CloseComponent();
                }
            }
        }));
        builder.CloseComponent();
    }

    private void Unobserve()
    {
        foreach (var command in observed) command.Changed -= Refresh;
        observed.Clear();
    }

    /// <summary>Stops following the commands.</summary>
    public void Dispose() => Unobserve();
}

/// <summary>
/// A line between groups of items in a <see cref="Toolbar" />. It has the <c>separator</c> role and the
/// <c>orientation</c> of the line, across the bar: vertical in a horizontal toolbar.
/// </summary>
public sealed class ToolbarSeparator : Control
{
    [CascadingParameter] internal Toolbar? Toolbar { get; set; }

    /// <summary>Gets the NSS element name emitted by ToolbarSeparator.</summary>
    protected override string ElementName => "ToolbarSeparator";

    /// <summary>Adds the separator role and the orientation of the line.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "separator");
        builder.AddAttribute(11, "orientation", Toolbar?.Orientation == Orientation.Vertical ? "horizontal" : "vertical");
    }
}

/// <summary>Empty space that pushes the following items of a <see cref="Toolbar" /> or <see cref="ActionBar" /> to the far end.</summary>
public sealed class ToolbarSpacer : Control
{
    /// <summary>Gets the NSS element name emitted by ToolbarSpacer.</summary>
    protected override string ElementName => "ToolbarSpacer";

    /// <summary>Hides the spacer from assistive technologies.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "semantic-hidden", true);
}

/// <summary>
/// A named set of related items in a <see cref="Toolbar" />, such as the alignment buttons: it has the <c>group</c> role,
/// takes its name from <see cref="Label" /> and keeps the toolbar's single Tab stop and arrow navigation.
/// </summary>
public sealed class ToolbarGroup : ContentControl
{
    /// <summary>Gets or sets the name of the group for assistive technologies.</summary>
    [Parameter] public string? Label { get; set; }

    [CascadingParameter] internal Toolbar? Toolbar { get; set; }

    /// <summary>Gets the NSS element name emitted by ToolbarGroup.</summary>
    protected override string ElementName => "ToolbarGroup";

    /// <summary>Adds the group role, the name and the orientation of the toolbar.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", Label);
        builder.AddAttribute(12, "orientation", Vocabulary.Name(Toolbar?.Orientation ?? Orientation.Horizontal));
    }
}
