namespace Retro.Crt.Input;

/// <summary>
/// Outcome of a parse attempt — see <see cref="InputParser.TryParseEvent"/>
/// and friends.
/// </summary>
public enum InputParseStatus : byte
{
    /// <summary>A complete event was decoded; <c>consumed</c> says how many input chars to advance.</summary>
    Ok,
    /// <summary>The buffer ends mid-sequence; caller should wait for more input. <c>consumed</c> is 0.</summary>
    Incomplete,
    /// <summary>Unrecognized bytes — caller should skip <c>consumed</c> chars (≥1) and try again.</summary>
    Invalid,
}

/// <summary>
/// Pure parsers for terminal input streams: ANSI key escape sequences,
/// SGR-encoded mouse reports (mode 1006), control bytes, and printable
/// chars. Stateless — pass the unread tail of your input buffer in,
/// advance by the returned <c>consumed</c> count on success.
/// </summary>
/// <remarks>
/// <para>
/// What's recognized:
/// </para>
/// <list type="bullet">
///   <item><description>Single control bytes: ESC, Enter, Tab, Backspace, Ctrl+letter.</description></item>
///   <item><description>Alt-prefixed printables: <c>ESC</c> followed by a printable char.</description></item>
///   <item><description>CSI cursor keys: <c>ESC[A/B/C/D/H/F</c>, plus modified <c>ESC[1;mod A..F</c>.</description></item>
///   <item><description>CSI tilde keys: <c>ESC[2~</c>=Insert, <c>3~</c>=Delete, <c>5~</c>=PageUp, <c>6~</c>=PageDown, plus F5..F12 as <c>15~..24~</c>; modifiers via <c>ESC[code;mod ~</c>.</description></item>
///   <item><description>SS3 function keys: <c>ESC O P/Q/R/S</c> = F1..F4. Modified F1..F4 arrive as <c>ESC[1;mod P..S</c>.</description></item>
///   <item><description>SGR mouse reports: <c>ESC[&lt;button;x;y M/m</c>.</description></item>
///   <item><description>Printable BMP chars (anything ≥ 0x20 outside control bytes).</description></item>
/// </list>
/// </remarks>
public static class InputParser
{
    /// <summary>
    /// Try to parse the next event (key or mouse) from
    /// <paramref name="input"/>. The discriminator on the returned
    /// <paramref name="ev"/> tells you which slot is populated.
    /// </summary>
    public static InputParseStatus TryParseEvent(
        ReadOnlySpan<char> input,
        out InputEvent ev,
        out int consumed)
    {
        // Mouse events start with ESC[< — disambiguate before falling
        // through to the general key parser.
        if (input.Length >= 3 && input[0] == '\x1b' && input[1] == '[' && input[2] == '<')
        {
            var st = TryParseMouse(input, out var mouse, out consumed);
            ev = st == InputParseStatus.Ok ? new InputEvent(mouse) : default;
            return st;
        }

        var s = TryParseKey(input, out var key, out consumed);
        ev = s == InputParseStatus.Ok ? new InputEvent(key) : default;
        return s;
    }

    /// <summary>
    /// Try to parse one key event from <paramref name="input"/>.
    /// Reports printable BMP chars as <see cref="Key.Glyph"/> and decoded
    /// special keys as their named <see cref="Key"/> value. Mouse
    /// reports (CSI &lt;…) are NOT handled here — they short-circuit
    /// to <see cref="InputParseStatus.Invalid"/>.
    /// </summary>
    public static InputParseStatus TryParseKey(
        ReadOnlySpan<char> input,
        out KeyEvent key,
        out int consumed)
    {
        key = default;
        consumed = 0;
        if (input.IsEmpty) return InputParseStatus.Incomplete;

        var first = input[0];

        // Bare ESC, possibly followed by an Alt-prefixed char or a CSI/SS3 sequence.
        if (first == '\x1b')
        {
            if (input.Length == 1)
            {
                // Could be a real ESC press or the start of a longer
                // sequence still arriving — in v1 we report Incomplete
                // and let the caller decide via timeout. Concrete
                // input loops typically wait ~50 ms; if no more bytes
                // arrive, they synthesize a bare Escape.
                return InputParseStatus.Incomplete;
            }

            var second = input[1];
            if (second == '[')
                return ParseCsi(input, out key, out consumed);
            if (second == 'O')
                return ParseSs3(input, out key, out consumed);

            // ESC + printable → Alt+<char>. ESC followed by another
            // ESC is reported as a bare Escape with the second ESC
            // left in the buffer.
            if (second == '\x1b')
            {
                key = new KeyEvent(Key.Escape);
                consumed = 1;
                return InputParseStatus.Ok;
            }
            if (second >= 0x20 && second < 0x7f)
            {
                key = new KeyEvent(Key.Glyph, second, KeyModifiers.Alt);
                consumed = 2;
                return InputParseStatus.Ok;
            }

            // Unknown two-byte sequence — skip the ESC, let the next
            // call decide what the trailing byte is.
            consumed = 1;
            return InputParseStatus.Invalid;
        }

        // Single control bytes that map cleanly onto named keys.
        switch (first)
        {
            case '\t':
                key = new KeyEvent(Key.Tab);
                consumed = 1;
                return InputParseStatus.Ok;
            case '\r':
            case '\n':
                key = new KeyEvent(Key.Enter);
                consumed = 1;
                return InputParseStatus.Ok;
            case '\x7f':       // DEL — sent by most Unix terminals as Backspace.
            case '\b':         // BS  — what Windows console sends.
                key = new KeyEvent(Key.Backspace);
                consumed = 1;
                return InputParseStatus.Ok;
        }

        // Ctrl+letter: 0x01..0x1A maps to Ctrl+A..Ctrl+Z. We surface the
        // *uppercase* letter in Char and set the Ctrl modifier — the
        // letter case is irrelevant since terminals don't distinguish.
        if (first >= 0x01 && first <= 0x1a)
        {
            var letter = (char)('A' + (first - 1));
            key = new KeyEvent(Key.Glyph, letter, KeyModifiers.Ctrl);
            consumed = 1;
            return InputParseStatus.Ok;
        }

        // Printable: anything else in the BMP. Ignore stray sub-0x20
        // control bytes we haven't named.
        if (first >= 0x20)
        {
            key = new KeyEvent(Key.Glyph, first);
            consumed = 1;
            return InputParseStatus.Ok;
        }

        consumed = 1;
        return InputParseStatus.Invalid;
    }

    /// <summary>
    /// Try to parse one SGR-encoded mouse report (xterm mode 1006) from
    /// <paramref name="input"/>. The expected envelope is
    /// <c>ESC [ &lt; button ; x ; y M_or_m</c>.
    /// </summary>
    public static InputParseStatus TryParseMouse(
        ReadOnlySpan<char> input,
        out MouseEvent mouse,
        out int consumed)
    {
        mouse = default;
        consumed = 0;
        if (input.Length < 3) return InputParseStatus.Incomplete;
        if (input[0] != '\x1b' || input[1] != '[' || input[2] != '<')
        {
            consumed = 1;
            return InputParseStatus.Invalid;
        }

        // Walk through three semicolon-separated decimals, then a
        // terminator that's either 'M' (press / motion) or 'm' (release).
        var pos = 3;
        if (!TryReadInt(input, ref pos, out var btn))    return Tail(input, pos);
        if (pos >= input.Length || input[pos] != ';')    return Tail(input, pos);
        pos++;
        if (!TryReadInt(input, ref pos, out var x))      return Tail(input, pos);
        if (pos >= input.Length || input[pos] != ';')    return Tail(input, pos);
        pos++;
        if (!TryReadInt(input, ref pos, out var y))      return Tail(input, pos);

        if (pos >= input.Length) return InputParseStatus.Incomplete;
        var term = input[pos];
        if (term != 'M' && term != 'm')
        {
            consumed = pos + 1;
            return InputParseStatus.Invalid;
        }
        pos++;

        // Decode the SGR mouse "button" code:
        //   bits 0..1 = button (0 left, 1 middle, 2 right, 3 motion-only)
        //   bit  2    = Shift
        //   bit  3    = Alt
        //   bit  4    = Ctrl
        //   bit  5    = motion (mouse moved while held)
        //   bit  6    = wheel (in which case low bits encode wheel direction)
        var modifiers = KeyModifiers.None;
        if ((btn & 0x04) != 0) modifiers |= KeyModifiers.Shift;
        if ((btn & 0x08) != 0) modifiers |= KeyModifiers.Alt;
        if ((btn & 0x10) != 0) modifiers |= KeyModifiers.Ctrl;

        var motion = (btn & 0x20) != 0;
        var wheel  = (btn & 0x40) != 0;
        var low    = btn & 0x03;

        MouseButton button;
        MouseEventKind kind;

        if (wheel)
        {
            button = low == 0 ? MouseButton.WheelUp : MouseButton.WheelDown;
            kind   = MouseEventKind.Wheel;
        }
        else if (motion)
        {
            // Motion event: low bits identify the dragged button, or 3
            // for "no button held" (pure cursor motion).
            button = low switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Middle,
                2 => MouseButton.Right,
                _ => MouseButton.None,
            };
            kind = button == MouseButton.None ? MouseEventKind.Move : MouseEventKind.Drag;
        }
        else
        {
            button = low switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Middle,
                2 => MouseButton.Right,
                _ => MouseButton.None,
            };
            kind = term == 'M' ? MouseEventKind.Press : MouseEventKind.Release;
        }

        mouse = new MouseEvent(button, kind, x, y, modifiers);
        consumed = pos;
        return InputParseStatus.Ok;
    }

    private static InputParseStatus Tail(ReadOnlySpan<char> input, int pos)
    {
        // Helper: classify a partial parse based on whether we ran off
        // the end of the buffer (Incomplete) or hit unexpected bytes
        // (Invalid). Pos points at the stop-byte position when invalid.
        return pos >= input.Length
            ? InputParseStatus.Incomplete
            : InputParseStatus.Invalid;
    }

    private static InputParseStatus ParseCsi(
        ReadOnlySpan<char> input,
        out KeyEvent key,
        out int consumed)
    {
        key = default;
        consumed = 0;

        // input starts with ESC[ at this point; pos is the index of the
        // first byte AFTER the [. We peel off optional "params" (semicolon-
        // separated decimals), then the final letter.
        var pos = 2;

        // First parameter (the "primary" — usually a key code or the
        // sentinel "1" for arrow keys).
        var primary = -1;
        if (pos < input.Length && IsDigit(input[pos]))
            TryReadInt(input, ref pos, out primary);

        // Optional ; modifier
        var modByte = -1;
        if (pos < input.Length && input[pos] == ';')
        {
            pos++;
            if (!TryReadInt(input, ref pos, out modByte))
                return Tail(input, pos);
        }

        if (pos >= input.Length) return InputParseStatus.Incomplete;

        var final = input[pos];
        pos++; // include the final byte in consumed

        var modifiers = DecodeModifierByte(modByte);

        // Two final-byte families: tilde-terminated (CSI N~) and
        // letter-terminated (CSI A/B/.../P/Q/R/S, etc).
        if (final == '~')
        {
            var k = TildeKey(primary);
            if (k == Key.None)
            {
                consumed = pos;
                return InputParseStatus.Invalid;
            }
            key = new KeyEvent(k, '\0', modifiers);
            consumed = pos;
            return InputParseStatus.Ok;
        }

        // Letter-terminated CSI (cursor keys, F1..F4 with modifier).
        var named = LetterKey(final);
        if (named == Key.None)
        {
            consumed = pos;
            return InputParseStatus.Invalid;
        }

        key = new KeyEvent(named, '\0', modifiers);
        consumed = pos;
        return InputParseStatus.Ok;
    }

    private static InputParseStatus ParseSs3(
        ReadOnlySpan<char> input,
        out KeyEvent key,
        out int consumed)
    {
        // ESC O P/Q/R/S → F1/F2/F3/F4 (no modifiers possible in SS3 form).
        key = default;
        consumed = 0;
        if (input.Length < 3) return InputParseStatus.Incomplete;

        var k = input[2] switch
        {
            'P' => Key.F1,
            'Q' => Key.F2,
            'R' => Key.F3,
            'S' => Key.F4,
            _   => Key.None,
        };

        if (k == Key.None)
        {
            consumed = 3;
            return InputParseStatus.Invalid;
        }

        key = new KeyEvent(k);
        consumed = 3;
        return InputParseStatus.Ok;
    }

    private static Key TildeKey(int code) => code switch
    {
        2  => Key.Insert,
        3  => Key.Delete,
        5  => Key.PageUp,
        6  => Key.PageDown,
        7  => Key.Home,    // alternative encoding (some Unix terms)
        8  => Key.End,     // alternative encoding (some Unix terms)
        15 => Key.F5,
        17 => Key.F6,
        18 => Key.F7,
        19 => Key.F8,
        20 => Key.F9,
        21 => Key.F10,
        23 => Key.F11,
        24 => Key.F12,
        _  => Key.None,
    };

    private static Key LetterKey(char final) => final switch
    {
        'A' => Key.Up,
        'B' => Key.Down,
        'C' => Key.Right,
        'D' => Key.Left,
        'H' => Key.Home,
        'F' => Key.End,
        'P' => Key.F1,
        'Q' => Key.F2,
        'R' => Key.F3,
        'S' => Key.F4,
        _   => Key.None,
    };

    private static KeyModifiers DecodeModifierByte(int modByte)
    {
        // Xterm encodes modifiers as 1 + bitmask, so subtract 1 first.
        // -1 (no modifier given) and bytes outside 1..16 → no modifiers.
        if (modByte < 2 || modByte > 16) return KeyModifiers.None;
        var bits = modByte - 1;
        var m = KeyModifiers.None;
        if ((bits & 1) != 0) m |= KeyModifiers.Shift;
        if ((bits & 2) != 0) m |= KeyModifiers.Alt;
        if ((bits & 4) != 0) m |= KeyModifiers.Ctrl;
        if ((bits & 8) != 0) m |= KeyModifiers.Meta;
        return m;
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool TryReadInt(ReadOnlySpan<char> input, ref int pos, out int value)
    {
        value = 0;
        var start = pos;
        while (pos < input.Length && IsDigit(input[pos]))
        {
            // Saturate at int.MaxValue / 10 to keep the parser tame
            // even on adversarial input.
            if (value > 0x7fff_ffff / 10) return false;
            value = value * 10 + (input[pos] - '0');
            pos++;
        }
        return pos > start;
    }
}
