using Nazor.Controls;
using Nazor.Controls.Primitives;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Navigation" group.
    private static IEnumerable<GalleryEntry> NavigationEntries() =>
    [
        new("transitions", "Content and layout transitions", "Navigation", "Replace pages, preserve keyed collection state and connect measured layouts.", typeof(ContentTransition), typeof(Examples.Navigation.TransitionsSample), typeof(LayoutTransition), typeof(ItemsTransition<int>)),
        new("tabs", "Tabs", "Navigation", "Switch between panels using the tab strip.", typeof(Tabs), typeof(Examples.Navigation.TabsSample), typeof(Tab)),
        new("navigation-view", "NavigationView", "Navigation", "Adapt a keyed hierarchy between left, compact, minimal and top navigation.", typeof(NavigationView<Examples.Navigation.NavigationViewSample.Section>), typeof(Examples.Navigation.NavigationViewSample)),
        new("breadcrumb", "BreadcrumbBar", "Navigation", "Navigate a typed path with measured ancestor overflow.", typeof(BreadcrumbBar<Examples.Navigation.BreadcrumbSample.PathNode>), typeof(Examples.Navigation.BreadcrumbSample)),
        new("pips-pager", "PipsPager", "Navigation", "Select a page through a bounded window of dots, with optional navigation arrows.", typeof(PipsPager), typeof(Examples.Navigation.PipsPagerSample)),
        new GalleryEntry("frame", "Frame", "Navigation", "Navigate between routes and use the window's back and forward history.", typeof(Frame), typeof(Examples.Navigation.FrameSample)).WithSupportingSource<Examples.Navigation.FrameRoutePreview>(),
    ];
}
