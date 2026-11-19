namespace Nazor.Style;

/// <summary>The scene-clock policy used by a NumberPresenter, selected by the NSS number-motion property.</summary>
public enum NumberMotionKind : byte
{
    /// <summary>Applies targets immediately.</summary>
    None,
    /// <summary>Moves at a constant rate; number-motion-duration specifies the time per scalar unit.</summary>
    Linear,
    /// <summary>Moves increases at the configured constant rate and applies decreases immediately.</summary>
    LinearIncrease,
    /// <summary>Decays the remaining distance by the configured factor per normalized 60Hz reference frame.</summary>
    Exponential,
}
