namespace Nazor;

internal interface IWindowDecorationContent
{
    RenderFragment TitleBar { get; }
    RenderFragment? CaptionButtons(bool left);
}
