namespace Retro.Crt;

/// <summary>Border style for <see cref="Table"/>.</summary>
public enum TableBorder : byte
{
    /// <summary>
    /// Full unicode box-drawing border around every cell, with
    /// junctions and a separator between header and body. ASCII
    /// fallback (<c>+</c>/<c>-</c>/<c>|</c>) on terminals without
    /// unicode support. Default.
    /// </summary>
    Box = 0,

    /// <summary>
    /// No borders at all — columns are aligned via padding only,
    /// header row is bold, no separator line. Reads cleaner in
    /// dense logs and passes through redirection without escape
    /// glyphs.
    /// </summary>
    None = 1,
}
