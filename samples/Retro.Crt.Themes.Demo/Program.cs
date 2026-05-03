using Retro.Crt;

// Walks through every built-in theme so the viewer sees the difference
// at a glance. Each theme renders the same five-element scene
// (banner, body line, status row, prompt teaser) in its own palette,
// so you can compare them side by side. Tuned to ~16 s end-to-end —
// fits comfortably inside an asciinema cast budget.

Crt.ResetColor();
Crt.WriteLine();

foreach (var theme in Themes.All)
{
    PrintTheme(theme);
    Thread.Sleep(1700);
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

    Banner.Box(
        [t.Name.ToUpperInvariant(), "retro.crt theme"],
        fg: t.Accent);

    Crt.WriteLine();

    using (Crt.WithStyle(fg: t.Foreground))
        Crt.WriteLine(" > system online.");

    Crt.Write(" status: ");
    using (Crt.WithStyle(fg: t.Success, bold: true)) Crt.Write("OK");
    Crt.Write("    warn: ");
    using (Crt.WithStyle(fg: t.Warn,    bold: true)) Crt.Write("WARN");
    Crt.Write("    error: ");
    using (Crt.WithStyle(fg: t.Error,   bold: true)) Crt.WriteLine("FAIL");

    using (Crt.WithStyle(fg: t.Muted))
        Crt.Write(" -- ");
    using (Crt.WithStyle(fg: t.Foreground))
        Crt.WriteLine($"theme: {t.Name}");

    Crt.WriteLine();
}
