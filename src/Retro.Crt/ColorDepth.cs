namespace Retro.Crt;

/// <summary>
/// What the terminal can actually render. Detected once at startup;
/// every emitted color is quantized down to the highest depth this
/// terminal understands so an <c>ESC[38;2;R;G;B</c> never reaches a
/// 16-color VT520.
/// </summary>
public enum ColorDepth : byte
{
    /// <summary>
    /// No ANSI escapes — output is redirected, <c>NO_COLOR</c> is set,
    /// <c>TERM=dumb</c>, or VT enablement failed on Windows. Colors are
    /// dropped silently.
    /// </summary>
    None = 0,

    /// <summary>
    /// 16 SGR colors (<c>30..37</c>, <c>90..97</c>). Truecolor and
    /// 256-color values are quantized to the nearest of the 16 BIOS
    /// anchors before emission.
    /// </summary>
    Standard16 = 1,

    /// <summary>
    /// xterm 256-color palette. Truecolor values are quantized to the
    /// closest 6×6×6 cube entry or 24-step grayscale ramp.
    /// </summary>
    Xterm256 = 2,

    /// <summary>
    /// 24-bit truecolor. Modern terminals (Windows Terminal, iTerm2,
    /// gnome-terminal, kitty, alacritty, modern xterm).
    /// </summary>
    Truecolor = 3,
}
