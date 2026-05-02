namespace Retro.Crt.Internals;

/// <summary>
/// Single source of truth for the box-drawing, bar, and cursor glyphs. Picks
/// pretty unicode forms when <see cref="TerminalCapabilities.SupportsUnicode"/>
/// is true, falls back to 7-bit ASCII otherwise.
/// </summary>
internal static class Glyphs
{
    public static bool Unicode => TerminalCapabilities.SupportsUnicode;

    // Box drawing.
    public static char BoxTopLeft     => Unicode ? '┌' : '+';
    public static char BoxTopRight    => Unicode ? '┐' : '+';
    public static char BoxBottomLeft  => Unicode ? '└' : '+';
    public static char BoxBottomRight => Unicode ? '┘' : '+';
    public static char BoxHorizontal  => Unicode ? '─' : '-';
    public static char BoxVertical    => Unicode ? '│' : '|';

    // Progress bar.
    public static char BarFull  => Unicode ? '█' : '#';
    public static char BarEmpty => Unicode ? '░' : '-';

    // Typewriter cursor.
    public static char CursorBlock     => Unicode ? '▌' : '|';
    public static char CursorUnderline => '_';
}
