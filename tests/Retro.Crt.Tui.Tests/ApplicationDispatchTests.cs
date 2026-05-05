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
    public void Wheel_routes_to_focus_regardless_of_cursor_position()
    {
        var hover  = new RecordingView { Bounds = new Rect(0, 0, 10, 5) };
        var focus  = new RecordingView { IsFocusable = true, Bounds = new Rect(20, 0, 10, 5) };
        var root = new TestContainer { Children = { hover, focus } };
        var app  = new Application(root);
        app.FocusNext(); // focuses `focus` (only focusable)

        DispatchMouse(app, new MouseEvent(MouseButton.WheelDown, MouseEventKind.Wheel, 5, 3));

        Assert.Equal(0, hover.MouseEvents); // cursor over hover, but ignored
        Assert.Equal(1, focus.MouseEvents);
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

    private static void DispatchMouse(Application app, MouseEvent ev)
    {
        var m = typeof(Application).GetMethod(
            "DispatchMouse",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(app, [ev]);
    }

    private sealed class RecordingView : View
    {
        public int MouseEvents { get; private set; }
        public override void OnDraw(ScreenBuffer screen) { }
        public override void OnMouse(MouseEvent mouse, Application app) => MouseEvents++;
    }

    private sealed class TestContainer : Container
    {
        protected override void ArrangeChildren() { /* children set their own Bounds */ }
    }
}
