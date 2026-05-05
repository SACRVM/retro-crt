using Retro.Crt;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests;

public class FocusTests
{
    [Fact]
    public void New_view_has_no_focus()
    {
        var v = new RecordingView();
        Assert.False(v.HasFocus);
    }

    [Fact]
    public void Application_picks_first_focusable_via_FocusNext()
    {
        var a = new RecordingView { IsFocusable = true };
        var b = new RecordingView { IsFocusable = true };
        var root = new TestContainer { Children = { a, b } };
        var app = new Application(root);

        app.FocusNext();

        Assert.Same(a, app.Focus);
        Assert.True(a.HasFocus);
        Assert.False(b.HasFocus);
    }

    [Fact]
    public void FocusNext_wraps_around()
    {
        var a = new RecordingView { IsFocusable = true };
        var b = new RecordingView { IsFocusable = true };
        var root = new TestContainer { Children = { a, b } };
        var app = new Application(root);

        app.FocusNext(); // a
        app.FocusNext(); // b
        app.FocusNext(); // wraps to a

        Assert.Same(a, app.Focus);
    }

    [Fact]
    public void FocusPrevious_walks_in_reverse_with_wrap()
    {
        var a = new RecordingView { IsFocusable = true };
        var b = new RecordingView { IsFocusable = true };
        var root = new TestContainer { Children = { a, b } };
        var app = new Application(root);

        app.FocusPrevious(); // b (wraps from initial null)

        Assert.Same(b, app.Focus);
    }

    [Fact]
    public void Skips_non_focusable_views()
    {
        var a = new RecordingView { IsFocusable = false };
        var b = new RecordingView { IsFocusable = true };
        var root = new TestContainer { Children = { a, b } };
        var app = new Application(root);

        app.FocusNext();

        Assert.Same(b, app.Focus);
    }

    [Fact]
    public void Recurses_into_nested_containers()
    {
        var leaf = new RecordingView { IsFocusable = true };
        var inner = new TestContainer { Children = { leaf } };
        var root = new TestContainer { Children = { inner } };
        var app = new Application(root);

        app.FocusNext();

        Assert.Same(leaf, app.Focus);
    }

    [Fact]
    public void Setting_focus_marks_old_and_new_dirty()
    {
        var a = new RecordingView { IsFocusable = true };
        var b = new RecordingView { IsFocusable = true };
        var root = new TestContainer { Children = { a, b } };
        var app = new Application(root);

        app.FocusNext();           // a focused
        ClearDirtyVia(a);
        ClearDirtyVia(b);

        app.FocusNext();           // b takes focus

        Assert.True(a.IsDirty);    // lost focus → repaint
        Assert.True(b.IsDirty);    // gained focus → repaint
    }

    [Fact]
    public void Empty_tree_focus_is_a_no_op()
    {
        var root = new TestContainer();
        var app = new Application(root);

        app.FocusNext();

        Assert.Null(app.Focus);
    }

    private static void ClearDirtyVia(View view)
    {
        var m = typeof(View).GetMethod(
            "ClearDirty",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(view, null);
    }

    private sealed class RecordingView : View
    {
        public override void OnDraw(ScreenBuffer screen) { }
    }

    private sealed class TestContainer : Container
    {
        protected override void ArrangeChildren() { }
    }
}
