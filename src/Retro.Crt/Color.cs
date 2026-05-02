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
}
