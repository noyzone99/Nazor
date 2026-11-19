using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Moves through the pages of a long collection: buttons for the first, previous, numbered, next and last pages, with
/// ellipses where pages are skipped. <see cref="Page" /> (<c>@bind-Page</c>) is one-based; the number of pages is
/// <see cref="PageCount" />, or is computed from <see cref="TotalItems" /> and <see cref="PageSize" />
/// (<c>@bind-PageSize</c>, chosen among <see cref="PageSizes" /> when <see cref="ShowPageSize" /> is set).
/// <see cref="SiblingCount" /> pages are shown on each side of the current one and <see cref="BoundaryCount" /> at each
/// end. The element <c>Pagination</c> (role <c>group</c>, named "Pagination") holds the buttons <c>PaginationFirst</c>,
/// <c>PaginationPrevious</c>, <c>PaginationPage</c> (the current one has <c>current</c>), <c>PaginationNext</c> and
/// <c>PaginationLast</c>, each one a Tab stop with a localized name, the <c>PaginationEllipsis</c> parts,
/// <c>PaginationSummary</c> and <c>PaginationPageSize</c> (with <c>PaginationPageSizeLabel</c> and a
/// <see cref="Select{TValue}" />).
/// </summary>
public sealed class Pagination : Control
{
    private static readonly IReadOnlyList<int> DefaultPageSizes = [10, 25, 50, 100];
    private readonly ControllableState<int> page = new(1);
    private readonly ControllableState<int> pageSize = new(10);
    private readonly string labelId = UniqueId.Create("pagination-size");

    /// <summary>Gets or sets the current page, starting from one.</summary>
    [Parameter] public int Page { get; set; } = 1;

    /// <summary>Gets or sets the callback invoked when the user goes to another page.</summary>
    [Parameter] public EventCallback<int> PageChanged { get; set; }

    /// <summary>Gets or sets the number of pages; when null it is computed from <see cref="TotalItems" /> and <see cref="PageSize" />.</summary>
    [Parameter] public int? PageCount { get; set; }

    /// <summary>Gets or sets the number of items of the collection, used with <see cref="PageSize" /> when <see cref="PageCount" /> is null.</summary>
    [Parameter] public int? TotalItems { get; set; }

    /// <summary>Gets or sets the number of items per page.</summary>
    [Parameter] public int PageSize { get; set; } = 10;

    /// <summary>Gets or sets the callback invoked when the user chooses another page size.</summary>
    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>Gets or sets the page sizes offered by <see cref="ShowPageSize" />; 10, 25, 50 and 100 when null.</summary>
    [Parameter] public IReadOnlyList<int>? PageSizes { get; set; }

    /// <summary>Gets or sets how many pages are shown on each side of the current page.</summary>
    [Parameter] public int SiblingCount { get; set; } = 1;

    /// <summary>Gets or sets how many pages are always shown at each end.</summary>
    [Parameter] public int BoundaryCount { get; set; } = 1;

    /// <summary>Gets or sets whether the buttons for the first and the last page are shown.</summary>
    [Parameter] public bool ShowFirstLast { get; set; }

    /// <summary>Gets or sets whether the buttons for the previous and the next page are shown.</summary>
    [Parameter] public bool ShowPreviousNext { get; set; } = true;

    /// <summary>Gets or sets whether a drop-down list lets the user choose the page size.</summary>
    [Parameter] public bool ShowPageSize { get; set; }

    /// <summary>Gets or sets whether a localized "Page 3 of 10" summary is shown.</summary>
    [Parameter] public bool ShowSummary { get; set; }

    /// <summary>Gets or sets whether the pagination rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets the current page.</summary>
    public int CurrentPage => Math.Clamp(page.Value, 1, Count);

    /// <summary>Gets the number of pages, at least one.</summary>
    public int Count => Math.Max(1, PageCount ?? (int)Math.Ceiling((TotalItems ?? 0) / (double)Math.Max(1, pageSize.Value)));

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "Pagination";

    /// <summary>
    /// Computes the pages to show: the first and last <paramref name="boundaryCount" /> pages, the current page with
    /// <paramref name="siblingCount" /> pages on each side, and null for an ellipsis in place of the pages skipped.
    /// </summary>
    /// <param name="current">The current page, starting from one.</param>
    /// <param name="count">The number of pages.</param>
    /// <param name="siblingCount">The pages on each side of the current one.</param>
    /// <param name="boundaryCount">The pages at each end.</param>
    /// <returns>The page numbers in order, with null for each ellipsis.</returns>
    public static IReadOnlyList<int?> Pages(int current, int count, int siblingCount = 1, int boundaryCount = 1)
    {
        if (count < 1) return [];
        ArgumentOutOfRangeException.ThrowIfNegative(siblingCount);
        ArgumentOutOfRangeException.ThrowIfNegative(boundaryCount);
        current = Math.Clamp(current, 1, count);
        var start = Enumerable.Range(1, Math.Min(boundaryCount, count));
        var endFirst = Math.Max(count - boundaryCount + 1, boundaryCount + 1);
        var end = endFirst <= count ? Enumerable.Range(endFirst, count - endFirst + 1).ToList() : [];
        var siblingsStart = Math.Max(Math.Min(current - siblingCount, count - boundaryCount - siblingCount * 2 - 1), boundaryCount + 2);
        var siblingsEnd = Math.Min(Math.Max(current + siblingCount, boundaryCount + siblingCount * 2 + 2), end.Count > 0 ? end[0] - 2 : count - 1);
        var pages = new List<int?>(start.Select(p => (int?)p));
        if (siblingsStart > boundaryCount + 2) pages.Add(null);
        else if (boundaryCount + 1 < count - boundaryCount) pages.Add(boundaryCount + 1);
        for (var p = siblingsStart; p <= siblingsEnd; p++) pages.Add(p);
        if (siblingsEnd < count - boundaryCount - 1) pages.Add(null);
        else if (count - boundaryCount > boundaryCount) pages.Add(count - boundaryCount);
        pages.AddRange(end.Select(p => (int?)p));
        var result = new List<int?>(pages.Count);
        var last = 0;
        foreach (var p in pages)
        {
            if (p is null)
            {
                if (result.Count > 0 && result[^1] is not null) result.Add(null);
                continue;
            }
            if (p <= last || p > count) continue;
            if (result.Count > 0 && result[^1] is null && p == last + 1) result.RemoveAt(result.Count - 1);
            result.Add(p);
            last = p.Value;
        }
        return result;
    }

    /// <summary>Validates the parameters and synchronizes the page and the page size.</summary>
    protected override void OnParametersSet()
    {
        if (PageCount is < 0) throw new ArgumentOutOfRangeException(nameof(PageCount));
        if (TotalItems is < 0) throw new ArgumentOutOfRangeException(nameof(TotalItems));
        if (PageCount is null && TotalItems is null) throw new InvalidOperationException("Pagination requires PageCount or TotalItems.");
        if (PageSize < 1) throw new ArgumentOutOfRangeException(nameof(PageSize));
        if (SiblingCount < 0) throw new ArgumentOutOfRangeException(nameof(SiblingCount));
        if (BoundaryCount < 0) throw new ArgumentOutOfRangeException(nameof(BoundaryCount));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (PageSizes is { } sizes && (sizes.Count == 0 || sizes.Any(size => size < 1))) throw new ArgumentException("PageSizes must contain positive sizes.", nameof(PageSizes));
        page.Sync(Page);
        pageSize.Sync(PageSize);
    }

    /// <summary>Adds the group role, its localized name, the size and the disabled state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null && LabeledBy is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Get("Pagination"));
        builder.AddAttribute(12, "size", Vocabulary.Name(Size));
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "page-count", Count);
    }

    /// <summary>Adds the navigation buttons, the pages, the summary and the page size chooser.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var current = CurrentPage;
        var count = Count;
        if (ShowFirstLast) AddStep(builder, 1000, "PaginationFirst", "FirstPage", 1, current == 1, "chevron-left", 2);
        if (ShowPreviousNext) AddStep(builder, 1020, "PaginationPrevious", "PreviousPage", current - 1, current == 1, "chevron-left", 1);
        foreach (var number in Pages(current, count, SiblingCount, BoundaryCount))
        {
            if (number is not { } p)
            {
                builder.OpenElement(1040, "PaginationEllipsis");
                builder.AddAttribute(1041, "semantic-hidden", true);
                builder.AddContent(1042, "…");
                builder.CloseElement();
                continue;
            }
            builder.OpenElement(1050, "PaginationPage");
            builder.SetKey(p);
            builder.AddAttribute(1051, "role", "button");
            builder.AddAttribute(1052, "focusable", !Disabled);
            builder.AddAttribute(1053, "current", p == current);
            builder.AddAttribute(1054, "page", p);
            builder.AddAttribute(1055, "accessible-name", ControlStrings.Format("PageNumber", p));
            builder.AddAttribute(1056, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => GoToAsync(p)));
            builder.AddContent(1057, p.ToString(CultureInfo.CurrentCulture));
            builder.CloseElement();
        }
        if (ShowPreviousNext) AddStep(builder, 1060, "PaginationNext", "NextPage", current + 1, current == count, "chevron-right", 1);
        if (ShowFirstLast) AddStep(builder, 1080, "PaginationLast", "LastPage", count, current == count, "chevron-right", 2);
        if (ShowSummary)
        {
            builder.OpenElement(1100, "PaginationSummary");
            builder.AddAttribute(1101, "role", "status");
            builder.AddContent(1102, ControlStrings.Format("PageOf", current, count));
            builder.CloseElement();
        }
        if (ShowPageSize)
        {
            builder.OpenElement(1110, "PaginationPageSize");
            builder.OpenElement(1111, "PaginationPageSizeLabel");
            builder.AddAttribute(1112, "id", labelId);
            builder.AddContent(1113, ControlStrings.Get("ItemsPerPage"));
            builder.CloseElement();
            builder.OpenComponent<Select<int>>(1114);
            builder.AddComponentParameter(1115, nameof(Select<int>.Items), PageSizes ?? DefaultPageSizes);
            builder.AddComponentParameter(1116, nameof(Select<int>.Value), pageSize.Value);
            builder.AddComponentParameter(1117, nameof(Select<int>.ValueChanged), EventCallback.Factory.Create<int>(this, SetPageSizeAsync));
            builder.AddComponentParameter(1118, nameof(Select<int>.LabeledBy), labelId);
            builder.AddComponentParameter(1119, nameof(Select<int>.Disabled), Disabled);
            builder.AddComponentParameter(1120, nameof(Select<int>.Size), Size);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private void AddStep(RenderTreeBuilder builder, int sequence, string name, string label, int target, bool disabled, string glyph, int glyphs)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "focusable", !disabled && !Disabled);
        builder.AddAttribute(sequence + 3, "disabled", disabled);
        builder.AddAttribute(sequence + 4, "accessible-name", ControlStrings.Get(label));
        builder.AddAttribute(sequence + 5, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => GoToAsync(target)));
        for (var i = 0; i < glyphs; i++)
        {
            builder.OpenElement(sequence + 6, "PaginationGlyph");
            builder.SetKey(i);
            builder.AddAttribute(sequence + 7, "glyph", glyph);
            builder.AddAttribute(sequence + 8, "semantic-hidden", true);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    /// <summary>Goes to a page as a user action does, reporting the change.</summary>
    /// <param name="target">The page, clamped to the valid pages.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task GoToAsync(int target)
    {
        if (Disabled) return;
        target = Math.Clamp(target, 1, Count);
        if (!page.Set(target)) return;
        StateHasChanged();
        await PageChanged.InvokeAsync(target);
        await Events.RaiseChangeAsync(Element, target);
    }

    private async Task SetPageSizeAsync(int size)
    {
        var firstItem = (CurrentPage - 1) * pageSize.Value;
        if (size < 1 || !pageSize.Set(size)) return;
        await PageSizeChanged.InvokeAsync(size);
        var target = Math.Clamp(firstItem / size + 1, 1, Count);
        if (page.Set(target)) await PageChanged.InvokeAsync(target);
        StateHasChanged();
    }
}
