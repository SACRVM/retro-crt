using System.Diagnostics;
using Retro.Crt;
using Retro.Crt.Commander;
using Retro.Crt.Input;

if (!Crt.IsInteractive)
{
    Console.Error.WriteLine("Commander needs an interactive terminal — stdin/stdout look redirected.");
    return 1;
}

var width  = Crt.WindowWidth;
var height = Crt.WindowHeight;
if (width < 60 || height < 18)
{
    Console.Error.WriteLine($"Terminal too small ({width}x{height}); need at least 60x18.");
    return 1;
}

// Build the fake demo workspace under TEMP. Both panes start in the
// generated "left" / "right" subdirs; the workspace path acts as the
// pane-navigation root guard so destructive ops can never escape into
// the user's real filesystem.
var (workspaceRoot, leftDir, rightDir) = Workspace.Create();

using var alt    = Crt.UseAlternateScreen();
using var hidden = Crt.UseHiddenCursor();
using var raw    = RawMode.Enter();

var bufA    = new ScreenBuffer(width, height);
var bufB    = new ScreenBuffer(width, height);
var current = bufA;
ScreenBuffer? previous = null;

App? app = null;

void Render()
{
    if (app is null) return;
    current.Clear();
    app.Draw(current);
    ScreenRenderer.Render(previous, current, Crt.Sink);
    Crt.Sink.Flush();
    previous = current;
    current  = ReferenceEquals(current, bufA) ? bufB : bufA;
}

app = new App(width, height, leftDir, rightDir, workspaceRoot, Render);

try
{
    var sw       = Stopwatch.StartNew();
    const int TickMs = 50;
    var nextTick = sw.ElapsedMilliseconds + TickMs;
    var dirty    = true;

    while (true)
    {
        if (dirty)
        {
            Render();
            dirty = false;
        }

        var now       = sw.ElapsedMilliseconds;
        var remaining = (int)(nextTick - now);

        if (remaining > 0 && TerminalInput.WaitForEvent(remaining, out var ev))
        {
            if (ev.Kind == InputEventKind.Key)
            {
                if (app.HandleKey(ev.Key)) return 0;
                dirty = true;
            }
            continue;
        }

        now = sw.ElapsedMilliseconds;
        if (now >= nextTick)
        {
            nextTick = now + TickMs;
            if (app.Tick()) dirty = true;
        }
    }
}
finally
{
    Workspace.Cleanup(workspaceRoot);
}
