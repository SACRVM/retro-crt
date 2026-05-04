using Retro.Crt;

// Fake POST / boot sequence in the spirit of an early-90s AMIBIOS.
// Cycles through Banner, Typewriter, Spinner, ProgressBar, Log, and a
// blinking final prompt — the everything-bagel of the library, dressed
// up so it actually looks like a system booting. ~22 s end-to-end.
//
// Recipe for a real fullscreen retro takeover:
//   UseAlternateScreen — preserve the user's shell, restore on exit
//   UseTheme           — set the theme's bg/fg as the active SGR
//   PaintBackground    — actually fill every cell with that bg
// The Dos theme is mostly black on black so the painted bg is subtle;
// switch to AmberCrt / GreenCrt / Midnight to see the full effect.
var t = Themes.Dos;

using var alt = Crt.UseAlternateScreen();
using var theme = Crt.UseTheme(t);
Crt.PaintBackground();
Crt.GotoXY(1, 1);

PrintBiosBanner();
Pause();

PrintMemoryTest();
Pause();

PrintHardwareDetection(t);
Pause();

PrintKernelLoad(t);
Pause();

PrintServiceStartup(t);
Pause();

PrintReady(t);
PrintShellPrompt(t);

// Hold the final frame so the user sees it before the alt-screen
// scope tears down and their shell reappears.
Thread.Sleep(1200);
return 0;

static void Pause() => Thread.Sleep(450);

static void PrintBiosBanner()
{
    Banner.Box(
        ["AMIBIOS (C) 1992", "American Megatrends, Inc.", "BIOS Version 1.04.04"],
        fg: Color.LightCyan);
}

static void PrintMemoryTest()
{
    Typewriter.TypeLine(" Memory Test:  640K Base       OK", msPerChar: 18, fg: Color.LightGray);
    Typewriter.TypeLine(" Extended:    7168K High        OK", msPerChar: 18, fg: Color.LightGray);
}

static void PrintHardwareDetection(Theme t)
{
    Crt.Write(" Detecting CPU....... ");
    Thread.Sleep(700);
    using (Crt.WithStyle(fg: t.Success, bold: true))
        Crt.WriteLine("Intel 80486DX2/66 MHz");

    Crt.Write(" Detecting drives.... ");
    using (var s = Spinner.Show("scanning", SpinnerStyle.Pipe, color: t.Accent))
    {
        Thread.Sleep(1700);
        s.Stop("[ OK ]  C: 504 MB    D: CDROM", finalColor: t.Success);
    }

    Crt.Write(" Detecting network... ");
    using (var s = Spinner.Show("probing", SpinnerStyle.Pipe, color: t.Accent))
    {
        Thread.Sleep(1100);
        s.Stop("[ OK ]  NE2000 @ IRQ 3", finalColor: t.Success);
    }
}

static void PrintKernelLoad(Theme t)
{
    Crt.WriteLine();
    Log.Info("Loading kernel image from C:\\KERNEL.SYS");

    using var bar = ProgressBar.Start(100, width: 30, label: " kernel  ", color: Color.LightCyan);
    for (var i = 0; i <= 100; i += 4)
    {
        bar.Set(i);
        Thread.Sleep(35);
    }
}

static void PrintServiceStartup(Theme t)
{
    Log.Success("Kernel loaded into upper memory.");
    Thread.Sleep(250);
    Log.Info("Mounting filesystem...");
    Thread.Sleep(450);
    Log.Success("Filesystem mounted (FAT16).");
    Thread.Sleep(250);
    Log.Info("Starting services...");
    Thread.Sleep(450);
    Log.Success("retro.crt v0.4 ready.");
}

static void PrintReady(Theme t)
{
    Crt.WriteLine();
    using (Crt.WithStyle(fg: t.Success, bold: true))
        Crt.WriteLine(" SYSTEM READY.");
    Crt.WriteLine();
}

static void PrintShellPrompt(Theme t)
{
    using (Crt.WithStyle(fg: t.Accent, bold: true))
        Crt.Write(" C:\\> ");

    // Cursor sits and blinks as if waiting for the first command.
    Typewriter.Blink(1800, TypewriterCursor.Underline,
        fg: t.Foreground, blinkRateMs: 450);
    Crt.WriteLine();
}
