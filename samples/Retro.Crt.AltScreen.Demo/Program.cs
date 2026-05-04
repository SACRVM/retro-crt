using Retro.Crt;

// The point of this demo: scribble all over the terminal inside an
// alternate-screen scope, then leave the scope and watch the user's
// shell content reappear untouched. Try running it after `ls` or any
// command with visible output — when the demo exits, that output is
// still right there. No scrollback leak, no cleared screen.

Crt.WriteLine(" Retro.Crt — alt-screen demo");
Crt.WriteLine(" Press Enter to take over the terminal…");
Console.ReadLine();

using (Crt.UseAlternateScreen())
{
    var t = Themes.AmberCrt;
    using var theme = Crt.UseTheme(t);
    Crt.ClrScr();
    Crt.GotoXY(1, 1);

    Banner.Box(
        ["RETRO.CRT", "alternate screen buffer", "press Enter to leave"],
        fg: t.Accent, padding: 1, width: 44, align: BoxAlign.Center);

    Crt.WriteLine();
    Typewriter.TypeLine(" Same terminal, fresh canvas.", msPerChar: 25, fg: t.Foreground);
    Typewriter.TypeLine(" Your shell is paused behind us.", msPerChar: 25, fg: t.Foreground);
    Crt.WriteLine();

    using (Crt.WithStyle(fg: t.Muted))
        Crt.WriteLine(" (one beep before we leave…)");

    Thread.Sleep(900);
    Crt.Bell();

    Console.ReadLine();
}

Crt.WriteLine();
Crt.WriteLine(" Back. Your shell content above is exactly as you left it.");
Crt.WriteLine();
return 0;
