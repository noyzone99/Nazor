using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// A key combination such as Ctrl+S, written in the platform's notation by <see cref="ToDisplayString" />. Keys use the
/// names of <see cref="KeyboardEventArgs.Key" /> ("S", "Enter", "ArrowDown", "F2", "Delete"). The
/// <see cref="Primary" /> factory and the "Mod" prefix of <see cref="Parse" /> choose Command on macOS and Ctrl elsewhere,
/// so one declaration follows each platform's convention.
/// </summary>
/// <param name="Key">The key name.</param>
/// <param name="Modifiers">The modifier keys held with it.</param>
public readonly record struct KeyGesture(string Key, KeyModifiers Modifiers = KeyModifiers.None)
{
    /// <summary>Gets the modifier of the platform's primary shortcuts: Meta (Command) on macOS, Control elsewhere.</summary>
    public static KeyModifiers PrimaryModifier => OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

    /// <summary>Creates a gesture with the platform's primary modifier, such as Ctrl+S or ⌘S.</summary>
    /// <param name="key">The key name.</param>
    /// <param name="extra">Additional modifiers, such as Shift.</param>
    /// <returns>The gesture.</returns>
    public static KeyGesture Primary(string key, KeyModifiers extra = KeyModifiers.None) => new(key, PrimaryModifier | extra);

    /// <summary>
    /// Parses a gesture written as modifiers and a key joined by <c>+</c>, for example "Ctrl+Shift+S", "Alt+F4" or
    /// "Mod+S" (the platform's primary modifier). Modifier names are Ctrl, Control, Shift, Alt, Option, Meta, Cmd, Command,
    /// Win, Super and Mod, in any case.
    /// </summary>
    /// <param name="text">The gesture text.</param>
    /// <returns>The gesture.</returns>
    /// <exception cref="FormatException">The text has no key or an unknown modifier.</exception>
    public static KeyGesture Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts[^1].Length == 0)
        {
            if (text.EndsWith("++", StringComparison.Ordinal) || text == "+") parts = [.. parts[..^2], "+"];
            else throw new FormatException($"The gesture '{text}' has no key.");
        }
        var modifiers = KeyModifiers.None;
        foreach (var part in parts[..^1])
        {
            modifiers |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => KeyModifiers.Control,
                "shift" => KeyModifiers.Shift,
                "alt" or "option" => KeyModifiers.Alt,
                "meta" or "cmd" or "command" or "win" or "super" => KeyModifiers.Meta,
                "mod" => PrimaryModifier,
                _ => throw new FormatException($"Unknown modifier '{part}' in the gesture '{text}'."),
            };
        }
        return new KeyGesture(parts[^1], modifiers);
    }

    /// <summary>Gets whether a key event matches this gesture.</summary>
    /// <param name="e">The key event.</param>
    /// <returns>true if the key and the modifiers match.</returns>
    public bool Matches(KeyboardEventArgs e) => e.Modifiers == Modifiers && string.Equals(e.Key, Key, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the key labels in display order for the current platform, for example ["Ctrl", "Shift", "S"] or ["⇧", "⌘", "S"].</summary>
    /// <returns>The labels of the modifiers and the key.</returns>
    public IReadOnlyList<string> DisplayParts()
    {
        var mac = OperatingSystem.IsMacOS();
        var parts = new List<string>(5);
        if (mac)
        {
            if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("⌃");
            if (Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("⌥");
            if (Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("⇧");
            if (Modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("⌘");
        }
        else
        {
            if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(KeyModifiers.Meta)) parts.Add(OperatingSystem.IsWindows() ? "Win" : "Super");
            if (Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        }
        parts.Add(KeyLabel(Key, mac));
        return parts;
    }

    /// <summary>Gets the gesture in the platform's notation, such as "Ctrl+Shift+S" or "⇧⌘S".</summary>
    /// <returns>The display text.</returns>
    public string ToDisplayString() => string.Join(OperatingSystem.IsMacOS() ? "" : "+", DisplayParts());

    /// <summary>Gets the display text of the gesture.</summary>
    /// <returns>The display text.</returns>
    public override string ToString() => ToDisplayString();

    private static string KeyLabel(string key, bool mac) => key switch
    {
        "ArrowUp" => "↑",
        "ArrowDown" => "↓",
        "ArrowLeft" => "←",
        "ArrowRight" => "→",
        "Enter" => mac ? "↩" : "Enter",
        "Escape" => mac ? "⎋" : "Esc",
        "Backspace" => mac ? "⌫" : "Backspace",
        "Delete" => mac ? "⌦" : "Del",
        "Tab" => mac ? "⇥" : "Tab",
        " " or "Space" => mac ? "Space" : "Space",
        "PageUp" => mac ? "⇞" : "PgUp",
        "PageDown" => mac ? "⇟" : "PgDn",
        "Home" => mac ? "↖" : "Home",
        "End" => mac ? "↘" : "End",
        _ when key.Length == 1 => key.ToUpperInvariant(),
        _ => key,
    };
}

/// <summary>
/// An action that several controls present together: a button, a menu item, a toolbar item and a command palette entry
/// can show the same text, shortcut and enabled state, and run the same work. Change a property and every control that
/// shows the command updates; <see cref="ShortcutScope" /> makes its <see cref="Gesture" /> work from the keyboard.
/// Every member must be used from the UI thread.
/// </summary>
public sealed class Command
{
    private string text = "";
    private string? description;
    private KeyGesture? gesture;
    private bool enabled = true;
    private bool? isChecked;

    /// <summary>Creates a command.</summary>
    /// <param name="text">The text shown by controls.</param>
    /// <param name="execute">The work run when the command is invoked.</param>
    /// <param name="gesture">The optional keyboard shortcut.</param>
    public Command(string text, Func<Task> execute, KeyGesture? gesture = null)
    {
        Text = text;
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.gesture = gesture;
    }

    /// <summary>Creates a command that runs synchronous work.</summary>
    /// <param name="text">The text shown by controls.</param>
    /// <param name="execute">The work run when the command is invoked.</param>
    /// <param name="gesture">The optional keyboard shortcut.</param>
    public Command(string text, Action execute, KeyGesture? gesture = null)
        : this(text, () => { execute(); return Task.CompletedTask; }, gesture)
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    /// <summary>Gets the work run by <see cref="ExecuteAsync" />.</summary>
    public Func<Task> Execute { get; }

    /// <summary>Gets or sets the text shown by controls.</summary>
    public string Text { get => text; set => Set(ref text, value ?? throw new ArgumentNullException(nameof(value))); }

    /// <summary>Gets or sets a longer description, shown as a tooltip or in a command palette.</summary>
    public string? Description { get => description; set => Set(ref description, value); }

    /// <summary>Gets or sets the keyboard shortcut.</summary>
    public KeyGesture? Gesture { get => gesture; set => Set(ref gesture, value); }

    /// <summary>Gets or sets whether the command can run; controls showing it are disabled otherwise.</summary>
    public bool IsEnabled { get => enabled; set => Set(ref enabled, value); }

    /// <summary>Gets or sets the checked state of a toggle command, or null for an ordinary command.</summary>
    public bool? IsChecked { get => isChecked; set => Set(ref isChecked, value); }

    /// <summary>Raised after a property changes.</summary>
    public event Action? Changed;

    /// <summary>Runs the command when it is enabled.</summary>
    /// <returns>A task that completes when the work has finished.</returns>
    public Task ExecuteAsync() => enabled ? Execute() : Task.CompletedTask;

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Changed?.Invoke();
    }
}

/// <summary>
/// Makes the shortcuts of a set of commands work while it is rendered: each enabled <see cref="Command" /> with a
/// <see cref="Command.Gesture" /> runs when its keys are pressed anywhere in the window, before the focused control sees
/// them. The latest registration of a gesture wins, so a page can override an application shortcut.
/// </summary>
public sealed class ShortcutScope : ComponentBase, IDisposable
{
    private readonly List<IDisposable> registrations = [];
    private readonly List<Command> observed = [];

    /// <summary>Gets or sets the window's shortcut registry.</summary>
    [Inject] public KeyBindings Bindings { get; set; } = null!;

    /// <summary>Gets or sets the commands whose shortcuts are active.</summary>
    [Parameter] public IEnumerable<Command>? Commands { get; set; }

    /// <summary>Gets or sets whether the shortcuts are active; false keeps them registered but ignored.</summary>
    [Parameter] public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets content rendered unchanged.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Registers the current commands.</summary>
    protected override void OnParametersSet() => Register();

    /// <summary>Renders the child content.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);

    private void Register()
    {
        Unregister();
        foreach (var command in Commands ?? [])
        {
            observed.Add(command);
            command.Changed += Reregister;
            if (command.Gesture is not { } gesture) continue;
            var target = command;
            registrations.Add(Bindings.Register(gesture.Key, gesture.Modifiers,
                EventCallback.Factory.Create<KeyboardEventArgs>(this, e => Invoke(target, e))));
        }
    }

    private Task Invoke(Command command, KeyboardEventArgs e)
    {
        if (!Enabled || !command.IsEnabled) return Task.CompletedTask;
        e.Handled = true;
        return command.ExecuteAsync();
    }

    private void Reregister() => InvokeAsync(Register);

    private void Unregister()
    {
        foreach (var registration in registrations) registration.Dispose();
        registrations.Clear();
        foreach (var command in observed) command.Changed -= Reregister;
        observed.Clear();
    }

    /// <summary>Removes the registrations.</summary>
    public void Dispose() => Unregister();
}

/// <summary>A single keyboard shortcut that invokes a callback while the component is rendered.</summary>
public sealed class Shortcut : ComponentBase, IDisposable
{
    private IDisposable? registration;
    private (KeyGesture, bool)? registered;

    /// <summary>Gets or sets the window's shortcut registry.</summary>
    [Inject] public KeyBindings Bindings { get; set; } = null!;

    /// <summary>Gets or sets the key combination, for example <c>KeyGesture.Parse("Mod+K")</c>.</summary>
    [Parameter, EditorRequired] public KeyGesture Gesture { get; set; }

    /// <summary>Gets or sets the callback invoked when the keys are pressed.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnInvoke { get; set; }

    /// <summary>Gets or sets whether holding the keys repeats the callback.</summary>
    [Parameter] public bool AllowRepeat { get; set; }

    /// <summary>Registers the gesture when it changes.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Gesture.Key)) throw new InvalidOperationException("Shortcut requires a Gesture.");
        if (registered == (Gesture, AllowRepeat)) return;
        registration?.Dispose();
        registration = Bindings.Register(Gesture.Key, Gesture.Modifiers, EventCallback.Factory.Create<KeyboardEventArgs>(this, Invoke), AllowRepeat);
        registered = (Gesture, AllowRepeat);
    }

    private Task Invoke(KeyboardEventArgs e)
    {
        e.Handled = true;
        return OnInvoke.InvokeAsync(e);
    }

    /// <summary>Removes the registration.</summary>
    public void Dispose() => registration?.Dispose();
}
