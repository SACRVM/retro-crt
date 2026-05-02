namespace Retro.Crt;

/// <summary>
/// Whether — and how — a fake cursor should follow each character as the
/// <see cref="Typewriter"/> writes it.
/// </summary>
public enum TypewriterCursor : byte
{
    /// <summary>No cursor glyph between characters.</summary>
    None = 0,

    /// <summary>A solid block (<c>▌</c> or <c>|</c> on ASCII).</summary>
    Block = 1,

    /// <summary>An underscore cursor (<c>_</c>).</summary>
    Underline = 2,
}
