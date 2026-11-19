using Nazor.Controls;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor;
/// <summary>The buttons of <see cref = "IWindowService.ShowMessageAsync"/>.</summary>
public enum MessageBoxButtons
{
    /// <summary>Displays an OK button.</summary>
    Ok,
    /// <summary>Displays OK and Cancel buttons.</summary>
    OkCancel,
    /// <summary>Displays Yes and No buttons.</summary>
    YesNo,
    /// <summary>Displays Yes, No, and Cancel buttons.</summary>
    YesNoCancel,
}

/// <summary>The icon of <see cref = "IWindowService.ShowMessageAsync"/>.</summary>
public enum MessageBoxIcon
{
    /// <summary>Displays no standard icon.</summary>
    None,
    /// <summary>Displays the standard information icon.</summary>
    Information,
    /// <summary>Displays the standard warning icon.</summary>
    Warning,
    /// <summary>Displays the standard error icon.</summary>
    Error,
    /// <summary>Displays the standard question icon.</summary>
    Question,
}

/// <summary>The button chosen in a message box.</summary>
public enum MessageBoxResult
{
    /// <summary>Indicates that no button result was supplied.</summary>
    None,
    /// <summary>Indicates that the user chose OK.</summary>
    Ok,
    /// <summary>Indicates that the user canceled the dialog.</summary>
    Cancel,
    /// <summary>Indicates that the user chose Yes.</summary>
    Yes,
    /// <summary>Indicates that the user chose No.</summary>
    No,
}

internal sealed class MessageBoxView : WindowComponentBase
{
    [Parameter]
    public string Message { get; set; } = "";

    [Parameter]
    public string Title { get; set; } = "";

    [Parameter]
    public MessageBoxButtons Buttons { get; set; }

    [Parameter]
    public MessageBoxIcon Icon { get; set; }

    public static MessageBoxResult Dismissed(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.Ok => MessageBoxResult.Ok,
        MessageBoxButtons.OkCancel or MessageBoxButtons.YesNoCancel => MessageBoxResult.Cancel,
        _ => MessageBoxResult.None,
    };
    private static MessageBoxResult[] Results(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.OkCancel => [MessageBoxResult.Ok, MessageBoxResult.Cancel],
        MessageBoxButtons.YesNo => [MessageBoxResult.Yes, MessageBoxResult.No],
        MessageBoxButtons.YesNoCancel => [MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel],
        _ => [MessageBoxResult.Ok],
    };
    private static string Label(MessageBoxResult result) => result switch
    {
        MessageBoxResult.Ok => Strings.Ok,
        MessageBoxResult.Cancel => Strings.Cancel,
        MessageBoxResult.Yes => Strings.Yes,
        _ => Strings.No,
    };
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(4, (RenderFragment)(window =>
        {
            window.OpenElement(0, "MessageBoxBody");
            if (Icon != MessageBoxIcon.None)
            {
                window.OpenElement(1, "MessageBoxIcon");
                window.AddAttribute(2, "icon", Icon.ToString().ToLowerInvariant());
                window.AddContent(3, Icon switch
                {
                    MessageBoxIcon.Warning => "",
                    MessageBoxIcon.Error => "",
                    MessageBoxIcon.Question => "",
                    _ => "",
                });
                window.CloseElement();
            }

            window.OpenComponent<TextBlock>(4, typeof(MessageBoxView));
            window.AddComponentParameter(5, "IsTextSelectionEnabled", true);
            window.AddAttribute(6, "ChildContent", (RenderFragment)(text => text.AddContent(0, Message)));
            window.CloseComponent();
            window.CloseElement();
            window.OpenElement(10, "MessageBoxButtons");
            var first = true;
            foreach (var result in Results(Buttons))
            {
                window.OpenComponent<Button>(11, typeof(MessageBoxView));
                window.SetKey(result);
                window.AddComponentParameter(12, "IsDefault", first);
                window.AddComponentParameter(13, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Window.Close(result)));
                window.AddAttribute(14, "ChildContent", (RenderFragment)(label => label.AddContent(0, Label(result))));
                window.CloseComponent();
                first = false;
            }

            window.CloseElement();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = Title;
        Window.Width = 420d;
        Window.Height = 160d;
    }
}
