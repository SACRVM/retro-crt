using System.Diagnostics;
using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Snake;

if (!Crt.IsInteractive)
{
    Console.Error.WriteLine("Snake needs an interactive terminal — stdin/stdout look redirected.");
    return 1;
}

var width  = Crt.WindowWidth;
var height = Crt.WindowHeight;
if (width < 30 || height < 12)
{
    Console.Error.WriteLine($"Terminal too small ({width}x{height}); need at least 30x12.");
    return 1;
}

using var alt    = Crt.UseAlternateScreen();
using var hidden = Crt.UseHiddenCursor();
using var raw    = RawMode.Enter();

// Each Run() returns when the player either quits or asks for a
// restart on the game-over screen. Loop the whole thing so restarts
// hand back a fresh Game without re-entering raw mode / alt screen.
while (Run(width, height) == GameOverChoice.Restart) { }
return 0;

static GameOverChoice Run(int width, int height)
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
        var now       = sw.ElapsedMilliseconds;
        var remaining = (int)(nextTick - now);

        if (remaining > 0 && TerminalInput.WaitForEvent(remaining, out var ev))
        {
            if (ev.Kind == InputEventKind.Key)
            {
                if (ev.Key.Key == Key.Escape) return GameOverChoice.Quit;

                if (!game.IsAlive)
                {
                    if (ev.Key.Key == Key.Glyph && (ev.Key.Glyph is 'r' or 'R'))
                        return GameOverChoice.Restart;
                    continue;
                }

                var wasPaused = game.IsPaused;
                game.HandleKey(ev.Key);
                if (wasPaused != game.IsPaused) dirty = true;
            }
            continue;
        }

        // Tick boundary: advance the simulation.
        now = sw.ElapsedMilliseconds;
        if (now >= nextTick)
        {
            game.Step();
            nextTick = now + game.TickMs;
            dirty = true;
        }

        if (!dirty) continue;

        current.Clear();
        game.Draw(current);
        ScreenRenderer.Render(previous, current, Crt.Sink);
        Crt.Sink.Flush();
        previous = current;
        current  = ReferenceEquals(current, bufA) ? bufB : bufA;
        dirty    = false;
    }
}

internal enum GameOverChoice { Restart, Quit }
