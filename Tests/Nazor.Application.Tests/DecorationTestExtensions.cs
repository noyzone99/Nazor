namespace Nazor.Application.Tests;

internal static class DecorationTestExtensions
{
    internal static void ApplyDecorations(this IWindow window, WindowDecorations configuration)
    {
        var result = window.SetDecorationsAsync(WindowDecorationPolicy.Require(configuration));
        if (!result.IsCompleted) throw new InvalidOperationException("This fixture requires synchronous native decoration dispatch.");
        result.GetAwaiter().GetResult();
    }

    internal static WindowDecorations ClientTitleBar(WindowTitleBarContent content = WindowTitleBarContent.Nazor,
        bool nativeButtons = true) => nativeButtons
        ? WindowDecorations.Native(content, WindowCaptionButtons.Native)
        : WindowDecorations.Nazor(content);
}
