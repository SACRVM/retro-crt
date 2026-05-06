using Retro.Crt.Input;

namespace Retro.Crt.Life;

internal enum Pattern
{
    Random,
    RPentomino,
    Acorn,
    Diehard,
    Pulsar,
    GliderGun,
}

/// <summary>
/// Conway's Game of Life on a wrapping (toroidal) grid. Cells store
/// their <i>age</i> in generations (0 = dead, 1+ = alive); the renderer
/// maps age to color so just-born cells flash bright and stable
/// structures fade to cool tones — a heat-map of activity over time.
/// </summary>
/// <remarks>
/// Vertical resolution doubles via <c>▀</c> half-blocks: each render
/// row packs two grid rows into one terminal cell, foreground showing
/// the upper grid cell, background showing the lower. Two
/// independently colored sub-pixels per terminal cell.
/// </remarks>
internal sealed class Game
{
    // Age caps at 255 — anything older just stays at the cool end of
    // the gradient. Plenty for pulsar/oscillator demos where ages reach
    // hundreds in long-running simulations.
    private const byte MaxAge = 255;

    private readonly int _renderWidth;
    private readonly int _renderHeight;
    private readonly int _bufWidth;
    private readonly int _bufHeight;
    private readonly Random _rng;

    private byte[,] _grid;
    private byte[,] _scratch;

    private int _tickMs = 80;
    private Pattern _current = Pattern.Random;

    public int Generation { get; private set; }
    public int Alive { get; private set; }
    public bool IsStarted { get; private set; }
    public bool IsPaused { get; private set; }
    public Pattern Current => _current;
    public int TickMs => _tickMs;

    public Game(int renderWidth, int renderHeight, int? seed = null)
    {
        _renderWidth  = renderWidth;
        _renderHeight = renderHeight;
        // Layout rows: 0 HUD, 1 top border, 2..h-3 field, h-2 footer,
        // h-1 bottom border. Field height = h-4, doubled for half-cell.
        _bufWidth     = renderWidth - 2;
        _bufHeight    = (renderHeight - 4) * 2;

        _grid    = new byte[_bufWidth, _bufHeight];
        _scratch = new byte[_bufWidth, _bufHeight];
        _rng     = seed is { } s ? new Random(s) : new Random();

        Seed(Pattern.Random);
    }

    public void Start() => IsStarted = true;

    /// <summary>
    /// Reseed with the same pattern. Re-randomizes when current pattern
    /// is <see cref="Pattern.Random"/>; replays the placement otherwise.
    /// </summary>
    public void Reset()
    {
        Seed(_current);
        IsPaused = false;
    }

    public void Seed(Pattern pattern)
    {
        _current = pattern;
        Array.Clear(_grid);
        Generation = 0;
        if (pattern == Pattern.Random)
        {
            SeedRandom(0.30);
            return;
        }

        var coords = pattern switch
        {
            Pattern.RPentomino => RPentomino,
            Pattern.Acorn      => Acorn,
            Pattern.Diehard    => Diehard,
            Pattern.Pulsar     => Pulsar,
            Pattern.GliderGun  => GosperGliderGun,
            _                  => throw new ArgumentOutOfRangeException(nameof(pattern)),
        };
        var (pw, ph) = BoundingBox(coords);

        // Glider Gun is meant to fire across the field — anchor it
        // top-left with a small inset so the gliders have travel room.
        // Other patterns center for symmetry.
        int ox, oy;
        if (pattern == Pattern.GliderGun)
        {
            ox = 2;
            oy = 2;
        }
        else
        {
            ox = (_bufWidth  - pw) / 2;
            oy = (_bufHeight - ph) / 2;
        }

        var alive = 0;
        for (var i = 0; i < coords.GetLength(0); i++)
        {
            var x = ox + coords[i, 0];
            var y = oy + coords[i, 1];
            if ((uint)x < (uint)_bufWidth && (uint)y < (uint)_bufHeight)
            {
                _grid[x, y] = 1;
                alive++;
            }
        }
        Alive = alive;
    }

    private void SeedRandom(double density)
    {
        var alive = 0;
        for (var y = 0; y < _bufHeight; y++)
        for (var x = 0; x < _bufWidth; x++)
        {
            if (_rng.NextDouble() < density) { _grid[x, y] = 1; alive++; }
            else                              _grid[x, y] = 0;
        }
        Alive = alive;
    }

    private static (int Width, int Height) BoundingBox(int[,] coords)
    {
        var w = 0;
        var h = 0;
        for (var i = 0; i < coords.GetLength(0); i++)
        {
            if (coords[i, 0] >= w) w = coords[i, 0] + 1;
            if (coords[i, 1] >= h) h = coords[i, 1] + 1;
        }
        return (w, h);
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

            byte now;
            if (was > 0)
            {
                // Conway S23: survives on 2 or 3 neighbors. Age bumps
                // by one each survived generation, capped so the age
                // gradient saturates rather than wraps.
                now = (n == 2 || n == 3)
                    ? (was < MaxAge ? (byte)(was + 1) : MaxAge)
                    : (byte)0;
            }
            else
            {
                // Conway B3: birth on exactly 3 neighbors, age 1.
                now = n == 3 ? (byte)1 : (byte)0;
            }
            _scratch[x, y] = now;
            if (now > 0) alive++;
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
            var nx = x + dx;
            if      (nx < 0)          nx = _bufWidth  - 1;
            else if (nx >= _bufWidth) nx = 0;
            var ny = y + dy;
            if      (ny < 0)           ny = _bufHeight - 1;
            else if (ny >= _bufHeight) ny = 0;
            if (_grid[nx, ny] > 0) count++;
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
                if (IsStarted && IsPaused) StepOnce();
                break;
            case '+':
                _tickMs = Math.Max(20, _tickMs - 20);
                break;
            case '-':
                _tickMs = Math.Min(500, _tickMs + 20);
                break;
            case '1': Seed(Pattern.Random);     IsPaused = false; break;
            case '2': Seed(Pattern.RPentomino); IsPaused = false; break;
            case '3': Seed(Pattern.Acorn);      IsPaused = false; break;
            case '4': Seed(Pattern.Diehard);    IsPaused = false; break;
            case '5': Seed(Pattern.Pulsar);     IsPaused = false; break;
            case '6': Seed(Pattern.GliderGun);  IsPaused = false; break;
        }
    }

    public void Draw(ScreenBuffer screen)
    {
        screen.Clear(new Cell(' ', Color.LightGray, Color.Black));
        DrawHud(screen);
        DrawFooter(screen);
        DrawBorder(screen);
        DrawField(screen);
        if (!IsStarted) DrawIntro(screen);
    }

    private void DrawHud(ScreenBuffer screen)
    {
        // Right-pad the numbers to fixed widths so the trailing portion
        // of the HUD doesn't shift when digits roll over (e.g.,
        // 99 → 100). Without padding every digit-count change makes the
        // rest of the row redraw, visible as a "wave" of flicker.
        var label = $" Life  ·  {PatternName(_current),-12}  ·  Gen {Generation,6}  ·  Alive {Alive,5}  ·  Tick {_tickMs,3}ms ";
        var clipped = label.Length > _renderWidth ? label.AsSpan(0, _renderWidth) : label.AsSpan();
        screen.FillRect(0, 0, _renderWidth, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
        screen.PutString(0, 0, clipped, Color.LightGray, Color.DarkBlue, CellAttrs.Bold);
    }

    private void DrawFooter(ScreenBuffer screen)
    {
        // Footer at row h-2 (just above the bottom border). Pause shows
        // a different command set as a visual cue that the sim is on
        // hold — same row, no separate overlay so we never collide with
        // a centered intro/dialog.
        var (text, fg, bg) = IsPaused
            ? ("  PAUSE  ·  N step  ·  SPACE resume  ·  Esc quit  ",
               Color.Black, Color.Yellow)
            : (" SPACE pause  ·  1-6 patterns  ·  R reset  ·  N step  ·  +/- speed  ·  Esc quit ",
               Color.LightGray, Color.DarkBlue);

        var y = _renderHeight - 2;
        screen.FillRect(0, y, _renderWidth, 1, new Cell(' ', fg, bg));
        var clipped = text.Length > _renderWidth ? text.AsSpan(0, _renderWidth) : text.AsSpan();
        screen.PutString(0, y, clipped, fg, bg, CellAttrs.Bold);
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
        var rows = _renderHeight - 4;  // matches _bufHeight / 2
        for (var ry = 0; ry < rows; ry++)
        {
            var topY = ry * 2;
            var botY = ry * 2 + 1;
            for (var rx = 0; rx < _bufWidth; rx++)
            {
                var topAge = _grid[rx, topY];
                var botAge = botY < _bufHeight ? _grid[rx, botY] : (byte)0;
                if (topAge == 0 && botAge == 0) continue;

                // '▀' upper-half-block: fg paints the top half, bg
                // paints the bottom. Gives us two independently colored
                // grid cells per terminal cell — the half-cell trick
                // with full color expressiveness.
                var topFg = AgeColor(topAge);
                var botBg = AgeColor(botAge);
                var attr  = (topAge == 1 || botAge == 1) ? CellAttrs.Bold : CellAttrs.None;
                screen[rx + 1, ry + 2] = new Cell('▀', topFg, botBg, attr);
            }
        }
    }

    private static Color AgeColor(byte age) => age switch
    {
        0       => Color.Black,        // dead — falls through; only shows as bg of '▀'
        1       => Color.Yellow,        // freshly born — spark
        2 or 3  => Color.LightGreen,    // young, active
        <= 9    => Color.DarkGreen,     // mature
        <= 29   => Color.LightCyan,     // stable
        _       => Color.DarkCyan,      // ancient
    };

    private static string PatternName(Pattern p) => p switch
    {
        Pattern.Random     => "Random",
        Pattern.RPentomino => "R-Pentomino",
        Pattern.Acorn      => "Acorn",
        Pattern.Diehard    => "Diehard",
        Pattern.Pulsar     => "Pulsar",
        Pattern.GliderGun  => "Glider Gun",
        _                  => "?",
    };

    private void DrawIntro(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                     ",
            "             L I F E                 ",
            "        Conway's automaton           ",
            "                                     ",
            "   Choose a pattern + SPACE start    ",
            "                                     ",
            "    1  Random soup     (~30% fill)   ",
            "    2  R-Pentomino     (~1100 gen)   ",
            "    3  Acorn           (~5200 gen)   ",
            "    4  Diehard         (130 gen die) ",
            "    5  Pulsar          (period 3)    ",
            "    6  Glider Gun      (forever)     ",
            "                                     ",
            "    SPACE  start / pause / resume    ",
            "    N      step (when paused)        ",
            "    R      reseed current pattern    ",
            "    +/-    faster / slower           ",
            "    Esc    quit                      ",
            "                                     ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
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

    // ─── Pattern coordinates ─────────────────────────────────────────
    // Each int[,] holds (x, y) offsets from the pattern's top-left.
    // Game.Seed centers them in the field (Glider Gun anchors top-left).

    /// <summary>
    /// .XX
    /// XX.
    /// .X.
    /// </summary>
    private static readonly int[,] RPentomino = {
        {1,0},{2,0},
        {0,1},{1,1},
        {1,2},
    };

    /// <summary>
    /// .X.....
    /// ...X...
    /// XX..XXX
    /// </summary>
    private static readonly int[,] Acorn = {
        {1,0},
        {3,1},
        {0,2},{1,2},{4,2},{5,2},{6,2},
    };

    /// <summary>
    /// ......X.
    /// XX......
    /// .X...XXX
    /// </summary>
    private static readonly int[,] Diehard = {
        {6,0},
        {0,1},{1,1},
        {1,2},{5,2},{6,2},{7,2},
    };

    /// <summary>
    /// 13×13 period-3 oscillator. Symmetric four-quadrant motif.
    /// </summary>
    private static readonly int[,] Pulsar = {
        // top edge
        {2,0},{3,0},{4,0},  {8,0},{9,0},{10,0},
        // left/right verticals (rows 2-4)
        {0,2},{5,2},{7,2},{12,2},
        {0,3},{5,3},{7,3},{12,3},
        {0,4},{5,4},{7,4},{12,4},
        // middle band
        {2,5},{3,5},{4,5},  {8,5},{9,5},{10,5},
        {2,7},{3,7},{4,7},  {8,7},{9,7},{10,7},
        // left/right verticals (rows 8-10)
        {0,8},{5,8},{7,8},{12,8},
        {0,9},{5,9},{7,9},{12,9},
        {0,10},{5,10},{7,10},{12,10},
        // bottom edge
        {2,12},{3,12},{4,12},  {8,12},{9,12},{10,12},
    };

    /// <summary>
    /// Gosper's Glider Gun — 36×9 bounding box. First infinite-growth
    /// pattern ever discovered (1970), keeps emitting gliders forever.
    /// </summary>
    private static readonly int[,] GosperGliderGun = {
        // Left block
        {0,4},{0,5},{1,4},{1,5},
        // Left ship
        {10,4},{10,5},{10,6},
        {11,3},{11,7},
        {12,2},{12,8},{13,2},{13,8},
        {14,5},
        {15,3},{15,7},
        {16,4},{16,5},{16,6},
        {17,5},
        // Right ship
        {20,2},{20,3},{20,4},
        {21,2},{21,3},{21,4},
        {22,1},{22,5},
        {24,0},{24,1},{24,5},{24,6},
        // Right block
        {34,2},{34,3},{35,2},{35,3},
    };
}
