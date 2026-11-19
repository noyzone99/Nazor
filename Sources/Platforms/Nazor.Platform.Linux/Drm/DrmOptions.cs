namespace Nazor.Platform.Linux;

/// <summary>Display selection for a Linux DRM/KMS single-view application. Captured by the platform host.</summary>
public sealed record DrmOptions
{
    /// <summary>The primary DRM device path, such as /dev/dri/card0; null searches connected cards.</summary>
    public string? DevicePath { get; init; }

    /// <summary>The connector identifier on the selected card; null selects its first connected output.</summary>
    public uint? ConnectorId { get; init; }

    /// <summary>The number of physical pixels per logical unit. Must be finite and between 0.5 and 8.</summary>
    public float Scale { get; init; } = 1;

    internal DrmOptions Validate()
    {
        if (!float.IsFinite(Scale) || Scale < 0.5f || Scale > 8)
            throw new ArgumentOutOfRangeException(nameof(Scale), "DRM scale must be between 0.5 and 8.");
        if (DevicePath is not null && (!DevicePath.StartsWith("/dev/dri/card", StringComparison.Ordinal) ||
            DevicePath.Length == 13 || !DevicePath[13..].All(char.IsAsciiDigit)))
            throw new ArgumentException("Select a primary DRM device such as /dev/dri/card0.", nameof(DevicePath));
        if (ConnectorId == 0) throw new ArgumentOutOfRangeException(nameof(ConnectorId));
        return this;
    }
}
