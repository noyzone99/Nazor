using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor.Themes.NazorLeaf;

/// <summary>The Leaf title-bar surface with its mark, display title and optional content slots. Decoration policy and the selected caption-button provider belong to the window.</summary>
/// <remarks>The element is TitleBar[source="nazor"] with the leaf class. Caption groups are supplied by the selected window configuration; this presenter does not select controls or inspect the operating system.</remarks>
public sealed class LeafTitlebar : ComponentBase
{

    /// <summary>Gets or sets the window that owns this title bar, supplied automatically.</summary>
    [Inject] public IWindow Window { get; set; } = null!;

    /// <summary>Gets or sets content at the leading edge, after the left leaf lights, such as a back button.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the application icon, used instead of the four-leaf mark.</summary>
    [Parameter] public RenderFragment? AppIcon { get; set; }

    /// <summary>Gets or sets whether the four-leaf mark is shown when no <see cref="AppIcon" /> is set; true by default.</summary>
    [Parameter] public bool ShowMark { get; set; } = true;

    /// <summary>Gets or sets the title text, often the window title.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets rich title content, used instead of <see cref="Title" />.</summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>Gets or sets a secondary text after the title, such as the document or the number of items.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets content in the middle that takes the free space, such as a search box.</summary>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary>Gets or sets content at the trailing edge, before the right leaf lights, such as an account button.</summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets content after the center, before the trailing content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the height variant.</summary>
    [Parameter] public TitleBarHeight Height { get; set; }

    /// <summary>Gets or sets additional space-separated NSS classes of the title bar.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the accessible name of the title bar; the title by default.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>Validates the height variant without changing the window's requested decorations.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Height)) throw new ArgumentOutOfRangeException(nameof(Height));
    }

    /// <summary>Renders a <see cref="TitleBar" /> declared where this title bar is declared, with the Leaf parts in its slots.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var icon = AppIcon ?? (ShowMark ? Mark : null);

        OpenDelegatedComponent<TitleBar>(builder, 0);
        builder.AddComponentParameter(1, nameof(TitleBar.Class), string.IsNullOrWhiteSpace(Class) ? "leaf" : "leaf " + Class);
        builder.AddComponentParameter(2, nameof(TitleBar.Height), Height);
        builder.AddComponentParameter(3, nameof(TitleBar.Title), Title);
        builder.AddComponentParameter(4, nameof(TitleBar.TitleContent), TitleContent);
        builder.AddComponentParameter(5, nameof(TitleBar.Subtitle), Subtitle);
        builder.AddComponentParameter(6, nameof(TitleBar.AppIcon), icon);
        builder.AddComponentParameter(7, nameof(TitleBar.Center), Center ?? Empty);
        builder.AddComponentParameter(8, nameof(TitleBar.ChildContent), ChildContent);
        builder.AddComponentParameter(9, nameof(TitleBar.AccessibleName), AccessibleName);
        builder.AddComponentParameter(10, nameof(TitleBar.Leading), Leading);
        builder.AddComponentParameter(11, nameof(TitleBar.Trailing), Trailing);
        builder.CloseComponent();
    }

    private static void Mark(RenderTreeBuilder builder)
    {
        builder.OpenComponent<LeafMark>(0);
        builder.AddComponentParameter(1, nameof(LeafMark.Class), "leaf-titlebar-mark");
        builder.CloseComponent();
    }

    private static void Empty(RenderTreeBuilder builder)
    {
    }

}
