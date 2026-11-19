using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;

var identity = new TargetIdentity(42, "process-instance", "runtime-instance", "app-instance", "window-instance", "session-instance");
CaptureStamp Stamp() => new(identity.SessionId, 1, "live", null, "2026-10-25T10:00:00.000Z");
TreeSnapshot Snapshot(long revision = 1) => new(Stamp(), revision, ["c1"], ["e1"],
    [new("c1", null, "Fixture.Counter", null, false, [], ["e1"])],
    [new("e1", null, "text", "#text", "Caffè 漢字 🙂", "c1", "c1", [])]);

if (args is ["--emit", var path])
{
    File.WriteAllText(path, DevToolsJson.Write(Snapshot()).ToJsonString());
    return;
}
if (args is ["--check", var pathToCheck])
{
    var snapshot = DevToolsJson.Read<TreeSnapshot>(JsonNode.Parse(File.ReadAllText(pathToCheck)));
    if (snapshot.Elements[0].Text != "Caffè 漢字 🙂") throw new InvalidDataException("The independent fixture lost Unicode data.");
    Console.WriteLine("PASS: independent JSON snapshot validated.");
    return;
}

var applied = 0;
using var peer = new DevToolsPeer(Console.OpenStandardInput(), Console.OpenStandardOutput(), "independent fixture",
    [DevToolsCapabilities.Tree, DevToolsCapabilities.Selection], new(identity, 1, "running", null));
peer.RequestHandler = async (request, token) =>
{
    if (request.Method == DevToolsProtocol.GetTree)
    {
        await Task.Delay(Math.Clamp(request.Parameters["delayMs"]?.GetValue<int>() ?? 0, 0, 100), token);
        return DevToolsJson.Write(Snapshot(1 + applied));
    }
    if (request.Method == DevToolsProtocol.Select)
    {
        await Task.Delay(100, token);
        request.ApplyMutation(() => applied++);
        return DevToolsJson.Write(new SelectionResult(Stamp(), "element", "e1"));
    }
    throw new DevToolsException(DevToolsErrors.MethodNotFound, "Unknown fixture method.");
};
peer.Start();
await peer.Closed;
