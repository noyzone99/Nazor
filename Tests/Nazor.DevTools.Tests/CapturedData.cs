using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

internal static class CapturedData
{
    internal static TargetState Target(long generation = 1, string state = "running", string? stop = null, string session = "session") =>
        new(new(42, "process", "runtime", "app", "window", session), generation, state, stop);
    internal static CaptureStamp Stamp(long generation = 1, string? stop = null, string session = "session") =>
        new(session, generation, stop is null ? "live" : "debugger", stop, DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
    internal static TreeSnapshot Tree(long revision = 1, CaptureStamp? stamp = null) => new(stamp ?? Stamp(), revision, ["c"], ["e"],
        [new("c", null, "Fixture.Page", null, false, ["child"], ["e"]) { RootId = "c" },
         new("child", "c", "Fixture.Card", "card", false, [], ["e"]) { RootId = "c", DeclaringComponentId = "c" }],
        [new("e", null, "element", "Card", null, "child", "c", ["text"]) { RootId = "c", IsRendered = false },
         new("text", "e", "text", "#text", "Captured content", "child", "c", []) { RootId = "c" }]);
    internal static TreeSnapshot LinkedTree()
    {
        var tree = Tree();
        return tree with
        {
            Components = [
                tree.Components[0] with { Children = ["framework", "twin", "empty"], OutputElements = ["e", "e2"] },
                new("framework", "c", "Nazor.Controls.Container", null, true, ["child"], ["e", "e2"]) { RootId = "c" },
                tree.Components[1] with { ParentId = "framework", OutputElements = ["e", "e2"], Parameters = [new("Title", "Captured title", "supplied")] },
                new("twin", "c", "Fixture.Card", "other-card", false, [], []) { RootId = "c" },
                new("empty", "c", "Fixture.Empty", null, false, [], []) { RootId = "c" }
            ],
            ElementRoots = ["e", "e2"],
            Elements = [tree.Elements[0] with { ElementId = "card-id", Classes = ["card", "selected"], States = ["focus"],
                Attributes = [new("title", "Captured attribute", "attribute")], Events = ["onclick"] }, tree.Elements[1],
                new("e2", null, "element", "Hidden", null, "child", "c", []) { RootId = "c", IsRendered = false }]
        };
    }
    internal static TreeDelta Delta(long revision = 2, CaptureStamp? stamp = null) => new(stamp ?? Stamp(), revision - 1, revision,
        ["c"], ["e"], [], [Tree().Elements[1] with { Text = "Changed content" }], [], []);
    internal static StyleDetails Style(string id = "e", CaptureStamp? stamp = null) => new(stamp ?? Stamp(), id,
        [new("color", "red", "theme")], [new("Card", null, null, [new("color", "red", "theme")])]);
    internal static FrameRecord Frame(long number) => new(number, ["render"], new(1, 2, 3, 4, 10), 4, 2, 2, [new(0, 0, 10, 10)]);
}
