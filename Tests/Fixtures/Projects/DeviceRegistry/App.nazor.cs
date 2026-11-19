using Microsoft.Extensions.Logging;
using Nazor;

namespace DeviceRegistry;

public partial class App
{
    private string filter = "";
    private bool activeOnly;
    private bool dark;
    private Device? selected;

    [Inject] public ILogger<App> Logger { get; set; } = null!;

    public IReadOnlyList<Device> Devices => Store.Devices;

    public Device? Selected => selected;

    public bool Dark => dark;

    private List<Device> VisibleDevices => Store.Devices
        .Where(device => (!activeOnly || device.Active) &&
            (device.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) || device.Code.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        .ToList();

    private string Status => $"{VisibleDevices.Count} di {Store.Devices.Count} dispositivi";

    private void Select(Device device) => selected = device;

    private void Reverse() => Store.Reverse();

    private void ToggleTheme() { dark = !dark; Window.StyleSheet = dark ? StyleSheets.Dark : StyleSheets.Light; }

    private void Add()
    {
        var device = Store.Add("Nuovo dispositivo", active: false);
        device.Name = $"Nuovo dispositivo {device.Id}";
        Logger.LogInformation("Added device {Code}", device.Code);
    }

    private void RemoveSelected()
    {
        if (selected is null) return;
        Store.Remove(selected);
        Logger.LogInformation("Removed device {Code}", selected.Code);
        selected = null;
    }
}
