using Retro.Crt.Input;

namespace Retro.Crt.Tests.Input;

public class InputParserKeyTests
{
    [Fact]
    public void Empty_input_is_incomplete()
    {
        var st = InputParser.TryParseKey(ReadOnlySpan<char>.Empty, out _, out var consumed);
        Assert.Equal(InputParseStatus.Incomplete, st);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void Bare_escape_alone_reports_incomplete_so_the_caller_can_apply_a_timeout()
    {
        // A lone ESC could be the user pressing Escape OR the start of
        // an escape sequence still in flight. The parser stays
        // optimistic and reports Incomplete; concrete input loops apply
        // a ~50 ms grace period before synthesizing a real Escape.
        var st = InputParser.TryParseKey("\x1b", out _, out var consumed);
        Assert.Equal(InputParseStatus.Incomplete, st);
        Assert.Equal(0, consumed);
    }

    [Theory]
    [InlineData("\t",   Key.Tab)]
    [InlineData("\r",   Key.Enter)]
    [InlineData("\n",   Key.Enter)]
    [InlineData("\b",   Key.Backspace)]
    [InlineData("\x7f", Key.Backspace)]
    public void Single_byte_named_keys_decode(string s, Key expected)
    {
        var st = InputParser.TryParseKey(s.AsSpan(), out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(1, consumed);
        Assert.Equal(expected, k.Key);
        Assert.Equal(KeyModifiers.None, k.Modifiers);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('?')]
    [InlineData(' ')]
    public void Printable_chars_decode_as_Char(char c)
    {
        var st = InputParser.TryParseKey([c], out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(1, consumed);
        Assert.Equal(Key.Glyph, k.Key);
        Assert.Equal(c, k.Glyph);
        Assert.Equal(KeyModifiers.None, k.Modifiers);
    }

    [Fact]
    public void Ctrl_letter_maps_byte_0x01_to_Ctrl_A()
    {
        var st = InputParser.TryParseKey("\x01", out var k, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(Key.Glyph, k.Key);
        Assert.Equal('A', k.Glyph);
        Assert.Equal(KeyModifiers.Ctrl, k.Modifiers);
    }

    [Fact]
    public void Ctrl_letter_maps_byte_0x1a_to_Ctrl_Z()
    {
        var st = InputParser.TryParseKey("\x1a", out var k, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal('Z', k.Glyph);
        Assert.Equal(KeyModifiers.Ctrl, k.Modifiers);
    }

    [Theory]
    [InlineData("\x1b[A", Key.Up)]
    [InlineData("\x1b[B", Key.Down)]
    [InlineData("\x1b[C", Key.Right)]
    [InlineData("\x1b[D", Key.Left)]
    [InlineData("\x1b[H", Key.Home)]
    [InlineData("\x1b[F", Key.End)]
    public void Plain_cursor_keys_decode(string seq, Key expected)
    {
        var st = InputParser.TryParseKey(seq.AsSpan(), out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(seq.Length, consumed);
        Assert.Equal(expected, k.Key);
        Assert.Equal(KeyModifiers.None, k.Modifiers);
    }

    [Theory]
    [InlineData("\x1b[2~",  Key.Insert)]
    [InlineData("\x1b[3~",  Key.Delete)]
    [InlineData("\x1b[5~",  Key.PageUp)]
    [InlineData("\x1b[6~",  Key.PageDown)]
    [InlineData("\x1b[15~", Key.F5)]
    [InlineData("\x1b[17~", Key.F6)]
    [InlineData("\x1b[24~", Key.F12)]
    public void Tilde_keys_decode(string seq, Key expected)
    {
        var st = InputParser.TryParseKey(seq.AsSpan(), out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(seq.Length, consumed);
        Assert.Equal(expected, k.Key);
    }

    [Theory]
    [InlineData("\x1bOP", Key.F1)]
    [InlineData("\x1bOQ", Key.F2)]
    [InlineData("\x1bOR", Key.F3)]
    [InlineData("\x1bOS", Key.F4)]
    public void Ss3_function_keys_decode(string seq, Key expected)
    {
        var st = InputParser.TryParseKey(seq.AsSpan(), out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(3, consumed);
        Assert.Equal(expected, k.Key);
    }

    [Fact]
    public void Shift_plus_arrow_decodes_with_shift_modifier()
    {
        // ESC[1;2A → Shift+Up. The "2" is xterm modifier-byte 2 (= bitmask 1 = Shift).
        var st = InputParser.TryParseKey("\x1b[1;2A".AsSpan(), out var k, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(Key.Up, k.Key);
        Assert.Equal(KeyModifiers.Shift, k.Modifiers);
    }

    [Fact]
    public void Ctrl_plus_arrow_decodes_with_ctrl_modifier()
    {
        // ESC[1;5A → Ctrl+Up. Modifier byte 5 = bitmask 4 = Ctrl.
        var st = InputParser.TryParseKey("\x1b[1;5A".AsSpan(), out var k, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(Key.Up, k.Key);
        Assert.Equal(KeyModifiers.Ctrl, k.Modifiers);
    }

    [Fact]
    public void Shift_ctrl_alt_combo_decodes_all_three_modifiers()
    {
        // Modifier byte 8 = bitmask 7 = Shift+Alt+Ctrl.
        var st = InputParser.TryParseKey("\x1b[1;8C".AsSpan(), out var k, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(Key.Right, k.Key);
        Assert.Equal(
            KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Ctrl,
            k.Modifiers);
    }

    [Fact]
    public void Modified_F5_decodes_via_tilde_form()
    {
        // ESC[15;5~ → Ctrl+F5.
        var st = InputParser.TryParseKey("\x1b[15;5~".AsSpan(), out var k, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(Key.F5, k.Key);
        Assert.Equal(KeyModifiers.Ctrl, k.Modifiers);
    }

    [Fact]
    public void Alt_prefixed_printable_decodes_as_alt_char()
    {
        var st = InputParser.TryParseKey("\x1bx".AsSpan(), out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(2, consumed);
        Assert.Equal(Key.Glyph, k.Key);
        Assert.Equal('x', k.Glyph);
        Assert.Equal(KeyModifiers.Alt, k.Modifiers);
    }

    [Fact]
    public void Double_escape_yields_a_single_Escape_and_leaves_the_rest()
    {
        // ESC followed by ESC: the outer ESC is reported as Escape, the
        // second one stays in the buffer for the next call. This is how
        // a real input loop handles "user pressed ESC twice".
        var st = InputParser.TryParseKey("\x1b\x1b".AsSpan(), out var k, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(1, consumed);
        Assert.Equal(Key.Escape, k.Key);
    }

    [Fact]
    public void Incomplete_csi_reports_incomplete()
    {
        // ESC[ with no final byte yet — caller should buffer more input.
        var st = InputParser.TryParseKey("\x1b[".AsSpan(), out _, out var consumed);
        Assert.Equal(InputParseStatus.Incomplete, st);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void Incomplete_csi_with_partial_params_reports_incomplete()
    {
        var st = InputParser.TryParseKey("\x1b[1;5".AsSpan(), out _, out var consumed);
        Assert.Equal(InputParseStatus.Incomplete, st);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void Unknown_csi_letter_reports_invalid_and_consumes_the_sequence()
    {
        // ESC[Z is xterm's "back-tab" — we don't model it yet, so the
        // parser must report Invalid and skip the whole sequence so the
        // input loop can move on instead of jamming.
        var st = InputParser.TryParseKey("\x1b[Z".AsSpan(), out _, out var consumed);
        Assert.Equal(InputParseStatus.Invalid, st);
        Assert.Equal(3, consumed);
    }

    [Fact]
    public void Unknown_tilde_code_reports_invalid()
    {
        var st = InputParser.TryParseKey("\x1b[99~".AsSpan(), out _, out var consumed);
        Assert.Equal(InputParseStatus.Invalid, st);
        Assert.Equal(5, consumed);
    }
}
