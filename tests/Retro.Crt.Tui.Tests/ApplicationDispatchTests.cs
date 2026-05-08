using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests;

/// <summary>
/// Exercises Application.DispatchMouse / DispatchKey via reflection so
/// we can test routing without spinning up a real terminal loop.
/// </summary>
public class ApplicationDispatchTests
{
    [Fact]
    public void Wheel_routes_to_view_under_cursor()
    {
        var hover  = new RecordingView { Bounds = new Rect(0, 0, 10, 5) };
        var focus  = new RecordingView { IsFocusable = true, Bounds = new Rect(20, 0, 10, 5) };
        var root = new TestContainer { Children = { hover, focus } };
        var app  = new Application(root);
        app.FocusNext(); // focuses `focus`

        // Cursor is over `hover`, even though `focus` owns the
        // keyboard focus. Browser-style routing: scroll the thing
        // you're hovering, not the focused widget.
        DispatchMouse(app, new MouseEvent(MouseButton.WheelDown, MouseEventKind.Wheel, 5, 3));

        Assert.Equal(1, hover.MouseEvents);
        Assert.Equal(0, focus.MouseEvents);
    }

    [Fact]
    public void Press_then_drag_route_to_capture_target()
    {
        var capture = new RecordingView { IsFocusable = true, Bounds = new Rect(0, 0, 10, 5) };
        var other   = new RecordingView { Bounds = new Rect(20, 0, 10, 5) };
        var root = new TestContainer { Children = { capture, other } };
        var app  = new Application(root);

        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Press, 5, 3));
        // Drag wanders into other's bounds — should still go to `capture`.
        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Drag, 25, 3));

        Assert.Equal(2, capture.MouseEvents);
        Assert.Equal(0, other.MouseEvents);
    }

    [Fact]
    public void Shift_modifier_drops_mouse_event_for_native_selection()
    {
        // Modern terminals reserve Shift+click / Shift+drag for native
        // text selection, bypassing the app's mouse capture. Some
        // forward the event anyway; the Application drops it so the
        // gesture works the same on every backend.
        var view = new RecordingView { IsFocusable = true, Bounds = new Rect(0, 0, 10, 5) };
        var root = new TestContainer { Children = { view } };
        var app  = new Application(root);

        DispatchMouse(app, new MouseEvent(
            MouseButton.Left, MouseEventKind.Press, 5, 3, KeyModifiers.Shift));
        DispatchMouse(app, new MouseEvent(
            MouseButton.Left, MouseEventKind.Drag, 6, 3, KeyModifiers.Shift));
        DispatchMouse(app, new MouseEvent(
            MouseButton.WheelDown, MouseEventKind.Wheel, 5, 3, KeyModifiers.Shift));

        Assert.Equal(0, view.MouseEvents);
    }

    [Fact]
    public void Non_shift_mouse_events_still_dispatch_normally()
    {
        // Negative case: bare mouse and Ctrl/Alt-modified mouse still
        // route to the view tree as usual — only Shift is reserved.
        var view = new RecordingView { IsFocusable = true, Bounds = new Rect(0, 0, 10, 5) };
        var root = new TestContainer { Children = { view } };
        var app  = new Application(root);

        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Press, 5, 3));
        DispatchMouse(app, new MouseEvent(
            MouseButton.Left, MouseEventKind.Press, 5, 3, KeyModifiers.Ctrl));
        DispatchMouse(app, new MouseEvent(
            MouseButton.Left, MouseEventKind.Press, 5, 3, KeyModifiers.Alt));

        Assert.Equal(3, view.MouseEvents);
    }

    [Fact]
    public void Release_clears_capture()
    {
        var capture = new RecordingView { IsFocusable = true, Bounds = new Rect(0, 0, 10, 5) };
        var other   = new RecordingView { IsFocusable = true, Bounds = new Rect(20, 0, 10, 5) };
        var root = new TestContainer { Children = { capture, other } };
        var app  = new Application(root);

        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Press,   5,  3));
        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Release, 5,  3));
        // Next drag (without an intervening press) goes to whoever's
        // under the cursor, since capture cleared.
        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Drag,    25, 3));

        Assert.Equal(2, capture.MouseEvents); // press + release
        Assert.Equal(1, other.MouseEvents);   // drag, after capture cleared
    }

    [Fact]
    public void Key_handled_by_focused_view_is_not_seen_by_root()
    {
        var focused = new KeyRecordingView { IsFocusable = true, Bounds = new Rect(0, 0, 5, 1), Handle = true };
        var root = new KeyRecordingContainer { Children = { focused } };
        var app  = new Application(root);
        app.FocusNext();

        DispatchKey(app, new KeyEvent(Key.Glyph, 'q'));

        Assert.Equal(1, focused.Keys);
        Assert.Equal(0, root.Keys);
    }

    [Fact]
    public void Key_unhandled_by_focused_view_bubbles_to_root()
    {
        var focused = new KeyRecordingView { IsFocusable = true, Bounds = new Rect(0, 0, 5, 1), Handle = false };
        var root = new KeyRecordingContainer { Children = { focused } };
        var app  = new Application(root);
        app.FocusNext();

        DispatchKey(app, new KeyEvent(Key.Glyph, 'q'));

        Assert.Equal(1, focused.Keys);
        Assert.Equal(1, root.Keys);
    }

    private static void DispatchMouse(Application app, MouseEvent ev)
    {
        var m = typeof(Application).GetMethod(
            "DispatchMouse",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(app, [ev]);
    }

    private static void DispatchKey(Application app, KeyEvent ev)
    {
        var m = typeof(Application).GetMethod(
            "DispatchKey",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(app, [ev]);
    }

    private sealed class RecordingView : View
    {
        public int MouseEvents { get; private set; }
        public override void OnDraw(ScreenBuffer screen) { }
        public override void OnMouse(MouseEvent mouse, Application app) => MouseEvents++;
    }

    private sealed class KeyRecordingView : View
    {
        public int Keys;
        public bool Handle;
        public override void OnDraw(ScreenBuffer screen) { }
        public override bool OnKey(KeyEvent key, Application app) { Keys++; return Handle; }
    }

    private sealed class KeyRecordingContainer : Container
    {
        public int Keys;
        protected override void ArrangeChildren() { }
        public override bool OnKey(KeyEvent key, Application app) { Keys++; return false; }
    }

    private sealed class TestContainer : Container
    {
        protected override void ArrangeChildren() { /* children set their own Bounds */ }
    }
}
