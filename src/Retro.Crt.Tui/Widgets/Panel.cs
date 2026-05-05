using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// A bordered, optionally titled rectangle filled with the panel's
/// <see cref="Background"/> color. Foundation widget — usable on its
/// own as a placeholder, or as the visual frame of a container that
/// hosts richer content drawn on top.
/// </summary>
public class Panel : View
{
    /// <summary>Title rendered in the top edge. <c>null</c> = no title.</summary>
    public string? Title { get; set; }

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    /// <summary>
    /// Highlight color for the border / title when <see cref="IsActive"/>
    /// is true. Use it to surface focus without owning a focus tree yet.
    /// </summary>
    public Color Accent { get; set; } = Color.LightCyan;

    /// <summary>When true, the border + title use <see cref="Accent"/>.</summary>
    public bool IsActive { get; set; }

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width < 2 || b.Height < 2) return;

        var fg = IsActive ? Accent : Foreground;
        var fill = new Cell(' ', fg, Background);

        // Interior fill (one cell inside each edge).
        screen.FillRect(b.X + 1, b.Y + 1, b.Width - 2, b.Height - 2, fill);

        // Border.
        var hLine = new Cell('─', fg, Background);
        var vLine = new Cell('│', fg, Background);

        for (var x = b.X + 1; x < b.X + b.Width - 1; x++)
        {
            screen[x, b.Y]                = hLine;
            screen[x, b.Y + b.Height - 1] = hLine;
        }
        for (var y = b.Y + 1; y < b.Y + b.Height - 1; y++)
        {
            screen[b.X,                y] = vLine;
            screen[b.X + b.Width - 1,  y] = vLine;
        }

        screen[b.X,                b.Y]                = new Cell('┌', fg, Background);
        screen[b.X + b.Width - 1,  b.Y]                = new Cell('┐', fg, Background);
        screen[b.X,                b.Y + b.Height - 1] = new Cell('└', fg, Background);
        screen[b.X + b.Width - 1,  b.Y + b.Height - 1] = new Cell('┘', fg, Background);

        // Title overlay — " title " centered on the top edge if it fits.
        if (!string.IsNullOrEmpty(Title) && b.Width >= 6)
        {
            var maxTitleLen = b.Width - 4;
            var label = Title.Length > maxTitleLen
                ? Title.AsSpan(0, maxTitleLen)
                : Title.AsSpan();
            var withPadding = string.Concat(" ", label, " ");
            var titleX = b.X + (b.Width - withPadding.Length) / 2;
            screen.PutString(titleX, b.Y, withPadding, fg, Background, CellAttrs.Bold);
        }
    }
}
