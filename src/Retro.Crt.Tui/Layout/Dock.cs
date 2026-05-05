namespace Retro.Crt.Tui.Layout;

/// <summary>
/// Side a <see cref="DockSpec"/> peels off the parent rect.
/// </summary>
public enum DockSide : byte
{
    Top,
    Bottom,
    Left,
    Right,
}

/// <summary>
/// One dock entry: peel <see cref="Cells"/> cells off the parent on
/// <see cref="Side"/>. A header bar is <c>(DockSide.Top, 1)</c>; a
/// statusbar is <c>(DockSide.Bottom, 1)</c>; a sidebar is
/// <c>(DockSide.Left, 20)</c>.
/// </summary>
public readonly record struct DockSpec(DockSide Side, int Cells);

/// <summary>
/// Peel docked strips off a parent rect, returning the remaining
/// center. Mirrors WinForms / Turbo Vision <c>Dock</c> — each strip
/// claims its full slice of cells from the current available space, in
/// the order given.
/// </summary>
public static class Dock
{
    /// <summary>
    /// Apply <paramref name="specs"/> to <paramref name="bounds"/>.
    /// <paramref name="output"/>[i] receives the rect allocated for
    /// spec i (in declaration order); the return value is the leftover
    /// center rect after every strip is peeled. <paramref name="output"/>
    /// must have the same length as <paramref name="specs"/>.
    /// </summary>
    /// <remarks>
    /// Strips are clamped to whatever space is still available — a
    /// header asking for 5 rows in a 3-row terminal gets 3, and the
    /// next strip in line gets 0. The center can shrink to
    /// <see cref="Rect.Empty"/> if the strips consume everything.
    /// </remarks>
    public static Rect Peel(Rect bounds, ReadOnlySpan<DockSpec> specs, Span<Rect> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(output.Length, specs.Length, nameof(output));

        var center = bounds;
        for (var i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            var want = Math.Max(0, spec.Cells);
            switch (spec.Side)
            {
                case DockSide.Top:
                {
                    var got = Math.Min(want, center.Height);
                    output[i] = new Rect(center.X, center.Y, center.Width, got);
                    center = new Rect(center.X, center.Y + got, center.Width, center.Height - got);
                    break;
                }
                case DockSide.Bottom:
                {
                    var got = Math.Min(want, center.Height);
                    output[i] = new Rect(center.X, center.Y + center.Height - got, center.Width, got);
                    center = new Rect(center.X, center.Y, center.Width, center.Height - got);
                    break;
                }
                case DockSide.Left:
                {
                    var got = Math.Min(want, center.Width);
                    output[i] = new Rect(center.X, center.Y, got, center.Height);
                    center = new Rect(center.X + got, center.Y, center.Width - got, center.Height);
                    break;
                }
                case DockSide.Right:
                {
                    var got = Math.Min(want, center.Width);
                    output[i] = new Rect(center.X + center.Width - got, center.Y, got, center.Height);
                    center = new Rect(center.X, center.Y, center.Width - got, center.Height);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(specs),
                        $"Unknown DockSide '{spec.Side}'.");
            }
        }
        return center;
    }
}
