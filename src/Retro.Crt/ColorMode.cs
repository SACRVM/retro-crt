namespace Retro.Crt;

/// <summary>
/// Determines how a <see cref="Color"/> is encoded in an ANSI escape.
/// </summary>
public enum ColorMode : byte
{
    /// <summary>
    /// 24-bit truecolor (<c>ESC[38;2;R;G;Bm</c>). Default for new colors.
    /// </summary>
    Truecolor = 0,

    /// <summary>
    /// One of the 16 standard SGR colors (<c>ESC[30..37m</c> /
    /// <c>ESC[90..97m</c>). Maps to the user's terminal palette so themed
    /// terminals (Solarized, Dracula, …) keep their identity.
    /// </summary>
    Standard16 = 1,

    /// <summary>
    /// xterm 256-color palette index 0..255 (<c>ESC[38;5;Nm</c>). The
    /// first 16 entries mirror <see cref="Standard16"/>; 16..231 form a
    /// 6×6×6 RGB cube; 232..255 are a 24-step grayscale ramp.
    /// </summary>
    Xterm256 = 2,

    /// <summary>
    /// The terminal's own default foreground / background — emits SGR
    /// <c>39</c> (default fg) / <c>49</c> (default bg) instead of a concrete
    /// color, so the cell inherits whatever the user's terminal is
    /// configured with. See <see cref="Color.Default"/>.
    /// </summary>
    Default = 3,
}
