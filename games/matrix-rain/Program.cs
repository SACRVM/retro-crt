using System.Diagnostics;
using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.MatrixRain;

if (!Crt.IsInteractive)
{
    Console.Error.WriteLine("Matrix Rain needs an interactive terminal — stdin/stdout look redirected.");
    return 1;
}

var width  = Crt.WindowWidth;
var height = Crt.WindowHeight;
if (width < 20 || height < 8)
{
    Console.Error.WriteLine($"Terminal too small ({width}x{height}); need at least 20x8.");
    return 1;
}

using var alt    = Crt.UseAlternateScreen();
using var hidden = Crt.UseHiddenCursor();
using var raw    = RawMode.Enter();

Run(width, height);
return 0;

static void Run(int width, int height)
{
    var rain     = new Rain(width, height);
    var bufA     = new ScreenBuffer(width, height);
    var bufB     = new ScreenBuffer(width, height);
    var current  = bufA;
    ScreenBuffer? previous = null;

    var sw       = Stopwatch.StartNew();
    var nextTick = sw.ElapsedMilliseconds + rain.TickMs;
    var dirty    = true;

    while (true)
    {
        if (dirty)
        {
            current.Clear();
            rain.Draw(current);
            ScreenRenderer.Render(previous, current, Crt.Sink);
            Crt.Sink.Flush();
            previous = current;
            current  = ReferenceEquals(current, bufA) ? bufB : bufA;
            dirty    = false;
        }

        var now       = sw.ElapsedMilliseconds;
        var remaining = (int)(nextTick - now);

        if (remaining > 0 && TerminalInput.WaitForEvent(remaining, out var ev))
        {
            if (ev.Kind == InputEventKind.Key)
            {
                // Esc quits — except while help is up, where any key
                // (including Esc) just closes the overlay.
                if (ev.Key.Key == Key.Escape && !rain.IsHelpOpen) return;

                var prevTick = rain.TickMs;
                rain.HandleKey(ev.Key);
                dirty = true;
                if (rain.TickMs != prevTick)
                    nextTick = sw.ElapsedMilliseconds + rain.TickMs;
            }
            continue;
        }

        now = sw.ElapsedMilliseconds;
        if (now >= nextTick)
        {
            rain.Step();
            nextTick = now + rain.TickMs;
            dirty = true;
        }
    }
}
