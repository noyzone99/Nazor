using Microsoft.Extensions.DependencyInjection;

namespace DeviceRegistry;

public sealed class Device(int id, string code, string name, bool active)
{
    public int Id { get; } = id;
    public string Code { get; } = code;
    public string Name { get; set; } = name;
    public bool Active { get; set; } = active;
}

public interface IDeviceStore
{
    IReadOnlyList<Device> Devices { get; }

    Device Add(string name, bool active);

    void Remove(Device device);

    void Reverse();
}

public sealed class InMemoryDeviceStore : IDeviceStore
{
    private readonly List<Device> devices =
    [
        new(1, "TMP-001", "Sensore temperatura", true),
        new(2, "LAB-014", "Centralina laboratorio", false),
        new(3, "AIR-203", "Misuratore qualità aria", true),
    ];
    private int nextId = 4;

    public IReadOnlyList<Device> Devices => devices;

    public Device Add(string name, bool active)
    {
        var id = nextId++;
        var device = new Device(id, $"NEW-{id:000}", name, active);
        devices.Add(device);
        return device;
    }

    public void Remove(Device device) => devices.Remove(device);

    public void Reverse() => devices.Reverse();
}

public static class DeviceRegistryServices
{
    public static IServiceCollection AddDeviceRegistry(this IServiceCollection services) =>
        services.AddSingleton<IDeviceStore, InMemoryDeviceStore>();
}
