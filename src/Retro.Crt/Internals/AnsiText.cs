namespace Retro.Crt.Internals;

/// <summary>
/// Helpers for reasoning about text that may contain ANSI escape
/// sequences. Used where layout needs the <em>visible</em> width of a
/// string rather than its raw <see cref="string.Length"/> — e.g. a
/// <see cref="Table"/> cell carrying an OSC 8 hyperlink built via
/// <see cref="Crt.Link"/>.
/// </summary>
internal static class AnsiText
{
    /// <summary>
    /// Count the printable columns in <paramref name="s"/>, skipping ANSI
    /// escape sequences (SGR / CSI, OSC, and bare two-char escapes). One
    /// char counts as one column — consistent with the rest of the library,
    /// which does not model wide / surrogate glyphs.
    /// </summary>
    public static int VisibleWidth(ReadOnlySpan<char> s)
    {
        var width = 0;
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] == '\x1b' && i + 1 < s.Length)
            {
                var next = s[i + 1];
                if (next == '[')
                {
                    // CSI — skip params/intermediates up to a final byte 0x40..0x7e.
                    i += 2;
                    while (i < s.Length && (s[i] < '\x40' || s[i] > '\x7e')) i++;
                    if (i < s.Length) i++;
                    continue;
                }
                if (next == ']')
                {
                    // OSC — skip up to a BEL or ST (ESC backslash) terminator.
                    i += 2;
                    while (i < s.Length && s[i] != '\a' && s[i] != '\x1b') i++;
                    if (i < s.Length)
                    {
                        if (s[i] == '\a') i++;
                        else if (i + 1 < s.Length && s[i + 1] == '\\') i += 2;
                        else i++;
                    }
                    continue;
                }
                // Other two-char escape (e.g. DECSC "ESC 7").
                i += 2;
                continue;
            }

            width++;
            i++;
        }
        return width;
    }

    /// <summary>
    /// Append <paramref name="content"/> to <paramref name="sb"/> followed
    /// by enough spaces to reach <paramref name="width"/> visible columns.
    /// Like <see cref="string.PadRight(int)"/> but escape-aware, so a cell
    /// containing a hyperlink lines up by what the user actually sees.
    /// </summary>
    public static void AppendPadded(System.Text.StringBuilder sb, string content, int width)
    {
        sb.Append(content);
        var deficit = width - VisibleWidth(content);
        if (deficit > 0) sb.Append(' ', deficit);
    }
}
