using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

// Stage 5a smoke: header + horizontal StackPanel of two focusable
// Panels + statusbar. Tab cycles real focus; q / Esc quits; click
// focuses a panel directly.

var sidebar = new Panel
{
    Title       = "Sidebar",
    Background  = Color.DarkBlue,
    Accent      = Color.LightCyan,
    IsFocusable = true,
};

var main = new Panel
{
    Title       = "Main",
    Background  = Color.Black,
    Accent      = Color.LightCyan,
    IsFocusable = true,
};

var body = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Sizes       = { LayoutSize.Cells(24), LayoutSize.Star() },
    Children    = { sidebar, main },
};

var root = new Frame { Children = { body } };
new Application(root).Run();
return 0;

internal sealed class Frame : Container
{
    private Rect _header;
    private Rect _status;

    protected override void ArrangeChildren()
    {
        Span<DockSpec> docks =
        [
            new DockSpec(DockSide.Top,    1),
            new DockSpec(DockSide.Bottom, 1),
        ];
        Span<Rect> strips = stackalloc Rect[2];
        var bodyRect = Dock.Peel(Bounds, docks, strips);
        _header = strips[0];
        _status = strips[1];

        if (Children.Count > 0) Children[0].Bounds = bodyRect;
    }

    public override void OnDraw(ScreenBuffer screen)
    {
        ArrangeChildren();
        screen.FillRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height,
            new Cell(' ', Color.LightGray, Color.Black));

        DrawHeader(screen, _header);
        DrawStatus(screen, _status);

        for (var i = 0; i < Children.Count; i++)
            Children[i].OnDraw(screen);
    }

    public override void OnKey(KeyEvent key, Application app)
    {
        if (key.Key == Key.Escape) { app.Exit(); return; }
        if (key.Key == Key.Glyph && (key.Glyph is 'q' or 'Q')) app.Exit();
    }

    private static void DrawHeader(ScreenBuffer s, Rect r)
    {
        s.FillRect(r.X, r.Y, r.Width, r.Height,
            new Cell(' ', Color.Black, Color.LightGray));
        s.PutString(r.X + 1, r.Y, "Retro.Crt.Tui — demo",
            Color.Black, Color.LightGray, CellAttrs.Bold);
    }

    private static void DrawStatus(ScreenBuffer s, Rect r)
    {
        s.FillRect(r.X, r.Y, r.Width, r.Height,
            new Cell(' ', Color.LightGray, Color.DarkGray));
        s.PutString(r.X + 1, r.Y, " Tab: next  Shift+Tab: prev  Q/Esc: quit",
            Color.LightGray, Color.DarkGray);
    }
}
