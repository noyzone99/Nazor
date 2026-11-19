using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Specifies where a search field's search icon appears beside its editor.</summary>
public enum SearchIconPlacement
{
    /// <summary>Uses the position selected by the active theme.</summary>
    ThemeDefault,
    /// <summary>Places the icon before the editor, after any scope selector.</summary>
    Start,
    /// <summary>Places the icon after the editor and clear action.</summary>
    End,
}

/// <summary>
/// A search editor with typed, templated suggestions. Text edits, suggestion choices and explicit query submissions
/// have distinct callbacks. Enter submits the current text, choosing the highlighted suggestion first when present;
/// clicking a suggestion only chooses it. The query icon can also submit. Asynchronous providers are canceled when
/// superseded, dismissed or disposed, and late results are ignored even when a provider ignores cancellation.
/// Default suggestion text highlights matching passages; an empty list shows a localized no-results label.
/// </summary>
/// <typeparam name="TItem">The type of suggestions.</typeparam>
public sealed class SearchField<TItem> : Control, IDisposable
{
    /// <inheritdoc />
    protected override int? ElementTabIndex => null;

    private readonly ControllableState<string> text = new("");
    private readonly ControllableState<string?> scope = new(null);
    private Combobox<TItem>? combobox;
    private EditableText? editor;
    private CancellationTokenSource? request;
    private IReadOnlyList<TItem> loaded = [];
    private Func<string, CancellationToken, Task<IReadOnlyList<TItem>>>? previousProvider;
    private int previousMaximum;
    private int generation;
    private bool loadQueued;
    private bool disposed;
    private bool previousInactive;

    /// <summary>Gets or sets the search text.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when an edit, clear or suggestion choice changes the text; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Gets or sets the callback invoked with the text on explicit query submission, after <see cref="QuerySubmitted" />.</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>Gets or sets the callback invoked for user text edits and clearing, independently of suggestion choices and external value assignments.</summary>
    [Parameter] public EventCallback<string> TextEdited { get; set; }

    /// <summary>Gets or sets the callback invoked when a suggestion is chosen; choosing alone does not submit a query.</summary>
    [Parameter] public EventCallback<TItem> SuggestionChosen { get; set; }

    /// <summary>Gets or sets the callback invoked by Enter or the query action, including a suggestion chosen by that Enter.</summary>
    [Parameter] public EventCallback<SearchQuery<TItem>> QuerySubmitted { get; set; }

    /// <summary>Gets or sets the hint shown while the text is empty; "Search" by default.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets the search icon position; the active theme chooses its position by default.</summary>
    [Parameter] public SearchIconPlacement IconPlacement { get; set; }

    /// <summary>Gets or sets suggestions offered when they contain the text, ignoring case.</summary>
    [Parameter] public IReadOnlyList<TItem>? Suggestions { get; set; }

    /// <summary>Gets or sets a function returning the suggestions for a text; it takes precedence over <see cref="Suggestions" />.</summary>
    [Parameter] public Func<string, IReadOnlyList<TItem>>? SuggestionProvider { get; set; }

    /// <summary>Gets or sets an asynchronous suggestion provider, taking precedence over synchronous providers and local suggestions. Providers should observe cancellation; stale results and errors are also rejected by request identity.</summary>
    [Parameter] public Func<string, CancellationToken, Task<IReadOnlyList<TItem>>>? AsyncSuggestionProvider { get; set; }

    /// <summary>Gets or sets the text copied from a chosen suggestion and used for default rendering and local matching; <c>ToString</c> is the default.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets the presentation of each suggestion, without changing the text copied on selection.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets whether suggestion text highlights matches of the trimmed query, ignoring case in the current culture. True by default; custom item templates can use <see cref="RenderHighlightedText" />.</summary>
    [Parameter] public bool HighlightMatches { get; set; } = true;

    /// <summary>Gets or sets an inline fragment for each matched passage, preserving its original text. Null renders a <see cref="Mark" />. Applies to default suggestion text and <see cref="RenderHighlightedText" /> when <see cref="HighlightMatches" /> is true.</summary>
    [Parameter] public RenderFragment<string>? HighlightTemplate { get; set; }

    /// <summary>Gets or sets local matching; by default a suggestion's text contains the trimmed query, ignoring case. Providers own their filtering.</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>Gets or sets the largest number of suggestions shown; 8 by default.</summary>
    [Parameter] public int MaxSuggestions { get; set; } = 8;

    /// <summary>Gets or sets content shown in the list when no suggestion matches, overriding <see cref="EmptyText" /> and the localized default label.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets the text shown when no suggestion matches. Null uses the localized "No results" label; <see cref="EmptyContent" /> takes precedence.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>Gets or sets pending-request content; the localized loading label is the default.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Gets or sets failed-request content; the localized error label is the default. Details are available through <see cref="SuggestionError" />.</summary>
    [Parameter] public RenderFragment? ErrorContent { get; set; }

    /// <summary>Gets or sets whether the search icon is an invokable query action. Enter can submit regardless of this setting.</summary>
    [Parameter] public bool IsQueryButtonEnabled { get; set; } = true;

    /// <summary>Gets or sets the scopes the search can be limited to; null or empty hides the scope choice.</summary>
    [Parameter] public IReadOnlyList<string>? Scopes { get; set; }

    /// <summary>Gets or sets the chosen scope.</summary>
    [Parameter] public string? Scope { get; set; }

    /// <summary>Gets or sets the callback invoked when the user chooses another scope; supports <c>@bind-Scope</c>.</summary>
    [Parameter] public EventCallback<string?> ScopeChanged { get; set; }

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the editor takes the focus when it appears.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current text.</summary>
    public string Text => text.Value;

    /// <summary>Gets the chosen scope.</summary>
    public string? CurrentScope => scope.Value;

    /// <summary>Gets whether the field offers suggestions.</summary>
    public bool HasSuggestions => Suggestions is not null || SuggestionProvider is not null || AsyncSuggestionProvider is not null;

    /// <summary>Gets whether an asynchronous suggestion request is queued or pending.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Gets the error of the latest accepted asynchronous request, or null after a new request or dismissal.</summary>
    public Exception? SuggestionError { get; private set; }

    /// <summary>Gets whether the suggestion popup is open.</summary>
    public bool Opened => combobox?.Opened == true;

    /// <summary>Gets the NSS element name emitted by SearchField.</summary>
    protected override string ElementName => "SearchField";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the text and the scope.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(IconPlacement)) throw new ArgumentOutOfRangeException(nameof(IconPlacement));
        if (MaxSuggestions < 1) throw new ArgumentOutOfRangeException(nameof(MaxSuggestions));
        var changed = text.Sync(Value ?? "");
        changed |= scope.Sync(Scope);
        changed |= !Equals(previousProvider, AsyncSuggestionProvider) || previousMaximum != MaxSuggestions || previousInactive != IsInactive;
        previousProvider = AsyncSuggestionProvider;
        previousMaximum = MaxSuggestions;
        previousInactive = IsInactive;
        if (changed) QueueSuggestions();
    }

    /// <summary>Gets the suggestions offered for the current text.</summary>
    /// <returns>The suggestions, at most <see cref="MaxSuggestions" />.</returns>
    public IReadOnlyList<TItem> CurrentSuggestions()
    {
        if (AsyncSuggestionProvider is not null) return loaded;
        var query = text.Value.Trim();
        if (SuggestionProvider is { } provider) return [.. provider(text.Value).Take(MaxSuggestions)];
        if (Suggestions is null || query.Length == 0) return [];
        return [.. Suggestions.Where(item => Filter is { } filter ? filter(item, query) : TextOf(item).Contains(query, StringComparison.CurrentCultureIgnoreCase)).Take(MaxSuggestions)];
    }

    /// <summary>Gets the text of a suggestion.</summary>
    /// <param name="item">The suggestion.</param>
    /// <returns>The text displayed and copied on selection.</returns>
    public string TextOf(TItem item) => ItemText is { } describe ? describe(item) : item?.ToString() ?? "";

    /// <summary>Renders inline text with nonoverlapping matches of the current trimmed query highlighted, for use inside a text block or paragraph in a custom <see cref="ItemTemplate" />. Matching ignores case in the current culture and preserves the original text. An empty or culture-ignorable query does not add highlights.</summary>
    /// <param name="value">The original suggestion text to render.</param>
    /// <returns>An inline fragment using <see cref="HighlightTemplate" /> or <see cref="Mark" />; plain text when <see cref="HighlightMatches" /> is false.</returns>
    public RenderFragment RenderHighlightedText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return builder =>
        {
            var query = text.Value.Trim();
            var offset = 0;
            while (HighlightMatches && query.Length > 0 && offset < value.Length)
            {
                var index = CultureInfo.CurrentCulture.CompareInfo.IndexOf(value.AsSpan(offset), query.AsSpan(), CompareOptions.IgnoreCase, out var length);
                if (index < 0 || length == 0) break;
                builder.OpenRegion(0);
                builder.AddContent(1, value.Substring(offset, index));
                var match = value.Substring(offset + index, length);
                if (HighlightTemplate is { } template) builder.AddContent(2, template(match));
                else
                {
                    builder.OpenComponent<Mark>(3);
                    builder.AddComponentParameter(4, nameof(Mark.ChildContent), (RenderFragment)(content => content.AddContent(0, match)));
                    builder.CloseComponent();
                }
                builder.CloseRegion();
                offset += index + length;
            }
            builder.AddContent(5, value[offset..]);
        };
    }

    private RenderFragment DefaultSuggestion(TItem item) => builder =>
    {
        builder.OpenComponent<TextBlock>(0);
        builder.AddComponentParameter(1, nameof(TextBlock.ChildContent), RenderHighlightedText(TextOf(item)));
        builder.CloseComponent();
    };

    /// <summary>Renders the plain search box, or delegates to a combo box when there are suggestions.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!HasSuggestions)
        {
            combobox = null;
            base.BuildRenderTree(builder);
            return;
        }
        editor = null;
        var attributes = new Dictionary<string, object?>(AdditionalAttributes ?? new Dictionary<string, object?>())
        {
            ["appearance"] = Appearance.ToString().ToLowerInvariant(),
            ["size"] = Vocabulary.Name(Size),
            ["empty"] = text.Value.Length == 0,
            ["invalid"] = Field?.Invalid == true,
            ["suggestions"] = true,
            ["search-icon-placement"] = IconPlacementName,
            ["busy"] = IsLoading,
            ["__internal_onfocusout#search"] = EventCallback.Factory.Create<FocusEventArgs>(this, FocusOutAsync),
        };
        var input = new Dictionary<string, object?>
        {
            [nameof(EditableText.InputMode)] = "search",
            [nameof(Control.AccessibleDescription)] = AccessibleDescription,
            [nameof(Control.DescribedBy)] = DescribedBy,
            [nameof(Control.TabIndex)] = TabIndex,
            ["__internal_onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync),
        };
        var suggestions = CurrentSuggestions();
        OpenDelegatedComponent<Combobox<TItem>>(builder, 2000);
        builder.AddComponentParameter(2001, nameof(Combobox<TItem>.As), "SearchField");
        builder.AddComponentParameter(2002, nameof(Combobox<TItem>.Text), text.Value);
        builder.AddComponentParameter(2003, nameof(Combobox<TItem>.TextChanged), EventCallback.Factory.Create<string>(this, TypedAsync));
        builder.AddComponentParameter(2004, nameof(Combobox<TItem>.Items), suggestions);
        builder.AddComponentParameter(2005, nameof(Combobox<TItem>.ItemText), (Func<TItem, string>)TextOf);
        builder.AddComponentParameter(2006, nameof(Combobox<TItem>.OnSelect), EventCallback.Factory.Create<TItem>(this, ChosenAsync));
        builder.AddComponentParameter(2007, nameof(Combobox<TItem>.Placeholder), Placeholder ?? ControlStrings.Get("Search"));
        builder.AddComponentParameter(2008, nameof(Combobox<TItem>.Disabled), Disabled);
        builder.AddComponentParameter(2009, nameof(Combobox<TItem>.AutoFocus), AutoFocus);
        builder.AddComponentParameter(2010, nameof(Combobox<TItem>.Leading), (RenderFragment)AddLeading);
        builder.AddComponentParameter(2011, nameof(Combobox<TItem>.InputAdornment), (RenderFragment)AddMark);
        builder.AddComponentParameter(2012, nameof(Combobox<TItem>.Trailing), (RenderFragment)AddTrailing);
        builder.AddComponentParameter(2013, nameof(Combobox<TItem>.EmptyContent), SuggestionStateContent);
        builder.AddComponentParameter(2014, nameof(Combobox<TItem>.InputAttributes), input);
        builder.AddComponentParameter(2015, nameof(Class), Class);
        builder.AddComponentParameter(2016, nameof(Id), Id);
        builder.AddComponentParameter(2017, nameof(AccessibleName), AccessibleName);
        builder.AddComponentParameter(2018, nameof(LabeledBy), LabeledBy);
        builder.AddComponentParameter(2019, nameof(Tooltip), Tooltip);
        builder.AddComponentParameter(2020, nameof(Style), Style);
        builder.AddComponentParameter(2021, nameof(AdditionalAttributes), attributes);
        builder.AddComponentParameter(2022, nameof(Combobox<TItem>.ItemTemplate), ItemTemplate ?? DefaultSuggestion);
        builder.AddComponentParameter(2023, nameof(Combobox<TItem>.FillOnSelect), false);
        builder.AddComponentParameter(2024, nameof(Combobox<TItem>.IsOpenChanged), EventCallback.Factory.Create<bool>(this, OpenChanged));
        builder.AddComponentReferenceCapture(2025, component => combobox = (Combobox<TItem>)component);
        builder.CloseComponent();
    }

    /// <summary>Exposes the combo box element as the control's element.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (combobox is not null) Element = combobox.Element;
    }

    /// <summary>Starts queued suggestion requests after rendering, so editing and dismissal remain responsive while a provider is pending.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the queued request is accepted, rejected or canceled.</returns>
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsInactive && combobox?.Opened == true) return combobox.SetOpenAsync(false);
        if (!loadQueued) return Task.CompletedTask;
        loadQueued = false;
        return LoadSuggestionsAsync();
    }

    /// <summary>Adds the frame states of the plain search box.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "empty", text.Value.Length == 0);
        builder.AddAttribute(14, "invalid", Field?.Invalid == true);
        builder.AddAttribute(15, "__internal_onpointerdown#field", EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame));
        builder.AddAttribute(16, "search-icon-placement", IconPlacementName);
        builder.AddAttribute(17, "__internal_onfocusout#search", EventCallback.Factory.Create<FocusEventArgs>(this, FocusOutAsync));
    }

    /// <summary>Adds the parts of the plain search box.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "SearchFieldLeading");
        AddLeading(builder);
        builder.CloseElement();
        builder.OpenRegion(1005);
        AddMark(builder);
        builder.CloseRegion();
        builder.OpenComponent<EditableText>(1010);
        builder.AddComponentParameter(1011, nameof(EditableText.As), "SearchFieldInput");
        builder.AddComponentParameter(1012, nameof(EditableText.Value), text.Value);
        builder.AddComponentParameter(1013, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, TypedAsync));
        builder.AddComponentParameter(1014, nameof(EditableText.Placeholder), Placeholder ?? ControlStrings.Get("Search"));
        builder.AddComponentParameter(1015, nameof(EditableText.Disabled), Disabled);
        builder.AddComponentParameter(1016, nameof(EditableText.Role), "searchbox");
        builder.AddComponentParameter(1017, nameof(EditableText.InputMode), "search");
        builder.AddComponentParameter(1018, nameof(EditableText.AutoFocus), AutoFocus);
        builder.AddComponentParameter(1019, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1020, nameof(Control.AccessibleDescription), AccessibleDescription);
        builder.AddComponentParameter(1021, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1022, nameof(Control.DescribedBy), DescribedBy);
        builder.AddComponentParameter(1023, nameof(Control.TabIndex), TabIndex);
        builder.AddComponentParameter(1024, nameof(Control.Id), Id is null ? null : Id + "-input");
        builder.AddComponentParameter(1025, "__internal_onkeydown#search", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddComponentReferenceCapture(1026, component => editor = (EditableText)component);
        builder.CloseComponent();
        builder.OpenElement(1030, "SearchFieldTrailing");
        AddTrailing(builder);
        builder.CloseElement();
    }

    private void AddLeading(RenderTreeBuilder builder)
    {
        if (Scopes is { Count: > 0 } scopes)
        {
            builder.OpenElement(0, "SearchFieldScope");
            builder.AddEventStopPropagationAttribute(1, "onpointerdown", true);
            builder.OpenComponent<Select<string>>(2);
            builder.AddComponentParameter(3, nameof(Select<string>.Items), scopes);
            builder.AddComponentParameter(4, nameof(Select<string>.Value), scope.Value);
            builder.AddComponentParameter(5, nameof(Select<string>.ValueChanged), EventCallback.Factory.Create<string>(this, ScopeChosenAsync));
            builder.AddComponentParameter(6, nameof(Select<string>.Disabled), IsInactive);
            builder.AddComponentParameter(7, nameof(Select<string>.Appearance), FieldAppearance.Plain);
            builder.AddComponentParameter(8, nameof(Select<string>.Size), Size);
            builder.AddComponentParameter(9, nameof(Select<string>.Placeholder), ControlStrings.Get("SearchScope"));
            builder.AddComponentParameter(10, nameof(Control.AccessibleName), ControlStrings.Get("SearchScope"));
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private string IconPlacementName => IconPlacement == SearchIconPlacement.ThemeDefault ? "theme-default" : IconPlacement.ToString().ToLowerInvariant();

    private void AddMark(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "SearchFieldMark");
        builder.AddAttribute(1, "glyph", "ring");
        builder.AddAttribute(2, "ring-fraction", 1.0);
        builder.AddAttribute(3, "semantic-hidden", !IsQueryButtonEnabled);
        if (IsQueryButtonEnabled)
        {
            builder.AddAttribute(4, "role", "button");
            builder.AddAttribute(5, "accessible-name", ControlStrings.Get("Search"));
            builder.AddAttribute(6, "tabindex", -1);
            builder.AddAttribute(7, "disabled", IsInactive);
            builder.AddAttribute(8, "preserve-focus-on-press", true);
            builder.AddAttribute(9, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, SearchAsync));
        }
        builder.CloseElement();
    }

    private void AddTrailing(RenderTreeBuilder builder)
    {
        if (text.Value.Length == 0 || IsInactive) return;
        builder.OpenElement(0, "SearchFieldClear");
        builder.AddAttribute(1, "role", "button");
        builder.AddAttribute(2, "accessible-name", ControlStrings.Get("Clear"));
        builder.AddAttribute(3, "glyph", "minus");
        builder.AddAttribute(4, "tabindex", -1);
        builder.AddAttribute(5, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClearAsync));
        builder.CloseElement();
    }

    /// <summary>Clears the text as the clear button does and reports it.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task ClearAsync()
    {
        if (IsInactive) return;
        CancelSuggestions();
        await SetTextAsync("", edited: true);
        if (combobox is not null) await combobox.SetOpenAsync(false);
        await FocusAsync(preventScroll: true);
    }

    /// <summary>Raises <see cref="OnSearch" /> with the current text, as Enter does.</summary>
    /// <returns>A task that completes when the callback has finished.</returns>
    public async Task SearchAsync()
    {
        if (IsInactive) return;
        CancelSuggestions();
        if (combobox is not null) await combobox.SetOpenAsync(false);
        await QuerySubmitted.InvokeAsync(new(text.Value, default, false));
        await OnSearch.InvokeAsync(text.Value);
    }

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false)
    {
        if (editor is not null) return editor.Element.FocusAsync(preventScroll);
        return combobox?.Element.Element is { } root && ControlTree.Descendants(root).FirstOrDefault(e => e.Name == "SearchFieldInput") is { } input
            ? new ElementReference(input).FocusAsync(preventScroll) : Task.CompletedTask;
    }

    private async Task TypedAsync(string value)
    {
        if (IsInactive || !await SetTextAsync(value, edited: true)) return;
        QueueSuggestions();
    }

    private async Task<bool> SetTextAsync(string value, bool edited)
    {
        if (!text.Set(value)) return false;
        StateHasChanged();
        await ValueChanged.InvokeAsync(value);
        if (edited) await TextEdited.InvokeAsync(value);
        return true;
    }

    private async Task ChosenAsync(TItem chosen)
    {
        if (IsInactive) return;
        CancelSuggestions();
        await SetTextAsync(TextOf(chosen), edited: false);
        if (combobox is not null) await combobox.SetTextAsync(text.Value);
        await SuggestionChosen.InvokeAsync(chosen);
    }

    private async Task ScopeChosenAsync(string? chosen)
    {
        if (!scope.Set(chosen)) return;
        StateHasChanged();
        await ScopeChanged.InvokeAsync(chosen);
        QueueSuggestions();
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || e.Modifiers != KeyModifiers.None) return;
        if (e.Key == "Escape" && (Opened || IsLoading))
        {
            e.Handled = true;
            CancelSuggestions();
            if (combobox is not null) await combobox.SetOpenAsync(false);
            return;
        }
        if (e.Key == "Escape" && text.Value.Length > 0)
        {
            e.Handled = true;
            await ClearAsync();
            return;
        }
        if (e.Key != "Enter") return;
        e.Handled = true;
        var suggestions = CurrentSuggestions();
        if (combobox is { Opened: true, CurrentIndex: >= 0 } combo && combo.CurrentIndex < suggestions.Count)
        {
            var chosen = suggestions[combo.CurrentIndex];
            await combo.ChooseAsync(combo.CurrentIndex);
            await QuerySubmitted.InvokeAsync(new(text.Value, chosen, true));
            await OnSearch.InvokeAsync(text.Value);
        }
        else await SearchAsync();
    }

    private void OpenChanged(bool open)
    {
        if (!open) CancelSuggestions();
    }

    private Task FocusOutAsync(FocusEventArgs e)
    {
        for (var node = e.RelatedTarget; node is not null; node = node.ParentElement)
            if (ReferenceEquals(node, Element.Element)) return Task.CompletedTask;
        CancelSuggestions();
        return combobox?.SetOpenAsync(false) ?? Task.CompletedTask;
    }

    private RenderFragment? SuggestionStateContent => IsLoading ? LoadingContent ?? (b => b.AddContent(0, ControlStrings.Get("Loading")))
        : SuggestionError is not null ? ErrorContent ?? (b => b.AddContent(0, ControlStrings.Get("Error")))
        : EmptyContent ?? (b => b.AddContent(0, EmptyText ?? ControlStrings.Get("NoResults")));

    private void CancelSuggestions()
    {
        generation++;
        request?.Cancel();
        request = null;
        loadQueued = false;
        loaded = [];
        IsLoading = false;
        SuggestionError = null;
    }

    private void QueueSuggestions()
    {
        CancelSuggestions();
        loadQueued = !disposed && !IsInactive && AsyncSuggestionProvider is not null;
        IsLoading = loadQueued;
        StateHasChanged();
    }

    /// <summary>Refreshes asynchronous suggestions for the current text without opening a dismissed popup. Synchronous and local sources are refreshed on rendering.</summary>
    /// <returns>A task that completes when the request finishes; provider errors are exposed through <see cref="SuggestionError" />.</returns>
    public Task RefreshSuggestionsAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        QueueSuggestions();
        loadQueued = false;
        return LoadSuggestionsAsync();
    }

    private async Task LoadSuggestionsAsync()
    {
        if (disposed || IsInactive || AsyncSuggestionProvider is not { } provider) return;
        var version = generation;
        var cancellation = request = new CancellationTokenSource();
        var query = text.Value;
        var maximum = MaxSuggestions;
        try
        {
            var results = await provider(query, cancellation.Token) ?? throw new InvalidOperationException("The suggestion provider returned no results collection.");
            if (disposed || cancellation.IsCancellationRequested) return;
            var bounded = results.Take(maximum).ToArray();
            await InvokeAsync(() =>
            {
                if (disposed || generation != version || cancellation.IsCancellationRequested) return;
                loaded = bounded;
                IsLoading = false;
                request = null;
                StateHasChanged();
            });
        }
        catch (Exception error)
        {
            if (disposed || cancellation.IsCancellationRequested) return;
            await InvokeAsync(() =>
            {
                if (disposed || generation != version || cancellation.IsCancellationRequested) return;
                loaded = [];
                SuggestionError = error;
                IsLoading = false;
                request = null;
                StateHasChanged();
            });
        }
        finally { cancellation.Dispose(); }
    }

    /// <summary>Cancels pending requests and ignores all subsequent provider completions.</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CancelSuggestions();
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        IsInactive || !ControlTree.IsFieldFramePress(e, Element)
            ? Task.CompletedTask : FocusAsync(preventScroll: true);
}
