namespace Nazor;

/// <summary>An immutable ordered menu. Item event subscriptions belong to the application.</summary>
public sealed class SystemTrayMenu
{
    /// <summary>Gets a shared menu with no entries.</summary>
    public static SystemTrayMenu Empty { get; } = new([]);
    /// <summary>Gets the immutable ordered entries copied from the supplied collection.</summary>
    public IReadOnlyList<SystemTrayMenuItem> Items { get; }
    /// <summary>Creates a validated SystemTrayMenu instance.</summary>
    public SystemTrayMenu(IEnumerable<SystemTrayMenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var copy = items.ToArray();
        if (copy.Any(item => item is null)) throw new ArgumentException("Menu items cannot be null.", nameof(items));
        Items = Array.AsReadOnly(copy);
    }
}

/// <summary>Base of the closed family of typed tray menu entries.</summary>
public abstract class SystemTrayMenuItem
{
    private protected SystemTrayMenuItem() { }
}

/// <summary>Presentation shared by commands, toggles and submenu headings.</summary>
public abstract class SystemTrayLabeledItem : SystemTrayMenuItem, ITrayLabel
{
    /// <summary>Gets the nonempty menu label without NUL.</summary>
    public string Text { get; }
    /// <summary>Gets whether this entry can be invoked.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Gets whether this entry is included in the menu.</summary>
    public bool Visible { get; init; } = true;
    /// <summary>Gets the optional menu icon; null means no icon. Image loading is asynchronous.</summary>
    public ImageSource? Icon { get; init; }
    private protected SystemTrayLabeledItem(string text)
    {
        ValidateText(text, nameof(text));
        Text = text;
    }
    internal static void ValidateText(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Contains('\0')) throw new ArgumentException("Text cannot contain NUL.", name);
    }
}

/// <summary>An executable native menu command with its own Invoked event.</summary>
public sealed class SystemTrayCommand : SystemTrayLabeledItem
{
    /// <summary>Gets the application command identifier, unique within this tray menu tree.</summary>
    public string Id { get; }
    /// <summary>Occurs when the user invokes this command on the UI thread.</summary>
    public event Action? Invoked;
    /// <summary>Creates a validated SystemTrayCommand instance.</summary>
    public SystemTrayCommand(string id, string text) : base(text) { ValidateText(id, nameof(id)); Id = id; }
    internal void Invoke() => Invoked?.Invoke();
}

/// <summary>A native separator with no command or toggle state.</summary>
public sealed class SystemTraySeparator : SystemTrayMenuItem { }

/// <summary>A labeled entry opening another typed menu.</summary>
public sealed class SystemTraySubmenu : SystemTrayLabeledItem
{
    /// <summary>Gets the immutable menu opened by this heading.</summary>
    public SystemTrayMenu Menu { get; }
    /// <summary>Creates a validated SystemTraySubmenu instance.</summary>
    public SystemTraySubmenu(string text, SystemTrayMenu menu) : base(text) => Menu = menu ?? throw new ArgumentNullException(nameof(menu));
}

/// <summary>Controlled checkbox. The event requests a value; publish a new menu to accept it.</summary>
public sealed class SystemTrayCheckBox : SystemTrayLabeledItem
{
    /// <summary>Gets the application command identifier, unique within this tray menu tree.</summary>
    public string Id { get; }
    /// <summary>Gets the declared checked state; activation requests a change without mutating it.</summary>
    public bool IsChecked { get; }
    /// <summary>Requests the new boolean value. Publish a new menu to accept the request.</summary>
    public event Action<bool>? CheckedChangeRequested;
    /// <summary>Creates a validated SystemTrayCheckBox instance.</summary>
    public SystemTrayCheckBox(string id, string text, bool isChecked) : base(text)
    { ValidateText(id, nameof(id)); Id = id; IsChecked = isChecked; }
    internal void Invoke() => CheckedChangeRequested?.Invoke(!IsChecked);
}

/// <summary>One typed radio choice; it can only be included inside a radio group.</summary>
public sealed class SystemTrayRadioOption<TValue> : ITrayLabel where TValue : notnull
{
    /// <summary>Gets the typed value represented by this choice.</summary>
    public TValue Value { get; }
    /// <summary>Gets the nonempty native menu label.</summary>
    public string Text { get; }
    /// <summary>Gets whether this choice can be selected.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Gets whether this choice is visible.</summary>
    public bool Visible { get; init; } = true;
    /// <summary>Gets the optional image rendered by the native menu host.</summary>
    public ImageSource? Icon { get; init; }
    /// <summary>Creates a typed choice with a non-null value and a valid label.</summary>
    public SystemTrayRadioOption(TValue value, string text)
    {
        ArgumentNullException.ThrowIfNull(value);
        SystemTrayLabeledItem.ValidateText(text, nameof(text));
        Value = value;
        Text = text;
    }
}

internal interface ITrayLabel
{
    string Text { get; }
    bool Enabled { get; }
    bool Visible { get; }
    ImageSource? Icon { get; }
}

/// <summary>Base representation of an inline typed radio group.</summary>
public abstract class SystemTrayRadioGroup : SystemTrayMenuItem
{
    private protected SystemTrayRadioGroup() { }
    internal abstract IEnumerable<TrayRadioChoice> Choices();
}

internal sealed record TrayRadioChoice(ITrayLabel Item, bool Selected, Action Invoke);

/// <summary>A controlled radio group expanded inline, with exactly one selected value.</summary>
public sealed class SystemTrayRadioGroup<TValue> : SystemTrayRadioGroup where TValue : notnull
{
    /// <summary>Gets the declared selection, which must match exactly one option.</summary>
    public TValue SelectedValue { get; }
    /// <summary>Gets the immutable radio choices with distinct values.</summary>
    public IReadOnlyList<SystemTrayRadioOption<TValue>> Options { get; }
    /// <summary>Requests a typed selection. The declared selection remains unchanged until the app publishes a new menu.</summary>
    public event Action<TValue>? SelectionChangeRequested;
    /// <summary>Creates a validated SystemTrayRadioGroup instance.</summary>
    public SystemTrayRadioGroup(TValue selectedValue, IEnumerable<SystemTrayRadioOption<TValue>> options)
    {
        ArgumentNullException.ThrowIfNull(selectedValue);
        ArgumentNullException.ThrowIfNull(options);
        var copy = options.ToArray();
        if (copy.Length == 0 || copy.Any(option => option is null)) throw new ArgumentException("A radio group requires non-null options.", nameof(options));
        if (copy.Select(option => option.Value).Distinct().Count() != copy.Length) throw new ArgumentException("Radio values must be unique.", nameof(options));
        if (!copy.Any(option => EqualityComparer<TValue>.Default.Equals(option.Value, selectedValue))) throw new ArgumentException("The selected value must belong to the group.", nameof(selectedValue));
        SelectedValue = selectedValue;
        Options = Array.AsReadOnly(copy);
    }
    internal override IEnumerable<TrayRadioChoice> Choices() => Options.Select(option => new TrayRadioChoice(option,
        EqualityComparer<TValue>.Default.Equals(option.Value, SelectedValue), () => SelectionChangeRequested?.Invoke(option.Value)));
}
