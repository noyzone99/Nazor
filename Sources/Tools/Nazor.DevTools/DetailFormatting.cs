using System.Globalization;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public static class DetailFormatting
{
    public static string Number(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
    public static string Milliseconds(double n) => n.ToString(n < 10 ? "0.0#" : "0.#", CultureInfo.InvariantCulture) + " ms";
    public static string Age(TimeSpan age) => age.TotalSeconds < 1 ? "just now" : age.TotalSeconds < 60 ? (long)age.TotalSeconds + " s ago" :
        age.TotalMinutes < 60 ? (long)age.TotalMinutes + " min ago" : (long)age.TotalHours + " h ago";
    public static string Capture(CaptureStamp stamp, TargetState? target, InspectorConnectionStatus connection)
    {
        var age = DateTimeOffset.UtcNow - DateTimeOffset.Parse(stamp.CapturedAtUtc, CultureInfo.InvariantCulture);
        var context = target is not null && stamp.SessionId == target.Identity.SessionId && stamp.Generation == target.Generation && stamp.StopId == target.StopId;
        return $"Retained {stamp.Source} capture · {Age(age)} · generation {stamp.Generation}" +
            (stamp.StopId is null ? "" : " · stop " + stamp.StopId) + (!context ? " · previous execution context" : "") +
            (connection != InspectorConnectionStatus.Connected || target?.State != "running" ? " · target unavailable for live refresh" : "");
    }
    public static string CapturedAt(CaptureStamp stamp) => "Captured " + stamp.CapturedAtUtc + " (UTC)";
    public static string Rect(WireRect? rect, double scale = 1) => rect is null ? "Unavailable" :
        $"x {Number(rect.X * scale)}, y {Number(rect.Y * scale)}, {Number(rect.Width * scale)} × {Number(rect.Height * scale)}";
    public static string Edges(WireEdges edges) => $"{Number(edges.Top)} / {Number(edges.Right)} / {Number(edges.Bottom)} / {Number(edges.Left)}";
    public static string Edge(double value) => value == 0 ? "–" : Number(value);
    public static string Origin(PropertyValue value, StyleDetails details) => value.Origin +
        (value.Rule is { } index && index < details.Rules.Length ? " · " + details.Rules[index].Selector : "") +
        (value.Declaration is { } declaration ? " · declaration " + declaration : "");
}
