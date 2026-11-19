using Nazor.Platform;

namespace Nazor.Samples.Windowing;

internal sealed class SampleBackend(IPlatformHost host) : IPlatformHost
{
    public string Name { get; private set; } = "Unavailable";

    public int Run(Func<IPlatform, int> application) => host.Run(platform =>
    {
        Name = platform.BackendName;
        return application(platform);
    });
}
