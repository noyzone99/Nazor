namespace Nazor.Graphics.Text;

/// <summary>Permitted synthetic font styles when a real face or variation axis is unavailable. None preserves unsynthesized rendering.</summary>
[Flags]
public enum FontSynthesis : byte
{
    /// <summary>Uses only real font faces and variation axes.</summary>
    None = 0,
    /// <summary>Permits simulated bold for a requested weight of at least 600 when the selected face is lighter.</summary>
    Weight = 1,
    /// <summary>Permits a 12-degree synthetic oblique when italic is requested but unavailable.</summary>
    Style = 2,
}
