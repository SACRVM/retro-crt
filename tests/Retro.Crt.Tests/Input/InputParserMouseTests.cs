using Retro.Crt.Input;

namespace Retro.Crt.Tests.Input;

public class InputParserMouseTests
{
    [Fact]
    public void Press_left_at_5_10_with_M_terminator()
    {
        // ESC[<0;5;10M → left button press at column 5, row 10.
        var st = InputParser.TryParseMouse("\x1b[<0;5;10M".AsSpan(), out var m, out var consumed);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(10, consumed);
        Assert.Equal(MouseButton.Left, m.Button);
        Assert.Equal(MouseEventKind.Press, m.Kind);
        Assert.Equal(5,  m.X);
        Assert.Equal(10, m.Y);
        Assert.Equal(KeyModifiers.None, m.Modifiers);
    }

    [Fact]
    public void Release_left_uses_lowercase_m_terminator()
    {
        var st = InputParser.TryParseMouse("\x1b[<0;5;10m".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(MouseButton.Left, m.Button);
        Assert.Equal(MouseEventKind.Release, m.Kind);
    }

    [Fact]
    public void Right_button_decodes_as_button_2()
    {
        var st = InputParser.TryParseMouse("\x1b[<2;1;1M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(MouseButton.Right, m.Button);
        Assert.Equal(MouseEventKind.Press, m.Kind);
    }

    [Fact]
    public void Wheel_up_decodes_via_button_64()
    {
        // Bit 6 (wheel) + low bits 00 (up) = 64.
        var st = InputParser.TryParseMouse("\x1b[<64;5;5M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(MouseButton.WheelUp, m.Button);
        Assert.Equal(MouseEventKind.Wheel, m.Kind);
    }

    [Fact]
    public void Wheel_down_decodes_via_button_65()
    {
        var st = InputParser.TryParseMouse("\x1b[<65;5;5M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(MouseButton.WheelDown, m.Button);
        Assert.Equal(MouseEventKind.Wheel, m.Kind);
    }

    [Fact]
    public void Drag_decodes_with_motion_bit_and_button_held()
    {
        // Bit 5 (motion) + low bits 00 (left held) = 32.
        var st = InputParser.TryParseMouse("\x1b[<32;7;7M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(MouseButton.Left, m.Button);
        Assert.Equal(MouseEventKind.Drag, m.Kind);
    }

    [Fact]
    public void Pure_motion_with_no_button_held_decodes_as_Move()
    {
        // Bit 5 (motion) + low bits 11 (no button) = 35.
        var st = InputParser.TryParseMouse("\x1b[<35;7;7M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(MouseButton.None, m.Button);
        Assert.Equal(MouseEventKind.Move, m.Kind);
    }

    [Fact]
    public void Shift_modifier_bit_is_decoded()
    {
        // Bit 2 (Shift) | left = 4.
        var st = InputParser.TryParseMouse("\x1b[<4;1;1M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(KeyModifiers.Shift, m.Modifiers);
    }

    [Fact]
    public void Ctrl_modifier_bit_is_decoded()
    {
        // Bit 4 (Ctrl) | left = 16.
        var st = InputParser.TryParseMouse("\x1b[<16;1;1M".AsSpan(), out var m, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(KeyModifiers.Ctrl, m.Modifiers);
    }

    [Fact]
    public void Incomplete_mouse_report_returns_incomplete()
    {
        var st = InputParser.TryParseMouse("\x1b[<0;5;10".AsSpan(), out _, out var consumed);
        Assert.Equal(InputParseStatus.Incomplete, st);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void Mouse_terminator_other_than_M_or_m_is_invalid()
    {
        var st = InputParser.TryParseMouse("\x1b[<0;5;10X".AsSpan(), out _, out var consumed);
        Assert.Equal(InputParseStatus.Invalid, st);
        Assert.True(consumed > 0);
    }

    [Fact]
    public void TryParseEvent_dispatches_mouse_when_input_is_a_mouse_report()
    {
        var st = InputParser.TryParseEvent("\x1b[<0;3;4M".AsSpan(), out var ev, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(InputEventKind.Mouse, ev.Kind);
        Assert.Equal(3, ev.Mouse.X);
        Assert.Equal(4, ev.Mouse.Y);
    }

    [Fact]
    public void TryParseEvent_dispatches_key_when_input_is_a_key_sequence()
    {
        var st = InputParser.TryParseEvent("\x1b[A".AsSpan(), out var ev, out _);
        Assert.Equal(InputParseStatus.Ok, st);
        Assert.Equal(InputEventKind.Key, ev.Kind);
        Assert.Equal(Key.Up, ev.Key.Key);
    }
}
