using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>A button of <see cref="CaptionButtons" />.</summary>
public enum CaptionButton
{
    /// <summary>Shows help; it runs <see cref="CaptionButtons.OnHelp" />.</summary>
    Help,
    /// <summary>Minimizes the window.</summary>
    Minimize,
    /// <summary>Maximizes the window, or restores it when it is maximized.</summary>
    Maximize,
    /// <summary>Closes the window.</summary>
    Close,
}

/// <summary>
/// Minimize, maximize (restore while maximized) and close buttons drawn by Nazor for a custom <see cref="TitleBar" />,
/// plus an optional help button, in the order of <see cref="Buttons" />. They follow the window through
/// <see cref="WindowCommands" />: minimize and maximize are disabled when the window does not allow them, the maximize
/// button shows the restore glyph and name while the window is maximized and, on Windows, is a native Snap Layouts area.
/// The element <c>CaptionButtons</c> (<c>group</c> role, <c>maximized</c> attribute) contains one
/// <c>CaptionButtonsButton</c> per button, with the <c>kind</c> attribute (<c>help</c>, <c>minimize</c>,
/// <c>maximize</c>, <c>restore</c> or <c>close</c>) and a <c>CaptionButtonsGlyph</c> drawn by the theme. The buttons have
/// localized accessible names and tooltips and, like native caption buttons, are not Tab stops unless
/// <see cref="IsTabStop" /> is set. While the window shows the system's own buttons they are hidden, unless
/// <see cref="HideWithSystemButtons" /> is false.
/// </summary>
public sealed class CaptionButtons : Control
{
    private static readonly IReadOnlyList<CaptionButton> DefaultOrder = [CaptionButton.Minimize, CaptionButton.Maximize, CaptionButton.Close];
    private WindowCommands? commands;

    /// <summary>Gets or sets the buttons in physical left-to-right order; minimize, maximize and close by default.</summary>
    [Parameter] public IReadOnlyList<CaptionButton>? Buttons { get; set; }

    /// <summary>Gets or sets the callback invoked by the help button.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnHelp { get; set; }

    /// <summary>Gets or sets whether the buttons are hidden while the window shows the system caption buttons; true by default.</summary>
    [Parameter] public bool HideWithSystemButtons { get; set; } = true;

    /// <summary>Gets or sets whether the buttons are reached with Tab; false by default, as native caption buttons.</summary>
    [Parameter] public bool IsTabStop { get; set; }

    /// <summary>Gets the NSS element name emitted by CaptionButtons.</summary>
    protected override string ElementName => "CaptionButtons";

    /// <summary>Converts the system's caption layout of one side into buttons, to follow the platform convention.</summary>
    /// <param name="buttons">The system buttons of one side, such as <c>WindowCommands.Layout.Right</c>.</param>
    /// <returns>The same buttons in the same order.</returns>
    public static IReadOnlyList<CaptionButton> FromLayout(IReadOnlyList<WindowButton> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        return [.. buttons.Select(button => button switch
        {
            WindowButton.Minimize => CaptionButton.Minimize,
            WindowButton.Maximize => CaptionButton.Maximize,
            _ => CaptionButton.Close,
        })];
    }

    /// <summary>Validates the buttons.</summary>
    protected override void OnParametersSet()
    {
        if (Buttons is null) return;
        if (Buttons.Any(button => !Enum.IsDefined(button))) throw new ArgumentOutOfRangeException(nameof(Buttons));
        if (Buttons.Distinct().Count() != Buttons.Count) throw new ArgumentException("CaptionButtons cannot repeat a button.", nameof(Buttons));
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
            base.BuildRenderTree(inner);
        }));
        builder.CloseComponent();
    }

    /// <summary>Adds the group role, its name and the window state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(13, "source", "nazor");
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Get("WindowControls"));
        builder.AddAttribute(12, "maximized", commands?.IsMaximized == true);
    }

    /// <summary>Adds the buttons.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (commands is not { } window) return;
        foreach (var button in Buttons ?? DefaultOrder)
        {
            var kind = button switch
            {
                CaptionButton.Help => "help",
                CaptionButton.Minimize => "minimize",
                CaptionButton.Maximize => window.IsMaximized ? "restore" : "maximize",
                _ => "close",
            };
            var name = ControlStrings.Get(kind switch { "help" => "Help", "minimize" => "Minimize", "maximize" => "Maximize", "restore" => "Restore", _ => "Close" });
            var enabled = button switch
            {
                CaptionButton.Minimize => window.CanMinimize,
                CaptionButton.Maximize => window.CanMaximize,
                _ => true,
            };
            builder.OpenElement(1000, "CaptionButtonsButton");
            builder.SetKey(button);
            builder.AddAttribute(1001, "role", "button");
            builder.AddAttribute(1002, "kind", kind);
            builder.AddAttribute(1003, "accessible-name", name);
            builder.AddAttribute(1004, "tooltip", name);
            builder.AddAttribute(1005, "disabled", !enabled);
            builder.AddAttribute(1006, "focusable", enabled);
            builder.AddAttribute(1007, "tabindex", IsTabStop ? null : -1);
            builder.AddAttribute(1008, "snap-layout-area", button == CaptionButton.Maximize && enabled && OperatingSystem.IsWindows());
            builder.AddAttribute(1009, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => RunAsync(button, e)));
            builder.OpenElement(1010, "CaptionButtonsGlyph");
            builder.AddAttribute(1011, "kind", kind);
            builder.AddAttribute(1012, "semantic-hidden", true);
            switch (kind)
            {
                case "minimize":
                    builder.AddAttribute(1013, "glyph", "minus");
                    break;
                case "restore":
                    builder.OpenElement(1014, "CaptionButtonsGlyphBack");
                    builder.CloseElement();
                    break;
                case "close":
                    builder.OpenElement(1015, "CaptionButtonsGlyphStroke");
                    builder.AddAttribute(1016, "stroke", "rising");
                    builder.CloseElement();
                    builder.OpenElement(1017, "CaptionButtonsGlyphStroke");
                    builder.AddAttribute(1018, "stroke", "falling");
                    builder.CloseElement();
                    break;
                case "help":
                    builder.AddContent(1019, "?");
                    break;
            }
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private Task RunAsync(CaptionButton button, PointerEventArgs e)
    {
        if (commands is not { } window) return Task.CompletedTask;
        return button switch
        {
            CaptionButton.Help => OnHelp.InvokeAsync(e),
            CaptionButton.Minimize => window.MinimizeAsync(),
            CaptionButton.Maximize => window.ToggleMaximizeAsync(),
            _ => window.CloseAsync(),
        };
    }
}
