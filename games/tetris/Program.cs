using System.Diagnostics;
using Retro.Crt;
using Retro.Crt.Input;
using Retro.Crt.Tetris;

if (!Crt.IsInteractive)
{
    Console.Error.WriteLine("Tetris needs an interactive terminal — stdin/stdout look redirected.");
    return 1;
}

var width  = Crt.WindowWidth;
var height = Crt.WindowHeight;
// Min size: field 20 cells wide + sidebar 12 + chrome → ~38 wide;
// 20 field rows + HUD/borders/footer → 24 tall. Round up for breathing
// room.
if (width < 40 || height < 24)
{
    Console.Error.WriteLine($"Terminal too small ({width}x{height}); need at least 40x24.");
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
