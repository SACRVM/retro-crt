using Retro.Crt;

// Renders the same scene in whatever color depth the host can support,
// so a viewer recording the cast under FORCE_COLOR=3 / =2 / =1 / NO_COLOR
// gets four files that show — side by side — exactly how Retro.Crt
// quantizes truecolor down to 256, then to BIOS-16, and finally to
// no-color. The detection summary is printed first so the cast itself
// is self-labelling.

PrintCapabilities();
Crt.WriteLine();

PrintGradientBar();
Crt.WriteLine();

PrintAmberCrtScene();
Crt.WriteLine();

PrintProgressDemo();
Crt.WriteLine();

PrintLogLines();
Crt.WriteLine();

Crt.ResetColor();
return 0;


static void PrintCapabilities()
{
    var report = Diagnostics.Capture();

    Banner.Box(
        ["Retro.Crt — capability fallback", report.ToString()],
        fg: Color.LightCyan,
        width: 78);
}


static void PrintGradientBar()
{
    // 60-step truecolor gradient. Under truecolor: smooth orange→pink.
    // Under 256: visible 6×6×6-cube banding. Under Standard16: collapses
    // to two or three BIOS anchors. Under None: just plain "▓" with no
    // color — the depth is missing, the structure stays.
    using (Crt.WithStyle(fg: Color.LightCyan))
        Crt.WriteLine(" gradient: 60 steps from orange to pink");
    Crt.WriteLine();

    const int steps = 60;
    var from = Color.Rgb(255, 140,   0);
    var to   = Color.Rgb(255,  20, 200);

    Crt.Write(" ");
    for (var i = 0; i < steps; i++)
    {
        var t = (double)i / (steps - 1);
        var c = Color.Rgb(
            Lerp(from.R, to.R, t),
            Lerp(from.G, to.G, t),
            Lerp(from.B, to.B, t));
        using (Crt.WithStyle(fg: c))
            Crt.Write("█");
    }
    Crt.WriteLine();
}


static void PrintAmberCrtScene()
{
    // The Amber CRT preset is the most theme-bound moment — its
    // accent (255, 220, 80) is a different cube entry than its
    // foreground (255, 176, 0). On 16-color the two collapse to the
    // same BIOS anchor and the visual hierarchy disappears: a clear
    // "this is where the depth matters" beat.
    using (Crt.UseTheme(Themes.AmberCrt))
    {
        Banner.Box(
            ["AMBER CRT", "phosphor on warm black"],
            width: 50,
            align: BoxAlign.Center);

        Crt.WriteLine();
        Crt.WriteLine(" > system online.");
        Crt.WriteLine(" > loading kernel modules...");
        Crt.WriteLine();

        Crt.Write(" status: ");
        using (Crt.WithStyle(fg: Themes.AmberCrt.Success, bold: true))
            Crt.Write("OK");
        Crt.Write("    warn: ");
        using (Crt.WithStyle(fg: Themes.AmberCrt.Warn, bold: true))
            Crt.Write("WARN");
        Crt.Write("    error: ");
        using (Crt.WithStyle(fg: Themes.AmberCrt.Error, bold: true))
            Crt.WriteLine("FAIL");
    }
}


static void PrintProgressDemo()
{
    // Animation gating now keys off Crt.IsInteractive, not ColorEnabled
    // — so under NO_COLOR=1 in a real terminal the bar still redraws in
    // place, just without the SGR fill color. Under pipe redirection
    // the bar collapses to a single final frame.
    using (Crt.UseTheme(Themes.AmberCrt))
    {
        using var bar = ProgressBar.Start(
            total: 100,
            width: 40,
            label: " loading");

        for (var i = 0; i <= 100; i += 4)
        {
            bar.Set(i);
            Thread.Sleep(20);
        }
    }
}


static void PrintLogLines()
{
    // Each level routes to its theme slot; under None the colors fall
    // away entirely but the level tags survive so the log stays
    // grep-friendly.
    using (Crt.UseTheme(Themes.AmberCrt))
    {
        Log.Info("kernel loaded — 4ms");
        Log.Success("checksum verified");
        Log.Warn("disk usage at 84%");
        Log.Error("failed to bind port 8080");
    }
}


static byte Lerp(byte a, byte b, double t)
{
    var v = a + (b - a) * t;
    return (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}
