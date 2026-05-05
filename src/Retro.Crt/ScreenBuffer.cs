namespace Retro.Crt;

/// <summary>
/// A double-buffer-friendly cell grid: a fixed-size 2D array of
/// <see cref="Cell"/> with helpers for clearing, writing strings, and
/// filling rectangles. Pair with <see cref="ScreenRenderer.Render"/> to
/// flush a frame to a terminal as minimal ANSI.
/// </summary>
/// <remarks>
/// <para>
/// One cell == one terminal column; v1 does not model wide / surrogate
/// glyphs.
/// </para>
/// <para>
/// Mutation is direct — there is no transactional commit. Build the
/// next frame in one buffer, render diff against the previous frame,
/// then swap. Allocation cost is one <see cref="Cell"/> array per
/// buffer; reuse buffers across frames instead of re-allocating.
/// </para>
/// </remarks>
public sealed class ScreenBuffer
{
    private readonly Cell[] _cells;

    public int Width { get; }
    public int Height { get; }

    public ScreenBuffer(int width, int height)
    {
        if (width  < 1) throw new ArgumentOutOfRangeException(nameof(width),  width,  "Width must be ≥ 1.");
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be ≥ 1.");
        Width  = width;
        Height = height;
        _cells = new Cell[width * height];
        Array.Fill(_cells, Cell.Empty);
    }

    /// <summary>
    /// Read or write a single cell. Throws on out-of-bounds — callers
    /// that want clipping should use <see cref="PutString"/> /
    /// <see cref="FillRect"/>.
    /// </summary>
    public Cell this[int x, int y]
    {
        get
        {
            if ((uint)x >= (uint)Width)  ThrowX(x);
            if ((uint)y >= (uint)Height) ThrowY(y);
            return _cells[y * Width + x];
        }
        set
        {
            if ((uint)x >= (uint)Width)  ThrowX(x);
            if ((uint)y >= (uint)Height) ThrowY(y);
            _cells[y * Width + x] = value;
        }
    }

    /// <summary>Fill every cell with <paramref name="fill"/>.</summary>
    public void Clear(Cell fill) => Array.Fill(_cells, fill);

    /// <summary>Fill every cell with <see cref="Cell.Empty"/>.</summary>
    public void Clear() => Clear(Cell.Empty);

    /// <summary>
    /// Write <paramref name="text"/> starting at <c>(x, y)</c>, one cell
    /// per char, painted with <paramref name="fg"/> /
    /// <paramref name="bg"/> / <paramref name="attrs"/>. Clips at the
    /// right edge and skips entirely if the row is off-screen — silent
    /// because string writes are the geometry hot path and exceptions
    /// turn every render loop into a try/catch.
    /// </summary>
    public void PutString(int x, int y, ReadOnlySpan<char> text, Color fg, Color bg, CellAttrs attrs = CellAttrs.None)
    {
        if ((uint)y >= (uint)Height) return;
        if (text.IsEmpty) return;

        var startX = x;
        var i = 0;
        if (startX < 0)
        {
            // Skip the part that would land in negative columns rather
            // than failing — same intent as the y-bound: clip cleanly.
            i = -startX;
            startX = 0;
            if (i >= text.Length) return;
        }

        var rowBase = y * Width;
        var maxLen  = Width - startX;
        var len     = text.Length - i;
        if (len > maxLen) len = maxLen;
        if (len <= 0) return;

        for (var k = 0; k < len; k++)
            _cells[rowBase + startX + k] = new Cell(text[i + k], fg, bg, attrs);
    }

    /// <summary>
    /// Fill a rectangle with <paramref name="fill"/>. Clips against the
    /// buffer bounds; a fully off-screen rect is a no-op.
    /// </summary>
    public void FillRect(int x, int y, int width, int height, Cell fill)
    {
        if (width <= 0 || height <= 0) return;

        var x0 = x < 0 ? 0 : x;
        var y0 = y < 0 ? 0 : y;
        var x1 = x + width;  if (x1 > Width)  x1 = Width;
        var y1 = y + height; if (y1 > Height) y1 = Height;
        if (x0 >= x1 || y0 >= y1) return;

        for (var row = y0; row < y1; row++)
        {
            var rowBase = row * Width;
            for (var col = x0; col < x1; col++)
                _cells[rowBase + col] = fill;
        }
    }

    /// <summary>
    /// Internal raw access for the diff renderer — avoids re-doing the
    /// indexer's bounds checks in tight loops. Returns the underlying
    /// row-major array; do not mutate from outside the assembly.
    /// </summary>
    internal ReadOnlySpan<Cell> AsSpan() => _cells;

    private static void ThrowX(int x) =>
        throw new ArgumentOutOfRangeException(nameof(x), x, "Column out of buffer bounds.");

    private static void ThrowY(int y) =>
        throw new ArgumentOutOfRangeException(nameof(y), y, "Row out of buffer bounds.");
}
