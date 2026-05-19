using System.Globalization;

namespace Retro.Crt.Internals;

/// <summary>
/// Pure string builders for the ANSI escape sequences Retro.Crt emits. No I/O.
/// Easy to snapshot-test.
/// </summary>
internal static class AnsiCodes
{
    public const string Csi = "\x1b[";

    public const string Reset       = Csi + "0m";
    public const string Bold        = Csi + "1m";
    public const string Dim         = Csi + "2m";
    public const string Underline   = Csi + "4m";
    public const string ClearScreen = Csi + "2J" + Csi + "H";
    public const string ClearToEol  = Csi + "K";
    public const string CursorLeft1 = Csi + "D";
    public const string HideCursor  = Csi + "?25l";
    public const string ShowCursor  = Csi + "?25h";

    // DECSC / DECRC — save and restore cursor position + SGR pen. Two-byte
    // escapes (no CSI), supported by every VT100-descendant terminal.
    public const string SaveCursor    = "\x1b" + "7";
    public const string RestoreCursor = "\x1b" + "8";

    // DECSTBM — set top and bottom margins of the scroll region. While
    // active, line feeds at the bottom of the region scroll only the
    // region instead of the whole screen, so a fixed footer can live
    // below it. ResetScrollRegion (no parameters) returns the region to
    // the full screen.
    public const string ResetScrollRegion = Csi + "r";

    // \x1b[?1049h saves cursor + switches to the alternate screen buffer
    // and clears it; \x1b[?1049l switches back and restores the cursor.
    // This is the same pair vim, less, htop and tmux use.
    public const string AlternateScreenEnter = Csi + "?1049h";
    public const string AlternateScreenLeave = Csi + "?1049l";

    // SGR-encoded mouse reports (xterm mode 1006 — extended coords as
    // decimal numbers, immune to the 223-column limit of the older
    // mode 1000). Combined with mode 1003 ("any-event tracking") so we
    // also receive bare motion, drag, and wheel events. Two enable +
    // two disable escapes — emitted in the order shown so the
    // any-event mode is installed AFTER the encoding upgrade, and
    // torn down in reverse.
    public const string MouseTrackingEnter = Csi + "?1006h" + Csi + "?1003h";
    public const string MouseTrackingLeave = Csi + "?1003l" + Csi + "?1006l";

    // Bracketed paste — when enabled, the terminal wraps clipboard
    // pastes in ESC[200~ ... ESC[201~ so applications can distinguish
    // typed input from injected text. Supported by xterm, iTerm2,
    // kitty, Windows Terminal, and most modern Linux terminals.
    public const string BracketedPasteEnter = Csi + "?2004h";
    public const string BracketedPasteLeave = Csi + "?2004l";

    // BEL — predates ANSI, every terminal honours it. No newline trigger,
    // so the writer must be flushed for the beep to actually ring.
    public const string Bell = "\a";

    public static string CursorLeft(int n)
    {
        if (n <= 0) return "";
        if (n == 1) return CursorLeft1;
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{n}D");
    }

    /// <summary>SU — scroll the screen contents up by <paramref name="n"/> lines.</summary>
    public static string ScrollUp(int n)
    {
        if (n <= 0) return "";
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{n}S");
    }

    /// <summary>
    /// DECSTBM — set the scroll region to the inclusive 1-based row range
    /// <paramref name="top"/> .. <paramref name="bottom"/>.
    /// </summary>
    public static string SetScrollRegion(int top, int bottom)
        => string.Create(CultureInfo.InvariantCulture, $"{Csi}{top};{bottom}r");

    public static string Foreground(Color c) => c.Mode switch
    {
        ColorMode.Truecolor  => $"{Csi}38;2;{c.R};{c.G};{c.B}m",
        ColorMode.Standard16 => Standard16Fg(c.Index),
        ColorMode.Xterm256   => Indexed256Fg(c.Index),
        _                    => Reset,
    };

    public static string Background(Color c) => c.Mode switch
    {
        ColorMode.Truecolor  => $"{Csi}48;2;{c.R};{c.G};{c.B}m",
        ColorMode.Standard16 => Standard16Bg(c.Index),
        ColorMode.Xterm256   => Indexed256Bg(c.Index),
        _                    => Reset,
    };

    public static string GotoXY(int column, int row)
    {
        // ANSI cursor position is 1-based: ESC[row;colH.
        var col = column < 1 ? 1 : column;
        var rw  = row    < 1 ? 1 : row;
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{rw};{col}H");
    }

    // IBM PC BIOS color order is (Black, Blue, Green, Cyan, Red, Magenta,
    // Brown, LightGray); ANSI SGR order is (Black, Red, Green, Yellow,
    // Blue, Magenta, Cyan, White) — Blue/Red and Cyan/Yellow are swapped.
    // This table maps a BIOS color index 0..7 onto its SGR offset.
    private static readonly byte[] BiosToSgr = [0, 4, 2, 6, 1, 5, 3, 7];

    // Pre-built SGR sequences for the 16 standard palette slots, indexed
    // by BIOS color index. These are the most frequently emitted escapes
    // (Log levels, themed CRT-style output) and caching kills the
    // string.Create allocation that otherwise happens on every emit.
    private static readonly string[] Standard16FgCache = BuildStandard16(isFg: true);
    private static readonly string[] Standard16BgCache = BuildStandard16(isFg: false);

    private static string[] BuildStandard16(bool isFg)
    {
        var result = new string[16];
        for (var i = 0; i < 16; i++)
        {
            var bright = i >= 8;
            var sgr = BiosToSgr[i & 0x7];
            var baseCode = isFg
                ? (bright ? 90 : 30)
                : (bright ? 100 : 40);
            result[i] = string.Create(CultureInfo.InvariantCulture, $"{Csi}{baseCode + sgr}m");
        }
        return result;
    }

    private static string Standard16Fg(byte index) => Standard16FgCache[index];
    private static string Standard16Bg(byte index) => Standard16BgCache[index];

    // 256 entries × two strings is small enough to pre-build at startup.
    // Building once saves a string.Create per emit at a flat ~9 KB cost.
    private static readonly string[] Indexed256FgCache = BuildIndexed256(isFg: true);
    private static readonly string[] Indexed256BgCache = BuildIndexed256(isFg: false);

    private static string[] BuildIndexed256(bool isFg)
    {
        var prefix = isFg ? 38 : 48;
        var result = new string[256];
        for (var i = 0; i < 256; i++)
            result[i] = string.Create(CultureInfo.InvariantCulture, $"{Csi}{prefix};5;{i}m");
        return result;
    }

    private static string Indexed256Fg(byte index) => Indexed256FgCache[index];
    private static string Indexed256Bg(byte index) => Indexed256BgCache[index];
}
