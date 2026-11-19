namespace Nazor.Rendering;

internal sealed class PresenceMotion(Action changed) : IDisposable
{
    private IOverlayMotion? provider;

    public TimeSpan Duration(ElementNode? surface, TimeSpan? duration = null)
    {
        var current = surface?.GetFeature<IOverlayMotion>();
        if (!ReferenceEquals(current, provider))
        {
            Dispose();
            provider = current;
            if (provider is not null) provider.PreferencesChanged += changed;
        }
        return provider?.ReducedMotion == true ? TimeSpan.Zero : duration ??
            (surface is { IsRemoved: false } ? provider?.GetExitDuration(surface) : null) ?? TimeSpan.Zero;
    }

    public void Dispose()
    {
        if (provider is not null) provider.PreferencesChanged -= changed;
        provider = null;
    }
}
