using System.ComponentModel;
using Nazor.Controls.Primitives;

namespace Nazor.Controls.Tests.Data;

public sealed class DataProviderPositionTests
{
    [Fact]
    public async Task Accepted_positions_survive_page_changes_and_retain_their_own_total()
    {
        var gate = new TaskCompletionSource<DataPage<int>>();
        var view = new DataView<int> { PageSize = 3, Page = 1, Provider = _ => Task.FromResult(new DataPage<int>([3, 4, 5], 8)) };
        await view.LoadAsync();
        var retained = view.Rows.ToArray();
        Assert.Equal([4, 5, 6], retained.Select(row => row.ItemPosition.Position));
        Assert.Equal([0, 1, 2], retained.Select(row => row.Index));
        Assert.All(retained, row => Assert.Same(view, row.ItemPosition.Sequence));
        view.Provider = _ => gate.Task;
        view.Page = 0;
        view.PageSize = 2;
        var pending = view.LoadAsync();
        Assert.Equal([4, 5, 6], view.Rows.Select(row => row.ItemPosition.Position));
        Assert.Equal(8, view.TotalCount);
        gate.SetResult(new DataPage<int>([10, 11], 2));
        await pending;
        Assert.Equal([1, 2], view.Rows.Select(row => row.ItemPosition.Position));
        Assert.All(view.Rows, row => Assert.Equal(2, row.ItemPosition.Count));
        Assert.All(retained, row => Assert.Equal(8, row.ItemPosition.Count));
    }

    [Theory]
    [InlineData("page")]
    [InlineData("page-size")]
    [InlineData("query")]
    [InlineData("sort")]
    [InlineData("provider")]
    public async Task Changing_a_request_cancels_it_before_another_load_starts(string setting)
    {
        var gate = new TaskCompletionSource<DataPage<int>>();
        DataRequest? request = null;
        var view = new DataView<int>
        {
            PageSize = 2,
            Provider = value => { request = value; return gate.Task; },
        };
        var pending = view.LoadAsync();
        var loadingStates = new List<bool>();
        view.Changed += () => loadingStates.Add(view.IsLoading);
        switch (setting)
        {
            case "page": view.Page = 2; break;
            case "page-size": view.PageSize = 3; break;
            case "query": view.Query = "new"; break;
            case "sort": view.SetSort("value", i => i, ListSortDirection.Descending); break;
            case "provider": view.Provider = _ => Task.FromResult(new DataPage<int>([9], 1)); break;
        }
        Assert.True(request!.CancellationToken.IsCancellationRequested);
        gate.SetResult(new DataPage<int>([0, 1], 10));
        await pending;
        Assert.True(view.NeedsLoad);
        Assert.False(view.IsLoading);
        Assert.False(loadingStates[^1]);
        Assert.Empty(view.Rows);
        Assert.Equal(0, view.TotalCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_results_and_faults_cannot_replace_a_newer_accepted_window(bool fault)
    {
        var old = new TaskCompletionSource<DataPage<int>>();
        var calls = 0;
        var view = new DataView<int> { PageSize = 2, Provider = _ => ++calls == 1 ? old.Task : Task.FromResult(new DataPage<int>([4, 5], 6)) };
        var pending = view.LoadAsync();
        view.Page = 2;
        await view.LoadAsync();
        if (fault) old.SetException(new InvalidOperationException("obsolete"));
        else old.SetResult(new DataPage<int>([0, 1], 100));
        await pending;
        Assert.Equal([5, 6], view.Rows.Select(row => row.ItemPosition.Position));
        Assert.Equal(6, view.TotalCount);
        Assert.Null(view.LoadError);
    }

    [Fact]
    public async Task Grouping_retains_provider_ordinals_even_for_equal_duplicate_items()
    {
        var view = new DataView<string>
        {
            PageSize = 4, Page = 1, GroupKey = value => value,
            Provider = _ => Task.FromResult(new DataPage<string>(["b", "a", "b", "a"], 12)),
        };
        await view.LoadAsync();
        var items = view.Rows.Where(row => row.Kind == DataRowKind.Item).ToArray();
        Assert.Equal(["a", "a", "b", "b"], items.Select(row => row.Item));
        Assert.Equal([0, 1, 2, 3], items.Select(row => row.Index));
        Assert.Equal([6, 8, 5, 7], items.Select(row => row.ItemPosition.Position));
        Assert.All(view.Rows.Where(row => row.Kind == DataRowKind.GroupHeader), row => Assert.Null(row.ItemPosition.Sequence));
        view.SetGroupExpanded("a", false);
        Assert.Equal([5, 7], view.Rows.Where(row => row.Kind == DataRowKind.Item).Select(row => row.ItemPosition.Position));
        view.ExpandAllGroups();
        view.GroupDirection = ListSortDirection.Descending;
        Assert.Equal([5, 7, 6, 8], view.Rows.Where(row => row.Kind == DataRowKind.Item).Select(row => row.ItemPosition.Position));
    }

    [Fact]
    public async Task Provider_membership_is_copied_and_removing_the_provider_ignores_its_pending_result()
    {
        var members = new List<int> { 2, 3 };
        var view = new DataView<int> { Source = [8, 9], PageSize = 2, Page = 1, Provider = _ => Task.FromResult(new DataPage<int>(members, 4)) };
        await view.LoadAsync();
        members.Clear();
        view.Refresh();
        Assert.Equal([2, 3], view.Items);
        Assert.Equal([3, 4], view.Rows.Select(row => row.ItemPosition.Position));
        var gate = new TaskCompletionSource<DataPage<int>>();
        view.Provider = _ => gate.Task;
        var pending = view.LoadAsync();
        view.Provider = null;
        view.Page = 0;
        Assert.Equal([8, 9], view.Items);
        gate.SetResult(new DataPage<int>([5, 6], 100));
        await pending;
        Assert.Equal([8, 9], view.Items);
        Assert.Equal([1, 2], view.Rows.Select(row => row.ItemPosition.Position));
        Assert.Equal(2, view.TotalCount);
    }

    [Theory]
    [InlineData(0, 2, 3, 10)]
    [InlineData(1, 2, 2, 3)]
    [InlineData(0, 2, 1, -1)]
    [InlineData(int.MaxValue, 2, 0, 0)]
    public async Task Malformed_windows_and_overflow_report_errors_without_publishing_positions(int page, int size, int count, int total)
    {
        var view = new DataView<int> { Page = page, PageSize = size, Provider = _ => Task.FromResult(new DataPage<int>(Enumerable.Range(0, count).ToArray(), total)) };
        await view.LoadAsync();
        Assert.NotNull(view.LoadError);
        Assert.Empty(view.Rows);
        Assert.Equal(0, view.TotalCount);
    }

    [Fact]
    public async Task An_empty_page_beyond_a_shrinking_total_has_no_item_position()
    {
        var view = new DataView<int> { PageSize = 2, Page = 9, Provider = _ => Task.FromResult(new DataPage<int>([], 3)) };
        await view.LoadAsync();
        Assert.Empty(view.Rows);
        Assert.Equal(3, view.TotalCount);
        Assert.Null(view.LoadError);
    }

    [Fact]
    public async Task Loading_observers_cannot_change_the_captured_request_into_a_different_window()
    {
        var requests = new List<DataRequest>();
        var view = new DataView<int> { PageSize = 2, Provider = request => { requests.Add(request); return Task.FromResult(new DataPage<int>([2, 3], 4)); } };
        void Move() => view.Page = 1;
        view.Changed += Move;
        await view.LoadAsync();
        Assert.Empty(requests);
        Assert.True(view.NeedsLoad);
        view.Changed -= Move;
        await view.LoadAsync();
        Assert.Equal(2, Assert.Single(requests).Skip);
        Assert.Equal([3, 4], view.Rows.Select(row => row.ItemPosition.Position));
    }
}
