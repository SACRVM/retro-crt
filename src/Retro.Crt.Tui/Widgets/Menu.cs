using Retro.Crt.Input;

namespace Retro.Crt.Tui.Widgets;

/// <summary>
/// One row in a <see cref="Menu"/>. <see cref="Action"/> fires when
/// the item is activated (Enter on the focused menu, or a left click
/// on its row). <see cref="IsEnabled"/> is honored at navigation time
/// — disabled items are skipped by the arrow keys and dimmed in the
/// rendering.
/// </summary>
public sealed record MenuItem(string Label, Action? Action = null, bool IsEnabled = true);

/// <summary>
/// Vertical list of <see cref="MenuItem"/>s. Focusable; Up / Down
/// move the selection (skipping disabled rows), Home / End jump to
/// the ends, Enter activates. The selected row inverts foreground
/// and background while the menu has focus, so the cursor stays
/// readable on monochrome terminals.
/// </summary>
public class Menu : View
{
    public Menu() { IsFocusable = true; }

    public IList<MenuItem> Items { get; } = new List<MenuItem>();

    public Color Foreground { get; set; } = Color.LightGray;

    public Color Background { get; set; } = Color.Black;

    public Color DisabledForeground { get; set; } = Color.DarkGray;

    private int _selected;

    /// <summary>
    /// Index of the highlighted row. Clamped to <c>[0, Items.Count - 1]</c>;
    /// reading on an empty menu returns <c>0</c>.
    /// </summary>
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            var clamped = Items.Count == 0 ? 0
                : value < 0 ? 0
                : value >= Items.Count ? Items.Count - 1
                : value;
            if (clamped == _selected) return;
            _selected = clamped;
            MarkDirty();
        }
    }

    /// <summary>The currently selected item, or <c>null</c> on an empty menu.</summary>
    public MenuItem? SelectedItem
        => Items.Count == 0 ? null : Items[Math.Clamp(_selected, 0, Items.Count - 1)];

    /// <summary>
    /// Fires after <see cref="SelectedIndex"/> changes, including via
    /// arrow-key navigation and mouse clicks.
    /// </summary>
    public event Action? SelectionChanged;

    /// <summary>
    /// Activate the selected item (run its <c>Action</c>). Public so
    /// callers can wire a global shortcut (e.g., F2) without needing to
    /// fake a key event.
    /// </summary>
    public void Activate()
    {
        var item = SelectedItem;
        if (item is { IsEnabled: true }) item.Action?.Invoke();
    }

    public override void OnDraw(ScreenBuffer screen)
    {
        var b = Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;

        screen.FillRect(b.X, b.Y, b.Width, b.Height, new Cell(' ', Foreground, Background));

        var rows = Math.Min(b.Height, Items.Count);
        for (var row = 0; row < rows; row++)
        {
            var item       = Items[row];
            var isSelected = row == _selected && HasFocus;
            var fg = isSelected ? Background
                   : item.IsEnabled ? Foreground
                   : DisabledForeground;
            var bg = isSelected ? Foreground : Background;
            var attrs = isSelected ? CellAttrs.Bold : CellAttrs.None;

            screen.FillRect(b.X, b.Y + row, b.Width, 1, new Cell(' ', fg, bg, attrs));
            screen.PutString(b.X + 1, b.Y + row, item.Label.AsSpan(), fg, bg, attrs);
        }
    }

    public override void OnKey(KeyEvent key, Application app)
    {
        switch (key.Key)
        {
            case Key.Up:    MoveSelection(-1); break;
            case Key.Down:  MoveSelection( 1); break;
            case Key.Home:  JumpTo(forward: true);  break;
            case Key.End:   JumpTo(forward: false); break;
            case Key.Enter: Activate();             break;
        }
    }

    public override void OnMouse(MouseEvent mouse, Application app)
    {
        if (mouse.Kind == MouseEventKind.Wheel)
        {
            switch (mouse.Button)
            {
                case MouseButton.WheelUp:   MoveSelection(-1); break;
                case MouseButton.WheelDown: MoveSelection( 1); break;
            }
            return;
        }

        if (mouse.Kind != MouseEventKind.Press || mouse.Button != MouseButton.Left) return;

        var localY = (mouse.Y - 1) - Bounds.Y;
        if (localY < 0 || localY >= Items.Count) return;

        if (!Items[localY].IsEnabled) return;

        var changed = localY != _selected;
        _selected = localY;
        if (changed)
        {
            MarkDirty();
            SelectionChanged?.Invoke();
        }
        Activate();
    }

    private void MoveSelection(int delta)
    {
        if (Items.Count == 0) return;

        // Skip disabled entries by scanning in `delta`'s direction
        // until we hit an enabled row or run out of items.
        var idx = _selected;
        for (var step = 0; step < Items.Count; step++)
        {
            idx += delta;
            if (idx < 0) idx = Items.Count - 1;
            else if (idx >= Items.Count) idx = 0;
            if (Items[idx].IsEnabled) break;
        }
        if (!Items[idx].IsEnabled) return;
        if (idx == _selected) return;

        _selected = idx;
        MarkDirty();
        SelectionChanged?.Invoke();
    }

    private void JumpTo(bool forward)
    {
        if (Items.Count == 0) return;
        var idx = forward ? 0 : Items.Count - 1;
        var dir = forward ? 1 : -1;
        while (idx >= 0 && idx < Items.Count && !Items[idx].IsEnabled)
            idx += dir;
        if (idx < 0 || idx >= Items.Count) return;
        if (idx == _selected) return;

        _selected = idx;
        MarkDirty();
        SelectionChanged?.Invoke();
    }
}
