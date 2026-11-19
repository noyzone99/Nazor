using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A short code typed one character per cell, such as a PIN or a verification code. <see cref="Value" />
/// (<c>@bind-Value</c>) is the code typed so far, without gaps; <see cref="OnComplete" /> reports it once every cell is
/// filled. Typing fills the current cell and moves to the next one, Backspace in an empty cell clears the previous one and
/// moves back, the arrows, Home and End move between the filled cells, and pasting fills the cells from the current one.
/// <see cref="Numeric" /> accepts digits only (letters and digits otherwise), <see cref="Mask" /> hides the characters.
/// The field is one Tab stop: Tab enters at the first empty cell. The element has the <c>group</c> role and is named by its
/// field; the parts are <c>PinFieldCell</c> editors (with <c>filled</c> and <c>index</c>, each described as "Character n of
/// m") and, with <see cref="GroupSize" />, <c>PinFieldSeparator</c> parts between groups of cells.
/// </summary>
public sealed class PinField : Control
{
    private readonly ControllableState<string> value = new("");
    private readonly string ownId = UniqueId.Create("pin");
    private EditableText?[] cells = [];
    private bool correcting;

    /// <summary>Gets or sets the number of cells; 6 by default.</summary>
    [Parameter] public int Length { get; set; } = 6;

    /// <summary>Gets or sets the code typed so far.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Gets or sets the callback invoked on every change of the code; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Gets or sets the callback invoked with the code when the last empty cell is filled.</summary>
    [Parameter] public EventCallback<string> OnComplete { get; set; }

    /// <summary>Gets or sets whether only digits are accepted; true by default, letters and digits otherwise.</summary>
    [Parameter] public bool Numeric { get; set; } = true;

    /// <summary>Gets or sets whether the characters are hidden.</summary>
    [Parameter] public bool Mask { get; set; }

    /// <summary>Gets or sets the character drawn for each hidden character.</summary>
    [Parameter] public char MaskCharacter { get; set; } = '●';

    /// <summary>Gets or sets the number of cells between separators, such as 3 for "123 456"; zero for none.</summary>
    [Parameter] public int GroupSize { get; set; }

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a code is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the entry cell takes the focus when it appears.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the code typed so far.</summary>
    public string Code => value.Value;

    /// <summary>Gets whether every cell is filled.</summary>
    public bool IsComplete => value.Value.Length == Length;

    /// <summary>Gets the index of the cell the Tab key enters: the first empty one, or the last.</summary>
    public int EntryIndex => Math.Min(value.Value.Length, Length - 1);

    /// <summary>Gets the NSS element name emitted by PinField.</summary>
    protected override string ElementName => "PinField";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the code.</summary>
    protected override void OnParametersSet()
    {
        if (Length < 1) throw new ArgumentOutOfRangeException(nameof(Length));
        if (GroupSize < 0) throw new ArgumentOutOfRangeException(nameof(GroupSize));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        value.Sync(Filter(Value ?? "", Length));
        if (cells.Length != Length) Array.Resize(ref cells, Length);
    }

    /// <summary>Gets whether a character is accepted in a cell.</summary>
    /// <param name="c">The character.</param>
    /// <returns>true for digits, and for letters unless <see cref="Numeric" />.</returns>
    public bool Accepts(char c) => Numeric ? char.IsAsciiDigit(c) : char.IsLetterOrDigit(c);

    private string Filter(string text, int limit) => new([.. text.Where(Accepts).Take(limit)]);

    private string CellId(int index) => index == 0 && Id is null && Field is { } field ? field.ControlId : (Id ?? ownId) + "-cell" + index.ToString(CultureInfo.InvariantCulture);

    private string CellText(int index) => index < value.Value.Length ? value.Value[index].ToString() : "";

    /// <summary>Adds the group role, the naming and the states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "invalid", Invalid || Field?.Invalid == true);
        builder.AddAttribute(14, "complete", IsComplete);
        builder.AddAttribute(15, "empty", value.Value.Length == 0);
        Field?.ApplyTo(builder, 16, LabeledBy, DescribedBy);
    }

    /// <summary>Adds one editor per cell and the separators.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var entry = EntryIndex;
        for (var i = 0; i < Length; i++)
        {
            var index = i;
            if (GroupSize > 0 && i > 0 && i % GroupSize == 0)
            {
                builder.OpenElement(1000, "PinFieldSeparator");
                builder.SetKey("separator" + i.ToString(CultureInfo.InvariantCulture));
                builder.AddAttribute(1001, "semantic-hidden", true);
                builder.CloseElement();
            }
            builder.OpenComponent<EditableText>(1010);
            builder.SetKey(index);
            builder.AddComponentParameter(1011, nameof(EditableText.As), "PinFieldCell");
            builder.AddComponentParameter(1012, nameof(EditableText.Value), CellText(index));
            builder.AddComponentParameter(1013, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, typed => TypedAsync(index, typed)));
            builder.AddComponentParameter(1014, nameof(EditableText.Password), Mask);
            builder.AddComponentParameter(1015, nameof(EditableText.MaskCharacter), MaskCharacter);
            builder.AddComponentParameter(1016, nameof(EditableText.Disabled), Disabled);
            builder.AddComponentParameter(1017, nameof(EditableText.Required), Required);
            builder.AddComponentParameter(1018, nameof(EditableText.Invalid), Invalid);
            builder.AddComponentParameter(1019, nameof(EditableText.InputMode), Numeric ? "number" : null);
            builder.AddComponentParameter(1020, nameof(EditableText.AutoFocus), AutoFocus && index == entry);
            builder.AddComponentParameter(1021, nameof(Control.AccessibleName), AccessibleName);
            builder.AddComponentParameter(1022, nameof(Control.AccessibleDescription), ControlStrings.Format("PinCharacter", index + 1, Length));
            builder.AddComponentParameter(1023, nameof(Control.LabeledBy), LabeledBy);
            builder.AddComponentParameter(1024, nameof(Control.Id), CellId(index));
            builder.AddComponentParameter(1025, nameof(Control.TabIndex), index == entry ? TabIndex : -1);
            builder.AddComponentParameter(1026, "filled", index < value.Value.Length);
            builder.AddComponentParameter(1027, "index", index);
            builder.AddComponentParameter(1028, "__internal_onkeydown#pin", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => KeyDownAsync(index, e)));
            builder.AddComponentParameter(1029, "__internal_onfocus#pin", EventCallback.Factory.Create<FocusEventArgs>(this, () => FocusedAsync(index)));
            builder.AddComponentReferenceCapture(1030, component => cells[index] = (EditableText)component);
            builder.CloseComponent();
        }
    }

    /// <summary>Replaces the code as typing would, keeping only accepted characters, and reports it.</summary>
    /// <param name="code">The new code.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task SetCodeAsync(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return SetValueAsync(Filter(code, Length));
    }

    /// <summary>Empties every cell and reports it.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task ClearAsync() => SetValueAsync("");

    /// <summary>Moves the keyboard focus to the entry cell.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) => FocusCellAsync(EntryIndex, preventScroll);

    private Task FocusCellAsync(int index, bool preventScroll = true) =>
        index >= 0 && index < cells.Length && cells[index] is { } cell ? cell.Element.FocusAsync(preventScroll) : Task.CompletedTask;

    private async Task TypedAsync(int index, string typed)
    {
        if (correcting || IsInactive) return;
        var old = CellText(index);
        var code = value.Value;
        var start = Math.Min(index, code.Length);
        string next;
        int focus;
        if (typed.Length == 0)
        {
            next = index < code.Length ? code.Remove(index, 1) : code;
            focus = Math.Min(index, Math.Min(next.Length, Length - 1));
        }
        else
        {
            var incoming = old.Length == 1 && typed.Length > 1 ? typed.StartsWith(old, StringComparison.Ordinal) ? typed[1..] : typed.EndsWith(old, StringComparison.Ordinal) ? typed[..^1] : typed : typed;
            var accepted = Filter(incoming, Length - start);
            if (accepted.Length == 0)
            {
                await CorrectAsync(index, old);
                return;
            }
            next = code[..start] + accepted + (start + accepted.Length < code.Length ? code[(start + accepted.Length)..] : "");
            focus = Math.Min(start + accepted.Length, Length - 1);
        }
        await SetValueAsync(next);
        await CorrectAsync(index, CellText(index));
        await FocusCellAsync(focus);
    }

    private async Task CorrectAsync(int index, string desired)
    {
        if (cells[index] is not { } cell || cell.Text == desired) return;
        correcting = true;
        try
        {
            await cell.SetTextAsync(desired);
        }
        finally
        {
            correcting = false;
        }
    }

    private async Task SetValueAsync(string next)
    {
        if (!value.Set(next)) return;
        StateHasChanged();
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next, commit: false);
        if (next.Length == Length) await OnComplete.InvokeAsync(next);
    }

    private async Task KeyDownAsync(int index, KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || e.Modifiers != KeyModifiers.None) return;
        var last = Math.Min(value.Value.Length, Length - 1);
        int? target = e.Key switch
        {
            "ArrowLeft" => Math.Max(0, index - 1),
            "ArrowRight" => Math.Min(index + 1, last),
            "Home" => 0,
            "End" => last,
            _ => null,
        };
        if (target is { } move)
        {
            e.Handled = true;
            await FocusCellAsync(move);
            return;
        }
        if (e.Key != "Backspace" || index < value.Value.Length || index == 0) return;
        e.Handled = true;
        await SetValueAsync(value.Value.Remove(index - 1, 1));
        await FocusCellAsync(index - 1);
    }

    private Task FocusedAsync(int index) => index > value.Value.Length ? FocusCellAsync(EntryIndex) : Task.CompletedTask;
}
