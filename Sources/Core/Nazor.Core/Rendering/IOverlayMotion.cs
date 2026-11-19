namespace Nazor.Rendering;

/// <summary>Host-resolved motion settings attached to an overlay element while it belongs to a presentation scene.</summary>
public interface IOverlayMotion
{
    /// <summary>Gets whether current host preferences require immediate state changes without exit motion.</summary>
    bool ReducedMotion { get; }

    /// <summary>Raised when the host's reduced-motion preference changes; receivers should request a render.</summary>
    event Action? PreferencesChanged;

    /// <summary>Gets the theme's exit duration for the retained surface.</summary>
    /// <param name="surface">The overlay element that carries this provider.</param>
    /// <returns>A nonnegative duration, or zero when motion is reduced or the surface is not rendered.</returns>
    TimeSpan GetExitDuration(ElementNode surface);

    /// <summary>Gets the theme's pointer-hover opening delay for a submenu trigger inside this overlay.</summary>
    /// <param name="trigger">The retained submenu item whose style supplies the delay.</param>
    /// <returns>The nonnegative delay, or zero when reduced motion applies. Providers without theme timing use 250ms.</returns>
    TimeSpan GetSubmenuOpenDelay(ElementNode trigger) => ReducedMotion ? TimeSpan.Zero : TimeSpan.FromMilliseconds(250);
}
