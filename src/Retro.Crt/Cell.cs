namespace Retro.Crt;

/// <summary>
/// Visual attributes a <see cref="Cell"/> can carry alongside its
/// foreground / background colors. Maps to a small subset of SGR
/// attributes that matter for retro UI work — bold for emphasis,
/// underline for hyperlinks / focused fields. Italic / strikethrough
/// can be added later if a real consumer needs them.
/// </summary>
[Flags]
public enum CellAttrs : byte
{
    None      = 0,
    Bold      = 1,
    Underline = 2,
}

/// <summary>
/// One cell of a <see cref="ScreenBuffer"/>: a single glyph plus the
/// foreground / background / attributes the diff renderer should paint
/// it with. Value-equality so cell-by-cell diffing is a straight
/// <c>==</c>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Glyph"/> is a single 16-bit char — surrogate pairs and
/// wide East-Asian glyphs are not modeled in v1; one cell == one column.
/// The name leaves room for a later grapheme-cluster representation
/// without renaming the field.
/// </para>
/// <para>
/// Use <see cref="Empty"/> as the default-fill for an unwritten buffer:
/// space on light-gray-on-black, the closest analogue of an empty
/// terminal row.
/// </para>
/// </remarks>
public readonly record struct Cell(
    char Glyph,
    Color Fg,
    Color Bg,
    CellAttrs Attrs = CellAttrs.None)
{
    /// <summary>
    /// Default empty cell — ASCII space on the classic DOS palette
    /// (<see cref="Color.LightGray"/> on <see cref="Color.Black"/>).
    /// Use this as the fill for a freshly created buffer.
    /// </summary>
    public static readonly Cell Empty = new(' ', Color.LightGray, Color.Black);
}
