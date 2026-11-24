using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Protocol.Tests;

public sealed class ContractTests
{
    [Fact]
    public void Renderer_diagnostics_round_trip_without_confusing_worker_time_with_ui_paint()
    {
        var frame = Frames(0,1).Frames[0] with
        {
            Renderer="GPU", RequestedRenderer="AUTO", GraphicsBackend="Skia OpenGL", GraphicsDevice="Test GPU",
            RenderWorkerFrame=9, RenderWorkerMs=2.5, GpuFramesReplaced=3,
        };
        var copy = DevToolsJson.Read<FrameRecord>(DevToolsJson.Write(frame));
        Assert.Equal("GPU",copy.Renderer);
        Assert.Equal(9,copy.RenderWorkerFrame);
        Assert.Equal(2.5,copy.RenderWorkerMs);
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(frame with { RenderWorkerMs = -1 }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(frame with { RenderWorkerFrame = -1 }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(frame with { Renderer = "AUTO" }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(frame with { GraphicsDevice = new string('x',2049) }));
    }

    internal static readonly TargetIdentity Identity = new(42, "process-instance", "runtime-instance", "app-instance", "window-instance", "session-instance");
    internal static TargetState State(long generation = 1, string state = "running", string? stop = null) => new(Identity, generation, state, stop);
    internal static RequestContext Context(long generation = 1, string? stop = null) => new(Identity.SessionId, generation, stop);
    internal static CaptureStamp Stamp(long generation = 1, string? stop = null) => new(Identity.SessionId, generation, stop is null ? "live" : "debugger", stop, "2026-10-25T10:00:00.0000000+00:00");
    internal static TreeSnapshot Snapshot(long revision = 1) => new(Stamp(), revision, ["c1"], ["e1"],
        [new("c1", null, "Company.Contatore漢字🙂", null, false, [], ["e1"])],
        [new("e1", null, "text", "#text", "Caffè 漢字 🙂", "c1", "c1", [])]);
    internal static TreeDelta Delta(long baseRevision, long revision) => new(Stamp(), baseRevision, revision, ["c1"], ["e1"], [], [], [], []);
    internal static FrameBatch Frames(long baseRevision, long revision) => new(Stamp(), baseRevision, revision,
        [new(revision, ["layout"], new(0, 1, 0, 2, 3), 2, 1, 1, [new(0, 0, 10, 10)])]);

    [Fact]
    public void Contracts_round_trip_without_runtime_or_platform_types()
    {
        var snapshot = Snapshot();
        var json = DevToolsJson.Write(snapshot);
        var result = DevToolsJson.Read<TreeSnapshot>(json);
        Assert.Equal(snapshot.Elements[0].Text, result.Elements[0].Text);
        Assert.Equal(snapshot.Components[0].TypeName, result.Components[0].TypeName);
        Assert.Equal(Identity, DevToolsJson.Read<TargetIdentity>(DevToolsJson.Write(Identity)));
        Assert.Equal(State(2, "paused", "stop-2"), DevToolsJson.Read<TargetState>(DevToolsJson.Write(State(2, "paused", "stop-2"))));
        DevToolsJson.Read<FrameBatch>(DevToolsJson.Write(Frames(0, 1)));
        DevToolsJson.Read<StyleDetails>(DevToolsJson.Write(new StyleDetails(Stamp(), "e1", [new("color", "red", "local")], [new(".item", "Theme.nss", 1, [])])));
        DevToolsJson.Read<GeometryDetails>(DevToolsJson.Write(new GeometryDetails(Stamp(), "e1", new(0, 0, 10, 20), new(-1, 0, 0, 0), new(1, 1, 1, 1), new(2, 2, 2, 2), 1.5)));
        Assert.DoesNotContain(typeof(TreeSnapshot).Assembly.GetReferencedAssemblies(), name => name.Name!.StartsWith("Nazor.", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => DevToolsJson.Write(new { RuntimeObject = new object() }));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"sessionId\":\"s\",\"generation\":1}")]
    [InlineData("{\"sessionId\":null,\"generation\":1,\"stopId\":null}")]
    [InlineData("{\"sessionId\":\"s\",\"generation\":-1,\"stopId\":null}")]
    [InlineData("{\"sessionId\":\"s\",\"generation\":9007199254740992,\"stopId\":null}")]
    [InlineData("{\"sessionId\":\"s\",\"sessionId\":\"other\",\"generation\":1,\"stopId\":null}")]
    public void Missing_null_duplicate_and_unsafe_numeric_fields_are_rejected(string json)
    {
        var exception = Assert.Throws<DevToolsException>(() => DevToolsJson.Read<RequestContext>(JsonNode.Parse(json)));
        Assert.Equal(DevToolsErrors.InvalidParams, exception.Code);
    }

    [Fact]
    public void Minor_version_extensions_are_ignored_but_bad_identity_provenance_and_links_are_rejected()
    {
        var json = DevToolsJson.Write(Context());
        json["futureField"] = true;
        Assert.Equal(Context(), DevToolsJson.Read<RequestContext>(json));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Stamp() with { Source = "debugger", StopId = null }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Stamp() with { Source = "live", StopId = "stop" }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(State(1, "paused")));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Snapshot() with { ElementRoots = [] }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Snapshot() with { Components = [Snapshot().Components[0] with { Children = ["missing"] }] }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Snapshot() with { Components = [new("c1", "c1", "Cycle", null, false, ["c1"], [])], ComponentRoots = [] }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Delta(1, 3)));
    }

    [Fact]
    public void Version_1_0_snapshots_remain_readable_with_unavailable_optional_metadata()
    {
        var json = DevToolsJson.Write(Snapshot());
        foreach (var component in json["components"]!.AsArray())
        {
            component!.AsObject().Remove("rootId");
            component.AsObject().Remove("declaringComponentId");
        }
        foreach (var element in json["elements"]!.AsArray())
        {
            element!.AsObject().Remove("rootId");
            element.AsObject().Remove("key");
            element.AsObject().Remove("isRendered");
        }
        var legacy = DevToolsJson.Read<TreeSnapshot>(json);
        Assert.Null(legacy.Elements[0].IsRendered);
        Assert.Null(legacy.Elements[0].RootId);
        Assert.Null(legacy.Components[0].DeclaringComponentId);
        var current = Snapshot() with { Elements = [Snapshot().Elements[0] with { RootId = "c1", IsRendered = false, Key = "k" }] };
        Assert.False(DevToolsJson.Read<TreeSnapshot>(DevToolsJson.Write(current)).Elements[0].IsRendered);
    }

    [Fact]
    public void Version_1_2_metadata_is_optional_bounded_and_keeps_callbacks_out_of_the_wire_contract()
    {
        var tree = Snapshot();
        tree.Components[0] = tree.Components[0] with { Parameters = [new("Title", "A title", "supplied")] };
        tree.Elements[0] = tree.Elements[0] with { ElementId = "markup-id", Attributes = [new("title", "Hint", "attribute")], Classes = ["card"], States = ["focus"], Events = ["onclick"] };
        var current = DevToolsJson.Read<TreeSnapshot>(DevToolsJson.Write(tree));
        Assert.Equal("A title", current.Components[0].Parameters![0].Value);
        Assert.Equal("markup-id", current.Elements[0].ElementId);
        var json = DevToolsJson.Write(tree);
        Assert.False(json["elements"]![0]!.AsObject().ContainsKey("isRendered"));
        Assert.True(json["elements"]![0]!.AsObject().ContainsKey("parentId"));
        json["components"]![0]!.AsObject().Remove("parameters");
        foreach (var field in new[] { "elementId", "attributes", "classes", "states", "events" }) json["elements"]![0]!.AsObject().Remove(field);
        var legacy = DevToolsJson.Read<TreeSnapshot>(json);
        Assert.Null(legacy.Components[0].Parameters); Assert.Null(legacy.Elements[0].Attributes);
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(current.Components[0] with { Parameters = Enumerable.Range(0, 129).Select(i => new PropertyValue(i.ToString(), "x", "supplied")).ToArray() }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(current.Components[0] with { Parameters = [new("x", "1", "supplied"), new("x", "2", "supplied")] }));
        var selection = new SelectionResult(Stamp(), "element", "e1") { IsInspecting = true, Revision = 1 };
        Assert.True(DevToolsJson.Read<SelectionResult>(DevToolsJson.Write(selection)).IsInspecting);
        DevToolsJson.Write(new InspectionRequest(Context(), true));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(selection with { Revision = 0 }));
    }

    [Fact]
    public void Version_1_3_style_geometry_and_frame_extensions_are_optional_and_validate_complete_values()
    {
        var styles = new StyleDetails(Stamp(), "e1", [new("color", "red", "rule") { Rule = 0, Declaration = 0 }],
            [new(".card", "app.nss", null, [new("color", "red", "winning") { Longhands = ["color"], Winning = ["color"] }]) { Level = "application", Order = 2, Specificity = "0,1,0", MediaMatches = true }])
            { CustomProperties = [new("--accent", "red", "rule")] };
        Assert.Equal("color", DevToolsJson.Read<StyleDetails>(DevToolsJson.Write(styles)).Rules[0].Declarations[0].Winning![0]);
        var geometry = new GeometryDetails(Stamp(), "e1", new(10, 20, 30, 40), new(1, 2, 3, 4), new(1, 1, 1, 1), new(2, 2, 2, 2), 1.5)
            { MarginBox = new(6, 19, 36, 44), PaddingBox = new(11, 21, 28, 38), ContentBox = new(13, 23, 24, 34) };
        Assert.NotNull(DevToolsJson.Read<GeometryDetails>(DevToolsJson.Write(geometry)).ContentBox);
        var legacy = DevToolsJson.Write(geometry); foreach (var key in new[] { "marginBox", "paddingBox", "contentBox" }) legacy.AsObject().Remove(key);
        Assert.Null(DevToolsJson.Read<GeometryDetails>(legacy).ContentBox);
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(styles with { Computed = [styles.Computed[0] with { Rule = 4 }] }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(styles.Rules[0].Declarations[0] with { Winning = ["margin"] }));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(new FrameTimings(0, 0, 0, 0, 0) { OverlayMs = -1 }));
    }

    [Fact]
    public void Contract_limits_accept_the_boundary_and_reject_the_next_value()
    {
        DevToolsJson.Write(Context(DevToolsProtocol.MaxSequence));
        DevToolsJson.Write(Snapshot() with { Elements = [Snapshot().Elements[0] with { Text = new('x', DevToolsProtocol.MaxStringLength) }] });
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(Snapshot() with { Elements = [Snapshot().Elements[0] with { Text = new('x', DevToolsProtocol.MaxStringLength + 1) }] }));
        var components = Enumerable.Range(0, DevToolsProtocol.MaxNodes).Select(i => new ComponentInfo("c" + i, null, "Component", null, false, [], [])).ToArray();
        DevToolsJson.Write(new TreeSnapshot(Stamp(), 1, components.Select(c => c.Id).ToArray(), [], components, []));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(new TreeSnapshot(Stamp(), 1, components.Select(c => c.Id).ToArray(), ["e1"], components, Snapshot().Elements)));
        var frames = Enumerable.Range(1, DevToolsProtocol.MaxFrames).Select(i => Frames(i - 1, i).Frames[0]).ToArray();
        DevToolsJson.Write(new FrameBatch(Stamp(), 0, 1, frames));
        Assert.Throws<DevToolsException>(() => DevToolsJson.Write(new FrameBatch(Stamp(), 0, 1, [.. frames, frames[^1] with { Number = 121 }])));
    }

    [Fact]
    public void Revision_gaps_require_resynchronization_and_never_advance_the_cursor_partially()
    {
        var cursor = new RevisionCursor(Context());
        Assert.Equal(RevisionOutcome.ResyncRequired, cursor.Accept(Delta(1, 2)));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Snapshot()));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Delta(1, 2)));
        Assert.Equal(RevisionOutcome.Stale, cursor.Accept(Delta(1, 2)));
        Assert.Equal(RevisionOutcome.ResyncRequired, cursor.Accept(Delta(3, 4)));
        Assert.Equal(RevisionOutcome.ResyncRequired, cursor.Accept(Delta(2, 3)));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Snapshot(4)));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Delta(4, 5)));
        Assert.Equal(RevisionOutcome.Stale, cursor.Accept(Snapshot(6) with { Capture = Stamp(2) }));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Frames(0, 1)));
        Assert.Equal(RevisionOutcome.ResyncRequired, cursor.Accept(Frames(2, 3)));
        Assert.Equal(RevisionOutcome.ResyncRequired, cursor.Accept(Frames(1, 2)));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Frames(3, 4), resynchronize: true));
        Assert.Equal(RevisionOutcome.Accepted, cursor.Accept(Frames(4, 5)));
        Assert.Equal(RevisionOutcome.Stale, new RevisionCursor(Context(1, "new-stop")).Accept(Snapshot()));
    }
}
