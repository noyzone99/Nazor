using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The level of a password strength score, written as the <c>level</c> attribute of the strength meter.</summary>
public enum PasswordStrengthLevel
{
    /// <summary>No password yet.</summary>
    None,
    /// <summary>A score below 0.25.</summary>
    Weak,
    /// <summary>A score from 0.25 to below 0.5.</summary>
    Fair,
    /// <summary>A score from 0.5 to below 0.75.</summary>
    Good,
    /// <summary>A score of 0.75 or more.</summary>
    Strong,
}

/// <summary>
/// A text field whose characters are masked. The parts are those of a text field (<c>PasswordFieldLeading</c>,
/// <c>PasswordFieldInput</c>, <c>PasswordFieldTrailing</c>, <c>PasswordFieldCounter</c>) plus:
/// <list type="bullet">
/// <item><c>PasswordFieldReveal</c>, a toggle button (<c>pressed</c> while the text is shown) named "Show password" or
/// "Hide password"; it is not a Tab stop, so Alt+F8 in the editor toggles it too, as in Windows.</item>
/// <item><c>PasswordFieldCapsLock</c>, a polite status shown while the editor has the focus and Caps Lock seems on. The
/// engine does not report the Caps Lock state, so the field infers it: a letter typed in upper case without Shift (or in
/// lower case with Shift) means Caps Lock is on, a letter typed in the expected case means it is off, and the Caps Lock key
/// flips a state already inferred. Until the first letter is typed the state is unknown and no warning shows.</item>
/// <item><c>PasswordFieldStrength</c>, a meter (0 to 1) computed by <see cref="Strength" />, with its <c>level</c>
/// attribute (<c>none</c>, <c>weak</c>, <c>fair</c>, <c>good</c>, <c>strong</c>), a <c>PasswordFieldStrengthBar</c> track
/// holding a <c>PasswordFieldStrengthFill</c> as wide as the score (<c>--nz-strength</c>), and a
/// <c>PasswordFieldStrengthText</c> naming the level.</item>
/// </list>
/// </summary>
public sealed class PasswordField : TextFieldBase
{
    private readonly ControllableState<bool> revealed = new(false);
    private bool shift;
    private bool? capsLock;
    private bool focused;

    /// <summary>Gets or sets whether the reveal button is offered; true by default.</summary>
    [Parameter] public bool Revealable { get; set; } = true;

    /// <summary>Gets or sets whether the text is shown instead of masked.</summary>
    [Parameter] public bool Revealed { get; set; }

    /// <summary>Gets or sets the callback invoked when the user shows or hides the text; supports <c>@bind-Revealed</c>.</summary>
    [Parameter] public EventCallback<bool> RevealedChanged { get; set; }

    /// <summary>Gets or sets whether the Caps Lock warning can appear; true by default.</summary>
    [Parameter] public bool ShowCapsLockWarning { get; set; } = true;

    /// <summary>
    /// Gets or sets the function that scores a password from 0 (weakest) to 1 (strongest); null hides the strength meter.
    /// <see cref="EstimateStrength" /> is a simple default.
    /// </summary>
    [Parameter] public Func<string, double>? Strength { get; set; }

    /// <summary>Gets or sets the character drawn for each masked character.</summary>
    [Parameter] public char MaskCharacter { get; set; } = '●';

    /// <summary>Gets whether the text is currently shown.</summary>
    public bool IsRevealed => Revealable && revealed.Value;

    /// <summary>Gets whether Caps Lock is inferred to be on; null while unknown.</summary>
    public bool? CapsLock => capsLock;

    /// <summary>Gets the NSS element name emitted by PasswordField.</summary>
    protected override string ElementName => "PasswordField";

    /// <summary>Gets that Enter does not insert a line break.</summary>
    protected override bool Multiline => false;

    /// <summary>Gets the level of a strength score.</summary>
    /// <param name="score">The score from 0 to 1.</param>
    /// <param name="empty">Whether there is no password yet.</param>
    /// <returns>The level.</returns>
    public static PasswordStrengthLevel LevelOf(double score, bool empty = false) => empty ? PasswordStrengthLevel.None : score switch
    {
        < 0.25 => PasswordStrengthLevel.Weak,
        < 0.5 => PasswordStrengthLevel.Fair,
        < 0.75 => PasswordStrengthLevel.Good,
        _ => PasswordStrengthLevel.Strong,
    };

    /// <summary>
    /// Scores a password from its length and the kinds of characters it mixes (lower case, upper case, digits, symbols):
    /// a rough estimate for forms without a dedicated policy, not a security measure.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <returns>A score from 0 to 1.</returns>
    public static double EstimateStrength(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length == 0) return 0;
        var kinds = (password.Any(char.IsLower) ? 1 : 0) + (password.Any(char.IsUpper) ? 1 : 0) + (password.Any(char.IsDigit) ? 1 : 0)
            + (password.Any(c => !char.IsLetterOrDigit(c)) ? 1 : 0);
        var length = Math.Min(password.Length, 16) / 16.0;
        return Math.Clamp(length * 0.6 + kinds / 4.0 * 0.4 - (password.Length < 6 ? 0.2 : 0), 0, 1);
    }

    /// <summary>Synchronizes the revealed state.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        revealed.Sync(Revealed);
    }

    /// <summary>Adds the frame states and the revealed state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        builder.AddAttribute(30, "revealed", IsRevealed);
        builder.AddAttribute(31, "caps-lock", CapsWarning);
    }

    private bool CapsWarning => ShowCapsLockWarning && focused && capsLock == true && !IsInactive;

    /// <summary>Adds the masking and the key tracking to the editor.</summary>
    /// <param name="builder">The builder positioned on the editor component.</param>
    protected override void AddEditorParameters(RenderTreeBuilder builder)
    {
        builder.AddComponentParameter(1030, nameof(EditableText.Password), !IsRevealed);
        builder.AddComponentParameter(1031, nameof(EditableText.MaskCharacter), MaskCharacter);
        builder.AddComponentParameter(1032, "__internal_onkeydown#password", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddComponentParameter(1033, "__internal_onkeyup#password", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyUp));
        builder.AddComponentParameter(1034, "__internal_onfocus#password", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetFocused(true)));
        builder.AddComponentParameter(1035, "__internal_onblur#password", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetFocused(false)));
    }

    /// <summary>Adds the Caps Lock warning and the reveal button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAfterInput(RenderTreeBuilder builder)
    {
        if (CapsWarning)
        {
            builder.OpenElement(1050, "PasswordFieldCapsLock");
            builder.AddAttribute(1051, "role", "status");
            builder.AddAttribute(1052, "live", "polite");
            builder.AddContent(1053, ControlStrings.Get("CapsLockOn"));
            builder.CloseElement();
        }
        if (!Revealable) return;
        builder.OpenElement(1060, "PasswordFieldReveal");
        builder.AddAttribute(1061, "role", "togglebutton");
        builder.AddAttribute(1062, "accessible-name", ControlStrings.Get(IsRevealed ? "HidePassword" : "ShowPassword"));
        builder.AddAttribute(1063, "pressed", IsRevealed);
        builder.AddAttribute(1064, "tabindex", -1);
        builder.AddAttribute(1065, "disabled", IsInactive);
        builder.AddAttribute(1066, "glyph", "ring");
        builder.AddAttribute(1067, "ring-fraction", IsRevealed ? 0.5 : 1.0);
        builder.AddAttribute(1068, "tooltip", ControlStrings.Get(IsRevealed ? "HidePassword" : "ShowPassword"));
        builder.AddAttribute(1069, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleFromButtonAsync));
        builder.CloseElement();
    }

    /// <summary>Adds the text field parts and the strength meter.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        base.AddContent(builder);
        if (Strength is null) return;
        var score = Text.Length == 0 ? 0 : Math.Clamp(Strength(Text) is var s && double.IsFinite(s) ? s : 0, 0, 1);
        var level = LevelOf(score, Text.Length == 0);
        var levelText = level == PasswordStrengthLevel.None ? null : ControlStrings.Get("Strength" + level);
        builder.OpenElement(1200, "PasswordFieldStrength");
        builder.AddAttribute(1201, "role", "meter");
        builder.AddAttribute(1202, "accessible-name", ControlStrings.Get("PasswordStrength"));
        builder.AddAttribute(1203, "accessible-description", levelText);
        builder.AddAttribute(1204, "range-min", 0.0);
        builder.AddAttribute(1205, "range-max", 1.0);
        builder.AddAttribute(1206, "range-value", score);
        builder.AddAttribute(1207, "level", level.ToString().ToLowerInvariant());
        builder.AddAttribute(1208, "style", "--nz-strength: " + (score * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%");
        builder.OpenElement(1210, "PasswordFieldStrengthBar");
        builder.OpenElement(1211, "PasswordFieldStrengthFill");
        builder.CloseElement();
        builder.CloseElement();
        if (levelText is not null)
        {
            builder.OpenElement(1220, "PasswordFieldStrengthText");
            builder.AddAttribute(1221, "semantic-hidden", true);
            builder.AddContent(1222, levelText);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    /// <summary>Infers the Caps Lock state from the case of a typed letter.</summary>
    /// <param name="previous">The text before the edit.</param>
    /// <param name="value">The new text.</param>
    /// <returns>A completed task.</returns>
    protected override Task TextChangedAsync(string previous, string value)
    {
        if (value.Length != previous.Length + 1) return Task.CompletedTask;
        var at = 0;
        while (at < previous.Length && previous[at] == value[at]) at++;
        var typed = value[at];
        if (char.ToUpperInvariant(typed) == char.ToLowerInvariant(typed)) return Task.CompletedTask;
        capsLock = char.IsUpper(typed) != shift;
        return Task.CompletedTask;
    }

    /// <summary>Shows or hides the text as the reveal button would.</summary>
    /// <param name="value">Whether the text is shown.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetRevealedAsync(bool value)
    {
        if (!Revealable || IsInactive || !revealed.Set(value)) return;
        StateHasChanged();
        await RevealedChanged.InvokeAsync(value);
    }

    private async Task ToggleFromButtonAsync()
    {
        await SetRevealedAsync(!revealed.Value);
        await FocusAsync(preventScroll: true);
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        shift = (e.Modifiers & KeyModifiers.Shift) != 0 || e.Key == "Shift";
        if (e.Key == "CapsLock" && capsLock is { } known)
        {
            capsLock = !known;
            StateHasChanged();
        }
        if (e.Handled || e.Key != "F8" || e.Modifiers != KeyModifiers.Alt || !Revealable) return Task.CompletedTask;
        e.Handled = true;
        return SetRevealedAsync(!revealed.Value);
    }

    private void KeyUp(KeyboardEventArgs e) => shift = e.Key != "Shift" && (e.Modifiers & KeyModifiers.Shift) != 0;

    private void SetFocused(bool value)
    {
        if (focused == value) return;
        focused = value;
        StateHasChanged();
    }
}
