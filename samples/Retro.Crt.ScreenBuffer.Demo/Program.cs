using Retro.Crt;

// Bouncing-ball demo on top of ScreenBuffer + ScreenRenderer. Two
// buffers, ping-pong between frames, only changed cells get repainted.
// Move the ball at ~30 fps for 5 seconds, then bow out gracefully.
//
// What this exercises:
//   - PutString for the static frame (border, banner)
//   - Cell indexer for the moving ball
//   - ScreenRenderer.Render for flicker-free incremental update
//   - UseAlternateScreen so the user's shell stays untouched

using var alt = Crt.UseAlternateScreen();
Console.CursorVisible = false;

var w = Math.Max(40, Math.Min(80, Crt.WindowWidth));
var h = Math.Max(15, Math.Min(20, Crt.WindowHeight));

var prev    = new ScreenBuffer(w, h);
var current = new ScreenBuffer(w, h);

var border = new Cell('#', Color.DarkCyan, Color.Black);
var ballGlyph = new Cell('O', Color.Yellow, Color.Black, CellAttrs.Bold);
var bgFill    = new Cell(' ', Color.LightGray, Color.Black);

// Static furniture: borders + banner. Painted into both prev and
// current so the FIRST frame's diff renders only the ball, not the
// chrome — which means we get the chrome for free without flicker.
PaintFurniture(current);
PaintFurniture(prev);

// Force the very first ball-render: leave prev with no ball, current
// with ball. The diff will paint exactly two cells (the ball position).
double bx = 5, by = 5, dx = 0.9, dy = 0.5;

var sw = new StringWriter();
ScreenRenderer.Render(previous: null, current, sw);
Console.Out.Write(sw.ToString());
Console.Out.Flush();

var deadline = DateTime.UtcNow.AddSeconds(5);
while (DateTime.UtcNow < deadline)
{
    // Snapshot prev = current so the next render diffs against the
    // frame we just drew.
    Copy(current, prev);

    // Erase old ball position (overdraw with bg), step physics.
    current[(int)bx, (int)by] = bgFill;

    bx += dx; by += dy;
    if (bx <= 1)     { bx = 1;     dx = -dx; }
    if (bx >= w - 2) { bx = w - 2; dx = -dx; }
    if (by <= 1)     { by = 1;     dy = -dy; }
    if (by >= h - 2) { by = h - 2; dy = -dy; }

    current[(int)bx, (int)by] = ballGlyph;

    sw = new StringWriter();
    ScreenRenderer.Render(prev, current, sw);
    Console.Out.Write(sw.ToString());
    Console.Out.Flush();

    Thread.Sleep(33); // ~30 fps
}

Console.CursorVisible = true;
return 0;

static void PaintFurniture(ScreenBuffer buf)
{
    var border = new Cell('#', Color.DarkCyan, Color.Black);
    var bgFill = new Cell(' ', Color.LightGray, Color.Black);

    buf.Clear(bgFill);
    // Top + bottom rows.
    buf.FillRect(0,           0,           buf.Width, 1, border);
    buf.FillRect(0,           buf.Height-1, buf.Width, 1, border);
    // Left + right columns.
    buf.FillRect(0,           0, 1, buf.Height, border);
    buf.FillRect(buf.Width-1, 0, 1, buf.Height, border);
    // Banner — first row, centered.
    var label = " Retro.Crt — ScreenBuffer demo ";
    var x = (buf.Width - label.Length) / 2;
    if (x > 0)
        buf.PutString(x, 0, label, Color.LightCyan, Color.DarkBlue, CellAttrs.Bold);
}

static void Copy(ScreenBuffer src, ScreenBuffer dst)
{
    // Cell-by-cell copy. ScreenBuffer doesn't expose its array, so
    // walk it through the indexer — fine for a demo, ~1500 cells/frame.
    for (var y = 0; y < src.Height; y++)
        for (var x = 0; x < src.Width; x++)
            dst[x, y] = src[x, y];
}
