using Retro.Crt.Input;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// Single-line clickable button. Focusable by default; activates on
/// Enter / Space when focused, or on a left-mouse press inside its
/// bounds. Renders a bracketed label that inverts the foreground /
/// background (plus bold) when focused, so the focus ring stays
/// readable in monochrome / NO_COLOR environments without a custom
/// accent palette.
/// </summary>
public class Button : View
{
    public Button() { IsFocusable = true; }

    public Button(string label, Action? onClick = null) : this()
    {
        Label = label;
        if (onClick is not null) Click += onClick;
    }

    /// <summary>The text shown inside the button. <c>null</c> renders as empty.</summary>
    public string? Label { get; set; }

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    /// <summary>Fired on activation — Enter / Space when focused, or a mouse press inside.</summary>
    public event Action? Click;

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width < 2 || b.Height < 1) return;

        var label = Label ?? string.Empty;

        // Focus = invert (fg ↔ bg) + bold. Works for any reasonable
        // (Foreground, Background) pair without needing a third
        // accent color, which tended to land in red-on-red territory
        // when the user picked a bright background already.
        var fg    = HasFocus ? Background : Foreground;
        var bg    = HasFocus ? Foreground : Background;
        var attrs = HasFocus ? CellAttrs.Bold : CellAttrs.None;

        screen.FillRect(b.X, b.Y, b.Width, b.Height, new Cell(' ', fg, bg));

        var midRow = b.Y + b.Height / 2;
        var inner  = b.Width - 2;

        Span<char> buf = stackalloc char[b.Width];
        buf.Fill(' ');
        buf[0]            = '[';
        buf[b.Width - 1]  = ']';

        if (inner > 0 && label.Length > 0)
        {
            var clipped = label.Length > inner
                ? label.AsSpan(0, inner)
                : label.AsSpan();
            var startInside = (inner - clipped.Length) / 2;
            for (var i = 0; i < clipped.Length; i++)
                buf[1 + startInside + i] = clipped[i];
        }

        screen.PutString(b.X, midRow, buf, fg, bg, attrs);
    }

    public override void OnKey(KeyEvent key, Application app)
    {
        if (key.Key == Key.Enter ||
            (key.Key == Key.Glyph && key.Glyph == ' '))
        {
            Click?.Invoke();
        }
    }

    public override void OnMouse(MouseEvent mouse, Application app)
    {
        if (mouse.Kind == MouseEventKind.Press && mouse.Button == MouseButton.Left)
        {
            Click?.Invoke();
        }
    }
}
