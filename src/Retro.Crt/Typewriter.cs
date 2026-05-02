using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Reveals text one character at a time. Optional fake cursor between
/// characters and optional <see cref="TypewriterFade.Alpha"/> fade-in
/// (brightness ramp on the final glyph). The trailing cursor, if any, is
/// always erased before <see cref="Type"/> returns.
/// <para>
/// Assumes one terminal cell per <see cref="char"/>: surrogate pairs
/// (emoji), combining marks, and wide CJK glyphs break the
/// cursor-overwrite tracking used by cursor mode and alpha fade. Stick
/// to BMP single-cell characters for animated reveals.
/// </para>
/// </summary>
public static class Typewriter
{
    /// <summary>
    /// Reveal <paramref name="text"/> character-by-character. Blocks the
    /// calling thread between characters via <see cref="Thread.Sleep(int)"/>.
    /// </summary>
    /// <param name="text">Text to reveal.</param>
    /// <param name="msPerChar">Time budget per character. Zero writes the whole string instantly.</param>
    /// <param name="fg">Static foreground color. Ignored when <paramref name="gradient"/> is set.</param>
    /// <param name="cursor">Cursor glyph shown while waiting for the next char.</param>
    /// <param name="fade">How each character appears.</param>
    /// <param name="gradient">If set, color is interpolated across the string. Both endpoints must be truecolor.</param>
    public static void Type(
        string text,
        int msPerChar = 30,
        Color? fg = null,
        TypewriterCursor cursor = TypewriterCursor.None,
        TypewriterFade fade = TypewriterFade.None,
        (Color from, Color to)? gradient = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Cursor and fade animations rely on CSI cursor-left to overwrite
        // the previous frame in place. With ANSI off (output redirected,
        // NO_COLOR, dumb terminal) those escapes don't apply, so we'd leave
        // every intermediate glyph on screen. Disable the animations and
        // dump the final string instead — readable in logs, still typed.
        var ansi = Crt.ColorEnabled;
        if (!ansi)
        {
            cursor = TypewriterCursor.None;
            fade = TypewriterFade.None;
        }

        // Fast path: no pacing requested, just dump the line.
        if (msPerChar <= 0)
        {
            if (gradient is { } g0 && g0.from.Mode == ColorMode.Truecolor && g0.to.Mode == ColorMode.Truecolor)
            {
                for (var i = 0; i < text.Length; i++)
                    WriteWithColor(text[i].ToString(), Banner.Interpolate(g0.from, g0.to, i, text.Length));
            }
            else if (fg is { } f0)
            {
                WriteWithColor(text, f0);
            }
            else
            {
                Console.Out.Write(text);
            }
            return;
        }

        var hasGradient = gradient is { } g
            && g.from.Mode == ColorMode.Truecolor
            && g.to.Mode == ColorMode.Truecolor;

        // Hide the terminal's native cursor for the whole reveal. Otherwise
        // it blinks at the write position between frames — visible as a
        // jittering caret on every character, even without our fake cursor.
        var hidCursor = false;
        if (ansi)
        {
            Console.Out.Write(AnsiCodes.HideCursor);
            hidCursor = true;
        }

        var colorActive = false;
        var prevCursor = false;

        for (var i = 0; i < text.Length; i++)
        {
            if (prevCursor)
            {
                Console.Out.Write(AnsiCodes.CursorLeft1);
                prevCursor = false;
            }

            var c = text[i];
            Color? color = hasGradient
                ? Banner.Interpolate(gradient!.Value.from, gradient.Value.to, i, text.Length)
                : fg;

            // Whitespace and non-printing chars: no fade (looks weird on
            // spaces, and \r / \n move the cursor anyway).
            if (c is ' ' or '\t' or '\r' or '\n' || char.IsControl(c))
            {
                if (ansi && color is { } w) { ApplyColor(w); colorActive = true; }
                Console.Out.Write(c);
                Sleep(msPerChar);
            }
            else
            {
                // Alpha fade only works in truecolor — Standard16 has no
                // brightness scaling. Outside truecolor we fall through to
                // the no-fade path so the timing still feels right.
                var canAlpha = fade == TypewriterFade.Alpha
                    && ansi
                    && color is { Mode: ColorMode.Truecolor };

                if (canAlpha)
                {
                    DoAlphaFade(c, color!.Value, msPerChar);
                    colorActive = true;
                }
                else
                {
                    if (ansi && color is { } n) { ApplyColor(n); colorActive = true; }
                    Console.Out.Write(c);
                    Sleep(msPerChar);
                }
            }

            // After CR/LF cursor-left tracking points at the new line, so a
            // fake cursor would clobber whatever was already there. Skip the
            // cursor for this step — the next char overwrites cleanly.
            var isLineBreak = c is '\r' or '\n';

            if (cursor != TypewriterCursor.None && i < text.Length - 1 && !isLineBreak)
            {
                Console.Out.Write(GlyphFor(cursor));
                prevCursor = true;
            }
        }

        // Always end without a trailing cursor.
        if (prevCursor)
        {
            // Erase the cursor glyph: move back, write space, move back.
            Console.Out.Write(AnsiCodes.CursorLeft1);
            Console.Out.Write(' ');
            Console.Out.Write(AnsiCodes.CursorLeft1);
        }
        if (colorActive) Console.Out.Write(AnsiCodes.Reset);
        if (hidCursor) Console.Out.Write(AnsiCodes.ShowCursor);
    }

    /// <summary>
    /// <see cref="Type"/> followed by a newline.
    /// </summary>
    public static void TypeLine(
        string text,
        int msPerChar = 30,
        Color? fg = null,
        TypewriterCursor cursor = TypewriterCursor.None,
        TypewriterFade fade = TypewriterFade.None,
        (Color from, Color to)? gradient = null)
    {
        Type(text, msPerChar, fg, cursor, fade, gradient);
        Console.Out.WriteLine();
    }

    private static void DoAlphaFade(char c, Color target, int totalMs)
    {
        const int frames = 4;
        var per = totalMs / frames;
        if (per < 1) per = 1;

        for (var i = 0; i < frames; i++)
        {
            if (i > 0) Console.Out.Write(AnsiCodes.CursorLeft1);

            // Brightness ramp: 0.25, 0.5, 0.75, 1.0. Final frame lands on
            // the real target color so any later styling reads correctly.
            var t = (i + 1) / (double)frames;
            var dim = Color.Rgb(
                (byte)(target.R * t),
                (byte)(target.G * t),
                (byte)(target.B * t));
            ApplyColor(dim);
            Console.Out.Write(c);
            Sleep(per);
        }
    }

    private static char GlyphFor(TypewriterCursor cursor) => cursor switch
    {
        TypewriterCursor.Block     => Glyphs.CursorBlock,
        TypewriterCursor.Underline => Glyphs.CursorUnderline,
        _                          => ' ',
    };

    private static void ApplyColor(Color c)
        => Console.Out.Write(AnsiCodes.Foreground(c));

    private static void WriteWithColor(string s, Color c)
    {
        if (Crt.ColorEnabled)
        {
            Console.Out.Write(AnsiCodes.Foreground(c));
            Console.Out.Write(s);
            Console.Out.Write(AnsiCodes.Reset);
        }
        else
        {
            Console.Out.Write(s);
        }
    }

    private static void Sleep(int ms)
    {
        if (ms > 0) Thread.Sleep(ms);
    }
}
