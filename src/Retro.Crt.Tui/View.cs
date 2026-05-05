using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui;

/// <summary>
/// Base class for everything <see cref="Application"/> can host: a
/// view paints itself into a <see cref="ScreenBuffer"/> region and
/// optionally reacts to keyboard / mouse events. v1 ships a single
/// root-view model — the user is responsible for arranging children
/// (with <see cref="Split"/> / <see cref="Dock"/>) and forwarding
/// events to them inside their own draw / handle methods.
/// </summary>
public abstract class View
{
    private bool _dirty = true;
    private bool _hasFocus;

    /// <summary>
    /// Region of the screen this view owns, in absolute screen
    /// coordinates. Set by <see cref="Application"/> at startup; user
    /// code may override during composition (e.g., a parent view
    /// propagating layout to children).
    /// </summary>
    public Rect Bounds { get; set; }

    /// <summary>
    /// True when the next frame must redraw this view. Cleared by
    /// <see cref="Application"/> after each <see cref="OnDraw"/>; set
    /// again with <see cref="MarkDirty"/> when state changes.
    /// </summary>
    public bool IsDirty => _dirty;

    /// <summary>
    /// When true, this view participates in Tab navigation and may
    /// receive routed key events while it owns the focus. Defaults to
    /// <c>false</c> — only widgets that actually consume keyboard
    /// input should opt in.
    /// </summary>
    public bool IsFocusable { get; set; }

    /// <summary>
    /// True when this view currently owns the application focus. Set
    /// by <see cref="Application"/>; flipping it auto-marks the view
    /// dirty so focus rings repaint on the next frame.
    /// </summary>
    public bool HasFocus => _hasFocus;

    /// <summary>Mark this view's painted state as out of date.</summary>
    public void MarkDirty() => _dirty = true;

    internal void ClearDirty() => _dirty = false;

    internal void SetFocus(bool hasFocus)
    {
        if (_hasFocus == hasFocus) return;
        _hasFocus = hasFocus;
        MarkDirty();
    }

    /// <summary>
    /// Yield this view (and its descendants, for containers) in
    /// Tab-traversal order. Default: yield <c>this</c> if focusable.
    /// </summary>
    internal virtual IEnumerable<View> EnumerateFocusable()
    {
        if (IsFocusable) yield return this;
    }

    /// <summary>
    /// Return the deepest view containing the given screen-space cell,
    /// or <c>null</c> if no view in this subtree does. Coordinates are
    /// 0-based (already converted from the terminal's 1-based mouse
    /// reports by <see cref="Application"/>).
    /// </summary>
    internal virtual View? HitTest(int x, int y)
        => Bounds.Contains(x, y) ? this : null;

    /// <summary>
    /// Paint into <paramref name="screen"/>. The view should write only
    /// inside <see cref="Bounds"/> — outside writes do nothing harmful
    /// (the buffer clips), but they pollute the diff with cells the
    /// view doesn't own.
    /// </summary>
    public abstract void OnDraw(ScreenBuffer screen);

    /// <summary>
    /// Handle a key event. Default implementation does nothing.
    /// Call <c>app.Exit()</c> to leave the event loop, or
    /// <see cref="MarkDirty"/> to request a redraw on the next frame.
    /// </summary>
    public virtual void OnKey(KeyEvent key, Application app) { }

    /// <summary>
    /// Handle a mouse event. Default implementation does nothing.
    /// </summary>
    public virtual void OnMouse(MouseEvent mouse, Application app) { }

    /// <summary>
    /// Handle a bracketed-paste event. <paramref name="text"/> is the
    /// raw clipboard contents — newlines, tabs, and ESC bytes are NOT
    /// re-interpreted as keys. Default implementation does nothing.
    /// </summary>
    public virtual void OnPaste(string text, Application app) { }
}
