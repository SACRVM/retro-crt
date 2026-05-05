namespace Retro.Crt.Input;

/// <summary>
/// Mouse button reported by the terminal. <see cref="None"/> covers
/// motion events with no button held.
/// </summary>
public enum MouseButton : byte
{
    None,
    Left,
    Middle,
    Right,
    WheelUp,
    WheelDown,
}

/// <summary>
/// What the user did with the mouse. <see cref="Move"/> is reported in
/// any-event tracking modes, <see cref="Drag"/> in motion-while-pressed
/// modes; both depend on which mouse mode the terminal is in.
/// </summary>
public enum MouseEventKind : byte
{
    Press,
    Release,
    Move,
    Drag,
    Wheel,
}

/// <summary>
/// One decoded mouse event. <see cref="X"/> / <see cref="Y"/> are 1-based
/// screen coordinates as the terminal reports them — same convention as
/// <c>Crt.GotoXY(column, row)</c>.
/// </summary>
public readonly record struct MouseEvent(
    MouseButton Button,
    MouseEventKind Kind,
    int X,
    int Y,
    KeyModifiers Modifiers = KeyModifiers.None);
