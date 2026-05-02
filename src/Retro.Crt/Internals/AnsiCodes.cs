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
    public const string CarriageReturnAndClear = "\r" + Csi + "K";
    public const string HideCursor  = Csi + "?25l";
    public const string ShowCursor  = Csi + "?25h";

    public static string CursorLeft(int n)
    {
        if (n <= 0) return "";
        if (n == 1) return CursorLeft1;
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{n}D");
    }

    public static string Foreground(Color c) => c.Mode switch
    {
        ColorMode.Truecolor  => $"{Csi}38;2;{c.R};{c.G};{c.B}m",
        ColorMode.Standard16 => Standard16Fg(c.Index),
        _                    => Reset,
    };

    public static string Background(Color c) => c.Mode switch
    {
        ColorMode.Truecolor  => $"{Csi}48;2;{c.R};{c.G};{c.B}m",
        ColorMode.Standard16 => Standard16Bg(c.Index),
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

    private static string Standard16Fg(byte index)
    {
        var bright = index >= 8;
        var sgr = BiosToSgr[index & 0x7];
        var code = bright ? 90 + sgr : 30 + sgr;
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{code}m");
    }

    private static string Standard16Bg(byte index)
    {
        var bright = index >= 8;
        var sgr = BiosToSgr[index & 0x7];
        var code = bright ? 100 + sgr : 40 + sgr;
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{code}m");
    }
}
