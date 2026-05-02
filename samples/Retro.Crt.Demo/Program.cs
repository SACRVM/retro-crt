using Retro.Crt;

// A quick tour of what Retro.Crt can do today. Run with `dotnet run` from
// samples/Retro.Crt.Demo and you should see colored output, a banner, and a
// tiny progress bar.

Crt.ResetColor();
Crt.WriteLine();

PrintBanner();

Crt.WriteLine();
PrintStandardPalette();

Crt.WriteLine();
PrintTruecolorGradient();

Crt.WriteLine();
PrintWithStyleDemo();

Crt.WriteLine();
await PrintFakeProgressAsync();

Crt.WriteLine();
Crt.ResetColor();
return 0;

static void PrintBanner()
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

    for (var i = 0; i < lines.Length; i++)
    {
        var t = (double)i / Math.Max(1, lines.Length - 1);
        var color = Color.Rgb(
            (byte)(80  + 175 * t),
            (byte)(220 - 100 * t),
            (byte)(255 - 80  * t));
        using (Crt.WithStyle(fg: color, bold: true))
            Crt.WriteLine(lines[i]);
    }
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

static async Task PrintFakeProgressAsync()
{
    using (Crt.WithStyle(fg: Color.LightGray))
        Crt.WriteLine("Fake download:");

    const int width = 30;
    const long total = 4_500_000;

    for (var step = 0; step <= width; step++)
    {
        var ratio = (double)step / width;
        var done = (long)(total * ratio);
        var bar = new string('█', step) + new string('░', width - step);
        var line = $" [{bar}] {(int)(ratio * 100),3:D}%  {done / 1024.0:F1} / {total / 1024.0:F1} KB";

        Crt.Write("\r");
        using (Crt.WithStyle(fg: Color.LightCyan))
            Crt.Write(line);

        await Task.Delay(40);
    }
    Crt.WriteLine();
}
