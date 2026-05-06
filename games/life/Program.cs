using System.Diagnostics;
using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Life;

if (!Crt.IsInteractive)
{
    Console.Error.WriteLine("Life needs an interactive terminal — stdin/stdout look redirected.");
    return 1;
}

var width  = Crt.WindowWidth;
var height = Crt.WindowHeight;
if (width < 40 || height < 16)
{
    Console.Error.WriteLine($"Terminal too small ({width}x{height}); need at least 40x16.");
    return 1;
}

using var alt    = Crt.UseAlternateScreen();
using var hidden = Crt.UseHiddenCursor();
using var raw    = RawMode.Enter();

Run(width, height);
return 0;

static void Run(int width, int height)
{
    var game     = new Game(width, height);
    var bufA     = new ScreenBuffer(width, height);
    var bufB     = new ScreenBuffer(width, height);
    var current  = bufA;
    ScreenBuffer? previous = null;

    var sw       = Stopwatch.StartNew();
    var nextTick = sw.ElapsedMilliseconds + game.TickMs;
    var dirty    = true;

    while (true)
    {
        if (dirty)
        {
            current.Clear();
            game.Draw(current);
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
                if (ev.Key.Key == Key.Escape) return;

                var prevStarted = game.IsStarted;
                var prevTick    = game.TickMs;

                game.HandleKey(ev.Key);

                // Glyph keys can trigger anything from a pause toggle
                // to a pattern reseed to a speed change — easier to
                // just redraw on every accepted key than to enumerate
                // every observable field. Cost is one frame per
                // keypress, well below the input rate.
                dirty = true;

                if (game.TickMs != prevTick || (game.IsStarted && !prevStarted))
                    nextTick = sw.ElapsedMilliseconds + game.TickMs;
            }
            continue;
        }

        now = sw.ElapsedMilliseconds;
        if (now >= nextTick)
        {
            game.Step();
            nextTick = now + game.TickMs;
            dirty = true;
        }
    }
}
