namespace Retro.Crt.Tui.Layout;

/// <summary>
/// Axis-aligned cell rectangle on a <c>ScreenBuffer</c>. Coordinates
/// are 0-based — column <c>X</c> through <c>X+Width-1</c>, row <c>Y</c>
/// through <c>Y+Height-1</c>. Empty rects (zero or negative width or
/// height) are valid layout outputs and mean "no space allocated".
/// </summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public static readonly Rect Empty = new(0, 0, 0, 0);

    /// <summary>True when <see cref="Width"/> or <see cref="Height"/> is &lt;= 0.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>One past the last column. Equal to <c>X + Width</c>.</summary>
    public int Right  => X + Width;
    /// <summary>One past the last row. Equal to <c>Y + Height</c>.</summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Shrink the rect by <paramref name="margin"/> cells on every side.
    /// The result is clamped to non-negative width / height.
    /// </summary>
    public Rect Inset(int margin) => Inset(margin, margin, margin, margin);

    /// <summary>
    /// Shrink the rect by per-side margins. Negative margins (i.e.
    /// growing) are accepted; the caller is responsible for staying
    /// inside any parent bounds. Width / height are clamped at zero.
    /// </summary>
    public Rect Inset(int left, int top, int right, int bottom)
    {
        var x = X + left;
        var y = Y + top;
        var w = Math.Max(0, Width  - left - right);
        var h = Math.Max(0, Height - top  - bottom);
        return new Rect(x, y, w, h);
    }

    /// <summary>
    /// Returns true if the cell at <paramref name="x"/>, <paramref name="y"/>
    /// is inside this rect.
    /// </summary>
    public bool Contains(int x, int y) =>
        x >= X && x < X + Width && y >= Y && y < Y + Height;
}
