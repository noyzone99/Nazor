using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Data.Fixtures;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Controls.Tests.Data;

[Collection("Controls")]
public sealed class DataGridProviderPositionTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();

    [Fact]
    public void Provider_styles_keep_the_accepted_window_during_loading_and_ignore_late_responses()
    {
        h.Scene.Styles.Application = NssCompiler.Compile("positions", "DataGridRow:nth-item(even) { opacity: .75; } DataGridRow:last-item { opacity: .5; }").StyleSheet;
        h.Render<ProviderGridProbe>();
        var probe = h.Component<ProviderGridProbe>();
        Complete(probe, 0, [3, 4, 5], 8);
        AssertPositions([4, 5, 6], 8);
        var rows = h.All("DataGridRow").ToArray();
        Assert.Equal(.75, h.Style(rows[0]).Opacity);
        Assert.Equal(1, h.Style(rows[1]).Opacity);
        Assert.Equal(true, rows[0].Attributes["alternate"]);
        Assert.Equal(false, rows[1].Attributes["alternate"]);
        Change(probe, () => { probe.Page = 2; probe.Size = 3; });
        AssertPositions([4, 5, 6], 8);
        Assert.Equal(true, h.One("DataGrid").Attributes["busy"]);
        Change(probe, () => { probe.Page = 0; probe.Size = 2; probe.Query = "new query"; });
        Assert.True(probe.Requests[1].Request.CancellationToken.IsCancellationRequested);
        Complete(probe, 2, [0, 1], 2);
        AssertPositions([1, 2], 2);
        var first = h.All("DataGridRow").First();
        Assert.Equal(1, h.Style(first).Opacity);
        Assert.Equal(.5, h.Style(h.All("DataGridRow").Last()).Opacity);
        Assert.True(new ItemPositionSelector(0, 1, false, ":first-item").Matches(first));
        Complete(probe, 1, [6, 7], 8);
        AssertPositions([1, 2], 2);
        Assert.Equal(false, h.One("DataGrid").Attributes["busy"]);
        Change(probe, () => probe.Details = value => b => b.AddContent(0, value));
        h.Click(h.All("DataGridDetailsCell").First());
        Assert.DoesNotContain("item-position", h.One("DataGridRowDetails").Attributes.Keys);
        AssertPositions([1, 2], 2);
    }

    [Fact]
    public void Retained_item_keys_refresh_styles_when_server_sorting_changes_their_positions()
    {
        h.Scene.Styles.Application = NssCompiler.Compile("positions", "DataGridRow:nth-item(even) { opacity: .75; } DataGridRow:last-item { opacity: .5; }").StyleSheet;
        h.Render<ProviderGridProbe>();
        var probe = h.Component<ProviderGridProbe>();
        Complete(probe, 0, [3, 4, 5], 8);
        var retained = h.All("DataGridRow").Last();
        Assert.Equal(.75, h.Style(retained).Opacity);
        Change(probe, () => { probe.Page = 0; probe.Query = "reverse"; });
        Complete(probe, 1, [5, 4, 3], 3);
        Assert.Same(retained, h.All("DataGridRow").First());
        Assert.Equal(1, h.Style(retained).Opacity);
        AssertPositions([1, 2, 3], 3);
        Assert.Equal(.5, h.Style(h.All("DataGridRow").Last()).Opacity);
    }

    [Fact]
    public void Virtualized_provider_rows_use_global_positions_after_scrolling()
    {
        h.Render<ProviderGridProbe>();
        var probe = h.Component<ProviderGridProbe>();
        Change(probe, () => { probe.Size = 200; probe.Page = 2; });
        Complete(probe, 1, Enumerable.Range(400, 200).ToArray(), 600);
        Assert.InRange(h.All("DataGridRow").Count(), 1, 30);
        h.Input.ScrollTo(h.One("DataGridBody"), null, 20_000);
        h.Frame();
        var last = h.All("DataGridRow").Last();
        var position = Assert.IsType<LogicalItemPosition>(last.Attributes["item-position"]);
        Assert.Equal(600, position.Position);
        Assert.Equal(600, position.Count);
        Assert.True(new ItemPositionSelector(0, 1, true, ":last-item").Matches(last));
        Assert.All(h.All("DataGridBodySpacer"), spacer => Assert.False(spacer.Attributes.ContainsKey("item-position")));
        Complete(probe, 0, [3, 4, 5], 8);
        Assert.Equal(600, Assert.IsType<LogicalItemPosition>(h.All("DataGridRow").Last().Attributes["item-position"]).Position);
    }

    private void Change(ProviderGridProbe probe, Action change)
    {
        change();
        h.Host.Renderer.Dispatcher.InvokeAsync(probe.Refresh);
        h.Frame();
    }

    private void Complete(ProviderGridProbe probe, int request, IReadOnlyList<int> items, int total)
    {
        probe.Requests[request].Completion.SetResult(new DataPage<int>(items, total));
        h.Host.RunUntil(() => !probe.View.IsLoading);
        h.Frame();
    }

    private void AssertPositions(int[] positions, int total)
    {
        var actual = h.All("DataGridRow").Select(row => Assert.IsType<LogicalItemPosition>(row.Attributes["item-position"])).ToArray();
        Assert.Equal(positions, actual.Select(position => position.Position));
        Assert.All(actual, position => Assert.Equal(total, position.Count));
    }
}
