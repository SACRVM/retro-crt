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
}
