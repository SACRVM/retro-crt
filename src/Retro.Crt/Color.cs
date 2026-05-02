namespace Retro.Crt;

/// <summary>
/// A console color in either 24-bit truecolor or one of the 16 standard SGR
/// slots. Use <see cref="Rgb"/> for arbitrary colors and the named static
/// fields (<see cref="LightCyan"/>, <see cref="Brown"/>, …) for the classic
/// DOS palette mapped onto the user's terminal theme.
/// </summary>
public readonly record struct Color
{
    public ColorMode Mode { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    /// <summary>
    /// Index 0..15 for <see cref="ColorMode.Standard16"/>; ignored otherwise.
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

    internal static Color Standard(byte index)
    {
        if (index > 15)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Standard16 index must be 0..15.");
        return new(ColorMode.Standard16, 0, 0, 0, index);
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
