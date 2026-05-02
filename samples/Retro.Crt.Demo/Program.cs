using Retro.Crt;

// A quick tour of what Retro.Crt can do today. Run with `dotnet run` from
// samples/Retro.Crt.Demo and you should see colored output, a banner, a tiny
// progress bar, log output, and a typewriter intro.

Crt.ResetColor();
Crt.WriteLine();

PrintGradientBanner();

Crt.WriteLine();
PrintBoxBanner();

Crt.WriteLine();
PrintStandardPalette();

Crt.WriteLine();
PrintTruecolorGradient();

Crt.WriteLine();
PrintWithStyleDemo();

Crt.WriteLine();
PrintLogDemo();

Crt.WriteLine();
PrintTypewriterDemo();

Crt.WriteLine();
PrintProgressBarDemo();

Crt.WriteLine();
Crt.ResetColor();
return 0;

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
        ["Retro.Crt 0.2", "Stage 2: Banner / Bar / Log / Typewriter"],
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
        Crt.WriteLine("Standard16 palette (terminal-themed):");

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
        Crt.WriteLine("Truecolor gradient:");

    const int width = 60;
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

static void PrintWithStyleDemo()
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
        Crt.WriteLine("Log levels:");

    Log.Debug("loading config from /etc/retro");
    Log.Info("system online");
    Log.Success("checksum verified");
    Log.Warn("disk usage at 84%");
    Log.Error("failed to bind port 8080");
}

static void PrintTypewriterDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Typewriter:");

    Typewriter.TypeLine(
        " plain typing...",
        msPerChar: 25,
        fg: Color.LightCyan);

    Typewriter.TypeLine(
        " with a fake cursor...",
        msPerChar: 30,
        fg: Color.LightGreen,
        cursor: TypewriterCursor.Block);

    Typewriter.TypeLine(
        " alpha fade-in (truecolor)...",
        msPerChar: 50,
        fg: Color.Rgb(255, 120, 200),
        fade: TypewriterFade.Alpha);

    Typewriter.TypeLine(
        " gradient + cursor + alpha fade",
        msPerChar: 40,
        cursor: TypewriterCursor.Block,
        fade: TypewriterFade.Alpha,
        gradient: (Color.Rgb(80, 220, 255), Color.Rgb(255, 120, 175)));
}

static void PrintProgressBarDemo()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Fake download:");

    const long total = 4_500_000;
    using var bar = ProgressBar.Start(total, width: 30, label: " download", color: Color.LightCyan);

    const int steps = 60;
    for (var step = 0; step <= steps; step++)
    {
        var done = (long)(total * ((double)step / steps));
        bar.Set(done);
        Thread.Sleep(120);
    }
}
