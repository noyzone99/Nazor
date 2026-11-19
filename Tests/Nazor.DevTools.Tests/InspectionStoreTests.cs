using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

public sealed class InspectionStoreTests
{
    private static InspectionStore Store()
    {
        var store = new InspectionStore();
        store.Bind(CapturedData.Target());
        store.Apply(CapturedData.Tree());
        return store;
    }

    [Fact]
    public void Captured_trees_are_detached_on_input_and_read_and_apply_complete_deltas_atomically()
    {
        var store = new InspectionStore();
        store.Bind(CapturedData.Target());
        var initial = CapturedData.Tree();
        store.Apply(initial);
        initial.Components[0].Children[0] = "tampered";
        var view = store.Read();
        view.Tree!.Elements[0].Children[0] = "tampered";
        Assert.Equal("child", store.Read().Tree!.Components[0].Children[0]);
        Assert.Equal("text", store.Read().Tree!.Elements[0].Children[0]);
        var delta = CapturedData.Delta();
        Assert.Equal(RevisionOutcome.Accepted, store.Apply(delta));
        delta.Elements[0] = delta.Elements[0] with { Text = "tampered" };
        Assert.Equal("Changed content", store.Read().Tree!.Elements[1].Text);
        DevToolsJson.Write(store.Read().Tree!);
    }

    [Fact]
    public void A_gap_blocks_later_deltas_until_full_resync_and_old_replies_cannot_roll_back()
    {
        var store = Store();
        Assert.Equal(RevisionOutcome.ResyncRequired, store.Apply(CapturedData.Delta(4)));
        Assert.Equal(1, store.Read().Tree!.Revision);
        Assert.True(store.Read().IsRetained);
        Assert.Equal(RevisionOutcome.ResyncRequired, store.Apply(CapturedData.Delta(2)));
        Assert.True(store.Read().NeedsTree);
        Assert.Equal(RevisionOutcome.Accepted, store.Apply(CapturedData.Tree(5)));
        Assert.False(store.Read().NeedsTree);
        Assert.Equal(RevisionOutcome.Stale, store.Apply(CapturedData.Tree(1)));
        Assert.Equal(RevisionOutcome.Stale, store.Apply(CapturedData.Delta(4)));
        Assert.Equal(5, store.Read().Tree!.Revision);
    }

    [Fact]
    public void Invalid_candidate_topology_never_changes_visible_nodes_selection_or_revision()
    {
        var store = Store();
        store.Select("component", "child");
        var invalid = CapturedData.Delta() with { Components = [], Elements = [], RemovedComponents = ["child"] };
        Assert.Equal(RevisionOutcome.ResyncRequired, store.Apply(invalid));
        Assert.Equal(1, store.Read().Tree!.Revision);
        Assert.Contains(store.Read().Tree!.Components, c => c.Id == "child");
        Assert.Equal("child", store.Read().Selection!.NodeId);
        Assert.Throws<DevToolsException>(() => store.Apply(CapturedData.Tree(2) with { ComponentRoots = ["missing"] }));
        Assert.Equal(1, store.Read().Tree!.Revision);
    }

    [Fact]
    public void Removing_an_instance_clears_its_local_selection_and_cached_details()
    {
        var store = Store();
        store.Select("element", "e");
        store.Apply(CapturedData.Style());
        var delta = new TreeDelta(CapturedData.Stamp(), 1, 2, ["c"], [],
            [CapturedData.Tree().Components[0] with { Children = [], OutputElements = [] }], [], ["child"], ["e", "text"]);
        Assert.Equal(RevisionOutcome.Accepted, store.Apply(delta));
        Assert.Null(store.Read().Selection);
        Assert.Empty(store.Read().Styles);
        Assert.False(store.Select("element", "e"));
        Assert.True(store.Select("component", "c"));
        Assert.Empty(store.Read().Tree!.Elements);
    }

    [Fact]
    public void Paused_and_disconnected_navigation_keeps_provenance_while_current_stop_data_stays_separate()
    {
        var store = Store();
        store.Apply(CapturedData.Style());
        store.Select("component", "child");
        var capture = store.Read().Tree!.Capture;
        store.Bind(CapturedData.Target(2, "paused", "stop-2"));
        Assert.True(store.Read().IsRetained);
        Assert.Equal(capture, store.Read().Tree!.Capture);
        Assert.True(store.Select("element", "e"));
        Assert.False(store.Apply(CapturedData.Style(stamp: CapturedData.Stamp())));
        Assert.Equal(RevisionOutcome.Stale, store.Apply(CapturedData.Delta(2)));
        var debugger = CapturedData.Tree(1, CapturedData.Stamp(2, "stop-2"));
        store.Apply(debugger);
        Assert.Empty(store.Read().Styles);
        Assert.Equal("stop-2", store.Read().Selection!.Capture.StopId);
        store.Bind(CapturedData.Target(3));
        Assert.Equal(RevisionOutcome.Stale, store.Apply(CapturedData.Tree(4, CapturedData.Stamp(2, "stop-2"))));
        store.Disconnected();
        Assert.True(store.Select("component", "child"));
        Assert.Equal(debugger.Capture, store.Read().Tree!.Capture);
        Assert.True(store.Read().IsRetained);
    }

    [Fact]
    public void Replacing_a_target_clears_old_tree_details_frames_and_selection_even_when_ids_overlap()
    {
        var store = Store();
        store.Select("element", "e");
        store.Apply(CapturedData.Style());
        store.Apply(new FrameBatch(CapturedData.Stamp(), 0, 1, [CapturedData.Frame(1)]));
        store.Connecting(CapturedData.Target(session: "other").Identity);
        var next = store.Read();
        Assert.Null(next.Tree);
        Assert.Null(next.Selection);
        Assert.Null(next.Frames);
        Assert.Empty(next.Styles);
        store.Bind(CapturedData.Target(session: "other"));
        Assert.Equal(RevisionOutcome.Stale, store.Apply(CapturedData.Tree()));
        store.Apply(CapturedData.Tree(stamp: CapturedData.Stamp(session: "other")));
        Assert.Equal("other", store.Read().Tree!.Capture.SessionId);
        Assert.Throws<DevToolsException>(() => store.Bind(CapturedData.Target(session: "other") with { Identity = CapturedData.Target(session: "other").Identity with { ProcessInstanceId = "reused-pid" } }));
    }

    [Fact]
    public void Detail_and_frame_caches_are_bounded_detached_and_remain_readable_after_disconnect()
    {
        var store = Store();
        var tree = CapturedData.Tree() with { ElementRoots = Enumerable.Range(0, 20).Select(i => "n" + i).ToArray(),
            Components = [CapturedData.Tree().Components[0] with { Children = [], OutputElements = [] }],
            Elements = Enumerable.Range(0, 20).Select(i => new ElementInfo("n" + i, null, "element", "node", null, "c", "c", [])).ToArray() };
        store.Apply(tree);
        for (var i = 0; i < 20; i++) store.Apply(CapturedData.Style("n" + i));
        Assert.Equal(InspectionStore.MaxDetailEntries, store.Read().Styles.Count);
        var details = CapturedData.Style("n19");
        store.Apply(details);
        details.Rules[0].Declarations[0] = new("color", "tampered", "bad");
        Assert.Equal("red", store.Read().Styles["n19"].Rules[0].Declarations[0].Value);
        for (var i = 1; i <= 125; i++) Assert.Equal(RevisionOutcome.Accepted, store.Apply(new FrameBatch(CapturedData.Stamp(), i - 1, i, [CapturedData.Frame(i)])));
        Assert.Equal(120, store.Read().Frames!.Frames.Length);
        Assert.Equal(6, store.Read().Frames!.Frames[0].Number);
        Assert.Equal(RevisionOutcome.ResyncRequired, store.Apply(new FrameBatch(CapturedData.Stamp(), 126, 127, [CapturedData.Frame(127)])));
        Assert.Equal(RevisionOutcome.ResyncRequired, store.Apply(new FrameBatch(CapturedData.Stamp(), 125, 126, [CapturedData.Frame(126)])));
        Assert.Equal(RevisionOutcome.Accepted, store.Apply(new FrameBatch(CapturedData.Stamp(), 129, 130, [CapturedData.Frame(130)]), resynchronize: true));
        store.Disconnected();
        var cached = store.Read();
        cached.Frames!.Frames[0].Causes[0] = "tampered";
        Assert.Equal("render", store.Read().Frames!.Frames[0].Causes[0]);
        Assert.Single(store.Read().Frames!.Frames);
        Assert.NotEmpty(store.Read().Styles);
    }

    [Fact]
    public void New_details_are_detached_and_selection_revisions_reject_late_or_removed_instances()
    {
        var store = new InspectionStore(); store.Bind(CapturedData.Target());
        var tree = CapturedData.LinkedTree(); store.Apply(tree);
        tree.Components[2].Parameters![0] = new("Title", "tampered", "supplied");
        var read = store.Read(); read.Tree!.Elements[0].Classes![0] = "tampered";
        Assert.Equal("Captured title", store.Read().Tree!.Components[2].Parameters![0].Value);
        Assert.Equal("card", store.Read().Tree!.Elements[0].Classes![0]);
        var chosen = new SelectionResult(CapturedData.Stamp(), "element", "e") { Revision = 10, IsInspecting = true };
        Assert.Equal(RevisionOutcome.Accepted, store.Apply(chosen));
        Assert.Equal(RevisionOutcome.Stale, store.Apply(chosen with { NodeId = "e2", Revision = 9, Capture = CapturedData.Stamp() }));
        Assert.Equal("e", store.Read().Selection!.NodeId);
        Assert.True(store.Read().IsInspecting);
        store.Select("component", "child");
        store.Apply(chosen with { Revision = 11 });
        Assert.Equal("child", store.Read().Selection!.NodeId);
        Assert.Equal(RevisionOutcome.ResyncRequired, store.Apply(chosen with { NodeId = "removed", Revision = 12 }));
        store.Bind(CapturedData.Target(2, "paused", "stop-2"));
        Assert.False(store.Read().IsInspecting);
        Assert.Equal(RevisionOutcome.Stale, store.Apply(chosen with { Revision = 12 }));
    }

    [Fact]
    public void Rich_style_arrays_are_detached_and_current_context_frames_survive_a_later_first_tree()
    {
        var store = new InspectionStore(); store.Bind(CapturedData.Target());
        store.Apply(new FrameBatch(CapturedData.Stamp(), 0, 1, [CapturedData.Frame(1)]), true);
        store.Apply(CapturedData.Tree());
        Assert.Single(store.ReadFrames()!.Frames);
        var details = CapturedData.Style() with { CustomProperties = [new("--accent", "red", "rule")],
            Rules = [new("Card", null, null, [new("color", "red", "winning") { Longhands = ["color"], Winning = ["color"] }])] };
        store.Apply(details);
        details.Rules[0].Declarations[0].Winning![0] = "tampered";
        var read = store.Read(); read.Styles["e"].Rules[0].Declarations[0].Longhands![0] = "tampered";
        Assert.Equal("color", store.Read().Styles["e"].Rules[0].Declarations[0].Winning![0]);
        Assert.Equal("color", store.Read().Styles["e"].Rules[0].Declarations[0].Longhands![0]);
        store.Bind(CapturedData.Target(2, "paused", "stop-2"));
        Assert.NotEmpty(store.Read().Styles); Assert.NotNull(store.ReadFrames());
        Assert.False(store.Apply(store.Read().Styles["e"]));
        store.Bind(CapturedData.Target(3)); store.Apply(CapturedData.Tree(stamp: CapturedData.Stamp(3)));
        Assert.Empty(store.Read().Styles); Assert.Null(store.ReadFrames());
    }

    [Fact]
    public void Unresponsive_status_does_not_invent_a_new_capture_and_clear_removes_retained_data()
    {
        var store = Store();
        var stamp = store.Read().Tree!.Capture;
        store.Unresponsive("No UI response");
        store.Bind(CapturedData.Target());
        Assert.Equal(InspectorConnectionStatus.Unresponsive, store.Read().Connection);
        Assert.True(store.Read().IsRetained);
        Assert.Equal(stamp, store.Read().Tree!.Capture);
        store.Clear();
        Assert.Null(store.Read().Target);
        Assert.Null(store.Read().Tree);
        Assert.Equal(InspectorConnectionStatus.Disconnected, store.Read().Connection);
    }
}
