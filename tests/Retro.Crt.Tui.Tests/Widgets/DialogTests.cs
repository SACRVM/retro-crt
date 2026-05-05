using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

namespace Retro.Crt.Tui.Tests.Widgets;

public class DialogTests
{
    [Fact]
    public void Show_modal_makes_dialog_active()
    {
        var (app, dlg) = MakeAppWithDialog();

        app.ShowModal(dlg);

        Assert.Same(dlg, app.Modal);
    }

    [Fact]
    public void Close_pops_modal_and_runs_closed_event()
    {
        var (app, dlg) = MakeAppWithDialog();
        var fired = 0;
        dlg.Closed += () => fired++;
        app.ShowModal(dlg);

        DispatchKey(app, new KeyEvent(Key.Escape));

        Assert.Null(app.Modal);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Escape_does_not_close_when_disabled()
    {
        var (app, dlg) = MakeAppWithDialog();
        dlg.CloseOnEscape = false;
        app.ShowModal(dlg);

        DispatchKey(app, new KeyEvent(Key.Escape));

        Assert.Same(dlg, app.Modal);
    }

    [Fact]
    public void Modal_traps_keys_from_root()
    {
        var rootKeys = 0;
        var root = new RecordingContainer { Bounds = new Rect(0, 0, 80, 24) };
        root.OnKeyHandler = _ => rootKeys++;

        var app = new Application(root);
        var dlg = new Dialog { Title = "modal" };
        app.ShowModal(dlg);

        DispatchKey(app, new KeyEvent(Key.Glyph, 'q'));

        Assert.Equal(0, rootKeys);
    }

    [Fact]
    public void Tab_cycles_focus_within_modal_only()
    {
        var rootBtn = new Button("root") { Bounds = new Rect(0, 0, 10, 1) };
        var root = new RecordingContainer { Bounds = new Rect(0, 0, 80, 24), Children = { rootBtn } };
        var app  = new Application(root);

        var ok     = new Button("OK");
        var cancel = new Button("Cancel");
        var dlg = new Dialog { Title = "test", Buttons = { ok, cancel } };
        app.ShowModal(dlg);

        // After ShowModal the first focusable inside the dialog owns
        // focus — the dialog itself isn't focusable, so it's the OK
        // button.
        Assert.Same(ok, app.Focus);

        app.FocusNext();
        Assert.Same(cancel, app.Focus);

        // Wraps within the modal — never visits rootBtn.
        app.FocusNext();
        Assert.Same(ok, app.Focus);
    }

    [Fact]
    public void Mouse_clicks_outside_modal_are_swallowed()
    {
        var rootBtn = new RecordingButton { Bounds = new Rect(0, 0, 10, 1) };
        var root = new RecordingContainer { Bounds = new Rect(0, 0, 80, 24), Children = { rootBtn } };
        var app  = new Application(root);

        var dlg = new Dialog { Title = "test", PreferredWidth = 20, PreferredHeight = 10 };
        app.ShowModal(dlg);

        // Click at (1,1) — well outside the centered dialog, on top of rootBtn.
        DispatchMouse(app, new MouseEvent(MouseButton.Left, MouseEventKind.Press, 1, 1));

        Assert.Equal(0, rootBtn.Clicks);
    }

    [Fact]
    public void Renders_centered_frame_with_title()
    {
        var screen = new ScreenBuffer(20, 10);
        var dlg = new Dialog
        {
            Title           = "hi",
            PreferredWidth  = 10,
            PreferredHeight = 5,
            Bounds          = new Rect(0, 0, 20, 10),
        };

        dlg.OnDraw(screen);

        // Centered: x = (20-10)/2 = 5, y = (10-5)/2 = 2.
        Assert.Equal('┌', screen[5, 2].Glyph);
        Assert.Equal('┐', screen[14, 2].Glyph);
        Assert.Equal('└', screen[5, 6].Glyph);
        Assert.Equal('┘', screen[14, 6].Glyph);
    }

    [Fact]
    public void MessageBox_shows_modal_with_label_and_ok_button()
    {
        var root = new RecordingContainer { Bounds = new Rect(0, 0, 80, 24) };
        var app  = new Application(root);

        var dlg = Dialog.MessageBox(app, "Title", "hello world");

        Assert.Same(dlg, app.Modal);
        Assert.Equal("Title", dlg.Title);
        Assert.IsType<Label>(dlg.Content);
        Assert.Single(dlg.Buttons);
        Assert.Equal("OK", dlg.Buttons[0].Label);
    }

    [Fact]
    public void MessageBox_ok_click_closes()
    {
        var root = new RecordingContainer { Bounds = new Rect(0, 0, 80, 24) };
        var app  = new Application(root);

        var dlg = Dialog.MessageBox(app, "X", "y");
        // The MessageBox helper wires the OK button's Click → dlg.Close;
        // invoking the button's key handler exercises the same path.
        dlg.Buttons[0].OnKey(new KeyEvent(Key.Enter), app);

        Assert.Null(app.Modal);
    }

    private static (Application, Dialog) MakeAppWithDialog()
    {
        var root = new RecordingContainer { Bounds = new Rect(0, 0, 80, 24) };
        var app = new Application(root);
        var dlg = new Dialog { Title = "test" };
        return (app, dlg);
    }

    private static void DispatchKey(Application app, KeyEvent ev)
    {
        var m = typeof(Application).GetMethod(
            "DispatchKey",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(app, [ev]);
    }

    private static void DispatchMouse(Application app, MouseEvent ev)
    {
        var m = typeof(Application).GetMethod(
            "DispatchMouse",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m!.Invoke(app, [ev]);
    }

    private sealed class RecordingContainer : Container
    {
        public Action<KeyEvent>? OnKeyHandler;
        protected override void ArrangeChildren() { }
        public override void OnKey(KeyEvent key, Application app) => OnKeyHandler?.Invoke(key);
    }

    private sealed class RecordingButton : View
    {
        public int Clicks;
        public RecordingButton() { IsFocusable = true; }
        public override void OnDraw(ScreenBuffer screen) { }
        public override void OnMouse(MouseEvent mouse, Application app)
        {
            if (mouse.Kind == MouseEventKind.Press) Clicks++;
        }
    }
}
