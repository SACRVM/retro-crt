using Retro.Crt;

// Walks through every built-in theme so the viewer sees the difference
// at a glance. Each theme renders the same five-element scene
// (banner, body line, status row, prompt teaser) in its own palette,
// inside its own UseTheme scope so the theme's background actually
// shows behind the text. Tuned to ~24 s end-to-end so all nine themes
// fit comfortably inside an asciinema cast budget.

Crt.ResetColor();
Crt.WriteLine();

foreach (var theme in Themes.All)
{
    PrintTheme(theme);
    Thread.Sleep(1500);
}

Crt.ResetColor();
return 0;

static void PrintTheme(Theme t)
{
    // Subtle separator row in the theme's muted slot — gives the cast
    // a clean cut between scenes without resorting to ClrScr.
    using (Crt.WithStyle(fg: t.Muted))
        Crt.WriteLine(new string('─', 50));
    Crt.WriteLine();

    using var theme = Crt.UseTheme(t);

    // Pad each line to a fixed width so the theme background renders as
    // a visible band — without padding only the typed cells carry bg.
    const int W = 50;

    Banner.Box(
        [t.Name.ToUpperInvariant(), "retro.crt theme"],
        fg: t.Accent, width: W, align: BoxAlign.Center);

    WritePadded(" > system online.", W, t.Foreground);

    var status = $" status: OK    warn: WARN    error: FAIL";
    Crt.Write(" status: ");
    using (Crt.WithStyle(fg: t.Success, bold: true)) Crt.Write("OK");
    Crt.Write("    warn: ");
    using (Crt.WithStyle(fg: t.Warn,    bold: true)) Crt.Write("WARN");
    Crt.Write("    error: ");
    using (Crt.WithStyle(fg: t.Error,   bold: true)) Crt.Write("FAIL");
    PadRest(status.Length, W);
    Crt.WriteLine();

    WritePadded($" -- theme: {t.Name}", W, t.Muted);

    Crt.WriteLine();
}

static void WritePadded(string text, int width, Color fg)
{
    using (Crt.WithStyle(fg: fg))
    {
        Crt.Write(text);
        if (text.Length < width) Crt.Write(new string(' ', width - text.Length));
        Crt.WriteLine();
    }
}

static void PadRest(int written, int width)
{
    if (written < width) Crt.Write(new string(' ', width - written));
}
