using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

public class AnsiCodesTests
{
    [Fact]
    public void Foreground_truecolor_emits_38_2_R_G_B()
    {
        var s = AnsiCodes.Foreground(Color.Rgb(10, 20, 30));
        Assert.Equal("\x1b[38;2;10;20;30m", s);
    }

    [Fact]
    public void Background_truecolor_emits_48_2_R_G_B()
    {
        var s = AnsiCodes.Background(Color.Rgb(10, 20, 30));
        Assert.Equal("\x1b[48;2;10;20;30m", s);
    }

    [Theory]
    // BIOS index → SGR foreground. BIOS swaps Blue/Red and Cyan/Yellow vs ANSI.
    [InlineData(0,  "\x1b[30m")]   // Black
    [InlineData(1,  "\x1b[34m")]   // DarkBlue   -> SGR Blue
    [InlineData(2,  "\x1b[32m")]   // DarkGreen
    [InlineData(3,  "\x1b[36m")]   // DarkCyan   -> SGR Cyan
    [InlineData(4,  "\x1b[31m")]   // DarkRed    -> SGR Red
    [InlineData(5,  "\x1b[35m")]   // DarkMagenta
    [InlineData(6,  "\x1b[33m")]   // Brown      -> SGR Yellow
    [InlineData(7,  "\x1b[37m")]   // LightGray
    [InlineData(8,  "\x1b[90m")]   // DarkGray
    [InlineData(9,  "\x1b[94m")]   // LightBlue
    [InlineData(10, "\x1b[92m")]   // LightGreen
    [InlineData(11, "\x1b[96m")]   // LightCyan
    [InlineData(12, "\x1b[91m")]   // LightRed
    [InlineData(13, "\x1b[95m")]   // LightMagenta
    [InlineData(14, "\x1b[93m")]   // Yellow
    [InlineData(15, "\x1b[97m")]   // White
    public void Foreground_standard16_picks_correct_sgr(byte index, string expected)
    {
        var s = AnsiCodes.Foreground(Color.Standard(index));
        Assert.Equal(expected, s);
    }

    [Theory]
    [InlineData(0,  "\x1b[40m")]
    [InlineData(1,  "\x1b[44m")]
    [InlineData(2,  "\x1b[42m")]
    [InlineData(3,  "\x1b[46m")]
    [InlineData(4,  "\x1b[41m")]
    [InlineData(5,  "\x1b[45m")]
    [InlineData(6,  "\x1b[43m")]
    [InlineData(7,  "\x1b[47m")]
    [InlineData(8,  "\x1b[100m")]
    [InlineData(9,  "\x1b[104m")]
    [InlineData(10, "\x1b[102m")]
    [InlineData(11, "\x1b[106m")]
    [InlineData(12, "\x1b[101m")]
    [InlineData(13, "\x1b[105m")]
    [InlineData(14, "\x1b[103m")]
    [InlineData(15, "\x1b[107m")]
    public void Background_standard16_picks_correct_sgr(byte index, string expected)
    {
        var s = AnsiCodes.Background(Color.Standard(index));
        Assert.Equal(expected, s);
    }

    [Theory]
    [InlineData(1, 1, "\x1b[1;1H")]
    [InlineData(10, 5, "\x1b[5;10H")]
    [InlineData(0, 0, "\x1b[1;1H")]    // 0 clamps to 1 (1-based)
    [InlineData(-3, 4, "\x1b[4;1H")]   // negative clamps to 1
    public void GotoXY_emits_row_then_column_one_based(int column, int row, string expected)
    {
        var s = AnsiCodes.GotoXY(column, row);
        Assert.Equal(expected, s);
    }

    [Fact]
    public void Constants_are_well_known_sequences()
    {
        Assert.Equal("\x1b[0m",      AnsiCodes.Reset);
        Assert.Equal("\x1b[1m",      AnsiCodes.Bold);
        Assert.Equal("\x1b[2m",      AnsiCodes.Dim);
        Assert.Equal("\x1b[4m",      AnsiCodes.Underline);
        Assert.Equal("\x1b[2J\x1b[H", AnsiCodes.ClearScreen);
        Assert.Equal("\x1b[K",        AnsiCodes.ClearToEol);
    }
}
