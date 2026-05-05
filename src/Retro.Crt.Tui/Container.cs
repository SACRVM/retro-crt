using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui;

/// <summary>
/// A <see cref="View"/> that hosts child views and arranges them on
/// each draw. Concrete subclasses (e.g., <c>StackPanel</c>) implement
/// <see cref="ArrangeChildren"/>; the base handles the universal
/// concerns — drawing the children in order, walking the tree for
/// focus traversal, and routing hit-tests to the deepest descendant.
/// </summary>
public abstract class Container : View
{
    /// <summary>
    /// Mutable list of child views, drawn in index order. Adding a
    /// child does not automatically mark the container dirty — pair
    /// edits with <see cref="View.MarkDirty"/> at a sensible moment.
    /// </summary>
    public IList<View> Children { get; } = new List<View>();

    /// <summary>
    /// Set <see cref="View.Bounds"/> on every child for the upcoming
    /// frame. Called once per <see cref="OnDraw"/> before the children
    /// paint themselves.
    /// </summary>
    protected abstract void ArrangeChildren();

    public override void OnDraw(ScreenBuffer screen)
    {
        ArrangeChildren();
        for (var i = 0; i < Children.Count; i++)
            Children[i].OnDraw(screen);
    }

    internal override IEnumerable<View> EnumerateFocusable()
    {
        if (IsFocusable) yield return this;
        for (var i = 0; i < Children.Count; i++)
            foreach (var f in Children[i].EnumerateFocusable())
                yield return f;
    }

    internal override View? HitTest(int x, int y)
    {
        // Walk children in reverse so a later child drawn on top of an
        // earlier sibling captures the click — same z-order rule the
        // diff renderer uses.
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            var hit = Children[i].HitTest(x, y);
            if (hit is not null) return hit;
        }
        return base.HitTest(x, y);
    }
}
