namespace Retro.Crt.Input;

/// <summary>
/// Logical key identifiers, decoded from ANSI escape sequences and
/// control-byte conventions. <see cref="KeyEvent.Glyph"/> carries the
/// actual printable when <see cref="Key"/> is <see cref="Key.Glyph"/>.
/// </summary>
public enum Key : byte
{
    None,
    /// <summary>A printable Unicode-BMP character — value in <see cref="KeyEvent.Glyph"/>.</summary>
    Glyph,
    Escape,
    Enter,
    Tab,
    Backspace,
    Delete,
    Insert,
    Up,
    Down,
    Left,
    Right,
    Home,
    End,
    PageUp,
    PageDown,
    F1,  F2,  F3,  F4,
    F5,  F6,  F7,  F8,
    F9,  F10, F11, F12,
}

/// <summary>
/// Modifier keys held while a key was generated. Encodes the standard
/// xterm modifier byte (<c>1 + (Shift?1:0) + (Alt?2:0) + (Ctrl?4:0)</c>)
/// after the <c>-1</c> normalization, plus a flag for the rare Meta /
/// Super key reported by some terminals.
/// </summary>
[Flags]
public enum KeyModifiers : byte
{
    None  = 0,
    Shift = 1,
    Alt   = 2,
    Ctrl  = 4,
    Meta  = 8,
}

/// <summary>
/// One decoded key press. <see cref="Key"/> identifies what was hit;
/// <see cref="Glyph"/> carries the character when <see cref="Key"/> is
/// <see cref="Input.Key.Glyph"/> (otherwise <c>'\0'</c>);
/// <see cref="Modifiers"/> reports any held modifiers the terminal
/// surfaced.
/// </summary>
public readonly record struct KeyEvent(
    Key Key,
    char Glyph = '\0',
    KeyModifiers Modifiers = KeyModifiers.None);
