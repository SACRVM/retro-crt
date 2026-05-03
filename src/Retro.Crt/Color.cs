using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// A console color in 24-bit truecolor, an xterm 256-palette index, or
/// one of the 16 standard SGR slots. Use <see cref="Rgb"/> for arbitrary
/// colors, <see cref="Indexed256"/> for xterm palette entries, and the
/// named static fields (<see cref="LightCyan"/>, <see cref="Brown"/>, …)
/// for the classic DOS palette mapped onto the user's terminal theme.
/// </summary>
public readonly record struct Color
{
    public ColorMode Mode { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    /// <summary>
    /// Palette index. 0..15 for <see cref="ColorMode.Standard16"/>,
    /// 0..255 for <see cref="ColorMode.Xterm256"/>; ignored for
    /// truecolor.
    /// </summary>
    public byte Index { get; }

    private Color(ColorMode mode, byte r, byte g, byte b, byte index)
    {
        Mode = mode;
        R = r;
        G = g;
        B = b;
        Index = index;
    }

    /// <summary>24-bit truecolor.</summary>
    public static Color Rgb(byte r, byte g, byte b) => new(ColorMode.Truecolor, r, g, b, 0);

    /// <summary>
    /// xterm 256-palette entry. Index 0..15 mirrors the user's terminal
    /// theme (same as <see cref="Standard"/>); 16..231 is the 6×6×6 RGB
    /// cube; 232..255 is the 24-step grayscale ramp.
    /// </summary>
    public static Color Indexed256(byte index)
        => new(ColorMode.Xterm256, 0, 0, 0, index);

    internal static Color Standard(byte index)
    {
        if (index > 15)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Standard16 index must be 0..15.");
        var (r, g, b) = ColorQuantization.BiosPalette[index];
        return new(ColorMode.Standard16, r, g, b, index);
    }

    // Classic DOS palette via the standard SGR slots. Index assignment matches
    // the IBM PC BIOS color codes so old-school muscle memory works.
    public static readonly Color Black        = Standard(0);
    public static readonly Color DarkBlue     = Standard(1);
    public static readonly Color DarkGreen    = Standard(2);
    public static readonly Color DarkCyan     = Standard(3);
    public static readonly Color DarkRed      = Standard(4);
    public static readonly Color DarkMagenta  = Standard(5);
    public static readonly Color Brown        = Standard(6);
    public static readonly Color LightGray    = Standard(7);
    public static readonly Color DarkGray     = Standard(8);
    public static readonly Color LightBlue    = Standard(9);
    public static readonly Color LightGreen   = Standard(10);
    public static readonly Color LightCyan    = Standard(11);
    public static readonly Color LightRed     = Standard(12);
    public static readonly Color LightMagenta = Standard(13);
    public static readonly Color Yellow       = Standard(14);
    public static readonly Color White        = Standard(15);

    /// <summary>
    /// Return a color that <paramref name="depth"/> can render exactly.
    /// Truecolor values get quantized to the closest 256-palette entry
    /// or BIOS-16 anchor; Xterm256 values get folded into Standard16 if
    /// the depth permits only 16 colors. Standard16 values pass through
    /// unchanged — they always render correctly. Returns the
    /// <c>default(Color)</c> sentinel for <see cref="ColorDepth.None"/>;
    /// callers should check capability before emitting.
    /// </summary>
    public Color For(ColorDepth depth) => depth switch
    {
        ColorDepth.Truecolor  => this,
        ColorDepth.Xterm256   => DownTo256(),
        ColorDepth.Standard16 => DownTo16(),
        _                     => default,
    };

    private Color DownTo256() => Mode switch
    {
        ColorMode.Truecolor  => Indexed256(ColorQuantization.ToXterm256(R, G, B)),
        _                    => this,
    };

    private Color DownTo16() => Mode switch
    {
        ColorMode.Standard16 => this,
        ColorMode.Truecolor  => Standard(ColorQuantization.ToStandard16(R, G, B)),
        ColorMode.Xterm256   => From256ToStandard16(Index),
        _                    => this,
    };

    private static Color From256ToStandard16(byte index)
    {
        // Indices 0..15 already are standard slots.
        if (index < 16) return Standard(index);
        // Cube entry: convert back to RGB and quantize.
        if (index < 232)
        {
            var i = index - 16;
            byte[] steps = [0, 95, 135, 175, 215, 255];
            var r = steps[i / 36];
            var g = steps[(i / 6) % 6];
            var b = steps[i % 6];
            return Standard(ColorQuantization.ToStandard16(r, g, b));
        }
        // Grayscale ramp.
        var v = (byte)(8 + (index - 232) * 10);
        return Standard(ColorQuantization.ToStandard16(v, v, v));
    }

    /// <summary>
    /// Public quantizer — find the closest BIOS-16 anchor for an
    /// arbitrary RGB triple. Useful when consumers want to pre-render
    /// theme colors for terminals that lack truecolor.
    /// </summary>
    public static byte QuantizeToStandard16(byte r, byte g, byte b)
        => ColorQuantization.ToStandard16(r, g, b);

    /// <summary>
    /// Public quantizer — find the closest xterm 256-palette entry for
    /// an arbitrary RGB triple. Returns an index in 16..255 (never the
    /// terminal-themed 0..15 slots).
    /// </summary>
    public static byte QuantizeToXterm256(byte r, byte g, byte b)
        => ColorQuantization.ToXterm256(r, g, b);

    /// <summary>
    /// Parse a CSS hex string: <c>#RRGGBB</c> or <c>#RGB</c> (case
    /// insensitive, with or without leading <c>#</c>). Returns a truecolor
    /// <see cref="Color"/>. Throws <see cref="FormatException"/> on bad input.
    /// </summary>
    public static Color FromHex(string hex)
    {
        if (TryFromHex(hex, out var c)) return c;
        throw new FormatException($"Not a valid hex color: '{hex}'.");
    }

    /// <summary>Non-throwing companion to <see cref="FromHex"/>.</summary>
    public static bool TryFromHex(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex)) return false;

        var span = hex.AsSpan().Trim();
        if (span.Length > 0 && span[0] == '#') span = span[1..];

        if (span.Length == 3)
        {
            if (!TryHex(span[0], out var r1)) return false;
            if (!TryHex(span[1], out var g1)) return false;
            if (!TryHex(span[2], out var b1)) return false;
            color = Rgb((byte)(r1 * 17), (byte)(g1 * 17), (byte)(b1 * 17));
            return true;
        }
        if (span.Length == 6)
        {
            if (!TryHexByte(span[..2],  out var r)) return false;
            if (!TryHexByte(span[2..4], out var g)) return false;
            if (!TryHexByte(span[4..6], out var b)) return false;
            color = Rgb(r, g, b);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Look up one of the named DOS palette entries by name (case
    /// insensitive). Accepts canonical names like <c>"LightCyan"</c> as
    /// well as the lower-cased variant.
    /// </summary>
    public static bool TryFromName(string? name, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(name)) return false;
        var n = name.Trim();

        // Manual switch keeps it AOT-clean (no reflection / dictionary).
        if (Eq(n, "Black"))        { color = Black;        return true; }
        if (Eq(n, "DarkBlue"))     { color = DarkBlue;     return true; }
        if (Eq(n, "DarkGreen"))    { color = DarkGreen;    return true; }
        if (Eq(n, "DarkCyan"))     { color = DarkCyan;     return true; }
        if (Eq(n, "DarkRed"))      { color = DarkRed;      return true; }
        if (Eq(n, "DarkMagenta"))  { color = DarkMagenta;  return true; }
        if (Eq(n, "Brown"))        { color = Brown;        return true; }
        if (Eq(n, "LightGray"))    { color = LightGray;    return true; }
        if (Eq(n, "DarkGray"))     { color = DarkGray;     return true; }
        if (Eq(n, "LightBlue"))    { color = LightBlue;    return true; }
        if (Eq(n, "LightGreen"))   { color = LightGreen;   return true; }
        if (Eq(n, "LightCyan"))    { color = LightCyan;    return true; }
        if (Eq(n, "LightRed"))     { color = LightRed;     return true; }
        if (Eq(n, "LightMagenta")) { color = LightMagenta; return true; }
        if (Eq(n, "Yellow"))       { color = Yellow;       return true; }
        if (Eq(n, "White"))        { color = White;        return true; }
        return false;
    }

    /// <summary>
    /// Parse either a hex string (<c>#RRGGBB</c> / <c>#RGB</c>) or a DOS
    /// palette name (<c>"LightCyan"</c>). Hex takes priority when input
    /// starts with <c>#</c>.
    /// </summary>
    public static bool TryParse(string? text, out Color color)
    {
        if (string.IsNullOrEmpty(text)) { color = default; return false; }
        var span = text.AsSpan().Trim();
        if (span.Length == 0) { color = default; return false; }
        if (span[0] == '#') return TryFromHex(text, out color);
        if (TryFromName(text, out color)) return true;
        // Allow bare hex (no '#') as a fallback.
        return TryFromHex(text, out color);
    }

    private static bool Eq(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool TryHex(char c, out int value)
    {
        if (c >= '0' && c <= '9') { value = c - '0'; return true; }
        if (c >= 'a' && c <= 'f') { value = 10 + (c - 'a'); return true; }
        if (c >= 'A' && c <= 'F') { value = 10 + (c - 'A'); return true; }
        value = 0; return false;
    }

    private static bool TryHexByte(ReadOnlySpan<char> pair, out byte b)
    {
        if (!TryHex(pair[0], out var hi)) { b = 0; return false; }
        if (!TryHex(pair[1], out var lo)) { b = 0; return false; }
        b = (byte)((hi << 4) | lo);
        return true;
    }
}
