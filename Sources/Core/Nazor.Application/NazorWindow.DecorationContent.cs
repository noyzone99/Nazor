using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor;

public sealed partial class NazorWindow : IWindowDecorationContent
{
    RenderFragment IWindowDecorationContent.TitleBar => Scene.Styles.Theme?.WindowDecorations?.TitleBar is { } presenter
        ? presenter(new(this, DecorationOwner)) : RenderStandardTitleBar;

    private void RenderStandardTitleBar(RenderTreeBuilder builder)
    {
        var component = Root.Component;
        builder.OpenComponent<TitleBar>(0, component.GetType(), component);
        builder.AddComponentParameter(1, nameof(TitleBar.Title), Title);
        if (EffectiveIcon is { } icon)
            builder.AddComponentParameter(2, nameof(TitleBar.AppIcon), (RenderFragment)(inner =>
            {
                inner.OpenComponent<Image>(0);
                inner.AddComponentParameter(1, nameof(Image.Source), icon);
                inner.AddComponentParameter(2, nameof(Image.Style), "width: 16px; height: 16px");
                inner.CloseComponent();
            }));
        builder.CloseComponent();
    }

    RenderFragment? IWindowDecorationContent.CaptionButtons(bool left)
    {
        if (AppliedDecorations is not { Configuration: var configuration }) return null;
        if (configuration.CaptionButtons == WindowCaptionButtons.User)
        {
            var content = DecorationOwner?.UserCaptionButtons(left);
            if (content is null) return null;
            return builder =>
            {
                builder.OpenElement(0, "CaptionButtons");
                builder.AddAttribute(1, "source", "user");
                builder.AddContent(2, content);
                builder.CloseElement();
            };
        }
        if (configuration.CaptionButtons != WindowCaptionButtons.Nazor) return null;
        if (Scene.Styles.Theme?.WindowDecorations?.CaptionButtons is { } presenter)
            return presenter(new(this, DecorationOwner, left));
        var layout = SystemButtonsLayout.Left.Count == 0 && SystemButtonsLayout.Right.Count == 0 ? WindowButtonsLayout.Default : SystemButtonsLayout;
        var buttons = left ? layout.Left : layout.Right;
        if (buttons.Count == 0) return null;
        return builder =>
        {
            builder.OpenComponent<CaptionButtons>(0);
            builder.AddComponentParameter(1, nameof(CaptionButtons.Buttons), CaptionButtons.FromLayout(buttons));
            builder.CloseComponent();
        };
    }
}
