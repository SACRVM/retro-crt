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

    private static string Standard16Fg(byte index)
    {
        // 0..7 -> 30..37, 8..15 -> 90..97
        var code = index < 8 ? 30 + index : 90 + (index - 8);
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{code}m");
    }

    private static string Standard16Bg(byte index)
    {
        // 0..7 -> 40..47, 8..15 -> 100..107
        var code = index < 8 ? 40 + index : 100 + (index - 8);
        return string.Create(CultureInfo.InvariantCulture, $"{Csi}{code}m");
    }
}
