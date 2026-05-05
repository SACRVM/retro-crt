using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

// Stage 4 smoke: a header, a sidebar (Panel), a main area (Panel), and
// a statusbar — wired through Dock + Split. Tab switches active panel,
// q quits.

var demo = new DemoView();
new Application(demo).Run();
return 0;

internal sealed class DemoView : View
{
    private readonly Panel _sidebar = new()
    {
        Title = "Sidebar",
        Foreground = Color.LightGray,
        Background = Color.DarkBlue,
        Accent = Color.LightCyan,
        IsActive = true,
    };

    private readonly Panel _main = new()
    {
        Title = "Main",
        Foreground = Color.LightGray,
        Background = Color.Black,
        Accent = Color.LightCyan,
    };

    public override void OnDraw(ScreenBuffer screen)
    {
        screen.FillRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height,
            new Cell(' ', Color.LightGray, Color.Black));

        // Dock header (1 row) + statusbar (1 row); body is the leftover.
        Span<DockSpec> docks = [
            new DockSpec(DockSide.Top,    1),
            new DockSpec(DockSide.Bottom, 1),
        ];
        Span<Rect> strips = stackalloc Rect[2];
        var body = Dock.Peel(Bounds, docks, strips);

        var header = strips[0];
        var status = strips[1];

        // Split body horizontally: 24-cell sidebar + remainder for main.
        Span<LayoutSize> sizes = [LayoutSize.Cells(24), LayoutSize.Star()];
        Span<Rect> bodyParts = stackalloc Rect[2];
        Split.Horizontal(body, sizes, bodyParts);

        _sidebar.Bounds = bodyParts[0];
        _main.Bounds    = bodyParts[1];

        DrawHeader(screen, header);
        DrawStatus(screen, status);

        _sidebar.OnDraw(screen);
        _main.OnDraw(screen);
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
        s.PutString(r.X + 1, r.Y, " Tab: switch panel │ Q: quit",
            Color.LightGray, Color.DarkGray);
    }

    public override void OnKey(KeyEvent key, Application app)
    {
        if (key.Key == Key.Glyph && (key.Glyph == 'q' || key.Glyph == 'Q'))
        {
            app.Exit();
            return;
        }
        if (key.Key == Key.Tab)
        {
            _sidebar.IsActive = !_sidebar.IsActive;
            _main.IsActive    = !_main.IsActive;
            MarkDirty();
        }
    }
}
