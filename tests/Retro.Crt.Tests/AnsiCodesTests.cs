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
    [InlineData(0, "\x1b[30m")]
    [InlineData(7, "\x1b[37m")]
    [InlineData(8, "\x1b[90m")]
    [InlineData(15, "\x1b[97m")]
    public void Foreground_standard16_picks_correct_sgr(byte index, string expected)
    {
        var s = AnsiCodes.Foreground(Color.Standard(index));
        Assert.Equal(expected, s);
    }

    [Theory]
    [InlineData(0, "\x1b[40m")]
    [InlineData(7, "\x1b[47m")]
    [InlineData(8, "\x1b[100m")]
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
