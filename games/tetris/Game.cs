using Retro.Crt.Input;

namespace Retro.Crt.Tetris;

/// <summary>
/// Classic Tetris on a 10×20 well. Seven tetrominoes (I/O/T/S/Z/L/J)
/// fall under gravity; the player slides them left/right, rotates with
/// wall-kicks, and slams them down with hard-drop. Filled rows clear,
/// award score that scales with simultaneous-clear count and current
/// level, and bump the level every 10 lines — gravity accelerates with
/// each level. Game over when a freshly spawned piece can't fit.
/// </summary>
internal sealed class Game
{
    public const int FieldW = 10;
    public const int FieldH = 20;
    /// <summary>
    /// Each game cell paints two terminal columns wide. Most monospace
    /// fonts are ~2:1 tall:wide per cell, so doubling horizontally
    /// keeps tetrominoes visually square instead of stretched-tall.
    /// </summary>
    public const int CellW = 2;

    // Shape table — every (piece, rotation) slot is a 16-char string
    // of a 4×4 bounding box, row-major. 'X' = filled, '.' = empty.
    // Strings (rather than (dx,dy) tuples) keep the table eyeball-
    // verifiable; the four-row layout matches what you'd sketch on
    // paper. Rotations follow Super Rotation System orientations so
    // pieces rotate around their natural pivot.
    private static readonly string[,] PieceShapes = new string[7, 4]
    {
        // I
        { "....XXXX........", "..X...X...X...X.", "........XXXX....", ".X...X...X...X.." },
        // O
        { ".XX..XX.........", ".XX..XX.........", ".XX..XX.........", ".XX..XX........." },
        // T
        { ".X..XXX.........", ".X...XX..X......", "....XXX..X......", ".X..XX...X......" },
        // S
        { ".XX.XX..........", ".X...XX...X.....", ".....XX.XX......", "X...XX...X......" },
        // Z
        { "XX...XX.........", "..X..XX..X......", "....XX...XX.....", ".X..XX..X......." },
        // L
        { "..X.XXX.........", ".X...X...XX.....", "....XXX.X.......", "XX...X...X......" },
        // J
        { "X...XXX.........", ".XX..X...X......", "....XXX...X.....", ".X...X..XX......" },
    };

    private static readonly Color[] PieceColors =
    {
        Color.LightCyan,    // I
        Color.Yellow,       // O
        Color.LightMagenta, // T
        Color.LightGreen,   // S
        Color.LightRed,     // Z
        Color.Brown,        // L  — DOS palette's orange slot
        Color.LightBlue,    // J
    };

    // Field stores piece-id + 1 (so 0 = empty, 1..7 = locked piece).
    private readonly int[,] _field = new int[FieldW, FieldH];

    // Active falling piece.
    private int _pieceId;
    private int _pieceRot;
    private int _pieceX;
    private int _pieceY;
    private int _nextId;

    private readonly Random _rng;
    private long _dropAccumMs;

    private readonly int _renderWidth;
    private readonly int _renderHeight;
    private readonly int _fieldOffsetX;
    private readonly int _fieldOffsetY;
    private readonly int _sidebarX;

    public int  Score      { get; private set; }
    public int  Level      { get; private set; }
    public int  Lines      { get; private set; }
    public bool IsStarted  { get; private set; }
    public bool IsAlive    { get; private set; } = true;
    public bool IsPaused   { get; private set; }
    public bool IsHelpOpen { get; private set; }

    /// <summary>30 ms tick — fast enough for crisp input + render, slow enough that gravity is a multiple of it via accumulator.</summary>
    public int TickMs { get; private set; } = 30;

    public Game(int width, int height, int? seed = null)
    {
        _renderWidth  = width;
        _renderHeight = height;
        _rng          = seed is { } s ? new Random(s) : new Random();

        // Layout: HUD (row 0), top border (1), arena (2..h-3), bottom
        // border (h-2), footer (h-1). Center the well+sidebar block in
        // the arena so the playfield sits toward the middle and the
        // stats panel gets breathing room rather than hugging the left
        // edge. Sidebar width is the widest sidebar line: a 4-cell
        // NEXT preview at CellW=2 → 8 columns.
        const int SidebarWidth = 8;
        const int Gap          = 4;
        var blockWidth = FieldW * CellW + 2 + Gap + SidebarWidth;
        var marginX    = (width - blockWidth) / 2;
        if (marginX < 1) marginX = 1;

        _fieldOffsetX = marginX + 1;
        _fieldOffsetY = 2;
        _sidebarX     = _fieldOffsetX + FieldW * CellW + 1 + Gap;

        _nextId = _rng.Next(7);
        SpawnNext();
    }

    public void Start() => IsStarted = true;

    private void SpawnNext()
    {
        _pieceId  = _nextId;
        _nextId   = _rng.Next(7);
        _pieceRot = 0;
        _pieceX   = (FieldW - 4) / 2;
        _pieceY   = 0;

        // If the spawn position is already blocked, the well is full —
        // canonical Tetris game-over.
        if (Collides(_pieceId, _pieceRot, _pieceX, _pieceY)) IsAlive = false;
    }

    private bool Collides(int pieceId, int rot, int x, int y)
    {
        var shape = PieceShapes[pieceId, rot];
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                if (shape[row * 4 + col] != 'X') continue;
                var fx = x + col;
                var fy = y + row;
                if (fx < 0 || fx >= FieldW || fy >= FieldH) return true;
                // Cells above the field are valid (spawn rows for tall
                // pieces poke above row 0). Only the floor and walls
                // are hard barriers.
                if (fy < 0) continue;
                if (_field[fx, fy] != 0) return true;
            }
        }
        return false;
    }

    private void LockPiece()
    {
        var shape = PieceShapes[_pieceId, _pieceRot];
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                if (shape[row * 4 + col] != 'X') continue;
                var fx = _pieceX + col;
                var fy = _pieceY + row;
                if (fy >= 0 && fy < FieldH && fx >= 0 && fx < FieldW)
                    _field[fx, fy] = _pieceId + 1;
            }
        }

        var cleared = ClearLines();
        Lines += cleared;
        // Classic Tetris (NES) line-clear scoring: scales with the
        // simultaneous-clear count (Tetris = 4 lines = 800 × level)
        // so the optimal play stops being one-line-at-a-time.
        Score += cleared switch
        {
            1 => 100 * (Level + 1),
            2 => 300 * (Level + 1),
            3 => 500 * (Level + 1),
            4 => 800 * (Level + 1),
            _ => 0,
        };
        Level = Lines / 10;

        SpawnNext();
    }

    private int ClearLines()
    {
        var cleared = 0;
        for (var y = FieldH - 1; y >= 0; y--)
        {
            var full = true;
            for (var x = 0; x < FieldW; x++)
            {
                if (_field[x, y] == 0) { full = false; break; }
            }
            if (!full) continue;

            cleared++;
            // Cascade everything above down by one row, then re-check
            // this y (which now holds what used to be y-1) for stacked
            // clears.
            for (var ty = y; ty > 0; ty--)
                for (var x = 0; x < FieldW; x++)
                    _field[x, ty] = _field[x, ty - 1];
            for (var x = 0; x < FieldW; x++) _field[x, 0] = 0;
            y++;
        }
        return cleared;
    }

    public void Step()
    {
        if (!IsStarted || !IsAlive || IsPaused || IsHelpOpen) return;

        _dropAccumMs += TickMs;
        var period = DropPeriodMs();
        // Catch-up loop in case a slow frame let the accumulator pass
        // multiple drop periods (or in case period is shorter than
        // the tick at high levels).
        while (_dropAccumMs >= period)
        {
            _dropAccumMs -= period;
            if (!Collides(_pieceId, _pieceRot, _pieceX, _pieceY + 1))
                _pieceY++;
            else
                LockPiece();
            if (!IsAlive) break;
        }
    }

    private int DropPeriodMs()
    {
        // Gravity: 800 ms at level 0, drops 50 ms per level, floor at
        // 50 ms — past level 15 you're playing the kill screen.
        var ms = 800 - Level * 50;
        return ms < 50 ? 50 : ms;
    }

    public void HandleKey(KeyEvent key)
    {
        if (IsHelpOpen)
        {
            IsHelpOpen = false;
            return;
        }

        if (!IsStarted)
        {
            if (key.Key == Key.Glyph && key.Glyph == ' ') Start();
            else if (key.Key == Key.Glyph && (key.Glyph is 'h' or 'H' or '?')) IsHelpOpen = true;
            return;
        }

        if (!IsAlive)
        {
            if (key.Key == Key.Glyph && (key.Glyph is 'r' or 'R')) Restart();
            return;
        }

        if (IsPaused)
        {
            if (key.Key == Key.Glyph && (key.Glyph is 'p' or 'P')) IsPaused = false;
            else if (key.Key == Key.Glyph && (key.Glyph is 'h' or 'H' or '?')) IsHelpOpen = true;
            return;
        }

        // Movement / rotate / drop
        if (key.Key == Key.Left  || (key.Key == Key.Glyph && (key.Glyph is 'a' or 'A')))
        {
            if (!Collides(_pieceId, _pieceRot, _pieceX - 1, _pieceY)) _pieceX--;
            return;
        }
        if (key.Key == Key.Right || (key.Key == Key.Glyph && (key.Glyph is 'd' or 'D')))
        {
            if (!Collides(_pieceId, _pieceRot, _pieceX + 1, _pieceY)) _pieceX++;
            return;
        }
        if (key.Key == Key.Down  || (key.Key == Key.Glyph && (key.Glyph is 's' or 'S')))
        {
            // Soft drop: one row + a 1-pt nudge for the player who
            // earned it.
            if (!Collides(_pieceId, _pieceRot, _pieceX, _pieceY + 1))
            {
                _pieceY++;
                Score++;
            }
            return;
        }
        if (key.Key == Key.Up    || (key.Key == Key.Glyph && (key.Glyph is 'w' or 'W')))
        {
            TryRotate(+1);
            return;
        }

        if (key.Key != Key.Glyph) return;
        switch (key.Glyph)
        {
            case ' ':
                HardDrop();
                break;
            case 'z' or 'Z':
                TryRotate(-1);
                break;
            case 'p' or 'P':
                IsPaused = true;
                break;
            case 'h' or 'H' or '?':
                IsHelpOpen = true;
                break;
        }
    }

    private void TryRotate(int delta)
    {
        var newRot = ((_pieceRot + delta) % 4 + 4) % 4;
        if (!Collides(_pieceId, newRot, _pieceX, _pieceY))
        {
            _pieceRot = newRot;
            return;
        }
        // Wall kicks — try shifts of ±1, ±2 columns. Catches the
        // common "rotate I against the right wall" / "rotate T into a
        // corner" cases without going full SRS kick tables.
        Span<int> kicks = stackalloc[] { 1, -1, 2, -2 };
        foreach (var k in kicks)
        {
            if (!Collides(_pieceId, newRot, _pieceX + k, _pieceY))
            {
                _pieceRot = newRot;
                _pieceX  += k;
                return;
            }
        }
    }

    private void HardDrop()
    {
        var dropped = 0;
        while (!Collides(_pieceId, _pieceRot, _pieceX, _pieceY + 1))
        {
            _pieceY++;
            dropped++;
        }
        // Hard drop pays 2 pts/cell — the muscle-memory speedrun of
        // classic Tetris. Bigger reward than soft drop since you're
        // committing without a chance to slide-correct.
        Score += dropped * 2;
        LockPiece();
    }

    public void Restart()
    {
        for (var x = 0; x < FieldW; x++)
            for (var y = 0; y < FieldH; y++)
                _field[x, y] = 0;
        Score = 0;
        Level = 0;
        Lines = 0;
        IsAlive = true;
        IsStarted = false;
        IsPaused = false;
        IsHelpOpen = false;
        _dropAccumMs = 0;
        _nextId = _rng.Next(7);
        SpawnNext();
    }

    public void Draw(ScreenBuffer screen)
    {
        screen.Clear(new Cell(' ', Color.LightGray, Color.Black));
        DrawHud(screen);
        DrawFooter(screen);
        DrawBorder(screen);
        DrawWell(screen);
        DrawField(screen);
        DrawPiece(screen);
        DrawSidebar(screen);
        if (!IsStarted)  DrawIntro(screen);
        if (!IsAlive)    DrawGameOver(screen);
        if (IsPaused)    DrawPaused(screen);
        if (IsHelpOpen)  DrawHelp(screen);
    }

    private void DrawHud(ScreenBuffer screen)
    {
        var label = $" Tetris  ·  Score {Score,6}  ·  Level {Level,2}  ·  Lines {Lines,3} ";
        var clipped = label.Length > _renderWidth ? label.AsSpan(0, _renderWidth) : label.AsSpan();
        screen.FillRect(0, 0, _renderWidth, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
        screen.PutString(0, 0, clipped, Color.LightGray, Color.DarkBlue, CellAttrs.Bold);
    }

    private void DrawFooter(ScreenBuffer screen)
    {
        var (text, fg, bg) = !IsAlive
            ? ("  GAME OVER  ·  R restart  ·  Esc quit  ",
               Color.White, Color.DarkRed)
            : IsPaused
                ? ("  PAUSE  ·  P resume  ·  H help  ·  Esc quit  ",
                   Color.Black, Color.Yellow)
                : (" Arrows/WASD move + soft drop  ·  W rot  ·  Z rot-  ·  SPACE hard drop  ·  P pause  ·  H help  ·  Esc quit ",
                   Color.LightGray, Color.DarkBlue);

        var y = _renderHeight - 1;
        screen.FillRect(0, y, _renderWidth, 1, new Cell(' ', fg, bg));
        var clipped = text.Length > _renderWidth ? text.AsSpan(0, _renderWidth) : text.AsSpan();
        screen.PutString(0, y, clipped, fg, bg, CellAttrs.Bold);
    }

    private void DrawBorder(ScreenBuffer screen)
    {
        var c = new Cell('░', Color.DarkGray, Color.Black);
        var topY    = 1;
        var bottomY = _renderHeight - 2;
        for (var x = 0; x < _renderWidth; x++)
        {
            screen[x, topY]    = c;
            screen[x, bottomY] = c;
        }
        for (var y = topY; y <= bottomY; y++)
        {
            screen[0, y]                    = c;
            screen[_renderWidth - 1, y]     = c;
        }
    }

    private void DrawWell(ScreenBuffer screen)
    {
        // Inner well frame around the playfield — thin '│' walls and a
        // '└┘' floor, all DarkGray so the field cells pop visually.
        var wall  = new Cell('│', Color.DarkGray, Color.Black);
        var floor = new Cell('─', Color.DarkGray, Color.Black);
        var bl    = new Cell('└', Color.DarkGray, Color.Black);
        var br    = new Cell('┘', Color.DarkGray, Color.Black);

        var leftX  = _fieldOffsetX - 1;
        var rightX = _fieldOffsetX + FieldW * CellW;
        var floorY = _fieldOffsetY + FieldH;

        for (var y = _fieldOffsetY; y < floorY; y++)
        {
            if ((uint)leftX  < (uint)_renderWidth) screen[leftX,  y] = wall;
            if ((uint)rightX < (uint)_renderWidth) screen[rightX, y] = wall;
        }
        for (var x = _fieldOffsetX; x < rightX; x++)
            if ((uint)floorY < (uint)_renderHeight) screen[x, floorY] = floor;

        if ((uint)leftX  < (uint)_renderWidth && (uint)floorY < (uint)_renderHeight) screen[leftX,  floorY] = bl;
        if ((uint)rightX < (uint)_renderWidth && (uint)floorY < (uint)_renderHeight) screen[rightX, floorY] = br;
    }

    private void DrawField(ScreenBuffer screen)
    {
        for (var y = 0; y < FieldH; y++)
        {
            for (var x = 0; x < FieldW; x++)
            {
                var v = _field[x, y];
                if (v == 0) continue;
                PaintCell(screen, x, y, PieceColors[v - 1]);
            }
        }
    }

    private void DrawPiece(ScreenBuffer screen)
    {
        var shape = PieceShapes[_pieceId, _pieceRot];
        var color = PieceColors[_pieceId];
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                if (shape[row * 4 + col] != 'X') continue;
                var fx = _pieceX + col;
                var fy = _pieceY + row;
                if (fy < 0 || fy >= FieldH || fx < 0 || fx >= FieldW) continue;
                PaintCell(screen, fx, fy, color);
            }
        }
    }

    /// <summary>Paint one game-cell of the field as two adjacent terminal cells of solid '█'.</summary>
    private void PaintCell(ScreenBuffer screen, int fx, int fy, Color color)
    {
        var sx = _fieldOffsetX + fx * CellW;
        var sy = _fieldOffsetY + fy;
        if ((uint)sy >= (uint)_renderHeight) return;
        var c = new Cell('█', color, Color.Black, CellAttrs.Bold);
        if ((uint)sx     < (uint)_renderWidth) screen[sx,     sy] = c;
        if ((uint)(sx+1) < (uint)_renderWidth) screen[sx + 1, sy] = c;
    }

    private void DrawSidebar(ScreenBuffer screen)
    {
        var x = _sidebarX;
        if (x >= _renderWidth - 2) return;

        var fg = Color.LightGray;
        var bg = Color.Black;

        var y = _fieldOffsetY;
        screen.PutString(x, y++, "NEXT".AsSpan(),  Color.Yellow, bg, CellAttrs.Bold);
        // Mini next-piece preview in a 4×2 area below the label.
        var nextShape = PieceShapes[_nextId, 0];
        var nextColor = PieceColors[_nextId];
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                if (nextShape[row * 4 + col] != 'X') continue;
                var sx = x + col * CellW;
                var sy = y + row;
                if ((uint)sx     < (uint)_renderWidth && (uint)sy < (uint)_renderHeight)
                    screen[sx,     sy] = new Cell('█', nextColor, bg, CellAttrs.Bold);
                if ((uint)(sx+1) < (uint)_renderWidth && (uint)sy < (uint)_renderHeight)
                    screen[sx + 1, sy] = new Cell('█', nextColor, bg, CellAttrs.Bold);
            }
        }
        y += 5;

        screen.PutString(x, y++, "SCORE".AsSpan(), Color.Yellow, bg, CellAttrs.Bold);
        screen.PutString(x, y++, $"{Score,7}".AsSpan(), fg, bg);
        y++;
        screen.PutString(x, y++, "LEVEL".AsSpan(), Color.Yellow, bg, CellAttrs.Bold);
        screen.PutString(x, y++, $"{Level,7}".AsSpan(), fg, bg);
        y++;
        screen.PutString(x, y++, "LINES".AsSpan(), Color.Yellow, bg, CellAttrs.Bold);
        screen.PutString(x, y++, $"{Lines,7}".AsSpan(), fg, bg);
    }

    private void DrawIntro(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                       ",
            "         T E T R I S                   ",
            "         a Retro.Crt demo              ",
            "                                       ",
            "    Arrows / WASD     move + soft drop ",
            "    W / Up            rotate           ",
            "    Z                 rotate (back)    ",
            "    SPACE             hard drop        ",
            "    P                 pause            ",
            "    H  ?              help             ",
            "    Esc               quit             ",
            "                                       ",
            "        SPACE  to start                ",
            "                                       ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
    }

    private void DrawGameOver(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                  ",
            "        G A M E   O V E R         ",
            "                                  ",
            $"      Final score: {Score,7}      ",
            $"      Lines:       {Lines,7}      ",
            $"      Level:       {Level,7}      ",
            "                                  ",
            "      R   restart                 ",
            "      Esc quit                    ",
            "                                  ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkRed);
    }

    private void DrawPaused(ScreenBuffer screen)
    {
        var lines = new[] {
            "                              ",
            "          P A U S E           ",
            "                              ",
            "    P   resume                ",
            "    H   help                  ",
            "    Esc quit                  ",
            "                              ",
        };
        DrawCenteredBox(screen, lines, Color.Black, Color.Yellow);
    }

    private void DrawHelp(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                              ",
            "             T E T R I S                      ",
            "                                              ",
            "    Arrows / WASD       move + soft drop      ",
            "    W / Up              rotate clockwise      ",
            "    Z                   rotate ccw            ",
            "    SPACE               hard drop             ",
            "    P                   pause / resume        ",
            "    H  ?                this help             ",
            "    Esc                 quit                  ",
            "                                              ",
            "    Single line clear     100 × (level+1)     ",
            "    Double                300 × (level+1)     ",
            "    Triple                500 × (level+1)     ",
            "    Tetris (4 lines)      800 × (level+1)     ",
            "    Soft drop                  +1 / cell      ",
            "    Hard drop                  +2 / cell      ",
            "                                              ",
            "    Level up every 10 lines — gravity         ",
            "    accelerates each level.                   ",
            "                                              ",
            "         Press any key to close               ",
            "                                              ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
    }

    private void DrawCenteredBox(ScreenBuffer screen, string[] lines, Color fg, Color bg)
    {
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Length > maxW) maxW = lines[i].Length;

        var w  = maxW + 4;
        var h  = lines.Length + 2;
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
