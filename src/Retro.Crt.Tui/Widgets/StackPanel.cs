using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// Lay children out along one axis using <see cref="Split"/>. Each
/// child gets the matching entry from <see cref="Sizes"/>; missing
/// entries default to <c>LayoutSize.Star()</c> so unsized children
/// share leftover space evenly.
/// </summary>
public class StackPanel : Container
{
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>
    /// Per-child track size. Indexed by child position. Entries past
    /// the last child are ignored; missing entries default to
    /// <c>LayoutSize.Star()</c>.
    /// </summary>
    public IList<LayoutSize> Sizes { get; } = new List<LayoutSize>();

    protected override void ArrangeChildren()
    {
        var n = Children.Count;
        if (n == 0) return;

        Span<LayoutSize> sizes = stackalloc LayoutSize[n];
        Span<Rect>       rects = stackalloc Rect[n];

        for (var i = 0; i < n; i++)
            sizes[i] = i < Sizes.Count ? Sizes[i] : LayoutSize.Star();

        if (Orientation == Orientation.Horizontal)
            Split.Horizontal(Bounds, sizes, rects);
        else
            Split.Vertical(Bounds, sizes, rects);

        for (var i = 0; i < n; i++)
            Children[i].Bounds = rects[i];
    }
}
