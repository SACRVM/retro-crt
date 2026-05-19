using Retro.Crt;

// The point of this demo: pin a fixed two-row status footer to the
// bottom of the terminal while normal Crt.WriteLine output keeps
// scrolling in the region above it. No alt-screen, no raw mode —
// the terminal's native scrollback, selection, and wheel still work.
//
// Try selecting one of the log lines with your mouse while the demo
// is running: native selection is intact. Compare with a Tui
// Application, which takes over the whole screen.

var theme = Themes.AmberCrt;
using var _ = Crt.UseTheme(theme);

Crt.WriteLine();
Crt.WriteLine(" Retro.Crt — sticky-footer demo");
Crt.WriteLine(" Lines scroll above the footer; the footer stays put.");
Crt.WriteLine();

var startedAt = DateTime.UtcNow;
var pushed    = 0;

using var footer = StickyFooter.Start(rows: 2, paint: buf =>
{
    var width  = buf.Width;
    var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;

    // Row 0: app name (left) + uptime (right).
    var left  = $" Retro.Crt {pushed,4} lines";
    var right = $"uptime {elapsed,5:0.0}s ";
    buf.PutString(0, 0, left.AsSpan(),                            theme.Foreground, Color.DarkBlue);
    buf.PutString(width - right.Length, 0, right.AsSpan(),        theme.Foreground, Color.DarkBlue);
    var fillLen = width - left.Length - right.Length;
    if (fillLen > 0)
        buf.FillRect(left.Length, 0, fillLen, 1,
            new Cell(' ', theme.Foreground, Color.DarkBlue));

    // Row 1: hotkey hint, dim.
    var hint = " [Ctrl-C] quit · scroll back with the wheel · select text natively";
    buf.PutString(0, 1, hint.AsSpan(),                            theme.Muted,      Color.Black);
});

// Emit a few rows so there's content to look at; then stream more
// lines on a worker so the footer's uptime visibly ticks while the
// log scrolls past above.
for (var i = 0; i < 5; i++)
{
    Crt.WriteLine($" boot: stage {i + 1}/5 ok");
    Thread.Sleep(180);
}

var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) => { args.Cancel = true; cancel.Cancel(); };

var producer = Task.Run(async () =>
{
    while (!cancel.IsCancellationRequested)
    {
        pushed++;
        Crt.WriteLine($" tick {pushed,4}  ·  {DateTime.Now:HH:mm:ss}");
        footer.Refresh();
        try { await Task.Delay(450, cancel.Token); }
        catch (OperationCanceledException) { break; }
    }
});

await producer;
return 0;
