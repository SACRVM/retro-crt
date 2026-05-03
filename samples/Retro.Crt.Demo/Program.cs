using Retro.Crt;

// A quick tour of what Retro.Crt can do. Tuned to record well as an
// asciinema cast (~20 s end-to-end) while still reading fine live —
// short Pause() calls between sections give the viewer time to absorb
// each block without making the live run feel slow.

Crt.ResetColor();
Crt.WriteLine();

PrintGradientBanner();
Pause();

PrintBoxBanner();
Pause();

PrintStandardPalette();
Pause();

PrintTruecolorGradient();
Pause();

PrintStatusLine();
Pause();

PrintLogDemo();
Pause();

PrintTableDemo();
Pause();

PrintSpinnerDemo();
Pause();

PrintTypewriterDemo();
Pause();

PrintProgressBarDemo();

Crt.WriteLine();
Crt.ResetColor();
return 0;

static void Pause() => Thread.Sleep(550);

static void PrintGradientBanner()
{
    string[] lines =
    [
        "  ____      _              ",
        " |  _ \\ ___| |_ _ __ ___  ",
        " | |_) / _ \\ __| '__/ _ \\ ",
        " |  _ <  __/ |_| | | (_) |",
        " |_| \\_\\___|\\__|_|  \\___/ ",
        "                            ",
        " Pascal vibes. Modern .NET. ",
    ];

    Banner.Gradient(lines, Color.Rgb(80, 220, 255), Color.Rgb(255, 120, 175));
}

static void PrintBoxBanner()
{
    Banner.Box(
        ["Retro.Crt", "tiny  ·  zero-dep  ·  AOT-clean"],
        fg: Color.LightCyan);
}

static void PrintStandardPalette()
{
    Color[] palette =
    [
        Color.Black, Color.DarkBlue, Color.DarkGreen, Color.DarkCyan,
        Color.DarkRed, Color.DarkMagenta, Color.Brown, Color.LightGray,
        Color.DarkGray, Color.LightBlue, Color.LightGreen, Color.LightCyan,
        Color.LightRed, Color.LightMagenta, Color.Yellow, Color.White,
    ];
    string[] names =
    [
        "Black ", "DarkBlu", "DarkGrn", "DarkCyn",
        "DarkRed", "DarkMag", "Brown  ", "LightGr",
        "DarkGry", "LtBlue ", "LtGreen", "LtCyan ",
        "LtRed  ", "LtMagen", "Yellow ", "White  ",
    ];

    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Standard16 palette:");

    for (var i = 0; i < palette.Length; i++)
    {
        using (Crt.WithStyle(fg: palette[i]))
            Crt.Write($" {names[i]} ");
        if ((i + 1) % 8 == 0) Crt.WriteLine();
    }
}

static void PrintTruecolorGradient()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Truecolor:");

    const int width = 50;
    for (var x = 0; x < width; x++)
    {
        var t = (double)x / (width - 1);
        var bg = Color.Rgb(
            (byte)(255 * t),
            (byte)(80  + 100 * Math.Sin(t * Math.PI)),
            (byte)(255 * (1 - t)));
        using (Crt.WithStyle(bg: bg))
            Crt.Write(" ");
    }
    Crt.WriteLine();
}

static void PrintStatusLine()
{
    Crt.Write(" status: ");
    using (Crt.WithStyle(fg: Color.LightGreen, bold: true))
        Crt.Write("OK");
    Crt.Write("    warn: ");
    using (Crt.WithStyle(fg: Color.Yellow, bold: true))
        Crt.Write("WARN");
    Crt.Write("    error: ");
    using (Crt.WithStyle(fg: Color.LightRed, bold: true))
        Crt.WriteLine("FAIL");
}

static void PrintLogDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Log:");

    Log.Debug("loading config from /etc/retro");
    Log.Info("system online");
    Log.Success("checksum verified");
    Log.Warn("disk usage at 84%");
    Log.Error("failed to bind port 8080");
}

static void PrintTableDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Table:");

    Table.Print(
        ["Demo",   "Time", "Vibe"],
        [
            ["tour",   "24s", "feature tour"],
            ["themes", "16s", "all 6 themes"],
            ["matrix", "25s", "wake up, neo"],
            ["boot",   "22s", "AMIBIOS POST"],
        ],
        headerColor: Color.LightCyan,
        borderColor: Color.DarkGray);
}

static void PrintSpinnerDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Spinner:");

    // Pipe (ASCII) is the safe default — every font has it, and it suits
    // the BIOS/Pascal vibe. Braille / Block / Arc need a font with the
    // matching unicode range (e.g. Cascadia Code, JetBrains Mono).
    using (var s = Spinner.Show(" connecting...", SpinnerStyle.Pipe, color: Color.LightCyan, msPerFrame: 80))
    {
        Thread.Sleep(900);
        s.Update(" handshake...");
        Thread.Sleep(900);
        s.Stop(" connected.", finalColor: Color.LightGreen);
    }
}

static void PrintTypewriterDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Typewriter:");

    // Underline cursor (`_`) is universal; Block (`▌`) needs a font with
    // the U+258C glyph (Cascadia Code, JetBrains Mono). MatrixBlock
    // (`█`) needs U+2588.
    Typewriter.TypeLine(
        " plain typing...",
        msPerChar: 22,
        fg: Color.LightCyan);

    Typewriter.TypeLine(
        " with a fake cursor...",
        msPerChar: 26,
        fg: Color.LightGreen,
        cursor: TypewriterCursor.Underline);

    // Matrix-style: chunky block cursor blinks for a beat, then types
    // a phrase, blinks again at the end. Pure Wachowski.
    Crt.Write(" ");
    Typewriter.Blink(800, TypewriterCursor.MatrixBlock, fg: Color.LightGreen);
    Typewriter.Type(
        "wake up, neo...",
        msPerChar: 70,
        fg: Color.LightGreen,
        cursor: TypewriterCursor.MatrixBlock);
    Typewriter.Blink(900, TypewriterCursor.MatrixBlock, fg: Color.LightGreen);
    Crt.WriteLine();

    Typewriter.TypeLine(
        " alpha fade-in (truecolor)...",
        msPerChar: 80,
        fg: Color.Rgb(255, 120, 200),
        fade: TypewriterFade.Alpha);

    Typewriter.TypeLine(
        " gradient + cursor + alpha fade",
        msPerChar: 55,
        cursor: TypewriterCursor.Underline,
        fade: TypewriterFade.Alpha,
        gradient: (Color.Rgb(80, 220, 255), Color.Rgb(255, 120, 175)));
}

static void PrintProgressBarDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Fake download:");

    const long total = 4_500_000;
    using var bar = ProgressBar.Start(total, width: 30, label: " download", color: Color.LightCyan);

    const int steps = 40;
    for (var step = 0; step <= steps; step++)
    {
        var done = (long)(total * ((double)step / steps));
        bar.Set(done);
        Thread.Sleep(75);
    }
}
