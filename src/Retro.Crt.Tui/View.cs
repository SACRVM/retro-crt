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

    /// <summary>Mark this view's painted state as out of date.</summary>
    public void MarkDirty() => _dirty = true;

    internal void ClearDirty() => _dirty = false;

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
}
