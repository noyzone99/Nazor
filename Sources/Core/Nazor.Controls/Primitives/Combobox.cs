using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// A text editor with a list of suggestions that the user moves through while the focus stays in the text: typing changes
/// <see cref="Text" /> (<c>@bind-Text</c>) and opens the list, Up and Down move the current suggestion, Enter or a click
/// chooses it (<see cref="OnSelect" />), Escape closes the list and then clears the text. Enter selection requires no
/// modifiers. Pointer selection preserves editor focus unless suggestion content moves it explicitly. Arrow navigation stops at
/// the ends and scrolls the current suggestion into view. The field frame forwards primary pointer presses to the editor,
/// including padding and decorative content. The caller filters <see cref="Items" /> from the text.
/// Parts: the editor (<c>{name}Input</c>), optional <see cref="Leading" /> and
/// <see cref="Trailing" /> content, the popup (<c>{name}Popup</c>), its options (<c>{name}Option</c>, with
/// <c>current</c>) and <c>{name}Empty</c>. Combo boxes, searches with suggestions, token fields and command palettes are
/// built on it.
/// </summary>
/// <typeparam name="TItem">The type of the suggestions.</typeparam>
public sealed class Combobox<TItem> : Control
{
    private readonly ControllableState<string> text = new("");
    private readonly ControllableState<bool> open = new(false);
    private int current = -1;
    private EditableText? editor;
    private bool scrollCurrent;

    /// <summary>Gets or sets the element name, <c>Combobox</c> by default.</summary>
    [Parameter] public string As { get; set; } = "Combobox";

    /// <summary>Gets or sets the text of the editor.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Gets or sets the callback invoked on every edit.</summary>
    [Parameter] public EventCallback<string> TextChanged { get; set; }

    /// <summary>Gets or sets the suggestions currently offered.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets the text of a suggestion, used when <see cref="ItemTemplate" /> is null and when one is chosen.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets how each suggestion is rendered.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the predicate identifying committed selected items independently of the current suggestion. Null uses the current suggestion as selected.</summary>
    [Parameter] public Func<TItem, bool>? IsItemSelected { get; set; }

    /// <summary>Gets or sets content shown when there are no suggestions and the list is open.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets the callback invoked with the chosen suggestion.</summary>
    [Parameter] public EventCallback<TItem> OnSelect { get; set; }

    /// <summary>Gets or sets whether choosing a suggestion copies its text into the editor.</summary>
    [Parameter] public bool FillOnSelect { get; set; } = true;

    /// <summary>Gets or sets whether the list is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the list opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets whether the list opens when the editor receives the focus.</summary>
    [Parameter] public bool OpenOnFocus { get; set; }

    /// <summary>Gets or sets the preferred position of an anchored suggestion list relative to the editor.</summary>
    [Parameter] public DropDownPlacement DropDownPlacement { get; set; }

    /// <summary>Gets or sets the positive preferred minimum number of visible suggestions in an anchored list, or null for no minimum preference. It must be at most a specified <see cref="PreferredMaxVisibleItems" />.</summary>
    [Parameter] public int? PreferredMinVisibleItems { get; set; }

    /// <summary>Gets or sets the positive preferred maximum number of visible suggestions in an anchored list, or null to use the styled size. Available space can reduce the viewport.</summary>
    [Parameter] public int? PreferredMaxVisibleItems { get; set; }

    /// <summary>Gets or sets the hint shown while the text is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether the combo box rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the editor takes the focus when it appears.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>Gets or sets content shown before the editor, such as a search mark.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets adornment content emitted directly beside the editor, so a theme can position it independently of the leading and trailing groups.</summary>
    [Parameter] public RenderFragment? InputAdornment { get; set; }

    /// <summary>Gets or sets content shown after the editor, such as a chevron or a clear button.</summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets whether the popup is rendered inline below the editor rather than as an anchored overlay.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>
    /// Gets or sets extra parameters and attributes of the editor, such as <c>ReadOnly</c>, <c>Required</c>, <c>Invalid</c>
    /// or a tagged internal event handler; they must not repeat the parameters the combo box sets itself (the text, the
    /// role, the placeholder, the disabled state, the naming and the focus behavior).
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, object?>? InputAttributes { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the index of the current suggestion, or -1.</summary>
    public int CurrentIndex => current;

    /// <summary>Gets whether the list is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Synchronizes the text and the open state.</summary>
    protected override void OnParametersSet()
    {
        DropDownOptions.Validate(DropDownPlacement, PreferredMinVisibleItems, PreferredMaxVisibleItems);
        text.Sync(Text ?? "");
        open.Sync(IsOpen);
        if (current >= Items.Count) current = Items.Count - 1;
    }

    /// <summary>Adds the expanded and disabled states and forwards frame presses to the editor.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "expanded", open.Value);
        builder.AddAttribute(11, "disabled", Disabled || Field?.Disabled == true);
        builder.AddAttribute(12, "__internal_onpointerdown#field", EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame));
    }

    /// <summary>Adds the editor, the side content and the popup.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, As + "Leading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        builder.AddContent(1005, InputAdornment);
        builder.OpenComponent<EditableText>(1010);
        builder.AddComponentParameter(1011, nameof(EditableText.As), As + "Input");
        builder.AddComponentParameter(1012, nameof(EditableText.Value), text.Value);
        builder.AddComponentParameter(1013, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, TypedAsync));
        builder.AddComponentParameter(1014, nameof(EditableText.Placeholder), Placeholder);
        builder.AddComponentParameter(1015, nameof(EditableText.Disabled), Disabled);
        builder.AddComponentParameter(1016, nameof(EditableText.Role), "combobox");
        builder.AddComponentParameter(1017, nameof(EditableText.AutoFocus), AutoFocus);
        builder.AddComponentParameter(1018, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1019, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1020, nameof(Control.Id), Id is null ? null : Id + "-input");
        builder.AddComponentParameter(1021, "expanded", open.Value);
        builder.AddComponentParameter(1022, "__internal_onkeydown#combobox", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddComponentParameter(1023, "__internal_onfocus#combobox", EventCallback.Factory.Create<FocusEventArgs>(this, () => OpenOnFocus ? SetOpenAsync(true) : Task.CompletedTask));
        builder.AddMultipleAttributes(1024, InputAttributes);
        builder.AddComponentReferenceCapture(1025, component => editor = (EditableText)component);
        builder.CloseComponent();
        if (Trailing is not null)
        {
            builder.OpenElement(1030, As + "Trailing");
            builder.AddContent(1031, Trailing);
            builder.CloseElement();
        }
        if (Inline)
        {
            if (!open.Value) return;
            builder.OpenElement(1040, As + "Popup");
            builder.AddAttribute(1041, "role", "listbox");
            builder.AddAttribute(1042, "preserve-focus-on-press", true);
            builder.AddAttribute(1043, "anchor-interaction", true);
            RenderOptions(builder);
            builder.CloseElement();
            return;
        }
        builder.OpenComponent<PopoverContainer>(1050);
        builder.AddComponentParameter(1051, nameof(PopoverContainer.As), As + "Popup");
        builder.AddComponentParameter(1052, nameof(PopoverContainer.IsOpen), open.Value);
        builder.AddComponentParameter(1053, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => value ? Task.CompletedTask : SetOpenAsync(false)));
        builder.AddComponentParameter(1054, nameof(PopoverContainer.Anchor), Element);
        builder.AddComponentParameter(1055, nameof(PopoverContainer.MatchAnchorWidth), true);
        builder.AddComponentParameter(1056, nameof(PopoverContainer.FocusOnOpen), false);
        builder.AddComponentParameter(1057, nameof(PopoverContainer.Role), "listbox");
        builder.AddComponentParameter(1058, nameof(PopoverContainer.ChildContent), (RenderFragment)RenderOptions);
        builder.AddComponentParameter(1059, nameof(PopoverContainer.AdditionalAttributes),
            new Dictionary<string, object?>(DropDownOptions.Attributes(DropDownPlacement, PreferredMinVisibleItems, PreferredMaxVisibleItems))
            {
                ["preserve-focus-on-press"] = true,
            });
        builder.AddComponentParameter(1060, nameof(PopoverContainer.AllowAnchorInteraction), true);
        builder.CloseComponent();
    }

    private void RenderOptions(RenderTreeBuilder builder)
    {
        if (Items.Count == 0 && EmptyContent is not null)
        {
            builder.OpenElement(0, As + "Empty");
            builder.AddAttribute(1, "role", "text");
            builder.AddContent(2, EmptyContent);
            builder.CloseElement();
            return;
        }
        for (var i = 0; i < Items.Count; i++)
        {
            var index = i;
            var item = Items[i];
            var selected = IsItemSelected?.Invoke(item) ?? index == current;
            builder.OpenElement(10, As + "Option");
            builder.SetKey(index);
            builder.AddAttribute(11, "role", "option");
            builder.AddAttribute(12, "current", index == current);
            builder.AddAttribute(13, "selected", selected);
            builder.AddAttribute(14, "position-in-set", index + 1);
            builder.AddAttribute(15, "size-of-set", Items.Count);
            builder.AddAttribute(16, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ChooseAsync(index)));
            builder.AddAttribute(17, "__internal_onpointerenter", EventCallback.Factory.Create<PointerEventArgs>(this, () => Highlight(index)));
            if (index == current || DropDownPlacement == DropDownPlacement.Center && selected)
                builder.AddAttribute(18, "__internal_onresize#combobox", EventCallback.Factory.Create<ResizeEventArgs>(this, ScrollCurrentIntoView));
            if (ItemTemplate is not null) builder.AddContent(19, ItemTemplate(item));
            else builder.AddContent(20, ItemText is { } describe ? describe(item) : item?.ToString());
            builder.CloseElement();
        }
    }

    /// <summary>Reveals a keyboard-highlighted suggestion or the initially selected centered option after rendering without moving editor focus.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender) => ScrollCurrentIntoView();

    private void ScrollCurrentIntoView()
    {
        if (!scrollCurrent || Element.Element is not { } root) return;
        var options = ControlTree.Descendants(root).Where(node => node.Name == As + "Option");
        var option = options.FirstOrDefault(node => node.Attributes.GetValueOrDefault("current") is true) ??
            (DropDownPlacement == DropDownPlacement.Center ? options.FirstOrDefault(node => node.Attributes.GetValueOrDefault("selected") is true) : null);
        if (option is null) return;
        var reference = new ElementReference(option);
        if (reference.GetBounds() is not { Width: > 0, Height: > 0 }) return;
        scrollCurrent = false;
        reference.ScrollIntoView();
    }

    private void Highlight(int index, bool scroll = false)
    {
        scrollCurrent = scroll;
        if (current == index) return;
        current = index;
        StateHasChanged();
    }

    private async Task TypedAsync(string value)
    {
        if (!text.Set(value)) return;
        current = -1;
        scrollCurrent = false;
        await TextChanged.InvokeAsync(value);
        await SetOpenAsync(true);
        StateHasChanged();
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled) return;
        switch (e.Key)
        {
            case "ArrowDown" or "ArrowUp" when (e.Modifiers & KeyModifiers.Alt) != 0:
                e.Handled = true;
                await SetOpenAsync(e.Key == "ArrowDown");
                return;
            case "ArrowDown" or "ArrowUp" when e.Modifiers == KeyModifiers.None:
                e.Handled = true;
                if (!open.Value)
                {
                    await SetOpenAsync(true);
                    if (Items.Count > 0) Highlight(e.Key == "ArrowDown" ? 0 : Items.Count - 1, scroll: true);
                    return;
                }
                if (Items.Count == 0) return;
                Highlight(e.Key == "ArrowDown" ? Math.Min(current + 1, Items.Count - 1) : current < 0 ? Items.Count - 1 : Math.Max(current - 1, 0), scroll: true);
                return;
            case "Enter" when e.Modifiers == KeyModifiers.None && open.Value && current >= 0:
                e.Handled = true;
                await ChooseAsync(current);
                return;
            case "Escape" when open.Value:
                e.Handled = true;
                await SetOpenAsync(false);
                return;
            case "Escape" when text.Value.Length > 0:
                e.Handled = true;
                await TypedAsync("");
                await SetOpenAsync(false);
                return;
        }
    }

    /// <summary>
    /// Replaces the text of the editor, even when the user has typed something the owner rejects, and reports it through
    /// <see cref="TextChanged" /> without opening the list.
    /// </summary>
    /// <param name="value">The new text.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetTextAsync(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var changed = text.Set(value);
        if (editor is not null) await editor.SetTextAsync(value);
        if (!changed) return;
        current = -1;
        scrollCurrent = false;
        StateHasChanged();
        await TextChanged.InvokeAsync(value);
    }

    /// <summary>Chooses the suggestion at an index as the user would.</summary>
    /// <param name="index">The index in <see cref="Items" />.</param>
    /// <returns>A task that completes when the choice has been reported.</returns>
    public async Task ChooseAsync(int index)
    {
        if (index < 0 || index >= Items.Count) return;
        var item = Items[index];
        if (FillOnSelect && ItemText is { } describe && text.Set(describe(item))) await TextChanged.InvokeAsync(text.Value);
        current = -1;
        await SetOpenAsync(false);
        await OnSelect.InvokeAsync(item);
        StateHasChanged();
    }

    /// <summary>Opens or closes the list.</summary>
    /// <param name="value">Whether the list should be open.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetOpenAsync(bool value)
    {
        if (Disabled && value) return;
        if (!open.Set(value)) return;
        if (value && DropDownPlacement == DropDownPlacement.Center) scrollCurrent = true;
        if (!value)
        {
            current = -1;
            scrollCurrent = false;
        }
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        Disabled || Field?.Disabled == true || !ControlTree.IsFieldFramePress(e, Element) || editor is null
            ? Task.CompletedTask : editor.Element.FocusAsync(preventScroll: true);
}
