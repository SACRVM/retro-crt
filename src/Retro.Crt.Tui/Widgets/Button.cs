using Retro.Crt.Input;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// Single-line clickable button. Focusable by default; activates on
/// Enter / Space when focused, or on a mouse press inside its bounds.
/// Renders a bracketed label that inverts foreground / background when
/// focused so the focus ring stays obvious in monochrome / NO_COLOR
/// environments.
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

    /// <summary>Color used for the label / brackets while the button has focus.</summary>
    public Color Accent { get; set; } = Color.LightCyan;

    /// <summary>Fired on activation — Enter / Space when focused, or a mouse press inside.</summary>
    public event Action? Click;

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width < 2 || b.Height < 1) return;

        var label = Label ?? string.Empty;
        var fg = HasFocus ? Background : Foreground;
        var bg = HasFocus ? Accent     : Background;

        // Top-row only — buttons are intentionally one-line widgets;
        // taller bounds get padded above and below.
        var midRow = b.Y + b.Height / 2;

        screen.FillRect(b.X, b.Y, b.Width, b.Height, new Cell(' ', Foreground, Background));

        // " [ Label ] " — brackets give a clear button shape even
        // without color; inversion adds emphasis when focused.
        var inner = b.Width - 2;
        if (inner < 0) inner = 0;
        Span<char> buf = stackalloc char[b.Width];
        buf.Fill(' ');
        if (b.Width >= 2)
        {
            buf[0]            = '[';
            buf[b.Width - 1]  = ']';
        }

        if (inner > 0 && label.Length > 0)
        {
            var clipped = label.Length > inner
                ? label.AsSpan(0, inner)
                : label.AsSpan();
            var startInside = (inner - clipped.Length) / 2;
            for (var i = 0; i < clipped.Length; i++)
                buf[1 + startInside + i] = clipped[i];
        }

        screen.PutString(b.X, midRow, buf, fg, bg, HasFocus ? CellAttrs.Bold : CellAttrs.None);
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
