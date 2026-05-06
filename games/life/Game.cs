using Retro.Crt.Input;

namespace Retro.Crt.Life;

/// <summary>
/// Conway's Game of Life on a wrapping (toroidal) grid. Two cell-array
/// buffers (current + scratch) ping-ponged on each step; the public API
/// is read-only state + <see cref="Step"/> + <see cref="Reset"/> +
/// <see cref="HandleKey"/>. Render through <see cref="Draw"/>; the host
/// chooses the cadence.
/// </summary>
/// <remarks>
/// Vertical resolution doubles via half-cell glyphs: each render row
/// packs two grid rows into one terminal cell using
/// <c>▀</c> / <c>▄</c> / <c>█</c> / <c>' '</c>. With monospace cells
/// being roughly twice as tall as wide on most fonts, that yields a
/// near-square pixel which keeps gliders looking like gliders.
/// </remarks>
internal sealed class Game
{
    private readonly int _renderWidth;
    private readonly int _renderHeight;
    private readonly int _bufWidth;
    private readonly int _bufHeight;
    private readonly Random _rng;

    private bool[,] _grid;
    private bool[,] _scratch;

    private int _tickMs = 80;

    public int Generation { get; private set; }
    public int Alive { get; private set; }
    public bool IsStarted { get; private set; }
    public bool IsPaused { get; private set; }

    /// <summary>Generations per second the host should target right now.</summary>
    public int TickMs => _tickMs;

    public Game(int renderWidth, int renderHeight, int? seed = null)
    {
        _renderWidth  = renderWidth;
        _renderHeight = renderHeight;

        // Layout: row 0 = HUD; row 1 = top border; rows 2..h-2 = field
        // (h-3 render rows, each holding 2 grid rows); row h-1 = bottom
        // border. One column of border on each side.
        _bufWidth  = renderWidth - 2;
        _bufHeight = (renderHeight - 3) * 2;

        _grid    = new bool[_bufWidth, _bufHeight];
        _scratch = new bool[_bufWidth, _bufHeight];
        _rng     = seed is { } s ? new Random(s) : new Random();

        Seed(0.30);
    }

    public void Start() => IsStarted = true;

    public void Reset()
    {
        Seed(0.30);
        IsPaused = false;
    }

    private void Seed(double density)
    {
        var alive = 0;
        for (var y = 0; y < _bufHeight; y++)
        for (var x = 0; x < _bufWidth; x++)
        {
            var on = _rng.NextDouble() < density;
            _grid[x, y] = on;
            if (on) alive++;
        }
        Generation = 0;
        Alive      = alive;
    }

    public void Step()
    {
        if (!IsStarted || IsPaused) return;
        StepOnce();
    }

    private void StepOnce()
    {
        var alive = 0;
        for (var y = 0; y < _bufHeight; y++)
        for (var x = 0; x < _bufWidth; x++)
        {
            var n   = CountNeighbors(x, y);
            var was = _grid[x, y];
            // Conway B3/S23: a live cell survives on 2 or 3 neighbors;
            // a dead cell ignites on exactly 3.
            var now = was ? (n == 2 || n == 3) : (n == 3);
            _scratch[x, y] = now;
            if (now) alive++;
        }
        (_grid, _scratch) = (_scratch, _grid);
        Alive = alive;
        Generation++;
    }

    private int CountNeighbors(int x, int y)
    {
        var count = 0;
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            // Toroidal wrap — keeps long-running runs from drifting off
            // the visible field. Closed universe; gliders re-enter the
            // opposite edge.
            var nx = x + dx;
            if      (nx < 0)          nx = _bufWidth  - 1;
            else if (nx >= _bufWidth) nx = 0;
            var ny = y + dy;
            if      (ny < 0)           ny = _bufHeight - 1;
            else if (ny >= _bufHeight) ny = 0;
            if (_grid[nx, ny]) count++;
        }
        return count;
    }

    public void HandleKey(KeyEvent key)
    {
        if (key.Key != Key.Glyph) return;
        switch (key.Glyph)
        {
            case ' ':
                if (!IsStarted) Start();
                else            IsPaused = !IsPaused;
                break;
            case 'r' or 'R':
                Reset();
                break;
            case 'n' or 'N':
                // Single-step the simulation when paused — useful for
                // tracing how a still-life forms or what just happened
                // at the moment the player paused.
                if (IsStarted && IsPaused) StepOnce();
                break;
            case '+':
                _tickMs = Math.Max(20, _tickMs - 20);
                break;
            case '-':
                _tickMs = Math.Min(500, _tickMs + 20);
                break;
        }
    }

    public void Draw(ScreenBuffer screen)
    {
        screen.Clear(new Cell(' ', Color.LightGray, Color.Black));
        DrawHud(screen);
        DrawBorder(screen);
        DrawField(screen);
        if      (!IsStarted) DrawIntro(screen);
        else if (IsPaused)   DrawPauseHint(screen);
    }

    private void DrawHud(ScreenBuffer screen)
    {
        var label = $" Life  ·  Gen {Generation}  ·  Alive {Alive}  ·  Tick {_tickMs}ms  ·  SPACE pause  ·  N step  ·  R reset  ·  +/- speed  ·  Esc quit ";
        var clipped = label.Length > _renderWidth ? label.AsSpan(0, _renderWidth) : label.AsSpan();
        screen.FillRect(0, 0, _renderWidth, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
        screen.PutString(0, 0, clipped, Color.LightGray, Color.DarkBlue, CellAttrs.Bold);
    }

    private void DrawBorder(ScreenBuffer screen)
    {
        var c = new Cell('░', Color.DarkGray, Color.Black);
        for (var x = 0; x < _renderWidth; x++)
        {
            screen[x, 1]                  = c;
            screen[x, _renderHeight - 1]  = c;
        }
        for (var y = 1; y < _renderHeight; y++)
        {
            screen[0, y]                  = c;
            screen[_renderWidth - 1, y]   = c;
        }
    }

    private void DrawField(ScreenBuffer screen)
    {
        var rows = _renderHeight - 3;     // playable rows between borders
        for (var ry = 0; ry < rows; ry++)
        {
            var topY = ry * 2;
            var botY = ry * 2 + 1;
            for (var rx = 0; rx < _bufWidth; rx++)
            {
                var top    = _grid[rx, topY];
                var bottom = botY < _bufHeight && _grid[rx, botY];
                var glyph = (top, bottom) switch
                {
                    (true,  true)  => '█',
                    (true,  false) => '▀',
                    (false, true)  => '▄',
                    _              => ' ',
                };
                if (glyph == ' ') continue;
                screen[rx + 1, ry + 2] = new Cell(glyph, Color.LightGreen, Color.Black);
            }
        }
    }

    private void DrawIntro(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                ",
            "          L I F E               ",
            "      Conway's automaton        ",
            "                                ",
            "    Press SPACE to start        ",
            "                                ",
            "    SPACE  pause / resume       ",
            "    N      single step (paused) ",
            "    R      reseed random        ",
            "    +/-    faster / slower      ",
            "    Esc    quit                 ",
            "                                ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
    }

    private void DrawPauseHint(ScreenBuffer screen)
    {
        var text = "  PAUSE  · N step · SPACE resume  ";
        var x = (_renderWidth - text.Length) / 2;
        var y = _renderHeight - 2;
        screen.PutString(x, y, text.AsSpan(), Color.Black, Color.Yellow, CellAttrs.Bold);
    }

    private void DrawCenteredBox(ScreenBuffer screen, string[] lines, Color fg, Color bg)
    {
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++) if (lines[i].Length > maxW) maxW = lines[i].Length;
        var w = maxW + 4;
        var h = lines.Length + 2;
        var x0 = (_renderWidth  - w) / 2;
        var y0 = (_renderHeight - h) / 2;

        screen.FillRect(x0, y0, w, h, new Cell(' ', fg, bg));
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            var lineX = x0 + (w - lines[i].Length) / 2;
            screen.PutString(lineX, y0 + 1 + i, lines[i].AsSpan(), fg, bg, CellAttrs.Bold);
        }
    }
}
