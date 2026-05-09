namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// One-cell-thick rule painted as a single repeating glyph across
/// every cell of <see cref="View.Bounds"/>. Useful as a visual band
/// between header / content / footer rows in a multi-section layout.
/// Default <see cref="Glyph"/> is U+2500 (`─`); set to `'═'` / `'┄'`
/// / `'·'` for different rule styles, or to any single character for
/// ASCII-only renderings (`'-'` / `'='`).
/// </summary>
/// <remarks>
/// Not focusable and does not consume input. The widget paints
/// whatever rectangle it's placed in, so a vertical rule is just a
/// <c>Bounds.Width = 1</c> instance with <c>Glyph = '│'</c>.
/// </remarks>
public class Separator : View
{
    public char Glyph { get; set; } = '─';

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;

        screen.FillRect(b.X, b.Y, b.Width, b.Height,
            new Cell(Glyph, Foreground, Background));
    }
}
