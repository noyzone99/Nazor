using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A field showing the chosen file paths with a Browse button. The controls cannot open native dialogs, so the
/// application supplies <see cref="Pick" /> (for example with the file dialogs of Nazor.Application) and the field shows
/// what it returns: <see cref="Paths" /> (<c>@bind-Paths</c>) lists the chosen paths, one unless <see cref="Multiple" />.
/// The Browse button is the field's Tab stop and is named by the enclosing <see cref="Field" />; Delete or Backspace on it
/// clear the paths, as the clear button does. The parts are <c>FileFieldValue</c> (with <c>placeholder</c> when empty,
/// the file names or "n files", the full paths as tooltip), <c>FileFieldClear</c> and <c>FileFieldBrowse</c> (with
/// <c>busy</c> while the picker is open).
/// </summary>
public sealed class FileField : Control
{
    private readonly ControllableState<IReadOnlyList<string>> paths = new([]);
    private bool picking;

    /// <summary>Gets or sets the chosen paths.</summary>
    [Parameter] public IReadOnlyList<string>? Paths { get; set; }

    /// <summary>Gets or sets the callback invoked when the user picks or clears paths; supports <c>@bind-Paths</c>.</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> PathsChanged { get; set; }

    /// <summary>
    /// Gets or sets the application's picker: it shows a dialog and returns the chosen paths, or null when the user
    /// cancels. Without it the Browse button is disabled.
    /// </summary>
    [Parameter] public Func<Task<IReadOnlyList<string>?>>? Pick { get; set; }

    /// <summary>Gets or sets whether several paths can be chosen; otherwise only the first path returned is kept.</summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>Gets or sets whether full paths are shown instead of file names.</summary>
    [Parameter] public bool ShowFullPath { get; set; }

    /// <summary>Gets or sets the text shown while no file is chosen; "No file selected" by default.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets the text of the Browse button; "Browse" by default.</summary>
    [Parameter] public string? BrowseText { get; set; }

    /// <summary>Gets or sets whether a button clears the chosen paths; true by default.</summary>
    [Parameter] public bool Clearable { get; set; } = true;

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a file is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the chosen paths.</summary>
    public IReadOnlyList<string> CurrentPaths => paths.Value;

    /// <summary>Gets whether the picker is open.</summary>
    public bool IsPicking => picking;

    /// <summary>Gets the NSS element name emitted by FileField.</summary>
    protected override string ElementName => "FileField";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the paths.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        paths.Sync(Paths ?? []);
    }

    /// <summary>Gets the text shown for the chosen paths, or null when there are none.</summary>
    /// <returns>The text.</returns>
    public string? DisplayText()
    {
        var current = paths.Value;
        if (current.Count == 0) return null;
        if (current.Count > 3) return ControlStrings.Format("FileCount", current.Count);
        return string.Join(", ", current.Select(path => ShowFullPath ? path : Path.GetFileName(path) is { Length: > 0 } name ? name : path));
    }

    /// <summary>Adds the frame states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "invalid", Invalid || Field?.Invalid == true);
        builder.AddAttribute(14, "empty", paths.Value.Count == 0);
        builder.AddAttribute(15, "multiple", Multiple);
    }

    /// <summary>Adds the value, the clear button and the Browse button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var shown = DisplayText();
        builder.OpenElement(1000, "FileFieldValue");
        builder.AddAttribute(1001, "role", "text");
        builder.AddAttribute(1002, "placeholder", shown is null);
        builder.AddAttribute(1003, "tooltip", shown is null ? null : string.Join("\n", paths.Value));
        builder.AddAttribute(1004, "semantic-hidden", true);
        builder.AddContent(1005, shown ?? Placeholder ?? ControlStrings.Get("NoFile"));
        builder.CloseElement();
        if (Clearable && shown is not null && !IsInactive)
        {
            builder.OpenElement(1010, "FileFieldClear");
            builder.AddAttribute(1011, "role", "button");
            builder.AddAttribute(1012, "accessible-name", ControlStrings.Get("Clear"));
            builder.AddAttribute(1013, "glyph", "minus");
            builder.AddAttribute(1014, "tabindex", -1);
            builder.AddAttribute(1015, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClearAsync));
            builder.CloseElement();
        }
        builder.OpenElement(1020, "FileFieldBrowse");
        builder.AddAttribute(1021, "role", "button");
        builder.AddAttribute(1022, "focusable", !IsInactive && Pick is not null);
        builder.AddAttribute(1023, "disabled", IsInactive || Pick is null);
        builder.AddAttribute(1024, "busy", picking);
        builder.AddAttribute(1025, "tabindex", TabIndex);
        builder.AddAttribute(1026, "accessible-description", shown ?? Placeholder ?? ControlStrings.Get("NoFile"));
        builder.AddAttribute(1027, "required", Required || Field?.Required == true);
        builder.AddAttribute(1028, "invalid", Invalid || Field?.Invalid == true);
        if (Field is { } field)
        {
            if (Id is null) builder.AddAttribute(1029, "id", field.ControlId);
            field.ApplyTo(builder, 1030, LabeledBy, DescribedBy);
        }
        builder.AddAttribute(1032, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, BrowseAsync));
        builder.AddAttribute(1033, "__internal_onkeydown#file", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddContent(1034, BrowseText ?? ControlStrings.Get("Browse"));
        builder.CloseElement();
    }

    /// <summary>Opens the application's picker as the Browse button does and keeps what it returns.</summary>
    /// <returns>A task that completes when the picker has closed and the change has been reported.</returns>
    public async Task BrowseAsync()
    {
        if (IsInactive || picking || Pick is null) return;
        picking = true;
        StateHasChanged();
        IReadOnlyList<string>? chosen;
        try
        {
            chosen = await Pick();
        }
        finally
        {
            picking = false;
            StateHasChanged();
        }
        if (chosen is null || chosen.Count == 0) return;
        await SetPathsAsync(Multiple ? [.. chosen] : [chosen[0]]);
    }

    /// <summary>Clears the chosen paths as the clear button does.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task ClearAsync() => IsInactive || paths.Value.Count == 0 ? Task.CompletedTask : SetPathsAsync([]);

    private async Task SetPathsAsync(IReadOnlyList<string> next)
    {
        paths.Set(next);
        StateHasChanged();
        await PathsChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.Key is not ("Delete" or "Backspace") || paths.Value.Count == 0) return Task.CompletedTask;
        e.Handled = true;
        return ClearAsync();
    }
}
