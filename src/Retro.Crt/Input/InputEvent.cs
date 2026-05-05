namespace Retro.Crt.Input;

/// <summary>
/// Discriminator on <see cref="InputEvent"/>. <see cref="None"/>
/// represents an "empty" input event (the <c>default</c> value of
/// the struct).
/// </summary>
public enum InputEventKind : byte
{
    None,
    Key,
    Mouse,
    Paste,
}

/// <summary>
/// Tagged-union input event: read <see cref="Kind"/> first, then access
/// the slot the discriminator names. Key and mouse events are
/// zero-allocation; paste events carry a <see cref="string"/> for the
/// pasted contents and so allocate one reference per event.
/// </summary>
public readonly struct InputEvent
{
    public InputEventKind Kind { get; }
    public KeyEvent Key { get; }
    public MouseEvent Mouse { get; }

    /// <summary>
    /// The pasted text when <see cref="Kind"/> is
    /// <see cref="InputEventKind.Paste"/>; otherwise <c>null</c>.
    /// </summary>
    public string? Paste { get; }

    public InputEvent(KeyEvent key)
    {
        Kind  = InputEventKind.Key;
        Key   = key;
        Mouse = default;
        Paste = null;
    }

    public InputEvent(MouseEvent mouse)
    {
        Kind  = InputEventKind.Mouse;
        Key   = default;
        Mouse = mouse;
        Paste = null;
    }

    public InputEvent(string paste)
    {
        Kind  = InputEventKind.Paste;
        Key   = default;
        Mouse = default;
        Paste = paste;
    }
}
