using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A searchable list of every command of an application, opened near the top of the window, usually with Ctrl+K or
/// Ctrl+Shift+P (<see cref="Gesture" />): typing filters the commands by their text and description (whole words first,
/// then any part, then letters in order), Up and Down move the current command while the focus stays in the search
/// field, Enter or a click runs it and closes the palette, Escape closes it. Each result shows the label, the
/// description and the shortcut. Commands come from <see cref="Commands" />, optionally grouped by
/// <see cref="GroupBy" />, and from <see cref="CommandPaletteGroup" /> children; disabled commands are not listed.
/// Parts: <c>CommandPaletteScrim</c>, <c>CommandPalette</c>, the search (<c>CommandPaletteSearch</c>,
/// <c>CommandPaletteSearchInput</c>, <c>CommandPaletteSearchPopup</c>, <c>CommandPaletteSearchOption</c> with
/// <c>current</c>, <c>CommandPaletteSearchEmpty</c>), <c>CommandPaletteGroupHeader</c>, <c>CommandPaletteItem</c>,
/// <c>CommandPaletteItemLabel</c>, <c>CommandPaletteItemDescription</c> and <c>CommandPaletteItemShortcut</c>.
/// </summary>
public sealed class CommandPalette : DialogBase
{
    private readonly List<CommandPaletteGroup> groups = [];
    private string query = "";

    /// <summary>Gets or sets the commands listed by the palette.</summary>
    [Parameter] public IEnumerable<Command>? Commands { get; set; }

    /// <summary>Gets or sets a function that gives the group of each command of <see cref="Commands" />, or null for none.</summary>
    [Parameter] public Func<Command, string?>? GroupBy { get; set; }

    /// <summary>Gets or sets <see cref="CommandPaletteGroup" /> children that add named groups of commands.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets a shortcut that opens and closes the palette while it is rendered, such as Mod+K.</summary>
    [Parameter] public KeyGesture? Gesture { get; set; }

    /// <summary>Gets or sets the hint shown in the empty search field.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets the text shown when no command matches.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>Gets or sets the most matches listed.</summary>
    [Parameter] public int MaxResults { get; set; } = 50;

    /// <summary>Gets or sets the callback invoked after a command has run from the palette.</summary>
    [Parameter] public EventCallback<Command> OnExecute { get; set; }

    /// <summary>Gets the current search text.</summary>
    public string Query => query;

    /// <summary>Gets the NSS element name emitted by CommandPalette.</summary>
    protected override string ElementName => "CommandPalette";

    /// <summary>Gets the top placement.</summary>
    protected override DialogPlacement SurfacePlacement => DialogPlacement.Top;

    /// <summary>Gets true: a press outside closes the palette.</summary>
    protected override bool DismissOnOutside => true;

    /// <summary>Validates the parameters.</summary>
    protected override void Validate()
    {
        if (MaxResults < 1) throw new ArgumentOutOfRangeException(nameof(MaxResults));
        if (Gesture is { Key: null or "" }) throw new ArgumentException("The gesture has no key.", nameof(Gesture));
    }

    /// <summary>Clears the search when the palette opens.</summary>
    /// <param name="value">The new open state.</param>
    protected override void OnOpenChanged(bool value)
    {
        if (value) query = "";
    }

    /// <summary>Renders the groups and the shortcut, which stay while the palette is closed.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildOutside(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<CommandPalette>>(0);
        builder.AddComponentParameter(1, "Value", this);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
        if (Gesture is not { } gesture) return;
        builder.OpenComponent<Shortcut>(10);
        builder.AddComponentParameter(11, nameof(Shortcut.Gesture), gesture);
        builder.AddComponentParameter(12, nameof(Shortcut.OnInvoke), EventCallback.Factory.Create<KeyboardEventArgs>(this, () => Opened ? CloseAsync() : OpenAsync()));
        builder.CloseComponent();
    }

    /// <summary>Adds the accessible name and the Enter fallback.</summary>
    /// <param name="attributes">The attributes forwarded to the surface.</param>
    protected override void AddSurfaceAttributes(IDictionary<string, object?> attributes)
    {
        if (AccessibleName is null && LabeledBy is null) attributes["accessible-name"] = ControlStrings.Get("CommandPalette");
        attributes["__internal_onkeydown#palette"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync);
    }

    /// <summary>Renders the search field and the matching commands.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Combobox<Entry>>(0);
        builder.AddComponentParameter(1, nameof(Combobox<Entry>.As), "CommandPaletteSearch");
        builder.AddComponentParameter(2, nameof(Combobox<Entry>.Text), query);
        builder.AddComponentParameter(3, nameof(Combobox<Entry>.TextChanged), EventCallback.Factory.Create<string>(this, value => query = value));
        builder.AddComponentParameter(4, nameof(Combobox<Entry>.Items), Matches());
        builder.AddComponentParameter(5, nameof(Combobox<Entry>.ItemText), (Func<Entry, string>)(entry => entry.Command.Text));
        builder.AddComponentParameter(6, nameof(Combobox<Entry>.ItemTemplate), (RenderFragment<Entry>)RenderEntry);
        builder.AddComponentParameter(7, nameof(Combobox<Entry>.EmptyContent), (RenderFragment)(empty => empty.AddContent(0, EmptyText ?? ControlStrings.Get("NoResults"))));
        builder.AddComponentParameter(8, nameof(Combobox<Entry>.OnSelect), EventCallback.Factory.Create<Entry>(this, entry => ExecuteAsync(entry.Command)));
        builder.AddComponentParameter(9, nameof(Combobox<Entry>.FillOnSelect), false);
        builder.AddComponentParameter(10, nameof(Combobox<Entry>.IsOpen), true);
        builder.AddComponentParameter(11, nameof(Combobox<Entry>.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => value ? Task.CompletedTask : CloseAsync()));
        builder.AddComponentParameter(12, nameof(Combobox<Entry>.Inline), true);
        builder.AddComponentParameter(13, nameof(Combobox<Entry>.AutoFocus), true);
        builder.AddComponentParameter(14, nameof(Combobox<Entry>.Placeholder), Placeholder ?? ControlStrings.Get("SearchCommands"));
        builder.AddComponentParameter(15, nameof(Control.AccessibleName), Placeholder ?? ControlStrings.Get("SearchCommands"));
        builder.CloseComponent();
    }

    private static RenderFragment RenderEntry(Entry entry) => builder =>
    {
        if (entry.Header is not null)
        {
            builder.OpenElement(0, "CommandPaletteGroupHeader");
            builder.AddAttribute(1, "semantic-hidden", true);
            builder.AddContent(2, entry.Header);
            builder.CloseElement();
        }
        builder.OpenElement(10, "CommandPaletteItem");
        builder.OpenElement(11, "CommandPaletteItemLabel");
        builder.AddContent(12, entry.Command.Text);
        builder.CloseElement();
        if (entry.Command.Description is { } description)
        {
            builder.OpenElement(20, "CommandPaletteItemDescription");
            builder.AddContent(21, description);
            builder.CloseElement();
        }
        if (entry.Command.Gesture is { } gesture)
        {
            builder.OpenComponent<ShortcutLabel>(30);
            builder.AddComponentParameter(31, nameof(ShortcutLabel.Gesture), gesture);
            builder.AddComponentParameter(32, nameof(ShortcutLabel.Class), "CommandPaletteItemShortcut");
            builder.CloseComponent();
        }
        builder.CloseElement();
    };

    /// <summary>Gets the commands that match the current search, in display order.</summary>
    /// <returns>The matching commands.</returns>
    public IReadOnlyList<Command> MatchingCommands() => [.. Matches().Select(entry => entry.Command)];

    private IReadOnlyList<Entry> Matches()
    {
        var sources = new List<(string? Group, Command Command)>();
        foreach (var command in Commands ?? []) sources.Add((GroupBy?.Invoke(command), command));
        foreach (var group in groups)
            foreach (var command in group.Commands ?? []) sources.Add((group.Name, command));
        var scored = sources.Where(source => source.Command.IsEnabled)
            .Select((source, index) => (source.Group, source.Command, Index: index, Score: Score(source.Command, query)))
            .Where(source => source.Score > 0)
            .ToList();
        var groupOrder = scored.Select(source => source.Group).Distinct().ToList();
        var ordered = scored
            .OrderBy(source => groupOrder.IndexOf(source.Group))
            .ThenByDescending(source => source.Score)
            .ThenBy(source => source.Index)
            .Take(MaxResults)
            .ToList();
        var entries = new List<Entry>(ordered.Count);
        string? previous = null;
        for (var i = 0; i < ordered.Count; i++)
        {
            var group = ordered[i].Group;
            entries.Add(new Entry(ordered[i].Command, group is not null && (i == 0 || group != previous) ? group : null));
            previous = group;
        }
        return entries;
    }

    internal static int Score(Command command, string query)
    {
        query = query.Trim();
        if (query.Length == 0) return 1;
        var text = command.Text;
        var compare = CultureInfo.CurrentCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        if (compare.IsPrefix(text, query, options)) return 100;
        var index = compare.IndexOf(text, query, options);
        if (index > 0 && !char.IsLetterOrDigit(text[index - 1])) return 80;
        if (index > 0) return 60;
        if (command.Description is { } description && compare.IndexOf(description, query, options) >= 0) return 40;
        return IsSubsequence(text, query) ? 20 : 0;
    }

    private static bool IsSubsequence(string text, string query)
    {
        var at = 0;
        foreach (var letter in query)
        {
            if (char.IsWhiteSpace(letter)) continue;
            var found = -1;
            for (var i = at; i < text.Length; i++)
            {
                if (char.ToUpperInvariant(text[i]) != char.ToUpperInvariant(letter)) continue;
                found = i;
                break;
            }
            if (found < 0) return false;
            at = found + 1;
        }
        return true;
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Key != "Enter" || e.Modifiers != KeyModifiers.None) return;
        var matches = Matches();
        if (matches.Count == 0) return;
        e.Handled = true;
        await CloseAsync();
        await ExecuteAsync(matches[0].Command);
    }

    private async Task ExecuteAsync(Command command)
    {
        await CloseAsync();
        await command.ExecuteAsync();
        await OnExecute.InvokeAsync(command);
    }

    /// <summary>Runs a command as choosing it in the palette does, then closes the palette.</summary>
    /// <param name="command">The command to run.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public Task RunAsync(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ExecuteAsync(command);
    }

    internal void Register(CommandPaletteGroup group)
    {
        if (groups.Contains(group)) return;
        groups.Add(group);
        StateHasChanged();
    }

    internal void Unregister(CommandPaletteGroup group)
    {
        if (groups.Remove(group)) StateHasChanged();
    }

    internal sealed record Entry(Command Command, string? Header);
}

/// <summary>
/// A named group of commands listed by the enclosing <see cref="CommandPalette" />, in the order of the groups. It renders
/// nothing itself.
/// </summary>
public sealed class CommandPaletteGroup : ComponentBase, IDisposable
{
    private CommandPalette? registered;

    /// <summary>Gets or sets the name shown above the group.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = "";

    /// <summary>Gets or sets the commands of the group.</summary>
    [Parameter] public IEnumerable<Command>? Commands { get; set; }

    /// <summary>Gets or sets the enclosing palette, supplied automatically.</summary>
    [CascadingParameter] public CommandPalette? Palette { get; set; }

    /// <summary>Registers with the palette.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("A command palette group needs a name.", nameof(Name));
        if (Palette is null) throw new InvalidOperationException("CommandPaletteGroup must be inside a CommandPalette.");
        if (registered == Palette) return;
        registered?.Unregister(this);
        registered = Palette;
        registered.Register(this);
    }

    /// <summary>Renders nothing.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    /// <summary>Leaves the palette.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
