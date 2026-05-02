namespace Retro.Crt;

/// <summary>
/// How each character "appears" before settling into its final form.
/// </summary>
public enum TypewriterFade : byte
{
    /// <summary>Final glyph appears immediately at full brightness.</summary>
    None = 0,

    /// <summary>
    /// The glyph is rendered in its final shape from the start, but its
    /// foreground color ramps from dim to full over a few frames — like
    /// the character is fading in at constant hue. Requires the resolved
    /// foreground color to be truecolor; on Standard16 (which has no
    /// brightness scaling) the fade is skipped.
    /// </summary>
    Alpha = 1,
}
