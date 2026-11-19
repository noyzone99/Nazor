using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

internal interface IDBusObject
{
    ObjectPath ObjectPath { get; }
}

// These attributes are read by the static binding generator, not at runtime.
[AttributeUsage(AttributeTargets.Interface)]
internal sealed class DBusInterfaceAttribute(string name) : Attribute
{
    internal string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
internal sealed class ArgumentAttribute : Attribute;
