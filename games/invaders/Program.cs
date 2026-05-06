using System.Diagnostics;
using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Invaders;

if (!Crt.IsInteractive)
{
    Console.Error.WriteLine("Invaders needs an interactive terminal — stdin/stdout look redirected.");
    return 1;
}

var width  = Crt.WindowWidth;
var height = Crt.WindowHeight;
if (width < 50 || height < 22)
{
    Console.Error.WriteLine($"Terminal too small ({width}x{height}); need at least 50x22.");
    return 1;
}

using var alt    = Crt.UseAlternateScreen();
using var hidden = Crt.UseHiddenCursor();
using var raw    = RawMode.Enter();

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
                if (ev.Key.Key == Key.Escape && !game.IsHelpOpen) return GameOverChoice.Quit;

                game.HandleKey(ev.Key);
                dirty = true;
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

internal enum GameOverChoice { Restart, Quit }
