using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Nazor.DevTools.Protocol;

public static class DevToolsJson
{
    static DevToolsJson() => Options.MakeReadOnly(populateMissingResolver: true);

    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = DevToolsProtocol.MaxDepth,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                type =>
                {
                    foreach (var property in type.Properties)
                        if (property.AttributeProvider?.IsDefined(typeof(OptionalFieldAttribute), inherit: false) == true)
                            property.ShouldSerialize = static (_, value) => value is not null;
                }
            }
        }
    };

    public static T Read<T>(JsonNode? node)
    {
        Contract(typeof(T));
        try
        {
            CheckShape(node);
            using var document = JsonDocument.Parse(node!.ToJsonString());
            CheckRecord(document.RootElement, typeof(T), false);
            var value = node.Deserialize<T>(Options)!;
            CheckValue(value);
            return value;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or InvalidDataException or ArgumentException or FormatException or OverflowException)
        {
            throw new DevToolsException(DevToolsErrors.InvalidParams, "Invalid DevTools payload: " + exception.Message);
        }
    }

    public static JsonNode Write<T>(T value)
    {
        Contract(typeof(T));
        var node = JsonSerializer.SerializeToNode(value, Options)!;
        Read<T>(node);
        return node;
    }

    internal static void CheckShape(JsonNode? node)
    {
        if (node is null) throw new DevToolsException(DevToolsErrors.InvalidParams, "A payload is required.");
        using var document = JsonDocument.Parse(node.ToJsonString(), new() { MaxDepth = DevToolsProtocol.MaxDepth });
        var count = 0;
        var characters = 0;
        Walk(document.RootElement);
        void Walk(JsonElement element)
        {
            if (++count > 65536) throw new InvalidDataException("Too many JSON values.");
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Length > 128 || !names.Add(property.Name) || names.Count > 64)
                        throw new InvalidDataException("Duplicate or excessive JSON properties.");
                    Walk(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                if (element.GetArrayLength() > DevToolsProtocol.MaxNodes) throw new InvalidDataException("The JSON array is too large.");
                foreach (var child in element.EnumerateArray()) Walk(child);
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                var length = element.GetString()!.Length;
                characters += length;
                if (length > DevToolsProtocol.MaxStringLength || characters > DevToolsProtocol.MaxMessageBytes)
                    throw new InvalidDataException("The JSON text is too large.");
            }
        }
    }

    private static void CheckRecord(JsonElement element, Type type, bool nullable)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            Require(nullable || Nullable.GetUnderlyingType(type) is not null, "A required field is null.");
            return;
        }
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsArray)
        {
            Require(element.ValueKind == JsonValueKind.Array, "Expected an array.");
            foreach (var child in element.EnumerateArray()) CheckRecord(child, type.GetElementType()!, false);
        }
        else if (type.Assembly == typeof(ProtocolVersion).Assembly)
        {
            Require(element.ValueKind == JsonValueKind.Object, "Expected an object.");
            foreach (var property in type.GetProperties())
            {
                if (property.IsDefined(typeof(OptionalFieldAttribute)) && !element.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(property.Name), out _)) continue;
                Require(element.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(property.Name), out var child), "Missing " + property.Name + ".");
                CheckRecord(child, property.PropertyType, new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable);
            }
        }
        else if (type == typeof(long)) Require(element.TryGetInt64(out var number) && number >= 0 && number <= DevToolsProtocol.MaxSequence, "Invalid sequence number.");
        else if (type == typeof(double)) Require(element.TryGetDouble(out var number) && double.IsFinite(number), "Invalid finite number.");
        else Require(type == typeof(string) || type == typeof(int) || type == typeof(bool), "Only plain contract fields are allowed.");
    }

    private static void CheckValue(object? value)
    {
        switch (value)
        {
            case InitializeRequest request:
                Require(!string.IsNullOrWhiteSpace(request.ClientName), "A client name is required.");
                Capabilities(request.Capabilities);
                break;
            case InitializeResult result:
                Require(!string.IsNullOrWhiteSpace(result.PeerName), "A peer name is required.");
                Capabilities(result.Capabilities);
                break;
            case ProtocolVersion version: Require(version.Major > 0 && version.Minor >= 0, "Invalid protocol version."); break;
            case TargetIdentity identity:
                Require(identity.ProcessId > 0, "Invalid process ID.");
                foreach (var id in new[] { identity.ProcessInstanceId, identity.RuntimeInstanceId, identity.ApplicationId, identity.WindowId, identity.SessionId }) Id(id);
                break;
            case TargetState state:
                Require(state.State is "running" or "paused" or "exited", "Invalid target state.");
                Require(state.Generation > 0 && (state.State == "paused" ? state.StopId is not null : state.StopId is null), "Invalid stop identity.");
                OptionalId(state.StopId);
                break;
            case RequestContext context: Id(context.SessionId); OptionalId(context.StopId); Require(context.Generation > 0, "Invalid generation."); break;
            case CaptureStamp stamp:
                Id(stamp.SessionId);
                Require(stamp.Generation > 0 && stamp.Source is "live" or "retained" or "debugger", "Invalid capture provenance.");
                Require(stamp.Source != "debugger" || stamp.StopId is not null, "A debugger capture requires a stop identity.");
                Require(stamp.Source != "live" || stamp.StopId is null, "A live capture cannot belong to a debugger stop.");
                OptionalId(stamp.StopId);
                Require(TryUtc(stamp.CapturedAtUtc, out _), "Expected a UTC RFC 3339 timestamp.");
                break;
            case SelectionRequest selection: Selection(selection.Kind, selection.NodeId); break;
            case SelectionResult selection: Selection(selection.Kind, selection.NodeId); Require(selection.Revision is null or > 0, "Invalid selection revision."); break;
            case NodeRequest node: Id(node.ElementId); break;
            case ComponentInfo component:
                Id(component.Id); OptionalId(component.ParentId); Require(!string.IsNullOrWhiteSpace(component.TypeName), "A component type is required.");
                OptionalId(component.RootId); OptionalId(component.DeclaringComponentId);
                Properties(component.Parameters);
                Ids(component.Children); Ids(component.OutputElements);
                break;
            case ElementInfo element:
                Id(element.Id); OptionalId(element.ParentId); OptionalId(element.ProducingComponentId); OptionalId(element.DeclaringComponentId);
                OptionalId(element.RootId);
                Properties(element.Attributes);
                foreach (var values in new[] { element.Classes, element.States, element.Events })
                    Require(values is null || values.Length <= 128, "Too many node details.");
                Require(element.Kind is "element" or "text", "Invalid element kind."); Ids(element.Children);
                break;
            case TreeSnapshot snapshot:
                Require(snapshot.Revision > 0 && snapshot.Components.Length + snapshot.Elements.Length <= DevToolsProtocol.MaxNodes, "Invalid snapshot size/revision.");
                CheckTree(snapshot);
                break;
            case TreeDelta delta:
                Require(delta.BaseRevision > 0 && delta.Revision == delta.BaseRevision + 1 && delta.Components.Length + delta.Elements.Length <= DevToolsProtocol.MaxNodes, "Invalid tree revision/size.");
                Ids(delta.ComponentRoots); Ids(delta.ElementRoots); Ids(delta.RemovedComponents); Ids(delta.RemovedElements);
                Require(delta.Components.Select(n => n.Id).Distinct().Count() == delta.Components.Length && delta.Elements.Select(n => n.Id).Distinct().Count() == delta.Elements.Length, "Duplicate delta IDs.");
                Require(!delta.Components.Any(n => delta.RemovedComponents.Contains(n.Id)) && !delta.Elements.Any(n => delta.RemovedElements.Contains(n.Id)), "A delta both removes and updates an ID.");
                break;
            case FrameBatch batch:
                Require(batch.Revision == batch.BaseRevision + 1 && batch.Frames.Length is > 0 and <= DevToolsProtocol.MaxFrames, "Invalid frame revision/size.");
                Require(batch.Frames.Zip(batch.Frames.Skip(1), (a, b) => b.Number > a.Number).All(b => b), "Frames must be ordered.");
                break;
            case DebuggerStatus debug:
                Require(debug.ProcessId > 0 && debug.State is "running" or "paused" or "exited", "Invalid debugger state.");
                Require(debug.State == "paused" ? !string.IsNullOrEmpty(debug.StopId) : debug.StopId is null, "Invalid debugger stop.");
                break;
            case DebuggerFields fields:
                Id(fields.ComponentId);
                Require(fields.Capture.Source == "debugger" && fields.Fields.Length <= 128, "Invalid debugger fields.");
                break;
            case DebuggerReadRequest read:
                Require(read.ProcessId > 0 && read.Generation > 0, "Invalid debugger target.");
                Id(read.SessionId); Id(read.StopId); Id(read.ComponentId); break;
            case FrameRecord frame:
                Require(frame.Renderer is null or "GPU" or "CPU" && frame.RequestedRenderer is null or "AUTO" or "GPU" or "CPU", "Invalid renderer selection.");
                Require(frame.RenderWorkerFrame is null or >= 0 && frame.GpuFramesReplaced is null or >= 0, "Invalid rendering counter.");
                Require(frame.RenderWorkerMs is null || double.IsFinite(frame.RenderWorkerMs.Value) && frame.RenderWorkerMs >= 0, "Invalid render worker timing.");
                Require((frame.GraphicsBackend?.Length ?? 0) <= 128 && (frame.GraphicsDevice?.Length ?? 0) <= 2048 && (frame.FallbackReason?.Length ?? 0) <= 4096, "Renderer details exceed the wire budget.");
                Require(frame.StylesRecomputed is null or >= 0, "Invalid style counter.");
                Require((frame.HasNodeCount != false || frame.NodeCount == 0) && (frame.HasPaintCount != false || frame.PaintCount == 0) && (frame.HasDamage != false || frame.Damage.Length == 0), "Unavailable frame values must use empty legacy slots.");
                Require(frame.Number > 0 && frame.NodeCount >= 0 && frame.LayoutCount >= 0 && frame.PaintCount >= 0 && frame.Damage.Length <= 128, "Invalid frame data."); break;
            case FrameTimings timings:
                Require(timings.OverlayMs is null or >= 0 && timings.ElapsedMs is null or >= 0, "Invalid optional timing.");
                Require(timings.StyleMs >= 0 && timings.LayoutMs >= 0 && timings.DisplayListMs >= 0 && timings.PaintMs >= 0 && timings.TotalMs >= 0, "Negative timing."); break;
            case WireRect rect: Require(rect.Width >= 0 && rect.Height >= 0, "Negative rectangle size."); break;
            case GeometryDetails geometry: Id(geometry.ElementId); Require(geometry.Scale > 0, "Invalid scale."); break;
            case PropertyValue property:
                Require(property.Rule is null or >= 0 && property.Declaration is null or >= 0, "Invalid declaration origin.");
                if (property.Longhands is { } longhands) { Require(longhands.Length <= 128, "Too many longhands."); Ids(longhands); }
                if (property.Winning is { } winning) { Require(winning.Length <= 128 && property.Longhands is not null && winning.All(property.Longhands.Contains), "Invalid winning longhands."); Ids(winning); }
                break;
            case StyleDetails styles:
                Id(styles.ElementId);
                Require(styles.Computed.Concat(styles.CustomProperties ?? []).All(v => v.Rule is null || v.Rule < styles.Rules.Length), "Invalid computed rule origin.");
                break;
            case StyleRule rule: Require((rule.Line is null or > 0) && (rule.Order is null or >= 0), "Invalid source line/order."); break;
        }
        if (value is Array array) foreach (var item in array) CheckValue(item);
        else if (value is not null && value.GetType().Assembly == typeof(ProtocolVersion).Assembly)
            foreach (var property in value.GetType().GetProperties()) CheckValue(property.GetValue(value));
    }

    private static void CheckTree(TreeSnapshot snapshot)
    {
        var components = snapshot.Components.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var elements = snapshot.Elements.ToDictionary(n => n.Id, StringComparer.Ordinal);
        Ids(snapshot.ComponentRoots); Ids(snapshot.ElementRoots);
        Require(snapshot.ComponentRoots.All(id => components.ContainsKey(id) && components[id].ParentId is null) &&
            snapshot.ElementRoots.All(id => elements.ContainsKey(id) && elements[id].ParentId is null), "Invalid tree roots.");
        Require(components.Values.Where(n => n.ParentId is null).Select(n => n.Id).ToHashSet().SetEquals(snapshot.ComponentRoots) &&
            elements.Values.Where(n => n.ParentId is null).Select(n => n.Id).ToHashSet().SetEquals(snapshot.ElementRoots), "Missing tree roots.");
        foreach (var node in components.Values)
        {
            Require((node.ParentId is null || components.ContainsKey(node.ParentId)) && node.Children.All(id => components.ContainsKey(id) && components[id].ParentId == node.Id) &&
                node.OutputElements.All(elements.ContainsKey) && (node.RootId is null || snapshot.ComponentRoots.Contains(node.RootId)) &&
                (node.DeclaringComponentId is null || components.ContainsKey(node.DeclaringComponentId)), "Invalid component links.");
            if (node.ParentId is { } owner) Require(components[owner].Children.Contains(node.Id), "Missing component child link.");
        }
        foreach (var node in elements.Values)
        {
            Require((node.ParentId is null || elements.ContainsKey(node.ParentId)) && node.Children.All(id => elements.ContainsKey(id) && elements[id].ParentId == node.Id) &&
                (node.ProducingComponentId is null || components.ContainsKey(node.ProducingComponentId)) &&
                (node.DeclaringComponentId is null || components.ContainsKey(node.DeclaringComponentId)) &&
                (node.RootId is null || snapshot.ComponentRoots.Contains(node.RootId)), "Invalid element links.");
            if (node.ParentId is { } owner) Require(elements[owner].Children.Contains(node.Id), "Missing element child link.");
        }
        Reachable(snapshot.ComponentRoots, components.Count, id => components[id].Children);
        Reachable(snapshot.ElementRoots, elements.Count, id => elements[id].Children);
    }

    private static void Reachable(string[] roots, int count, Func<string, string[]> children)
    {
        var visited = new HashSet<string>();
        var pending = new Stack<string>(roots);
        while (pending.TryPop(out var id))
        {
            Require(visited.Add(id), "Duplicate or cyclic tree link.");
            foreach (var child in children(id)) pending.Push(child);
        }
        Require(visited.Count == count, "Unreachable or cyclic tree nodes.");
    }

    private static void Contract(Type type)
    {
        if (type.Assembly != typeof(ProtocolVersion).Assembly || !type.IsSealed || type.GetMethod("<Clone>$") is null)
            throw new ArgumentException("Only DevTools DTO records are wire contracts.");
    }

    internal static bool TryUtc(string? value, out DateTimeOffset date) => DateTimeOffset.TryParseExact(value,
        ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"],
        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date) && date.Offset == TimeSpan.Zero;

    private static void Capabilities(string[] capabilities)
    {
        Require(capabilities.Length <= 32 && capabilities.Distinct(StringComparer.Ordinal).Count() == capabilities.Length, "Invalid capability list.");
        foreach (var capability in capabilities) Id(capability);
    }

    private static void Properties(PropertyValue[]? values) => Require(values is null ||
        values.Length <= 128 && values.Select(v => v.Name).Distinct(StringComparer.Ordinal).Count() == values.Length, "Invalid node properties.");
    private static void Selection(string kind, string? id) { Require(kind is "component" or "element", "Invalid selection kind."); OptionalId(id); }
    private static void OptionalId(string? id) { if (id is not null) Id(id); }
    private static void Ids(string[] ids) { foreach (var id in ids) Id(id); Require(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length, "Duplicate IDs."); }
    private static void Id(string id) => Require(!string.IsNullOrWhiteSpace(id) && id.Length <= 128, "Invalid opaque ID.");
    private static void Require(bool condition, string message) { if (!condition) throw new DevToolsException(DevToolsErrors.InvalidParams, message); }
}
