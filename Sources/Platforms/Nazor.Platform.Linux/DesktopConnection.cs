using Tmds.DBus.Protocol;
using P = Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

internal sealed class DesktopConnection(string address) : IDisposable
{
    internal P.DBusConnection Raw { get; } = new(address);
    private readonly Dictionary<string, List<IDisposable>> subscriptions = new();
    internal async Task<(string LocalName, bool Connected)> ConnectAsync()
    {
        await Raw.ConnectAsync().ConfigureAwait(false);
        return (Raw.UniqueName ?? "", true);
    }
    internal T CreateProxy<T>(string service, ObjectPath path) where T : class, IDBusObject => DesktopBindings.Proxy<T>(this, service, path);
    internal async Task RegisterObjectsAsync(IEnumerable<IDBusObject> values)
    {
        foreach (var value in values)
        {
            var owned = await DesktopBindings.Subscribe(this, value).ConfigureAwait(false);
            try { Raw.AddMethodHandler(new Handler(value)); subscriptions.Add(value.ObjectPath.ToString(), owned); }
            catch { foreach (var subscription in owned) subscription.Dispose(); throw; }
        }
    }
    internal void UnregisterObject(IDBusObject value)
    {
        Raw.RemoveMethodHandler(value.ObjectPath.ToString());
        if (subscriptions.Remove(value.ObjectPath.ToString(), out var owned)) foreach (var subscription in owned) subscription.Dispose();
    }
    public void Dispose()
    {
        foreach (var owned in subscriptions.Values) foreach (var subscription in owned) subscription.Dispose();
        subscriptions.Clear();
        Raw.Dispose();
    }
    internal Task<T> Call<T>(P.MessageBuffer message, P.MessageValueReader<T> reader) => Raw.CallMethodAsync(message, reader);
    internal Task Call(P.MessageBuffer message) => Raw.CallMethodAsync(message);
    internal Task<IDisposable> Watch<T>(string service, ObjectPath path, string iface, string member, P.MessageValueReader<T> reader, Action<T> handler) =>
        Raw.WatchSignalAsync(service, path.ToString(), iface, member, reader, (Action<P.Notification<T>>)(notification =>
        {
            if (notification.HasValue) handler(notification.Value);
        }), flags: P.ObserverFlags.None, emitOnCapturedContext: false).AsTask();

    internal async Task<string?> ResolveServiceOwnerAsync(string service)
    {
        try { return await Call(OwnerRequest(service), static (message, _) => message.GetBodyReader().ReadString()).ConfigureAwait(false); }
        catch (DBusErrorReplyException error) when (error.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner") { return null; }
    }
    private P.MessageBuffer OwnerRequest(string service)
    {
        using var writer = Raw.GetMessageWriter();
        writer.WriteMethodCallHeader(destination: "org.freedesktop.DBus", path: "/org/freedesktop/DBus", @interface: "org.freedesktop.DBus", member: "GetNameOwner", signature: "s");
        writer.WriteString(service);
        return writer.CreateMessage();
    }
    internal readonly record struct OwnerChanged(string ServiceName, string NewOwner);
    internal async Task<IDisposable> ResolveServiceOwnerAsync(string service, Action<OwnerChanged> changed, Action<Exception> failed)
    {
        var subscription = await Raw.AddMatchAsync(new P.MatchRule { Type = P.MessageType.Signal, Sender = "org.freedesktop.DBus", Path = "/org/freedesktop/DBus", Interface = "org.freedesktop.DBus", Member = "NameOwnerChanged", Arg0 = service },
            static (message, _) => { var reader = message.GetBodyReader(); var name = reader.ReadString(); reader.ReadString(); return new OwnerChanged(name, reader.ReadString()); },
            notification => { if (notification.HasValue) changed(notification.Value); else if (notification.IsCompletion) failed(notification.CompletionReason); },
            emitOnCapturedContext: false, flags: P.ObserverFlags.EmitOnConnectionClosed | P.ObserverFlags.EmitOnConnectionFailed).ConfigureAwait(false);
        try { changed(new(service, await ResolveServiceOwnerAsync(service).ConfigureAwait(false) ?? "")); return subscription; }
        catch { subscription.Dispose(); throw; }
    }

    internal delegate void WriteValue<T>(ref P.MessageWriter writer, T value);
    internal delegate void WriteBody(ref P.MessageWriter writer);
    internal void Signal(ObjectPath path, string iface, string member, string signature, WriteBody write)
    {
        var writer = Raw.GetMessageWriter();
        try { writer.WriteSignalHeader(path: path.ToString(), @interface: iface, member: member, signature: signature); write(ref writer); Raw.TrySendMessage(writer.CreateMessage()); }
        finally { writer.Dispose(); }
    }
    internal static async ValueTask Reply<T>(P.MethodContext context, Task<T> result, string signature, WriteValue<T> write) =>
        ReplyValue(context, await result.ConfigureAwait(false), signature, write);
    private static void ReplyValue<T>(P.MethodContext context, T value, string signature, WriteValue<T> write)
    {
        var writer = context.CreateReplyWriter(signature);
        try { write(ref writer, value); context.Reply(writer.CreateMessage()); }
        finally { writer.Dispose(); }
    }
    internal static async ValueTask Reply(P.MethodContext context, Task result)
    {
        await result.ConfigureAwait(false);
        ReplyValue(context, 0, "", static (ref P.MessageWriter _, int _) => { });
    }
    private sealed class Handler(IDBusObject value) : P.IPathMethodHandler
    {
        public string Path => value.ObjectPath.ToString();
        public bool HandlesChildPaths => false;
        public ValueTask HandleMethodAsync(P.MethodContext context)
        {
            if (context.IsDBusIntrospectRequest) { context.ReplyIntrospectXml(DesktopBindings.Introspection(value)); return default; }
            context.DisposesAsynchronously = true;
            _ = Complete(context);
            return default;
        }
        private async Task Complete(P.MethodContext context)
        {
            using (context)
            {
                try { await DesktopBindings.Handle(value, context).ConfigureAwait(false); }
                catch (DBusErrorReplyException error) { context.ReplyError(error.ErrorName, error.ErrorMessage); }
                catch (Exception error) { context.ReplyError("org.freedesktop.DBus.Error.Failed", error.Message); }
            }
        }
    }

    internal static object ReadVariant(P.VariantValue value) => value.Type switch
    {
        P.VariantValueType.Byte => value.GetByte(),
        P.VariantValueType.Bool => value.GetBool(),
        P.VariantValueType.Int16 => value.GetInt16(),
        P.VariantValueType.UInt16 => value.GetUInt16(),
        P.VariantValueType.Int32 => value.GetInt32(),
        P.VariantValueType.UInt32 => value.GetUInt32(),
        P.VariantValueType.Int64 => value.GetInt64(),
        P.VariantValueType.UInt64 => value.GetUInt64(),
        P.VariantValueType.Double => value.GetDouble(),
        P.VariantValueType.String => value.GetString(),
        P.VariantValueType.ObjectPath => new ObjectPath(value.GetObjectPathAsString()),
        P.VariantValueType.Variant => ReadVariant(value.GetVariantValue()),
        P.VariantValueType.Dictionary => Enumerable.Range(0, value.Count).Select(value.GetDictionaryEntry).ToDictionary(pair => pair.Key.GetString(), pair => ReadVariant(pair.Value)),
        P.VariantValueType.Array => ReadArray(value),
        P.VariantValueType.Struct => Enumerable.Range(0, value.Count).Select(i => ReadVariant(value.GetItem(i))).ToArray(),
        _ => throw new NotSupportedException("Unsupported desktop variant: " + value.Type),
    };
    private static object ReadArray(P.VariantValue value) => value.ItemType switch
    {
        P.VariantValueType.Byte => value.GetArray<byte>(),
        P.VariantValueType.String => value.GetArray<string>(),
        P.VariantValueType.Int32 => value.GetArray<int>(),
        P.VariantValueType.UInt32 => value.GetArray<uint>(),
        P.VariantValueType.Bool => value.GetArray<bool>(),
        P.VariantValueType.Double => value.GetArray<double>(),
        _ => Enumerable.Range(0, value.Count).Select(i => ReadVariant(value.GetItem(i))).ToArray(),
    };
}
