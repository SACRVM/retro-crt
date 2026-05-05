using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

// Stage 5c smoke: header + horizontal split (Sidebar Panel | LogViewer)
// + footer row with two Buttons (Quit, Append). Tab cycles focus
// through Sidebar → LogViewer → Quit → Append; arrows / wheel scroll
// the log; Enter/Space activates the focused button; mouse clicks
// focus + activate. Q / Esc quit globally.

var sidebar = new Panel
{
    Title       = "Sidebar",
    Background  = Color.DarkBlue,
    Accent      = Color.LightCyan,
    IsFocusable = true,
};

var log = new LogViewer
{
    Foreground     = Color.LightGray,
    Background     = Color.Black,
    ScrollbarTrack = Color.DarkGray,
    ScrollbarThumb = Color.LightCyan,
};

for (var i = 0; i < 30; i++)
{
    var color = (i % 5) switch
    {
        0 => Color.LightCyan,
        1 => Color.Yellow,
        2 => Color.LightGreen,
        3 => Color.LightRed,
        _ => (Color?)null,
    };
    log.Append($"  log line {i:00} — Tab to focus, ↑/↓ or wheel to scroll", color);
}

var body = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Sizes       = { LayoutSize.Cells(24), LayoutSize.Star() },
    Children    = { sidebar, log },
};

Application? appRef = null;
var counter = 0;

var quitButton = new Button("Quit", () => appRef?.Exit())
{
    Foreground = Color.LightGray,
    Background = Color.DarkRed,
    Accent     = Color.LightRed,
};

var appendButton = new Button("Append", () =>
    log.Append($"  -> append #{++counter} from button click", Color.LightGreen))
{
    Foreground = Color.LightGray,
    Background = Color.DarkGray,
    Accent     = Color.Yellow,
};

var buttonRow = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Sizes       = { LayoutSize.Star(), LayoutSize.Cells(12), LayoutSize.Cells(12), LayoutSize.Star() },
    Children    = { new Spacer(), quitButton, appendButton, new Spacer() },
};

var content = new StackPanel
{
    Orientation = Orientation.Vertical,
    Sizes       = { LayoutSize.Star(), LayoutSize.Cells(1) },
    Children    = { body, buttonRow },
};

var root = new Frame { Children = { content } };
appRef = new Application(root);
appRef.Run();
return 0;

internal sealed class Spacer : View
{
    public override void OnDraw(ScreenBuffer screen) { /* invisible filler */ }
}

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
        s.PutString(r.X + 1, r.Y, " Tab: focus  ↑↓/wheel: scroll  Enter/Space: activate  Q/Esc: quit",
            Color.LightGray, Color.DarkGray);
    }
}
