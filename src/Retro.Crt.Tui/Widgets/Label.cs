namespace Retro.Crt.Tui.Widgets;

/// <summary>Horizontal alignment for a <see cref="Label"/>'s text.</summary>
public enum TextAlign : byte
{
    Left,
    Center,
    Right,
}

/// <summary>
/// Static text. Not focusable. <see cref="Text"/> is split on
/// <c>\n</c> for multi-line rendering; lines longer than the bounds
/// width are clipped (no soft-wrap in v1 — keep the markup tight or
/// pre-wrap in the caller).
/// </summary>
public class Label : View
{
    public Label() { }

    public Label(string text)
    {
        Text = text;
    }

    public string? Text { get; set; }

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    public TextAlign Align { get; set; } = TextAlign.Left;

    public CellAttrs Attrs { get; set; } = CellAttrs.None;

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;

        screen.FillRect(b.X, b.Y, b.Width, b.Height,
            new Cell(' ', Foreground, Background));

        if (string.IsNullOrEmpty(Text)) return;

        var text = Text;
        var lineStart = 0;
        var row = 0;

        while (lineStart <= text.Length && row < b.Height)
        {
            var nl = text.IndexOf('\n', lineStart);
            var end = nl < 0 ? text.Length : nl;
            var line = text.AsSpan(lineStart, end - lineStart);

            if (line.Length > b.Width) line = line[..b.Width];

            var x = Align switch
            {
                TextAlign.Center => b.X + (b.Width - line.Length) / 2,
                TextAlign.Right  => b.X + b.Width - line.Length,
                _                => b.X,
            };

            screen.PutString(x, b.Y + row, line, Foreground, Background, Attrs);

            row++;
            if (nl < 0) break;
            lineStart = nl + 1;
        }
    }
}
