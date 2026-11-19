using System.ComponentModel;
using Nazor.Controls.Primitives;

namespace Nazor.Controls.Tests.Data;

public sealed class DataViewTests
{
    private sealed record Row(int Id, string Name, string Team, int Score);

    private static readonly Row[] Rows =
    [
        new(1, "Ada", "Red", 30), new(2, "Bob", "Blue", 10), new(3, "Cy", "Red", 20), new(4, "Di", "Blue", 30),
        new(5, "Ed", "Green", 20), new(6, "Flo", "Red", 10), new(7, "Gus", "Blue", 20),
    ];

    [Fact]
    public void Without_settings_the_view_shows_the_source_in_order()
    {
        var view = new DataView<Row> { Source = Rows };
        Assert.Equal(Rows, view.Items);
        Assert.Equal(7, view.TotalCount);
        Assert.Equal(1, view.PageCount);
        Assert.All(view.Rows, r => Assert.Equal(DataRowKind.Item, r.Kind));
        Assert.Equal(Enumerable.Range(0, 7), view.Rows.Select(r => r.Index));
        Assert.Empty(view.Groups);
    }

    [Fact]
    public void Sorting_by_several_keys_is_stable()
    {
        var view = new DataView<Row> { Source = Rows };
        view.SetSort("score", r => r.Score, ListSortDirection.Descending);
        Assert.Equal([1, 4, 3, 5, 7, 2, 6], view.Items.Select(r => r.Id));
        view.SetSort("team", r => r.Team, ListSortDirection.Ascending, add: true);
        Assert.Equal([4, 1, 7, 5, 3, 2, 6], view.Items.Select(r => r.Id));
        Assert.Equal(ListSortDirection.Ascending, view.SortDirectionOf("team"));
        Assert.Equal(1, view.SortIndexOf("team"));
        Assert.Equal(-1, view.SortIndexOf("name"));
        view.SetSort("score", r => r.Score, null, add: true);
        Assert.Equal([2, 4, 7, 5, 1, 3, 6], view.Items.Select(r => r.Id));
        view.SetSort("name", r => r.Name, ListSortDirection.Descending);
        Assert.Single(view.Sorts);
        Assert.Equal(7, view.Items[0].Id);
    }

    [Fact]
    public void Nulls_sort_first_and_mixed_numbers_compare_by_value()
    {
        object?[] values = [3, null, 1.5, 2L];
        var view = new DataView<object?> { Source = values };
        view.SetSort("v", v => v, ListSortDirection.Ascending);
        Assert.Equal([null, 1.5, 2L, 3], view.Items);
    }

    [Fact]
    public void A_filter_and_paging_window_the_items()
    {
        var view = new DataView<Row> { Source = Rows, Filter = r => r.Score >= 20, PageSize = 2 };
        Assert.Equal(5, view.TotalCount);
        Assert.Equal(3, view.PageCount);
        Assert.Equal([1, 3], view.Items.Select(r => r.Id));
        view.Page = 2;
        Assert.Equal([7], view.Items.Select(r => r.Id));
        Assert.Equal(4, view.Rows[0].Index);
        view.Page = 9;
        Assert.Equal([7], view.Items.Select(r => r.Id));
        Assert.Equal(5, view.FilteredItems.Count);
    }

    [Fact]
    public void Groups_have_headers_counts_and_collapse()
    {
        var view = new DataView<Row> { Source = Rows, GroupKey = r => r.Team };
        Assert.Equal(["Blue", "Green", "Red"], view.Groups.Select(g => (string)g.Key!));
        Assert.Equal([3, 1, 3], view.Groups.Select(g => g.Count));
        Assert.Equal(10, view.Rows.Count);
        Assert.Equal(DataRowKind.GroupHeader, view.Rows[0].Kind);
        Assert.Equal([2, 4, 7], view.Rows.Skip(1).Take(3).Select(r => r.Item.Id));
        view.ToggleGroup("Blue");
        Assert.False(view.IsGroupExpanded("Blue"));
        Assert.Equal(7, view.Rows.Count);
        Assert.False(view.Groups[0].IsExpanded);
        view.CollapseAllGroups();
        Assert.Equal(3, view.Rows.Count);
        view.ExpandAllGroups();
        Assert.Equal(10, view.Rows.Count);
        view.GroupDirection = ListSortDirection.Descending;
        Assert.Equal("Red", view.Groups[0].Key);
        view.SetSort("score", r => r.Score, ListSortDirection.Ascending);
        Assert.Equal([6, 3, 1], view.Groups[0].Items.Select(r => r.Id));
    }

    [Fact]
    public void Group_headers_repeat_on_each_page_with_the_full_count()
    {
        var view = new DataView<Row> { Source = Rows, GroupKey = r => r.Team, PageSize = 2, Page = 1 };
        Assert.Equal(DataRowKind.GroupHeader, view.Rows[0].Kind);
        Assert.Equal("Blue", view.Rows[0].Group!.Key);
        Assert.Equal(3, view.Rows[0].Group!.Count);
        Assert.Equal(DataRowKind.GroupHeader, view.Rows[2].Kind);
        Assert.Equal("Green", view.Rows[2].Group!.Key);
    }

    [Fact]
    public void Changes_raise_changed_and_refresh_recomputes_mutated_sources()
    {
        var list = new List<Row>(Rows);
        var view = new DataView<Row> { Source = list };
        var changes = 0;
        view.Changed += () => changes++;
        Assert.Equal(7, view.TotalCount);
        var version = view.Version;
        list.Add(new Row(8, "Hal", "Red", 5));
        Assert.Equal(7, view.TotalCount);
        view.Refresh();
        Assert.Equal(8, view.TotalCount);
        Assert.NotEqual(version, view.Version);
        view.PageSize = 3;
        view.PageSize = 3;
        Assert.Equal(2, changes);
        Assert.Throws<ArgumentOutOfRangeException>(() => view.Page = -1);
    }

    [Fact]
    public async Task A_provider_receives_the_request_and_supplies_the_page()
    {
        var requests = new List<DataRequest>();
        var view = new DataView<Row>
        {
            Provider = request =>
            {
                requests.Add(request);
                IEnumerable<Row> query = Rows;
                if (request.Query is string team) query = query.Where(r => r.Team == team);
                if (request.Sorts.FirstOrDefault() is { Key: "score" } sort)
                    query = sort.Direction == ListSortDirection.Ascending ? query.OrderBy(r => r.Score) : query.OrderByDescending(r => r.Score);
                var all = query.ToList();
                return Task.FromResult(new DataPage<Row>([.. all.Skip(request.Skip).Take(request.Take ?? int.MaxValue)], all.Count));
            },
            PageSize = 2,
        };
        Assert.True(view.NeedsLoad);
        await view.LoadAsync();
        Assert.False(view.NeedsLoad);
        Assert.Equal(7, view.TotalCount);
        Assert.Equal(4, view.PageCount);
        Assert.Equal([1, 2], view.Items.Select(r => r.Id));
        view.Query = "Red";
        view.SetSort("score", r => r.Score, ListSortDirection.Descending);
        view.Page = 1;
        await view.LoadAsync();
        var last = requests[^1];
        Assert.Equal(2, last.Skip);
        Assert.Equal(2, last.Take);
        Assert.Equal([new DataSort("score", ListSortDirection.Descending)], last.Sorts);
        Assert.Equal([6], view.Items.Select(r => r.Id));
        Assert.Equal(3, view.TotalCount);
        await view.LoadAsync();
        Assert.Equal(2, requests.Count);
        await view.LoadAsync(force: true);
        Assert.Equal(3, requests.Count);
    }

    [Fact]
    public async Task A_failing_provider_reports_the_error_and_a_newer_request_wins()
    {
        var gate = new TaskCompletionSource<DataPage<Row>>();
        var calls = 0;
        var view = new DataView<Row>
        {
            Provider = request => ++calls == 1 ? gate.Task : Task.FromResult(new DataPage<Row>([Rows[0]], 1)),
        };
        var first = view.LoadAsync();
        Assert.True(view.IsLoading);
        await view.LoadAsync(force: true);
        Assert.False(view.IsLoading);
        gate.SetResult(new DataPage<Row>(Rows, 7));
        await first;
        Assert.Equal(1, view.TotalCount);
        view.Provider = _ => throw new InvalidOperationException("offline");
        await view.LoadAsync();
        Assert.Equal("offline", view.LoadError?.Message);
        Assert.Empty(view.Items);
    }
}
