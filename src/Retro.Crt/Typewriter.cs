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

        var ansi = ResolveAnsi(ref cursor, ref fade);

        if (msPerChar <= 0)
        {
            DumpInstantly(text, fg, gradient);
            return;
        }

        var hasGradient = HasGradient(gradient);
        var hidCursor = false;
        if (ansi)
        {
            Console.Out.Write(AnsiCodes.HideCursor);
            hidCursor = true;
        }

        var colorActive = false;
        var prevCursor = false;

        try
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (prevCursor)
                {
                    Console.Out.Write(AnsiCodes.CursorLeft1);
                    prevCursor = false;
                }

                var c = text[i];
                var color = ResolveColor(i, text.Length, hasGradient, gradient, fg);
                EmitChar(c, color, ansi, fade, ref colorActive, msPerChar);

                if (ShouldEmitCursor(c, cursor, i, text.Length))
                {
                    Console.Out.Write(GlyphFor(cursor));
                    prevCursor = true;
                }

                // Sleep AFTER the cursor emit so the cursor is visible
                // during the dwell. Alpha fade already consumed
                // msPerChar internally — skip the extra sleep.
                if (!ConsumesOwnTime(c, fade, ansi, color))
                    Sleep(msPerChar);
            }
        }
        finally
        {
            FinishReveal(prevCursor, colorActive, hidCursor);
        }
    }

    /// <summary>
    /// Async variant of <see cref="Type"/>. Pauses with
    /// <see cref="Task.Delay(int, CancellationToken)"/>, so cancellation
    /// aborts the reveal immediately and the <c>finally</c> block restores
    /// the cursor and color before propagating
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    public static async Task TypeAsync(
        string text,
        int msPerChar = 30,
        Color? fg = null,
        TypewriterCursor cursor = TypewriterCursor.None,
        TypewriterFade fade = TypewriterFade.None,
        (Color from, Color to)? gradient = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return;

        var ansi = ResolveAnsi(ref cursor, ref fade);

        if (msPerChar <= 0)
        {
            DumpInstantly(text, fg, gradient);
            return;
        }

        var hasGradient = HasGradient(gradient);
        var hidCursor = false;
        if (ansi)
        {
            Console.Out.Write(AnsiCodes.HideCursor);
            hidCursor = true;
        }

        var colorActive = false;
        var prevCursor = false;

        try
        {
            for (var i = 0; i < text.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (prevCursor)
                {
                    Console.Out.Write(AnsiCodes.CursorLeft1);
                    prevCursor = false;
                }

                var c = text[i];
                var color = ResolveColor(i, text.Length, hasGradient, gradient, fg);
                await EmitCharAsync(c, color, ansi, fade, msPerChar, cancellationToken).ConfigureAwait(false);
                if (color is not null && ansi) colorActive = true;

                if (ShouldEmitCursor(c, cursor, i, text.Length))
                {
                    Console.Out.Write(GlyphFor(cursor));
                    prevCursor = true;
                }

                if (!ConsumesOwnTime(c, fade, ansi, color))
                    await DelayAsync(msPerChar, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            FinishReveal(prevCursor, colorActive, hidCursor);
        }
    }

    /// <summary><see cref="Type"/> followed by a newline.</summary>
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

    /// <summary>
    /// Sit at the current cursor position and blink a fake cursor for
    /// <paramref name="totalMs"/> milliseconds — useful for the Matrix-
    /// style "pause before the next phrase" beats. The glyph is erased
    /// before this method returns, so the next write starts on a clean
    /// cell. Without ANSI support the call is a plain
    /// <see cref="Thread.Sleep(int)"/>.
    /// </summary>
    /// <param name="totalMs">How long to blink, in total. Values ≤0 return immediately.</param>
    /// <param name="cursor">Glyph to blink. <see cref="TypewriterCursor.None"/> just sleeps.</param>
    /// <param name="fg">Optional color for the cursor.</param>
    /// <param name="blinkRateMs">Half-period of the blink — time the cursor stays in each on/off state.</param>
    public static void Blink(
        int totalMs,
        TypewriterCursor cursor = TypewriterCursor.MatrixBlock,
        Color? fg = null,
        int blinkRateMs = 500)
    {
        if (totalMs <= 0) return;

        var ansi = Crt.ColorEnabled;
        if (!ansi || cursor == TypewriterCursor.None)
        {
            Sleep(totalMs);
            return;
        }

        if (blinkRateMs < 1) blinkRateMs = 1;
        var glyph = GlyphFor(cursor);

        Console.Out.Write(AnsiCodes.HideCursor);
        var colorActive = false;
        try
        {
            var elapsed = 0;
            var on = true;
            while (elapsed < totalMs)
            {
                if (fg is { } c) { ApplyColor(c); colorActive = true; }
                Console.Out.Write(on ? glyph : ' ');
                Console.Out.Write(AnsiCodes.CursorLeft1);

                var step = Math.Min(blinkRateMs, totalMs - elapsed);
                Sleep(step);
                elapsed += step;
                on = !on;
            }

            // Land on a clean cell so the next write starts fresh.
            Console.Out.Write(' ');
            Console.Out.Write(AnsiCodes.CursorLeft1);
        }
        finally
        {
            if (colorActive) Console.Out.Write(AnsiCodes.Reset);
            Console.Out.Write(AnsiCodes.ShowCursor);
        }
    }

    /// <summary>Async variant of <see cref="Blink"/> with cancellation support.</summary>
    public static async Task BlinkAsync(
        int totalMs,
        TypewriterCursor cursor = TypewriterCursor.MatrixBlock,
        Color? fg = null,
        int blinkRateMs = 500,
        CancellationToken cancellationToken = default)
    {
        if (totalMs <= 0) return;

        var ansi = Crt.ColorEnabled;
        if (!ansi || cursor == TypewriterCursor.None)
        {
            await DelayAsync(totalMs, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (blinkRateMs < 1) blinkRateMs = 1;
        var glyph = GlyphFor(cursor);

        Console.Out.Write(AnsiCodes.HideCursor);
        var colorActive = false;
        try
        {
            var elapsed = 0;
            var on = true;
            while (elapsed < totalMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fg is { } c) { ApplyColor(c); colorActive = true; }
                Console.Out.Write(on ? glyph : ' ');
                Console.Out.Write(AnsiCodes.CursorLeft1);

                var step = Math.Min(blinkRateMs, totalMs - elapsed);
                await DelayAsync(step, cancellationToken).ConfigureAwait(false);
                elapsed += step;
                on = !on;
            }

            Console.Out.Write(' ');
            Console.Out.Write(AnsiCodes.CursorLeft1);
        }
        finally
        {
            if (colorActive) Console.Out.Write(AnsiCodes.Reset);
            Console.Out.Write(AnsiCodes.ShowCursor);
        }
    }

    /// <summary><see cref="TypeAsync"/> followed by a newline.</summary>
    public static async Task TypeLineAsync(
        string text,
        int msPerChar = 30,
        Color? fg = null,
        TypewriterCursor cursor = TypewriterCursor.None,
        TypewriterFade fade = TypewriterFade.None,
        (Color from, Color to)? gradient = null,
        CancellationToken cancellationToken = default)
    {
        await TypeAsync(text, msPerChar, fg, cursor, fade, gradient, cancellationToken).ConfigureAwait(false);
        Console.Out.WriteLine();
    }

    // ─── shared helpers ──────────────────────────────────────────────────

    private static bool ResolveAnsi(ref TypewriterCursor cursor, ref TypewriterFade fade)
    {
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
        return ansi;
    }

    private static bool HasGradient((Color from, Color to)? gradient)
        => gradient is { } g
            && g.from.Mode == ColorMode.Truecolor
            && g.to.Mode == ColorMode.Truecolor;

    private static Color? ResolveColor(int i, int n, bool hasGradient, (Color from, Color to)? gradient, Color? fg)
        => hasGradient
            ? Banner.Interpolate(gradient!.Value.from, gradient.Value.to, i, n)
            : fg;

    private static bool ShouldEmitCursor(char c, TypewriterCursor cursor, int i, int n)
    {
        // After CR/LF cursor-left tracking points at the new line, so a
        // fake cursor would clobber whatever was already there. Skip the
        // cursor for this step — the next char overwrites cleanly.
        if (c is '\r' or '\n') return false;
        return cursor != TypewriterCursor.None && i < n - 1;
    }

    private static void DumpInstantly(string text, Color? fg, (Color from, Color to)? gradient)
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
    }

    // EmitChar writes the glyph (with alpha fade if applicable). The
    // outer loop is responsible for the post-char sleep so that a fake
    // cursor written after the char is actually visible during the
    // dwell, not flashed-and-overwritten between iterations. Alpha
    // fade does its own internal pacing and consumes the per-char
    // budget itself — see <see cref="ConsumesOwnTime"/>.
    private static void EmitChar(char c, Color? color, bool ansi, TypewriterFade fade,
                                 ref bool colorActive, int msPerChar)
    {
        if (c is ' ' or '\t' or '\r' or '\n' || char.IsControl(c))
        {
            if (ansi && color is { } w) { ApplyColor(w); colorActive = true; }
            Console.Out.Write(c);
            return;
        }

        if (CanAlpha(fade, ansi, color))
        {
            DoAlphaFade(c, color!.Value, msPerChar);
            colorActive = true;
            return;
        }

        if (ansi && color is { } n) { ApplyColor(n); colorActive = true; }
        Console.Out.Write(c);
    }

    private static async Task EmitCharAsync(char c, Color? color, bool ansi, TypewriterFade fade,
                                            int msPerChar, CancellationToken ct)
    {
        if (c is ' ' or '\t' or '\r' or '\n' || char.IsControl(c))
        {
            if (ansi && color is { } w) ApplyColor(w);
            Console.Out.Write(c);
            return;
        }

        if (CanAlpha(fade, ansi, color))
        {
            await DoAlphaFadeAsync(c, color!.Value, msPerChar, ct).ConfigureAwait(false);
            return;
        }

        if (ansi && color is { } n) ApplyColor(n);
        Console.Out.Write(c);
    }

    /// <summary>
    /// True when the EmitChar path internally consumes <c>msPerChar</c>
    /// (e.g. alpha fade renders four sub-frames). The outer loop must
    /// not sleep again, otherwise the per-char time doubles.
    /// </summary>
    private static bool ConsumesOwnTime(char c, TypewriterFade fade, bool ansi, Color? color)
    {
        if (c is ' ' or '\t' or '\r' or '\n' || char.IsControl(c)) return false;
        return CanAlpha(fade, ansi, color);
    }

    private static bool CanAlpha(TypewriterFade fade, bool ansi, Color? color)
        => fade == TypewriterFade.Alpha
            && ansi
            && color is { Mode: ColorMode.Truecolor };

    private static void DoAlphaFade(char c, Color target, int totalMs)
    {
        const int frames = 4;
        var per = totalMs / frames;
        if (per < 1) per = 1;

        for (var i = 0; i < frames; i++)
        {
            if (i > 0) Console.Out.Write(AnsiCodes.CursorLeft1);
            ApplyColor(DimColor(target, i, frames));
            Console.Out.Write(c);
            Sleep(per);
        }
    }

    private static async Task DoAlphaFadeAsync(char c, Color target, int totalMs, CancellationToken ct)
    {
        const int frames = 4;
        var per = totalMs / frames;
        if (per < 1) per = 1;

        for (var i = 0; i < frames; i++)
        {
            if (i > 0) Console.Out.Write(AnsiCodes.CursorLeft1);
            ApplyColor(DimColor(target, i, frames));
            Console.Out.Write(c);
            await DelayAsync(per, ct).ConfigureAwait(false);
        }
    }

    private static Color DimColor(Color target, int frameIndex, int frames)
    {
        // Brightness ramp: 0.25, 0.5, 0.75, 1.0. Final frame lands on the
        // real target color so any later styling reads correctly.
        var t = (frameIndex + 1) / (double)frames;
        return Color.Rgb(
            (byte)(target.R * t),
            (byte)(target.G * t),
            (byte)(target.B * t));
    }

    private static void FinishReveal(bool prevCursor, bool colorActive, bool hidCursor)
    {
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

    private static char GlyphFor(TypewriterCursor cursor) => cursor switch
    {
        TypewriterCursor.Block       => Glyphs.CursorBlock,
        TypewriterCursor.Underline   => Glyphs.CursorUnderline,
        TypewriterCursor.MatrixBlock => Glyphs.CursorMatrix,
        _                            => ' ',
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

    private static Task DelayAsync(int ms, CancellationToken ct)
        => ms > 0 ? Task.Delay(ms, ct) : Task.CompletedTask;
}
