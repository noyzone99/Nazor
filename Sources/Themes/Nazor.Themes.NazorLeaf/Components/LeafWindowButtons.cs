using Nazor.Controls;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Themes.NazorLeaf;

/// <summary>The side of the title bar on which <see cref="LeafWindowButtons" /> take their buttons from the system layout.</summary>
public enum LeafWindowButtonsSide
{
    /// <summary>The buttons the system places on the right; minimize, maximize and close when the system has no setting.</summary>
    Right,
    /// <summary>The buttons the system places on the left, as with <c>button-layout</c> on GNOME or on macOS.</summary>
    Left,
}

/// <summary>
/// The leaf lights of the Leaf theme: minimize, maximize (restore while maximized) and close drawn by Nazor as three
/// small leaves that take the colors of the four-leaf mark when the pointer rests on the group. They follow the system
/// placement of the caption buttons (<see cref="WindowCommands.Layout" />) for one <see cref="Side" />, so a desktop that
/// puts the buttons on the left gets them on the left, and they disappear while the window shows the system's own
/// buttons (Windows and macOS), where the system draws them.
/// </summary>
/// <remarks>
/// The element <c>LeafWindowButtons</c> (<c>group</c> role, <c>side</c> and <c>maximized</c> attributes) contains one
/// <c>LeafWindowButton</c> per button, with the <c>kind</c> attribute (<c>minimize</c>, <c>maximize</c>,
/// <c>restore</c> or <c>close</c>). Each button holds the <c>LeafWindowButtonLeaf</c> and its
/// <c>LeafWindowButtonGlyph</c>, drawn by <c>LeafWindowButtonStroke</c> parts (<c>stroke</c> = <c>bar</c>,
/// <c>rising</c>, <c>falling</c> or <c>corner</c>, with <c>at</c> = <c>top-end</c> or <c>bottom-start</c> for the
/// corner). The buttons have localized names and tooltips and, like native caption buttons, are not Tab stops unless
/// <see cref="IsTabStop" /> is set. The maximize button is a native Snap Layouts area on Windows.
/// </remarks>
public sealed class LeafWindowButtons : Control
{
    private static readonly IReadOnlyList<WindowButton> DefaultRight = [WindowButton.Minimize, WindowButton.Maximize, WindowButton.Close];
    private WindowCommands? commands;

    /// <summary>Gets or sets the side whose system buttons are drawn; the right side by default.</summary>
    [Parameter] public LeafWindowButtonsSide Side { get; set; }

    /// <summary>Gets or sets the buttons in physical left-to-right order, instead of the system layout of <see cref="Side" />.</summary>
    [Parameter] public IReadOnlyList<WindowButton>? Buttons { get; set; }

    /// <summary>Gets or sets whether the buttons are hidden while the window shows the system caption buttons; true by default.</summary>
    [Parameter] public bool HideWithSystemButtons { get; set; } = true;

    /// <summary>Gets or sets whether the buttons are reached with Tab; false by default, as native caption buttons.</summary>
    [Parameter] public bool IsTabStop { get; set; }

    /// <summary>Gets the NSS element name emitted by LeafWindowButtons.</summary>
    protected override string ElementName => "LeafWindowButtons";

    /// <summary>
    /// Gets the buttons drawn for a side of a caption layout: the layout's own buttons, or minimize, maximize and close on
    /// the right when the layout has none on either side.
    /// </summary>
    /// <param name="layout">The system caption layout.</param>
    /// <param name="side">The side of the title bar.</param>
    /// <returns>The buttons in physical left-to-right order, possibly none.</returns>
    public static IReadOnlyList<WindowButton> ButtonsFor(WindowButtonsLayout layout, LeafWindowButtonsSide side)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (layout.Left.Count == 0 && layout.Right.Count == 0) return side == LeafWindowButtonsSide.Right ? DefaultRight : [];
        return side == LeafWindowButtonsSide.Left ? layout.Left : layout.Right;
    }

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Side)) throw new ArgumentOutOfRangeException(nameof(Side));
        if (Buttons is null) return;
        if (Buttons.Any(button => !Enum.IsDefined(button))) throw new ArgumentOutOfRangeException(nameof(Buttons));
        if (Buttons.Distinct().Count() != Buttons.Count) throw new ArgumentException("LeafWindowButtons cannot repeat a button.", nameof(Buttons));
    }

    /// <summary>Renders the buttons inside a <see cref="WindowCommands" /> that follows the window.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<WindowCommands>(0);
        builder.AddComponentParameter(1, nameof(WindowCommands.ChildContent), (RenderFragment<WindowCommands>)(context => inner =>
        {
            commands = context;
            if (HideWithSystemButtons && context.ShowsSystemButtons) return;
            if ((Buttons ?? ButtonsFor(context.Layout, Side)).Count == 0) return;
            base.BuildRenderTree(inner);
        }));
        builder.CloseComponent();
    }

    /// <summary>Adds the group role, its name, the side and the window state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", LeafStrings.Get("WindowControls"));
        builder.AddAttribute(12, "side", Side == LeafWindowButtonsSide.Left ? "left" : "right");
        builder.AddAttribute(13, "maximized", commands?.IsMaximized == true);
    }

    /// <summary>Adds the buttons.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (commands is not { } window) return;
        foreach (var button in Buttons ?? ButtonsFor(window.Layout, Side))
        {
            var kind = button switch
            {
                WindowButton.Minimize => "minimize",
                WindowButton.Maximize => window.IsMaximized ? "restore" : "maximize",
                _ => "close",
            };
            var name = LeafStrings.Get(kind switch { "minimize" => "Minimize", "maximize" => "Maximize", "restore" => "Restore", _ => "Close" });
            var enabled = button switch
            {
                WindowButton.Minimize => window.CanMinimize,
                WindowButton.Maximize => window.CanMaximize,
                _ => true,
            };
            builder.OpenElement(1000, "LeafWindowButton");
            builder.SetKey(button);
            builder.AddAttribute(1001, "role", "button");
            builder.AddAttribute(1002, "kind", kind);
            builder.AddAttribute(1003, "accessible-name", name);
            builder.AddAttribute(1004, "tooltip", name);
            builder.AddAttribute(1005, "disabled", !enabled);
            builder.AddAttribute(1006, "focusable", enabled);
            builder.AddAttribute(1007, "tabindex", IsTabStop ? null : -1);
            builder.AddAttribute(1008, "snap-layout-area", button == WindowButton.Maximize && enabled && OperatingSystem.IsWindows());
            builder.AddAttribute(1009, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => RunAsync(button)));
            builder.OpenElement(1010, "LeafWindowButtonLeaf");
            builder.AddAttribute(1011, "kind", kind);
            builder.AddAttribute(1012, "semantic-hidden", true);
            builder.OpenElement(1013, "LeafWindowButtonGlyph");
            builder.AddAttribute(1014, "kind", kind);
            switch (kind)
            {
                case "minimize":
                    Stroke(builder, "bar");
                    break;
                case "maximize":
                    Stroke(builder, "rising");
                    Stroke(builder, "corner", "top-end");
                    break;
                case "restore":
                    Stroke(builder, "rising");
                    Stroke(builder, "corner", "bottom-start");
                    break;
                default:
                    Stroke(builder, "rising");
                    Stroke(builder, "falling");
                    break;
            }
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private static void Stroke(RenderTreeBuilder builder, string stroke, string? at = null)
    {
        builder.OpenElement(1020, "LeafWindowButtonStroke");
        builder.AddAttribute(1021, "stroke", stroke);
        builder.AddAttribute(1022, "at", at);
        builder.CloseElement();
    }

    private Task RunAsync(WindowButton button)
    {
        if (commands is not { } window) return Task.CompletedTask;
        return button switch
        {
            WindowButton.Minimize => window.MinimizeAsync(),
            WindowButton.Maximize => window.ToggleMaximizeAsync(),
            _ => window.CloseAsync(),
        };
    }
}
