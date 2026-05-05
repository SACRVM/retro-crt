using System.Text;
using Retro.Crt.Input;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// Single-line text editor. Focusable; printable glyphs insert at the
/// cursor, Left / Right / Home / End / Backspace / Delete edit, Enter
/// fires <see cref="Submit"/>. The cursor block is rendered as an
/// inverted cell at the caret column so it stays visible on terminals
/// that hide the real text cursor (most do, since the alt screen and
/// raw mode usually leave it parked).
/// </summary>
/// <remarks>
/// v1 stores the text in a <see cref="StringBuilder"/> — cheap inserts
/// / deletes and no per-keystroke string allocation. Long text scrolls
/// horizontally so the caret stays inside the visible window.
/// </remarks>
public class TextBox : View
{
    private readonly StringBuilder _text = new();
    private int _cursor;
    private int _scroll;

    public TextBox() { IsFocusable = true; }

    public TextBox(string text) : this()
    {
        if (!string.IsNullOrEmpty(text))
        {
            _text.Append(text);
            _cursor = _text.Length;
        }
    }

    /// <summary>The current text. Setting replaces the buffer and parks the cursor at the end.</summary>
    public string Text
    {
        get => _text.ToString();
        set
        {
            _text.Clear();
            if (!string.IsNullOrEmpty(value)) _text.Append(value);
            if (MaxLength > 0 && _text.Length > MaxLength)
                _text.Length = MaxLength;
            _cursor = _text.Length;
            _scroll = 0;
            TextChanged?.Invoke();
            MarkDirty();
        }
    }

    /// <summary>Cursor index, clamped to <c>[0, Text.Length]</c>.</summary>
    public int CursorPosition
    {
        get => _cursor;
        set
        {
            var clamped = value < 0 ? 0 : value > _text.Length ? _text.Length : value;
            if (clamped == _cursor) return;
            _cursor = clamped;
            MarkDirty();
        }
    }

    /// <summary>Hard cap on character count. <c>0</c> means unlimited.</summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Text shown in <see cref="PlaceholderForeground"/> when the buffer
    /// is empty. <c>null</c> renders as nothing.
    /// </summary>
    public string? Placeholder { get; set; }

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    public Color PlaceholderForeground { get; set; } = Color.DarkGray;

    /// <summary>Fired after every edit that changes <see cref="Text"/>.</summary>
    public event Action? TextChanged;

    /// <summary>Fired when the user presses Enter while focused.</summary>
    public event Action? Submit;

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width < 1 || b.Height < 1) return;

        screen.FillRect(b.X, b.Y, b.Width, b.Height, new Cell(' ', Foreground, Background));

        var row = b.Y + b.Height / 2;
        EnsureCursorVisible(b.Width);

        if (_text.Length == 0 && !HasFocus && !string.IsNullOrEmpty(Placeholder))
        {
            var ph = Placeholder!.Length > b.Width
                ? Placeholder.AsSpan(0, b.Width)
                : Placeholder.AsSpan();
            screen.PutString(b.X, row, ph, PlaceholderForeground, Background);
            return;
        }

        var visible = _text.Length - _scroll;
        if (visible > b.Width) visible = b.Width;

        if (visible > 0)
        {
            Span<char> buf = stackalloc char[visible];
            for (var i = 0; i < visible; i++) buf[i] = _text[_scroll + i];
            screen.PutString(b.X, row, buf, Foreground, Background);
        }

        if (HasFocus)
        {
            var caretCol = b.X + (_cursor - _scroll);
            if (caretCol >= b.X && caretCol < b.X + b.Width)
            {
                var glyph = _cursor < _text.Length ? _text[_cursor] : ' ';
                screen[caretCol, row] = new Cell(glyph, Background, Foreground, CellAttrs.Bold);
            }
        }
    }

    public override void OnKey(KeyEvent key, Application app)
    {
        switch (key.Key)
        {
            case Key.Glyph when key.Glyph >= ' ':
                Insert(key.Glyph);
                break;
            case Key.Backspace:
                if (_cursor > 0)
                {
                    _text.Remove(_cursor - 1, 1);
                    _cursor--;
                    OnEdited();
                }
                break;
            case Key.Delete:
                if (_cursor < _text.Length)
                {
                    _text.Remove(_cursor, 1);
                    OnEdited();
                }
                break;
            case Key.Left:
                if (_cursor > 0) { _cursor--; MarkDirty(); }
                break;
            case Key.Right:
                if (_cursor < _text.Length) { _cursor++; MarkDirty(); }
                break;
            case Key.Home:
                if (_cursor != 0) { _cursor = 0; MarkDirty(); }
                break;
            case Key.End:
                if (_cursor != _text.Length) { _cursor = _text.Length; MarkDirty(); }
                break;
            case Key.Enter:
                Submit?.Invoke();
                break;
        }
    }

    public override void OnMouse(MouseEvent mouse, Application app)
    {
        if (mouse.Kind != MouseEventKind.Press || mouse.Button != MouseButton.Left) return;

        var b = Bounds;
        var localX = (mouse.X - 1) - b.X;
        if (localX < 0) localX = 0;
        var target = _scroll + localX;
        if (target > _text.Length) target = _text.Length;
        if (target != _cursor) { _cursor = target; MarkDirty(); }
    }

    private void Insert(char glyph)
    {
        if (MaxLength > 0 && _text.Length >= MaxLength) return;
        _text.Insert(_cursor, glyph);
        _cursor++;
        OnEdited();
    }

    private void OnEdited()
    {
        TextChanged?.Invoke();
        MarkDirty();
    }

    private void EnsureCursorVisible(int width)
    {
        if (width < 1) return;
        if (_cursor < _scroll) _scroll = _cursor;
        else if (_cursor >= _scroll + width) _scroll = _cursor - width + 1;
        if (_scroll < 0) _scroll = 0;
    }
}
