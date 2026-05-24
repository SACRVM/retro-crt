namespace Retro.Crt.Internals;

/// <summary>
/// Pure string-builder for <see cref="Rule"/>. No I/O, no colors — the
/// public widget wraps this with ANSI styling. Snapshot-testable.
/// </summary>
internal static class RuleRenderer
{
    /// <summary>
    /// Build a horizontal rule <paramref name="width"/> cells wide using
    /// <paramref name="horizontal"/> as the line glyph. With a non-empty
    /// <paramref name="title"/> the label sits centered inside one space of
    /// breathing room on each side (<c>── TITLE ──────</c>); a title wider
    /// than the rule is truncated to fit.
    /// </summary>
    public static string Build(string? title, int width, char horizontal)
    {
        if (width < 1) width = 1;

        if (string.IsNullOrEmpty(title))
            return new string(horizontal, width);

        var label = " " + title + " ";
        if (label.Length >= width)
            return label.Length > width ? label[..width] : label;

        var remaining = width - label.Length;
        var left = remaining / 2;
        var right = remaining - left;
        return new string(horizontal, left) + label + new string(horizontal, right);
    }
}
