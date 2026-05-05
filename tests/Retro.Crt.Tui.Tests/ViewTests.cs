using Retro.Crt;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Tests;

public class ViewTests
{
    [Fact]
    public void New_view_starts_dirty()
    {
        Assert.True(new RecordingView().IsDirty);
    }

    [Fact]
    public void MarkDirty_after_clear_re_arms_redraw()
    {
        var view = new RecordingView();
        ClearDirtyVia(view);
        Assert.False(view.IsDirty);

        view.MarkDirty();
        Assert.True(view.IsDirty);
    }

    [Fact]
    public void Application_construction_requires_a_root()
    {
        Assert.Throws<ArgumentNullException>(() => new Application(null!));
    }

    [Fact]
    public void Application_exposes_its_root_view()
    {
        var view = new RecordingView();
        var app  = new Application(view);

        Assert.Same(view, app.Root);
    }

    private static void ClearDirtyVia(View view)
    {
        // Application.ClearDirty is internal; reach in via reflection so
        // tests don't need to spin up a real event loop.
        var m = typeof(View).GetMethod(
            "ClearDirty",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(view, null);
    }

    private sealed class RecordingView : View
    {
        public override void OnDraw(ScreenBuffer screen) { }
    }
}
