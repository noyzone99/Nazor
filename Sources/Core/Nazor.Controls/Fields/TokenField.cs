using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A field that collects several values as tokens (chips) followed by an editor. <see cref="Values" />
/// (<c>@bind-Values</c>) lists the tokens. Typing filters <see cref="Suggestions" /> (the values already chosen are left
/// out); choosing a suggestion adds it. Enter or a <see cref="Separators" /> character turns the typed text into a token
/// through <see cref="CreateToken" /> (by default the matching suggestion, or the text itself for strings), so pasting
/// "a, b, c" adds three tokens. Backspace in an empty editor removes the last token, and each token has a remove button.
/// With <see cref="MaxTokens" /> the editor becomes read-only once the limit is reached (the <c>full</c> attribute).
/// Built on <see cref="Combobox{TItem}" />: the parts are <c>TokenFieldLeading</c> holding <c>TokenFieldTokens</c> (a
/// list) with one <c>TokenFieldToken</c> per value (<c>TokenFieldTokenText</c> and <c>TokenFieldTokenRemove</c>), then
/// <c>TokenFieldInput</c>, <c>TokenFieldPopup</c>, <c>TokenFieldOption</c> and <c>TokenFieldEmpty</c>.
/// </summary>
/// <typeparam name="TItem">The type of the values.</typeparam>
public sealed class TokenField<TItem> : Control
{
    private readonly ControllableState<IReadOnlyList<TItem>> values = new([]);
    private Combobox<TItem>? combobox;
    private string text = "";

    /// <summary>Gets or sets the chosen values.</summary>
    [Parameter] public IReadOnlyList<TItem>? Values { get; set; }

    /// <summary>Gets or sets the callback invoked when the user adds or removes a value; supports <c>@bind-Values</c>.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> ValuesChanged { get; set; }

    /// <summary>Gets or sets the values suggested while typing.</summary>
    [Parameter] public IReadOnlyList<TItem>? Suggestions { get; set; }

    /// <summary>Gets or sets whether a suggestion matches the typed text; by default its text contains the text, ignoring case.</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>Gets or sets the text of a value; <c>ToString</c> by default.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets how a suggestion is rendered in the list.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets how the content of a token is rendered; its text by default.</summary>
    [Parameter] public RenderFragment<TItem>? TokenTemplate { get; set; }

    /// <summary>
    /// Gets or sets the function turning typed text into a value, or null to reject it. When not set, the suggestion whose
    /// text equals the text is used, and for string values the text itself.
    /// </summary>
    [Parameter] public Func<string, TItem?>? CreateToken { get; set; }

    /// <summary>Gets or sets the characters that end a token while typing; a comma and a semicolon by default.</summary>
    [Parameter] public string Separators { get; set; } = ",;";

    /// <summary>Gets or sets the largest number of tokens; zero means unlimited.</summary>
    [Parameter] public int MaxTokens { get; set; }

    /// <summary>Gets or sets whether the same value can be added more than once.</summary>
    [Parameter] public bool AllowDuplicates { get; set; }

    /// <summary>Gets or sets the largest number of suggestions shown; 8 by default.</summary>
    [Parameter] public int MaxSuggestions { get; set; } = 8;

    /// <summary>Gets or sets content shown in the list when no suggestion matches; null closes the list instead.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets the hint shown while there are no tokens and no text.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether the tokens cannot be changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a value is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the chosen values.</summary>
    public IReadOnlyList<TItem> CurrentValues => values.Value;

    /// <summary>Gets the text being typed.</summary>
    public string Text => text;

    /// <summary>Gets whether the limit of tokens is reached.</summary>
    public bool IsFull => MaxTokens > 0 && values.Value.Count >= MaxTokens;

    /// <summary>Gets the NSS element name emitted by TokenField.</summary>
    protected override string ElementName => "TokenField";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the values.</summary>
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Separators);
        if (MaxTokens < 0) throw new ArgumentOutOfRangeException(nameof(MaxTokens));
        if (MaxSuggestions < 1) throw new ArgumentOutOfRangeException(nameof(MaxSuggestions));
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        values.Sync(Values ?? []);
    }

    /// <summary>Gets the text of a value.</summary>
    /// <param name="item">The value.</param>
    /// <returns>Its text.</returns>
    public string TextOf(TItem item) => ItemText is { } describe ? describe(item) : item?.ToString() ?? "";

    /// <summary>Gets the suggestions offered for the current text.</summary>
    /// <returns>The matching suggestions not chosen yet, at most <see cref="MaxSuggestions" />.</returns>
    public IReadOnlyList<TItem> CurrentSuggestions()
    {
        var query = text.Trim();
        if (Suggestions is null || query.Length == 0 || IsFull) return [];
        return [.. Suggestions.Where(item => (AllowDuplicates || !Contains(item))
            && (Filter is { } filter ? filter(item, query) : TextOf(item).Contains(query, StringComparison.CurrentCultureIgnoreCase))).Take(MaxSuggestions)];
    }

    private bool Contains(TItem item) => values.Value.Contains(item, EqualityComparer<TItem>.Default);

    /// <summary>Delegates the rendering to a combo box primitive named after the control.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var attributes = new Dictionary<string, object?>(AdditionalAttributes ?? new Dictionary<string, object?>())
        {
            ["appearance"] = Appearance.ToString().ToLowerInvariant(),
            ["size"] = Vocabulary.Name(Size),
            ["empty"] = values.Value.Count == 0 && text.Length == 0,
            ["readonly"] = ReadOnly,
            ["full"] = IsFull,
            ["invalid"] = Invalid || Field?.Invalid == true,
            ["__internal_onkeydown#tokens"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync),
            ["__internal_onpointerdown#field"] = EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame),
        };
        var input = new Dictionary<string, object?>
        {
            [nameof(EditableText.ReadOnly)] = ReadOnly || IsFull,
            [nameof(EditableText.Required)] = Required,
            [nameof(EditableText.Invalid)] = Invalid,
            [nameof(Control.AccessibleDescription)] = AccessibleDescription,
            [nameof(Control.DescribedBy)] = DescribedBy,
        };
        OpenDelegatedComponent<Combobox<TItem>>(builder, 0);
        builder.AddComponentParameter(1, nameof(Combobox<TItem>.As), "TokenField");
        builder.AddComponentParameter(2, nameof(Combobox<TItem>.Text), text);
        builder.AddComponentParameter(3, nameof(Combobox<TItem>.TextChanged), EventCallback.Factory.Create<string>(this, TypedAsync));
        builder.AddComponentParameter(4, nameof(Combobox<TItem>.Items), CurrentSuggestions());
        builder.AddComponentParameter(5, nameof(Combobox<TItem>.ItemText), (Func<TItem, string>)TextOf);
        builder.AddComponentParameter(6, nameof(Combobox<TItem>.ItemTemplate), ItemTemplate);
        builder.AddComponentParameter(7, nameof(Combobox<TItem>.OnSelect), EventCallback.Factory.Create<TItem>(this, ChosenAsync));
        builder.AddComponentParameter(8, nameof(Combobox<TItem>.FillOnSelect), false);
        builder.AddComponentParameter(9, nameof(Combobox<TItem>.EmptyContent), EmptyContent);
        builder.AddComponentParameter(10, nameof(Combobox<TItem>.Placeholder), values.Value.Count == 0 ? Placeholder : null);
        builder.AddComponentParameter(11, nameof(Combobox<TItem>.Disabled), Disabled);
        builder.AddComponentParameter(12, nameof(Combobox<TItem>.Leading), (RenderFragment)AddTokens);
        builder.AddComponentParameter(13, nameof(Combobox<TItem>.InputAttributes), input);
        builder.AddComponentParameter(14, nameof(Class), Class);
        builder.AddComponentParameter(15, nameof(Id), Id);
        builder.AddComponentParameter(16, nameof(AccessibleName), AccessibleName);
        builder.AddComponentParameter(17, nameof(LabeledBy), LabeledBy);
        builder.AddComponentParameter(18, nameof(Tooltip), Tooltip);
        builder.AddComponentParameter(19, nameof(Style), Style);
        builder.AddComponentParameter(20, nameof(AdditionalAttributes), attributes);
        builder.AddComponentReferenceCapture(21, component => combobox = (Combobox<TItem>)component);
        builder.CloseComponent();
    }

    /// <summary>Exposes the combo box element as the control's element.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (combobox is not null) Element = combobox.Element;
    }

    private void AddTokens(RenderTreeBuilder builder)
    {
        var current = values.Value;
        if (current.Count == 0) return;
        builder.OpenElement(0, "TokenFieldTokens");
        builder.AddAttribute(1, "role", "list");
        builder.AddAttribute(2, "accessible-name", ControlStrings.Get("SelectedItems"));
        builder.AddEventStopPropagationAttribute(3, "onpointerdown", true);
        for (var i = 0; i < current.Count; i++)
        {
            var index = i;
            var item = current[i];
            var label = TextOf(item);
            builder.OpenElement(10, "TokenFieldToken");
            builder.SetKey(index);
            builder.AddAttribute(11, "role", "listitem");
            builder.AddAttribute(12, "position-in-set", index + 1);
            builder.AddAttribute(13, "size-of-set", current.Count);
            builder.AddAttribute(14, "accessible-name", label);
            builder.OpenElement(15, "TokenFieldTokenText");
            if (TokenTemplate is not null) builder.AddContent(16, TokenTemplate(item));
            else builder.AddContent(17, label);
            builder.CloseElement();
            if (!ReadOnly && !IsInactive)
            {
                builder.OpenElement(20, "TokenFieldTokenRemove");
                builder.AddAttribute(21, "role", "button");
                builder.AddAttribute(22, "accessible-name", ControlStrings.Format("RemoveItem", label));
                builder.AddAttribute(23, "glyph", "minus");
                builder.AddAttribute(24, "tabindex", -1);
                builder.AddAttribute(25, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => RemoveFromButtonAsync(index)));
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    /// <summary>Adds a value as the user would, unless the field is full, read-only or already holds it.</summary>
    /// <param name="item">The value to add.</param>
    /// <returns>true if the value was added.</returns>
    public async Task<bool> AddAsync(TItem item)
    {
        if (IsInactive || ReadOnly || IsFull || item is null || (!AllowDuplicates && Contains(item))) return false;
        await SetValuesAsync([.. values.Value, item]);
        return true;
    }

    /// <summary>Removes the value at an index as its remove button does.</summary>
    /// <param name="index">The index of the token.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task RemoveAtAsync(int index)
    {
        if (IsInactive || ReadOnly || index < 0 || index >= values.Value.Count) return Task.CompletedTask;
        var next = values.Value.ToList();
        next.RemoveAt(index);
        return SetValuesAsync(next);
    }

    /// <summary>Turns the typed text into a token, as Enter does.</summary>
    /// <returns>true if a token was added.</returns>
    public async Task<bool> CommitTextAsync()
    {
        if (text.Trim().Length == 0 || !await CreateAsync(text.Trim())) return false;
        await SetTextAsync("");
        return true;
    }

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) =>
        combobox?.Element.Element is { } root && ControlTree.Descendants(root).FirstOrDefault(e => e.Name == "TokenFieldInput") is { } input
            ? new ElementReference(input).FocusAsync(preventScroll) : Task.CompletedTask;

    private Task<bool> CreateAsync(string typed)
    {
        TItem? item;
        if (CreateToken is { } create) item = create(typed);
        else if (Suggestions is not null && Suggestions.Any(s => string.Equals(TextOf(s), typed, StringComparison.CurrentCultureIgnoreCase)))
            item = Suggestions.First(s => string.Equals(TextOf(s), typed, StringComparison.CurrentCultureIgnoreCase));
        else if (typeof(TItem) == typeof(string)) item = (TItem)(object)typed;
        else item = default;
        return item is null ? Task.FromResult(false) : AddAsync(item);
    }

    private async Task TypedAsync(string typed)
    {
        if (Separators.Length > 0 && typed.IndexOfAny(Separators.ToCharArray()) >= 0)
        {
            var parts = typed.Split(Separators.ToCharArray());
            var rest = new List<string>();
            foreach (var part in parts[..^1])
                if (part.Trim().Length > 0 && !await CreateAsync(part.Trim())) rest.Add(part.Trim());
            rest.Add(parts[^1]);
            var remainder = string.Join(Separators[0].ToString(CultureInfo.InvariantCulture) + " ", rest.Where(r => r.Length > 0)).TrimStart();
            text = remainder;
            StateHasChanged();
            if (combobox is not null) await combobox.SetTextAsync(remainder);
            return;
        }
        await SetTextAsync(typed);
    }

    private async Task ChosenAsync(TItem item)
    {
        await AddAsync(item);
        await SetTextAsync("");
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || ReadOnly || e.Modifiers != KeyModifiers.None) return;
        if (e.Key == "Enter" && text.Trim().Length > 0)
        {
            e.Handled = true;
            await CommitTextAsync();
        }
        else if (e.Key == "Backspace" && text.Length == 0 && values.Value.Count > 0)
        {
            e.Handled = true;
            await RemoveAtAsync(values.Value.Count - 1);
        }
    }

    private async Task RemoveFromButtonAsync(int index)
    {
        await RemoveAtAsync(index);
        await FocusAsync(preventScroll: true);
    }

    private async Task SetTextAsync(string next)
    {
        text = next;
        StateHasChanged();
        if (combobox is null) return;
        await combobox.SetTextAsync(next);
        if (next.Length == 0) await combobox.SetOpenAsync(false);
    }

    private async Task SetValuesAsync(IReadOnlyList<TItem> next)
    {
        values.Set(next);
        StateHasChanged();
        await ValuesChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        IsInactive || !ControlTree.IsFieldFramePress(e, Element) ? Task.CompletedTask : FocusAsync(preventScroll: true);
}
