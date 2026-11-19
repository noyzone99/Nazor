using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A compact label for a status, a category or a chosen value, such as "Draft" or a recipient in a field. The label is
/// the content between the tags (<c>TagLabel</c>); before it come either <see cref="Leading" /> content
/// (<c>TagLeading</c>) or a colored <see cref="Dot" /> (<c>TagDot</c>). <see cref="IsLive" /> marks something in
/// progress (the <c>in-progress</c> attribute, announced as "in progress"). With <see cref="Removable" /> a
/// <c>TagRemove</c> button raises <see cref="OnRemove" />; Delete and Backspace on it do the same.
/// </summary>
public sealed class Tag : ContentControl
{
    /// <summary>Gets or sets content shown before the label, usually an <see cref="Icon" /> or an <see cref="Avatar" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets whether a dot in the tag's color is shown before the label.</summary>
    [Parameter] public bool Dot { get; set; }

    /// <summary>Gets or sets the meaning of the tag's color.</summary>
    [Parameter] public Intent Intent { get; set; }

    /// <summary>Gets or sets whether the tag marks something in progress, such as a running job.</summary>
    [Parameter] public bool IsLive { get; set; }

    /// <summary>Gets or sets whether a remove button is shown.</summary>
    [Parameter] public bool Removable { get; set; }

    /// <summary>Gets or sets the callback invoked when the remove button is pressed.</summary>
    [Parameter] public EventCallback OnRemove { get; set; }

    /// <summary>Gets or sets whether the tag rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets the NSS element name emitted by Tag.</summary>
    protected override string ElementName => "Tag";

    /// <summary>Validates the look.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    /// <summary>Adds the look and state attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "in-progress", IsLive);
        builder.AddAttribute(13, "removable", Removable);
        builder.AddAttribute(14, "disabled", Disabled);
        builder.AddAttribute(15, "role", "group");
    }

    /// <summary>Adds the leading content or dot, the label and the remove button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, "TagLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        else if (Dot)
        {
            builder.OpenElement(1005, "TagDot");
            builder.AddAttribute(1006, "semantic-hidden", true);
            builder.CloseElement();
        }
        builder.OpenElement(1010, "TagLabel");
        builder.AddAttribute(1011, "role", "text");
        builder.AddAttribute(1012, "accessible-description", IsLive ? ControlStrings.Get("InProgress") : null);
        builder.AddContent(1013, ChildContent);
        builder.CloseElement();
        if (!Removable) return;
        builder.OpenElement(1020, "TagRemove");
        builder.AddAttribute(1021, "role", "button");
        builder.AddAttribute(1022, "focusable", !Disabled);
        builder.AddAttribute(1023, "accessible-name", ControlStrings.Get("Remove"));
        builder.AddAttribute(1024, "onclick", EventCallback.Factory.Create<Nazor.Input.PointerEventArgs>(this, RemoveAsync));
        builder.AddAttribute(1025, "onkeydown", EventCallback.Factory.Create<Nazor.Input.KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddContent(1026, "×");
        builder.CloseElement();
    }

    private Task KeyDownAsync(Nazor.Input.KeyboardEventArgs e)
    {
        if (e.Key is not ("Delete" or "Backspace")) return Task.CompletedTask;
        e.Handled = true;
        return RemoveAsync();
    }

    private Task RemoveAsync() => Disabled ? Task.CompletedTask : OnRemove.InvokeAsync();
}
