using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tui;
using Retro.Crt.Tui.Layout;
using Retro.Crt.Tui.Widgets;

// Stage 5 widget tour:
//   header
//   ┌──────────────┬──────────────────────────────────────┐
//   │ menu         │ log viewer (scrollable, autoscroll)  │
//   │              │                                      │
//   │              ├──────────────────────────────────────┤
//   │              │ text box      [Send]                 │
//   └──────────────┴──────────────────────────────────────┘
//   status
//
// Tab cycles focus through the menu, log, text box, send button.
// Enter on the menu / send button activates. Enter in the text
// box sends the typed line to the log. F2 opens an About dialog
// (demonstrates the modal slot). Bracketed paste into the text
// box is reflected in the log as "[pasted N chars] preview".
// Esc quits when no modal is open. Q is NOT a global quit because
// it would collide with the text box.

var log = new LogViewer
{
    Foreground     = Color.LightGray,
    Background     = Color.Black,
    ScrollbarTrack = Color.DarkGray,
    ScrollbarThumb = Color.LightCyan,
};

for (var i = 0; i < 20; i++)
{
    var color = (i % 5) switch
    {
        0 => Color.LightCyan,
        1 => Color.Yellow,
        2 => Color.LightGreen,
        3 => Color.LightRed,
        _ => (Color?)null,
    };
    log.Append($"  log line {i:00} - tab to focus, arrows/wheel scroll", color);
}

Application? appRef = null;

var menu = new Menu
{
    Foreground = Color.LightGray,
    Background = Color.DarkBlue,
    Items =
    {
        new("Send greeting", () => log.Append("  Hi from the menu!", Color.LightGreen)),
        new("Clear log",     () => log.Clear()),
        new("About...",      () => ShowAbout(appRef!, log)),
        new("",              IsEnabled: false),
        new("Quit",          () => appRef?.Exit()),
    },
};

var input = new TextBox
{
    Placeholder = "type something and press Enter…",
    Foreground  = Color.LightGray,
    Background  = Color.DarkGray,
    MaxLength   = 200,
};

var sendButton = new Button("Send")
{
    Foreground = Color.LightGray,
    Background = Color.DarkBlue,
};

void SendInput()
{
    var text = input.Text;
    if (string.IsNullOrWhiteSpace(text)) return;
    log.Append($"  [you] {text}", Color.White);
    input.Text = string.Empty;
}

input.Submit  += SendInput;
sendButton.Click += SendInput;

input.TextChanged += () =>
{
    // Demo hook: surface live text-changed events as a status hint.
    // Disabled for now — uncomment if you want to see it fire.
    // log.Append($"  · changed → {input.Text.Length} chars", Color.DarkGray);
};

// Wrap input + send button in a row.
var inputRow = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Sizes       = { LayoutSize.Star(), LayoutSize.Cells(8) },
    Children    = { input, sendButton },
};

// Right pane: log on top, input row at the bottom.
var rightPane = new StackPanel
{
    Orientation = Orientation.Vertical,
    Sizes       = { LayoutSize.Star(), LayoutSize.Cells(1) },
    Children    = { log, inputRow },
};

// Two-column body: menu | right pane.
var body = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Sizes       = { LayoutSize.Cells(20), LayoutSize.Star() },
    Children    = { menu, rightPane },
};

var root = new Frame { Children = { body } };
appRef = new Application(root);
appRef.Run();
return 0;

static void ShowAbout(Application app, LogViewer log)
{
    var ok = new Button("OK")
    {
        Foreground = Color.LightGray,
        Background = Color.DarkBlue,
    };

    var dlg = new Dialog
    {
        Title           = "About Retro.Crt.Tui",
        PreferredWidth  = 50,
        PreferredHeight = 9,
        Buttons         = { ok },
    };
    dlg.Content = new AboutContent();
    dlg.Closed += () => log.Append("  · dialog closed", Color.DarkGray);

    ok.Click += dlg.Close;

    app.ShowModal(dlg);
}

internal sealed class AboutContent : View
{
    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        screen.FillRect(b.X, b.Y, b.Width, b.Height,
            new Cell(' ', Color.LightGray, Color.DarkBlue));
        if (b.Height >= 1)
            screen.PutString(b.X, b.Y,     "Stage 5 widget tour".AsSpan(),
                Color.White, Color.DarkBlue, CellAttrs.Bold);
        if (b.Height >= 3)
            screen.PutString(b.X, b.Y + 2, "Menu / TextBox / Dialog / paste".AsSpan(),
                Color.LightGray, Color.DarkBlue);
        if (b.Height >= 4)
            screen.PutString(b.X, b.Y + 3, "Esc closes this dialog.".AsSpan(),
                Color.LightCyan, Color.DarkBlue);
    }
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
        // Only Esc quits at the app level — and only when no modal is
        // open (Dialog handles its own Esc-to-close). Q would collide
        // with the TextBox glyph stream, so it's intentionally not a
        // global shortcut.
        if (key.Key == Key.Escape && app.Modal is null) { app.Exit(); return; }
        if (key.Key == Key.F2     && app.Modal is null) ShowAboutFromFrame(app);
    }

    private static void ShowAboutFromFrame(Application app)
    {
        // The frame can't reach the LogViewer directly — easiest path
        // is to walk the root's children list. For a one-off demo
        // shortcut this is fine; production code would route via a
        // shared command bus or pass the log down on construction.
        // We skip wiring it from here and let the menu's About item
        // handle it; F2 just shows a minimal dialog so the keyboard
        // shortcut still does something.
        var ok = new Button("OK")
        {
            Foreground = Color.LightGray,
            Background = Color.DarkBlue,
        };
        var dlg = new Dialog
        {
            Title           = "F2 shortcut",
            PreferredWidth  = 36,
            PreferredHeight = 7,
            Buttons         = { ok },
        };
        ok.Click += dlg.Close;
        app.ShowModal(dlg);
    }

    private static void DrawHeader(ScreenBuffer s, Rect r)
    {
        s.FillRect(r.X, r.Y, r.Width, r.Height,
            new Cell(' ', Color.Black, Color.LightGray));
        s.PutString(r.X + 1, r.Y, "Retro.Crt.Tui - Stage 5 demo",
            Color.Black, Color.LightGray, CellAttrs.Bold);
    }

    private static void DrawStatus(ScreenBuffer s, Rect r)
    {
        s.FillRect(r.X, r.Y, r.Width, r.Height,
            new Cell(' ', Color.LightGray, Color.DarkGray));
        s.PutString(r.X + 1, r.Y,
            " Tab: focus | arrows/wheel: scroll | Enter: activate/send | F2: about | Esc: quit",
            Color.LightGray, Color.DarkGray);
    }
}
