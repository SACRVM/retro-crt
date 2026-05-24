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
    [InlineData(0,   "\x1b[38;5;0m")]
    [InlineData(15,  "\x1b[38;5;15m")]
    [InlineData(196, "\x1b[38;5;196m")]
    [InlineData(232, "\x1b[38;5;232m")]
    [InlineData(255, "\x1b[38;5;255m")]
    public void Foreground_xterm256_emits_38_5_N(byte index, string expected)
    {
        var s = AnsiCodes.Foreground(Color.Indexed256(index));
        Assert.Equal(expected, s);
    }

    [Theory]
    [InlineData(0,   "\x1b[48;5;0m")]
    [InlineData(196, "\x1b[48;5;196m")]
    [InlineData(255, "\x1b[48;5;255m")]
    public void Background_xterm256_emits_48_5_N(byte index, string expected)
    {
        var s = AnsiCodes.Background(Color.Indexed256(index));
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

    [Fact]
    public void SetWindowTitle_emits_osc_2_terminated_with_bel()
    {
        Assert.Equal("\x1b]2;Husky: app\a", AnsiCodes.SetWindowTitle("Husky: app"));
    }

    [Fact]
    public void SetIconName_emits_osc_1_terminated_with_bel()
    {
        Assert.Equal("\x1b]1;tab\a", AnsiCodes.SetIconName("tab"));
    }

    [Fact]
    public void SetWindowTitle_strips_control_chars_that_could_break_the_escape()
    {
        // An embedded BEL / ESC / newline would terminate the OSC string
        // early (or smuggle in another escape) — all must be stripped.
        // Built from char codes to keep raw control bytes out of the source.
        var input = "a" + (char)0x1b + "b" + (char)0x07 + "c" + (char)0x0a + "d";
        var s = AnsiCodes.SetWindowTitle(input);
        Assert.Equal("\x1b]2;abcd\a", s);
    }

    [Fact]
    public void WindowTitle_stack_constants_are_xtwinops()
    {
        Assert.Equal("\x1b[22;0t", AnsiCodes.PushWindowTitle);
        Assert.Equal("\x1b[23;0t", AnsiCodes.PopWindowTitle);
    }

    [Fact]
    public void Foreground_default_emits_sgr_39()
    {
        Assert.Equal("\x1b[39m", AnsiCodes.Foreground(Color.Default));
    }

    [Fact]
    public void Background_default_emits_sgr_49()
    {
        Assert.Equal("\x1b[49m", AnsiCodes.Background(Color.Default));
    }

    [Fact]
    public void Hyperlink_wraps_text_in_osc_8()
    {
        var s = AnsiCodes.Hyperlink("docs", "https://example.com");
        Assert.Equal("\x1b]8;;https://example.com\adocs\x1b]8;;\a", s);
    }

    [Fact]
    public void Hyperlink_strips_control_chars_from_the_uri()
    {
        var uri = "http://x" + (char)0x1b + "/" + (char)0x07 + "y";
        var s = AnsiCodes.Hyperlink("label", uri);
        Assert.Equal("\x1b]8;;http://x/y\alabel\x1b]8;;\a", s);
    }
}
