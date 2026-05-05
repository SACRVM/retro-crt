using Retro.Crt.Input;
using Retro.Crt.Tui.Layout;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// Centered modal overlay with a titled border, a content area, and
/// an optional footer button row. Show via
/// <see cref="Application.ShowModal"/>; close with
/// <see cref="Close"/> or by pressing Esc (when
/// <see cref="CloseOnEscape"/> is true). The dialog dims and skips
/// the background — input is restricted to the dialog's subtree
/// while it is modal.
/// </summary>
/// <remarks>
/// Layout: the dialog reserves one row for the title, fills the
/// middle with <see cref="Content"/>, and reserves the bottom rows
/// for <see cref="Buttons"/> in a horizontal strip. Pass <c>null</c>
/// for <see cref="Content"/> to leave the body empty (a
/// confirmation-style dialog with just a title and buttons).
/// </remarks>
public class Dialog : Container
{
    /// <summary>Centered title rendered on the top border. <c>null</c> leaves the border blank.</summary>
    public string? Title { get; set; }

    /// <summary>Optional body view, sized to the interior between title and buttons.</summary>
    public View? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(_content, value)) return;
            if (_content is not null) Children.Remove(_content);
            _content = value;
            if (_content is not null) Children.Insert(0, _content);
            MarkDirty();
        }
    }
    private View? _content;

    /// <summary>Buttons rendered along the bottom edge, left-to-right.</summary>
    public IList<Button> Buttons { get; } = new List<Button>();

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.DarkBlue;

    public Color Border { get; set; } = Color.LightCyan;

    /// <summary>
    /// Width of the dialog rectangle in cells. <c>0</c> = use 60% of
    /// the screen width, clamped to <c>[20, screenWidth - 4]</c>.
    /// </summary>
    public int PreferredWidth { get; set; }

    /// <summary>
    /// Height of the dialog rectangle in cells. <c>0</c> = use 40% of
    /// the screen height, clamped to <c>[7, screenHeight - 2]</c>.
    /// </summary>
    public int PreferredHeight { get; set; }

    /// <summary>When true, an Esc key on the dialog calls <see cref="Close"/>. Default <c>true</c>.</summary>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Fires after <see cref="Close"/>. The action runs *before*
    /// <see cref="Application.CloseModal"/> tears the modal down, so
    /// handlers can read dialog state (e.g., a TextBox's Text) before
    /// it goes out of scope.
    /// </summary>
    public event Action? Closed;

    /// <summary>Close the dialog and pop it from the application's modal slot.</summary>
    public void Close()
    {
        Closed?.Invoke();
        _ownerApp?.CloseModal();
    }

    public override void OnKey(KeyEvent key, Application app)
    {
        // Track the owning app so Close() can pop the modal without
        // requiring callers to hold an Application reference.
        _ownerApp = app;
        if (CloseOnEscape && key.Key == Key.Escape) { Close(); return; }
    }

    private Application? _ownerApp;

    protected override void ArrangeChildren()
    {
        var screen = Bounds;
        var w = PreferredWidth  > 0 ? PreferredWidth  : Math.Max(20, screen.Width  * 6 / 10);
        var h = PreferredHeight > 0 ? PreferredHeight : Math.Max( 7, screen.Height * 4 / 10);
        if (w > screen.Width  - 4) w = Math.Max(20, screen.Width  - 4);
        if (h > screen.Height - 2) h = Math.Max( 7, screen.Height - 2);

        var rect = new Rect(
            screen.X + (screen.Width  - w) / 2,
            screen.Y + (screen.Height - h) / 2,
            w, h);
        _frame = rect;

        // Reserve top border (1) + bottom button row (1 if any).
        var hasButtons = Buttons.Count > 0;
        var contentRect = rect.Inset(1, 1, 1, hasButtons ? 2 : 1);
        _contentRect = contentRect;

        if (_content is not null) _content.Bounds = contentRect;

        if (hasButtons)
        {
            var rowY     = rect.Y + rect.Height - 2;
            var totalGap = 2;
            var perBtn   = Math.Max(8, (rect.Width - 2 - totalGap * (Buttons.Count - 1)) / Buttons.Count);
            var x        = rect.X + 1;
            for (var i = 0; i < Buttons.Count; i++)
            {
                Buttons[i].Bounds = new Rect(x, rowY, perBtn, 1);
                x += perBtn + totalGap;
            }
        }
    }

    private Rect _frame;
    private Rect _contentRect;

    public override void OnDraw(ScreenBuffer screen)
    {
        ArrangeChildren();

        var fill = new Cell(' ', Foreground, Background);
        screen.FillRect(_frame.X, _frame.Y, _frame.Width, _frame.Height, fill);

        var hLine = new Cell('─', Border, Background);
        var vLine = new Cell('│', Border, Background);

        var f = _frame;
        for (var x = f.X + 1; x < f.X + f.Width - 1; x++)
        {
            screen[x, f.Y]                = hLine;
            screen[x, f.Y + f.Height - 1] = hLine;
        }
        for (var y = f.Y + 1; y < f.Y + f.Height - 1; y++)
        {
            screen[f.X,               y] = vLine;
            screen[f.X + f.Width - 1, y] = vLine;
        }
        screen[f.X,               f.Y]                = new Cell('┌', Border, Background);
        screen[f.X + f.Width - 1, f.Y]                = new Cell('┐', Border, Background);
        screen[f.X,               f.Y + f.Height - 1] = new Cell('└', Border, Background);
        screen[f.X + f.Width - 1, f.Y + f.Height - 1] = new Cell('┘', Border, Background);

        if (!string.IsNullOrEmpty(Title) && f.Width >= 6)
        {
            var maxTitleLen = f.Width - 4;
            var label = Title.Length > maxTitleLen
                ? Title.AsSpan(0, maxTitleLen)
                : Title.AsSpan();
            var withPadding = string.Concat(" ", label, " ");
            var titleX = f.X + (f.Width - withPadding.Length) / 2;
            screen.PutString(titleX, f.Y, withPadding, Border, Background, CellAttrs.Bold);
        }

        _content?.OnDraw(screen);
        for (var i = 0; i < Buttons.Count; i++)
            Buttons[i].OnDraw(screen);
    }

    internal override IEnumerable<View> EnumerateFocusable()
    {
        if (IsFocusable) yield return this;
        if (_content is not null)
            foreach (var f in _content.EnumerateFocusable()) yield return f;
        for (var i = 0; i < Buttons.Count; i++)
            foreach (var f in Buttons[i].EnumerateFocusable()) yield return f;
    }

    internal override View? HitTest(int x, int y)
    {
        if (!_frame.Contains(x, y)) return null;

        for (var i = Buttons.Count - 1; i >= 0; i--)
        {
            var hit = Buttons[i].HitTest(x, y);
            if (hit is not null) return hit;
        }
        if (_content is not null)
        {
            var hit = _content.HitTest(x, y);
            if (hit is not null) return hit;
        }
        return this;
    }
}
