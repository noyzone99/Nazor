using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "App frame" group.
    private static IEnumerable<GalleryEntry> FrameEntries() =>
    [
        new("page-header", "PageHeader", "App frame", "Combine a title, subtitle and actions in a page header.", typeof(PageHeader), typeof(Examples.Frame.PageHeaderSample)),
        new GalleryEntry("window-chrome", "TitleBar and window controls", "App frame", "Open a separate window with custom title and caption controls.", typeof(TitleBar), typeof(Examples.Frame.WindowChromeSample), typeof(UserTitleBar), typeof(UserCaptionButtons), typeof(DragRegion), typeof(SnapLayoutArea), typeof(CaptionButtons), typeof(WindowCommands)).WithSupportingSource<Examples.Frame.WindowChromePreview>(),
    ];
}
