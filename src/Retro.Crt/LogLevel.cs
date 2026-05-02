namespace Retro.Crt;

/// <summary>
/// Severity levels recognised by <see cref="Log"/>. The integer values are
/// stable but not part of the public contract — compare by name.
/// </summary>
public enum LogLevel : byte
{
    Debug   = 0,
    Info    = 1,
    Success = 2,
    Warn    = 3,
    Error   = 4,
}
