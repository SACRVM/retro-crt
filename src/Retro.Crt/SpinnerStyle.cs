namespace Retro.Crt;

/// <summary>
/// Animation style for <see cref="Spinner"/>. Pick the frame set that
/// matches your aesthetic — <see cref="Pipe"/> is the classic ASCII
/// rotator and works on every terminal; the unicode variants (
/// <see cref="Braille"/>, <see cref="Block"/>, <see cref="Arc"/>)
/// silently fall back to <see cref="Pipe"/> on terminals without
/// unicode output.
/// </summary>
public enum SpinnerStyle
{
    /// <summary>Classic ASCII rotator: <c>|</c> <c>/</c> <c>-</c> <c>\</c>. Default.</summary>
    Pipe = 0,

    /// <summary>Three trailing dots: <c>.  </c> <c>.. </c> <c>...</c>. Pure ASCII.</summary>
    Dots,

    /// <summary>Smooth 10-frame braille spinner. Requires unicode.</summary>
    Braille,

    /// <summary>Four-corner quarter block. Requires unicode.</summary>
    Block,

    /// <summary>Rotating arc made of unicode quarter-circle glyphs. Requires unicode.</summary>
    Arc,
}
