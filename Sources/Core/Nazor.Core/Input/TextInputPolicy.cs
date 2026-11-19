namespace Nazor.Input;

/// <summary>Conventions for editing, without changing the physical modifiers delivered to the app.</summary>
/// <param name="PrimaryModifier">The primary modifier (KeyModifiers).</param>
/// <param name="WordModifier">The word modifier (KeyModifiers).</param>
/// <param name="LineModifier">The line modifier (KeyModifiers).</param>
/// <param name="HomeEndToDocument">The home end to document (bool).</param>
/// <param name="RedoWithY">The redo with y (bool).</param>
public sealed record TextInputPolicy(KeyModifiers PrimaryModifier, KeyModifiers WordModifier,
    KeyModifiers LineModifier = KeyModifiers.None, bool HomeEndToDocument = false, bool RedoWithY = true)
{
    /// <summary>Gets the default platform text-editing key conventions.</summary>
    public static TextInputPolicy Default { get; } = new(KeyModifiers.Control, KeyModifiers.Control);

    /// <summary>Gets the text-editing key conventions used on macOS.</summary>
    public static TextInputPolicy MacOS { get; } = new(KeyModifiers.Meta, KeyModifiers.Alt, KeyModifiers.Meta, true, false);

    /// <summary>Determines whether the modifier keys match this platform's command modifier convention.</summary>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool IsCommand(KeyModifiers modifiers) =>
        (modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == PrimaryModifier;
}

/// <summary>Continuous deltas are logical pixels; wheel detents are converted using backend settings.</summary>
public enum ScrollDeltaUnit
{
    /// <summary>Expresses wheel movement in detents, scaled using the backend's scroll settings.</summary>
    Notches,

    /// <summary>Expresses precision scrolling directly in logical pixel distances.</summary>
    LogicalPixels
}
