using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// The meaning of a <see cref="DialogButton" /> in the footer of a dialog, written as the <c>action-role</c> attribute
/// (<c>primary</c>, <c>secondary</c>, <c>cancel</c>, <c>destructive</c>, <c>help</c>) so that each theme orders the
/// buttons by its platform's convention.
/// </summary>
public enum DialogButtonRole
{
    /// <summary>An alternative action, such as Don't Save.</summary>
    Secondary,
    /// <summary>The main action, such as Save; the dialog's default-action policy or an explicit IsDefault selects its Enter behavior.</summary>
    Primary,
    /// <summary>The action that abandons the dialog; Escape and the close button use it.</summary>
    Cancel,
    /// <summary>An action that destroys data, such as Delete; drawn with the danger intent.</summary>
    Destructive,
    /// <summary>An action that explains the dialog without closing it.</summary>
    Help,
}

/// <summary>
/// The shared behavior of the window-local dialogs (<see cref="Dialog" />, <see cref="AlertDialog" />,
/// <see cref="Drawer" />, <see cref="CommandPalette" />), all built on <see cref="DialogContainer" />: an open state that
/// supports <c>@bind-IsOpen</c>, the <see cref="DialogButton" /> actions that close it and report their role through
/// <see cref="OnAction" />, and dismissal routed through the Cancel action: Escape and the close button run the enabled
/// <see cref="DialogButtonRole.Cancel" /> button when there is one, and otherwise invoke <see cref="OnDismiss" /> and
/// close when the dialog allows it.
/// </summary>
public abstract class DialogBase : ComponentBase
{
    private readonly ControllableState<bool> open = new(false);
    private readonly List<DialogButton> buttons = [];
    private bool ambiguousDefaultsReported;

    /// <summary>Gets or sets whether the dialog is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the dialog opens or closes from inside.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets whether Escape dismisses the dialog.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the dialog is dismissed without an action.</summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the callback invoked with the role of the <see cref="DialogButton" /> that closed the dialog.</summary>
    [Parameter] public EventCallback<DialogButtonRole> OnAction { get; set; }

    /// <summary>Gets or sets the element focused when the dialog closes; null uses the one focused before it opened.</summary>
    [Parameter] public ElementReference? ReturnFocus { get; set; }

    /// <summary>Gets or sets a callback that chooses the element focused when the dialog opens.</summary>
    [Parameter] public Func<ElementReference?>? InitialFocus { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the dialog surface.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the identifier of the dialog surface.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Gets or sets NSS declarations for the dialog surface only.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Gets or sets the accessible name; the title names the dialog when null.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>Gets or sets a longer description for assistive technologies.</summary>
    [Parameter] public string? AccessibleDescription { get; set; }

    /// <summary>Gets or sets the identifier of the element that names the dialog.</summary>
    [Parameter] public string? LabeledBy { get; set; }

    /// <summary>Gets or sets space-separated identifiers of the elements that describe the dialog.</summary>
    [Parameter] public string? DescribedBy { get; set; }

    /// <summary>Gets or sets unmatched attributes forwarded to the dialog surface.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>Gets whether the dialog is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the identifier given to the title part, which names the dialog.</summary>
    protected string TitleId { get; } = UniqueId.Create("dialog-title");

    /// <summary>Gets the identifier given to the description part.</summary>
    protected string DescriptionId { get; } = UniqueId.Create("dialog-description");

    /// <summary>Gets the element name of the surface; parts are named after it.</summary>
    protected abstract string ElementName { get; }

    /// <summary>Gets the accessible role of the surface.</summary>
    protected virtual string Role => "dialog";

    /// <summary>Gets where the surface appears.</summary>
    protected virtual DialogPlacement SurfacePlacement => DialogPlacement.Center;

    /// <summary>Gets whether the dialog confines interaction and the focus.</summary>
    protected virtual bool IsModal => true;

    /// <summary>Gets whether a scrim covers the window, or null to follow <see cref="IsModal" />.</summary>
    protected virtual bool? ScrimVisible => null;

    /// <summary>Gets whether a press outside dismisses the dialog.</summary>
    protected virtual bool DismissOnOutside => false;

    /// <summary>Gets whether the Cancel action receives the focus on open.</summary>
    protected virtual bool FocusCancel => false;

    /// <summary>Gets whether the dialog closes on dismissal when it has no Cancel action.</summary>
    protected virtual bool DismissWithoutCancel => true;

    /// <summary>Gets whether a title part is rendered, so the dialog is labeled by it.</summary>
    protected virtual bool HasTitle => false;

    /// <summary>Gets whether a description part is rendered, so the dialog is described by it.</summary>
    protected virtual bool HasDescription => false;

    /// <summary>Gets the role used as the implicit Enter action; Primary by default. Dialog overrides this with its explicit nullable DefaultButton selection.</summary>
    protected virtual DialogButtonRole? DefaultActionRole => DialogButtonRole.Primary;

    /// <summary>Gets the style attribute of the surface.</summary>
    protected virtual string? SurfaceStyle => Style;

    /// <summary>Synchronizes the open state and validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        open.Sync(IsOpen);
        Validate();
    }

    /// <summary>Rejects an open dialog with more than one effective default action after its buttons have registered.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        ValidateDefaultActions();
    }

    /// <summary>Validates the parameters of a derived dialog.</summary>
    protected virtual void Validate()
    {
    }

    /// <summary>Renders the content kept while the dialog is closed and the dialog surface while open or retained for exit.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenRegion(0);
        BuildOutside(builder);
        builder.CloseRegion();
        var attributes = new Dictionary<string, object?>();
        if (AdditionalAttributes is not null)
            foreach (var (name, value) in AdditionalAttributes) attributes[name] = value;
        AddSurfaceAttributes(attributes);
        attributes["__internal_preventDefault_ondismiss"] = true;
        attributes["__internal_ondismiss#dialog"] = EventCallback.Factory.Create<EventArgs>(this, DismissAsync);
        OpenDelegatedComponent<DialogContainer>(builder, 10);
        builder.AddComponentParameter(11, nameof(DialogContainer.As), ElementName);
        builder.AddComponentParameter(12, nameof(DialogContainer.IsOpen), open.Value);
        builder.AddComponentParameter(13, nameof(DialogContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => value ? Task.CompletedTask : CloseAsync()));
        builder.AddComponentParameter(14, nameof(DialogContainer.Modal), IsModal);
        builder.AddComponentParameter(15, nameof(DialogContainer.Placement), SurfacePlacement);
        builder.AddComponentParameter(16, nameof(DialogContainer.ShowScrim), ScrimVisible);
        builder.AddComponentParameter(17, nameof(DialogContainer.Role), Role);
        builder.AddComponentParameter(18, nameof(DialogContainer.PreferCancel), FocusCancel);
        builder.AddComponentParameter(19, nameof(OverlayContainer.CloseOnOutside), DismissOnOutside);
        builder.AddComponentParameter(20, nameof(OverlayContainer.CloseOnEscape), CloseOnEscape);
        builder.AddComponentParameter(21, nameof(OverlayContainer.ReturnFocus), ReturnFocus);
        builder.AddComponentParameter(22, nameof(OverlayContainer.InitialFocus), InitialFocus ?? DefaultInitialFocus);
        builder.AddComponentParameter(23, nameof(Control.Class), Class);
        builder.AddComponentParameter(24, nameof(Control.Id), Id);
        builder.AddComponentParameter(25, nameof(Control.Style), SurfaceStyle);
        builder.AddComponentParameter(26, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(27, nameof(Control.AccessibleDescription), AccessibleDescription);
        builder.AddComponentParameter(28, nameof(Control.LabeledBy), LabeledBy ?? (AccessibleName is null && HasTitle ? TitleId : null));
        builder.AddComponentParameter(29, nameof(Control.DescribedBy), DescribedBy ?? (HasDescription ? DescriptionId : null));
        builder.AddMultipleAttributes(30, attributes);
        builder.AddComponentParameter(31, nameof(DialogContainer.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<CascadingValue<DialogBase>>(0);
            content.AddComponentParameter(1, "Value", this);
            content.AddComponentParameter(2, "IsFixed", false);
            content.AddComponentParameter(3, "ChildContent", (RenderFragment)BuildContent);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }

    /// <summary>Renders content that stays while the dialog is closed, such as a shortcut that opens it.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void BuildOutside(RenderTreeBuilder builder)
    {
    }

    /// <summary>Adds attributes of the surface, such as the size, to the attributes forwarded to it.</summary>
    /// <param name="attributes">The attributes forwarded to the surface.</param>
    protected virtual void AddSurfaceAttributes(IDictionary<string, object?> attributes)
    {
    }

    /// <summary>Renders the parts of the dialog inside the surface.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected abstract void BuildContent(RenderTreeBuilder builder);

    /// <summary>Chooses the element focused on open when <see cref="InitialFocus" /> is null.</summary>
    /// <returns>The element to focus, or null to let the container choose.</returns>
    protected virtual ElementReference? DefaultInitialFocus() => null;

    /// <summary>Finds the element marked <c>autofocus</c> or, failing that, the first element in Tab order inside regions.</summary>
    /// <param name="regions">The regions searched in order.</param>
    /// <returns>The element to focus, or null.</returns>
    protected static ElementReference? FirstFocusable(params ElementReference[] regions)
    {
        var elements = regions.Select(region => region.Element).OfType<ElementNode>().Where(e => !e.IsRemoved).ToList();
        var candidates = elements.SelectMany(ControlTree.Descendants).Where(ControlTree.IsFocusable).ToList();
        var chosen = candidates.FirstOrDefault(e => e.Attributes.GetValueOrDefault("autofocus") is true) ??
            candidates.FirstOrDefault(e => e.Attributes.GetValueOrDefault("tabindex") is not int index || index >= 0);
        return chosen is null ? null : new ElementReference(chosen);
    }

    /// <summary>Opens the dialog.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the dialog without reporting an action.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    /// <summary>
    /// Dismisses the dialog as Escape does: runs the enabled Cancel action when there is one, otherwise invokes
    /// <see cref="OnDismiss" /> and closes when the dialog allows dismissal.
    /// </summary>
    /// <returns>A task that completes when the dialog has handled the dismissal.</returns>
    public async Task DismissAsync()
    {
        if (!open.Value) return;
        if (buttons.FirstOrDefault(button => button.Role == DialogButtonRole.Cancel && !button.IsInactive) is { } cancel)
        {
            await cancel.ActivateAsync(new PointerEventArgs { IsSynthetic = true });
            return;
        }
        if (!DismissWithoutCancel) return;
        await OnDismiss.InvokeAsync();
        await CloseAsync();
    }

    /// <summary>Gets whether the dialog has an enabled Cancel action.</summary>
    protected bool HasCancel => buttons.Any(button => button.Role == DialogButtonRole.Cancel && !button.IsInactive);

    /// <summary>Changes the open state and reports it.</summary>
    /// <param name="value">The new open state.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    protected async Task SetOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        if (!value) buttons.Clear();
        OnOpenChanged(value);
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }

    /// <summary>Lets a derived dialog reset its state when it opens or closes.</summary>
    /// <param name="value">The new open state.</param>
    protected virtual void OnOpenChanged(bool value)
    {
    }

    internal async Task CompleteAsync(DialogButtonRole role)
    {
        await OnAction.InvokeAsync(role);
        await CloseAsync();
    }

    internal void Register(DialogButton button)
    {
        if (!buttons.Contains(button)) buttons.Add(button);
    }

    internal void Unregister(DialogButton button) => buttons.Remove(button);

    internal bool IsDefaultAction(DialogButtonRole role) => DefaultActionRole == role;

    internal void ValidateDefaultActions()
    {
        if (!open.Value || buttons.Count(button => button.DefaultAction) <= 1)
        {
            ambiguousDefaultsReported = false;
            return;
        }
        if (ambiguousDefaultsReported) return;
        ambiguousDefaultsReported = true;
        throw new InvalidOperationException("A dialog can have only one default action. Select DefaultButton or set DialogButton.IsDefault so only one action is effective.");
    }
}

/// <summary>
/// An action in the footer of a dialog: a <see cref="Button" /> inside a <c>DialogButton</c> element that carries the
/// <c>action-role</c> attribute, so themes order the actions (Basic puts Help first and the primary action last), and the
/// <c>dialog-cancel</c> mark for the Cancel action. Activation runs <see cref="OnClick" /> and the
/// <see cref="Command" />, then closes the enclosing dialog and reports the role through its <c>OnAction</c>. A primary
/// action follows the enclosing dialog's default-action policy; the effective default uses the accent appearance unless
/// <see cref="Appearance" /> overrides it. A destructive action uses the danger
/// intent. Without content the button shows the role's usual label (OK, Cancel, Help).
/// </summary>
public sealed class DialogButton : ContentControl, IDisposable
{
    private Command? observed;
    private DialogBase? registered;

    /// <summary>Gets or sets the meaning of the action.</summary>
    [Parameter] public DialogButtonRole Role { get; set; }

    /// <summary>Gets or sets the callback invoked when the action runs.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>Gets or sets a command that supplies the label, the enabled state and the work.</summary>
    [Parameter] public Command? Command { get; set; }

    /// <summary>Gets or sets whether the action rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the action shows work in progress; it then rejects activation.</summary>
    [Parameter] public bool IsBusy { get; set; }

    /// <summary>Gets or sets whether running the action closes the dialog; null closes for every role except Help.</summary>
    [Parameter] public bool? CloseOnClick { get; set; }

    /// <summary>Gets or sets whether this is the default button pressed by Enter; null follows the enclosing dialog's policy, or uses Primary outside a dialog. A dialog rejects multiple effective defaults.</summary>
    [Parameter] public bool? IsDefault { get; set; }

    /// <summary>Gets or sets the appearance; null uses the accent appearance for the effective default action and the standard one otherwise.</summary>
    [Parameter] public ControlAppearance? Appearance { get; set; }

    /// <summary>Gets or sets the intent; null uses the danger intent for destructive actions.</summary>
    [Parameter] public Intent? Intent { get; set; }

    /// <summary>Gets or sets content shown before the label.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the enclosing dialog, supplied automatically.</summary>
    [CascadingParameter] public DialogBase? Dialog { get; set; }

    /// <summary>Gets the NSS element name emitted by DialogButton.</summary>
    protected override string ElementName => "DialogButton";

    internal bool IsInactive => Disabled || IsBusy || Command is { IsEnabled: false };

    internal bool DefaultAction => IsDefault ?? (Dialog is { } dialog ? dialog.IsDefaultAction(Role) : Role == DialogButtonRole.Primary);

    /// <summary>Validates the parameters, follows the command and registers with the dialog.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Role)) throw new ArgumentOutOfRangeException(nameof(Role));
        if (Appearance is { } appearance && !Enum.IsDefined(appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (Intent is { } intent && !Enum.IsDefined(intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (!ReferenceEquals(observed, Command))
        {
            if (observed is not null) observed.Changed -= Refresh;
            observed = Command;
            if (observed is not null) observed.Changed += Refresh;
        }
        if (registered != Dialog) registered?.Unregister(this);
        registered = Dialog;
        registered?.Register(this);
    }

    /// <summary>Validates the enclosing dialog's default action after all actions have registered, including an independently updated button.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender) => registered?.ValidateDefaultActions();

    private void Refresh() => InvokeAsync(StateHasChanged);

    /// <summary>Adds the role and the cancel mark.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "action-role", RoleName(Role));
        builder.AddAttribute(11, "dialog-cancel", Role == DialogButtonRole.Cancel);
    }

    /// <summary>Adds the button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Button>(1000);
        builder.AddComponentParameter(1001, nameof(Button.Appearance), Appearance ?? (DefaultAction ? ControlAppearance.Accent : ControlAppearance.Standard));
        builder.AddComponentParameter(1002, nameof(Button.Intent), Intent ?? (Role == DialogButtonRole.Destructive ? Controls.Intent.Danger : Controls.Intent.Neutral));
        builder.AddComponentParameter(1003, nameof(Button.IsDefault), DefaultAction);
        builder.AddComponentParameter(1004, nameof(Button.Disabled), Disabled || Command is { IsEnabled: false });
        builder.AddComponentParameter(1005, nameof(Button.IsBusy), IsBusy);
        builder.AddComponentParameter(1006, nameof(Button.Leading), Leading);
        builder.AddComponentParameter(1007, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
        builder.AddComponentParameter(1008, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1009, nameof(Control.Tooltip), Tooltip ?? Command?.Description);
        builder.AddComponentParameter(1010, nameof(Button.ChildContent), ChildContent ?? (RenderFragment)(label => label.AddContent(0, Label())));
        builder.CloseComponent();
    }

    private string Label() => Command?.Text ?? Role switch
    {
        DialogButtonRole.Primary => ControlStrings.Get("Ok"),
        DialogButtonRole.Cancel => ControlStrings.Get("Cancel"),
        DialogButtonRole.Help => ControlStrings.Get("Help"),
        _ => "",
    };

    /// <summary>Runs the action as a click does and closes the dialog unless <see cref="CloseOnClick" /> is false.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the action has run.</returns>
    public async Task ActivateAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        await OnClick.InvokeAsync(e);
        if (Command is { } command) await command.ExecuteAsync();
        if ((CloseOnClick ?? Role != DialogButtonRole.Help) && Dialog is { } dialog) await dialog.CompleteAsync(Role);
    }

    internal static string RoleName(DialogButtonRole role) => role switch
    {
        DialogButtonRole.Primary => "primary", DialogButtonRole.Cancel => "cancel", DialogButtonRole.Destructive => "destructive",
        DialogButtonRole.Help => "help", _ => "secondary",
    };

    /// <summary>Stops following the command and leaves the dialog.</summary>
    public void Dispose()
    {
        if (observed is not null) observed.Changed -= Refresh;
        observed = null;
        registered?.Unregister(this);
        registered = null;
    }
}

internal static class OverlayParts
{
    internal static void CloseButton(RenderTreeBuilder builder, int sequence, string name, object receiver, Func<Task> close)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "focusable", true);
        builder.AddAttribute(sequence + 3, "accessible-name", ControlStrings.Get("Close"));
        builder.AddAttribute(sequence + 4, "tooltip", ControlStrings.Get("Close"));
        builder.AddAttribute(sequence + 5, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(receiver, close));
        builder.OpenElement(sequence + 6, name + "Mark");
        builder.AddAttribute(sequence + 7, "semantic-hidden", true);
        builder.AddContent(sequence + 8, "×");
        builder.CloseElement();
        builder.CloseElement();
    }

    internal static void Part(RenderTreeBuilder builder, int sequence, string name, RenderFragment? content)
    {
        builder.OpenElement(sequence, name);
        builder.AddContent(sequence + 1, content);
        builder.CloseElement();
    }
}
