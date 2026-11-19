namespace Nazor.Input;

/// <summary>Window shortcuts, evaluated before the focused control. Dispose registrations with their owner.</summary>
public sealed class KeyBindings
{
    private readonly List<Binding> bindings = [];

    /// <summary>
    /// Registers a keyboard gesture for a command and returns a handle that removes the registration.
    /// </summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <param name="command">The callback invoked when the registered key and modifiers match.</param>
    /// <param name="allowRepeat">Whether repeated key-down events are allowed to invoke this command.</param>
    /// <returns>A disposable registration handle.</returns>
    public IDisposable Register(string key, KeyModifiers modifiers, EventCallback<KeyboardEventArgs> command, bool allowRepeat = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var binding = new Binding(this, key, modifiers, command, allowRepeat);
        bindings.Add(binding);
        return binding;
    }

    /// <summary>
    /// Matches a keyboard event against registered gestures, giving the most recent registration precedence. A
    /// matching binding consumes disallowed repeats without returning a callback delegate.
    /// </summary>
    /// <param name="args">The keyboard event whose key, modifiers, and repeat state are matched against registered
    /// bindings.</param>
    /// <param name="command">When this method returns, contains the command produced by the operation.</param>
    /// <returns>true if the key and modifiers match a registered binding, including a consumed repeat; otherwise,
    /// false.</returns>
    public bool TryGetCommand(KeyboardEventArgs args, out EventCallback<KeyboardEventArgs> command)
    {
        var binding = bindings.LastOrDefault(b => b.Modifiers == args.Modifiers && string.Equals(b.Key, args.Key, StringComparison.OrdinalIgnoreCase));
        command = default;
        if (binding is null) return false;
        if (!args.Repeat || binding.AllowRepeat) command = binding.Command;
        return true;
    }

    private sealed class Binding(KeyBindings owner, string key, KeyModifiers modifiers,
        EventCallback<KeyboardEventArgs> command, bool allowRepeat) : IDisposable
    {
        public string Key { get; } = key;
        public KeyModifiers Modifiers { get; } = modifiers;
        public EventCallback<KeyboardEventArgs> Command { get; } = command;
        public bool AllowRepeat { get; } = allowRepeat;
        public void Dispose() => owner.bindings.Remove(this);
    }
}
