namespace Retro.Crt.Input;

/// <summary>
/// Discriminator on <see cref="InputEvent"/> — either a key event or a
/// mouse event. <see cref="None"/> represents an "empty" input event
/// (the <c>default</c> value of the struct).
/// </summary>
public enum InputEventKind : byte
{
    None,
    Key,
    Mouse,
}

/// <summary>
/// Tagged-union input event: read <see cref="Kind"/> first, then access
/// either <see cref="Key"/> or <see cref="Mouse"/>. Zero-allocation
/// transport for the parser → consumer pipeline.
/// </summary>
public readonly struct InputEvent
{
    public InputEventKind Kind { get; }
    public KeyEvent Key { get; }
    public MouseEvent Mouse { get; }

    public InputEvent(KeyEvent key)
    {
        Kind  = InputEventKind.Key;
        Key   = key;
        Mouse = default;
    }

    public InputEvent(MouseEvent mouse)
    {
        Kind  = InputEventKind.Mouse;
        Key   = default;
        Mouse = mouse;
    }
}
