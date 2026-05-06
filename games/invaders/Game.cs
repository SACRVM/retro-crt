using Retro.Crt.Input;

namespace Retro.Crt.Invaders;

/// <summary>
/// Space Invaders-style shooter. An 11×3 grid of three enemy classes
/// marches left-right in lockstep, drops one row at every wall hit,
/// fires back at random, and animates between two frames each step.
/// Three destructible bunkers sit between the player and the formation;
/// bullets erode them cell-by-cell, and any enemy that descends through
/// a bunker eats the cells it overlaps. Game over when the formation
/// touches the player or lives hit zero.
/// </summary>
internal sealed class Game
{
    // Grid dimensions — 11×3 packs the screen the way the arcade
    // original did: one squid row up top, one crab row in the middle,
    // one octopus row at the bottom. (SI's 2-row crabs and 2-row
    // octopuses would push us past most terminal heights.) Sprites are
    // 3 wide × 2 tall, ColSpacing=4 / RowSpacing=3 leaves one-cell
    // gaps between formations.
    private const int Cols = 11;
    private const int Rows = 3;
    private const int ColSpacing = 4;
    private const int RowSpacing = 3;

    // Sprite footprint. Anchor convention: (center-x, top-y).
    // Sprite spans [centerX - SpriteHalfX .. centerX + SpriteHalfX]
    // horizontally and [topY .. topY + SpriteHeight - 1] vertically.
    private const int SpriteWidth  = 3;
    private const int SpriteHeight = 2;
    private const int SpriteHalfX  = SpriteWidth / 2;

    // Sprites — jagged arrays so the per-class frame count is explicit.
    // Player has one frame (no animation); each enemy class has two.
    private static readonly string[][] PlayerSprite =
    {
        new[] { "/^\\", "[#]" },
    };
    // Squid (top row, 30 pts) — arms wave between frames.
    private static readonly string[][] SquidSprite =
    {
        new[] { "\\M/", "|=|" },
        new[] { "/M\\", "|=|" },
    };
    // Crab (middle row, 20 pts) — legs swap stance.
    private static readonly string[][] CrabSprite =
    {
        new[] { "oWo", "\\v/" },
        new[] { "oWo", "/^\\" },
    };
    // Octopus (bottom row, 10 pts) — tentacles ripple.
    private static readonly string[][] OctopusSprite =
    {
        new[] { "(@)", "v_v" },
        new[] { "(@)", "_v_" },
    };

    // Bunkers — three destructible obstacles between player and grid.
    private const int NumBunkers   = 3;
    private const int BunkerWidth  = 5;
    private const int BunkerHeight = 3;
    private const char BunkerGlyph = '▓';

    // Ammo caps. Fixed-cap arrays so steady-state play allocates zero.
    private const int MaxPlayerBullets = 4;
    private const int MaxEnemyBullets  = 6;

    private readonly int _width, _height;
    private readonly int _arenaX0, _arenaY0, _arenaX1, _arenaY1;
    private readonly Random _rng;

    // Player
    private int _playerX;
    private int _fireCd;

    // Enemy grid: alive flags + group offset. Per-enemy world position
    // is `_enemyOffsetX + col * ColSpacing` / `_enemyOffsetY + row *
    // RowSpacing`. Two ints move the whole formation.
    private readonly bool[,] _alive;
    private int _enemyOffsetX;
    private int _enemyOffsetY;
    private int _enemyDir;          // +1 = right, -1 = left
    private int _enemyStepCd;
    private int _enemyStepPeriod;
    private int _aliveCount;
    private int _animFrame;         // toggles 0/1 on each formation step

    // Bullets — Y == -1 means slot is free.
    private readonly int[] _pbX = new int[MaxPlayerBullets];
    private readonly int[] _pbY = new int[MaxPlayerBullets];
    private readonly int[] _ebX = new int[MaxEnemyBullets];
    private readonly int[] _ebY = new int[MaxEnemyBullets];

    // Bunker state. _bunkerCenterX[b] is bunker b's horizontal center
    // (cell column); _bunkerCells[b, col, row] is true while that cell
    // is intact. _bunkerTopY is the same for all bunkers.
    private readonly int[]   _bunkerCenterX = new int[NumBunkers];
    private readonly bool[,,] _bunkerCells = new bool[NumBunkers, BunkerWidth, BunkerHeight];
    private int _bunkerTopY;

    public int  Score      { get; private set; }
    public int  Lives      { get; private set; } = 3;
    public int  Wave       { get; private set; } = 1;
    public bool IsStarted  { get; private set; }
    public bool IsAlive    { get; private set; } = true;
    public bool IsPaused   { get; private set; }
    public bool IsHelpOpen { get; private set; }

    /// <summary>Tick interval in ms — bullets and enemy stepping use this as a base unit. Currently fixed at 50 ms; reserved as a knob for future per-wave tempo tuning.</summary>
    public int TickMs { get; private set; } = 50;

    public Game(int width, int height, int? seed = null)
    {
        _width  = width;
        _height = height;
        _rng    = seed is { } s ? new Random(s) : new Random();

        // Layout rows: 0 HUD, 1 top border, 2..h-3 arena, h-2 bottom
        // border, h-1 footer.
        _arenaX0 = 1;
        _arenaY0 = 2;
        _arenaX1 = width  - 2;
        _arenaY1 = height - 3;

        _playerX = (_arenaX0 + _arenaX1) / 2;

        _alive = new bool[Cols, Rows];
        ResetBullets();
        InitBunkers();
        StartWave(1);
    }

    public void Start() => IsStarted = true;

    private void ResetBullets()
    {
        for (var i = 0; i < MaxPlayerBullets; i++) { _pbX[i] = 0; _pbY[i] = -1; }
        for (var i = 0; i < MaxEnemyBullets;  i++) { _ebX[i] = 0; _ebY[i] = -1; }
        _fireCd = 0;
    }

    /// <summary>
    /// Place the bunkers and fill them solid with a notched bottom (the
    /// classic SI arch silhouette). Called once on Restart — bunkers
    /// persist their damage between waves on purpose.
    /// </summary>
    private void InitBunkers()
    {
        var arenaW = _arenaX1 - _arenaX0 + 1;
        // Distribute (NumBunkers + 1) gaps around NumBunkers bunkers.
        var totalBunkerW = NumBunkers * BunkerWidth;
        var gap          = (arenaW - totalBunkerW) / (NumBunkers + 1);
        for (var b = 0; b < NumBunkers; b++)
        {
            var startX = _arenaX0 + gap * (b + 1) + BunkerWidth * b;
            _bunkerCenterX[b] = startX + BunkerWidth / 2;
            for (var c = 0; c < BunkerWidth; c++)
                for (var r = 0; r < BunkerHeight; r++)
                    _bunkerCells[b, c, r] = true;

            // Notched arch: middle cell of bottom row starts hollow.
            _bunkerCells[b, BunkerWidth / 2, BunkerHeight - 1] = false;
        }

        // Bunkers sit above the player with a 2-row breathing gap so
        // bullets have time to strike them and the player can slide
        // around without instantly clipping into them.
        _bunkerTopY = _arenaY1 - SpriteHeight - 1 - BunkerHeight;
    }

    private void StartWave(int wave)
    {
        Wave = wave;
        _aliveCount = Cols * Rows;
        for (var c = 0; c < Cols; c++)
            for (var r = 0; r < Rows; r++)
                _alive[c, r] = true;

        var gridW = (Cols - 1) * ColSpacing + SpriteWidth;
        _enemyOffsetX = _arenaX0 + SpriteHalfX
                      + (_arenaX1 - _arenaX0 + 1 - gridW) / 2;
        _enemyOffsetY = _arenaY0 + 1;
        _enemyDir     = 1;
        _animFrame    = 0;

        // Speed ramps with wave count and erodes further as enemies
        // die (see RecomputeStepPeriod). Floor at 2 ticks/step so even
        // a near-empty grid stays beatable.
        _enemyStepPeriod = Math.Max(4, 14 - (wave - 1) * 2);
        _enemyStepCd     = _enemyStepPeriod;

        ResetBullets();
    }

    public void Restart()
    {
        Score = 0;
        Lives = 3;
        IsAlive = true;
        IsStarted = false;
        IsPaused = false;
        IsHelpOpen = false;
        _playerX = (_arenaX0 + _arenaX1) / 2;
        InitBunkers();   // restore bunkers on a fresh game
        StartWave(1);
    }

    public void HandleKey(KeyEvent key)
    {
        if (IsHelpOpen)
        {
            IsHelpOpen = false;
            return;
        }

        // Movement is 1 cell per keypress for snappy control. Keyboard
        // auto-repeat handles "hold to slide" without us tracking hold
        // state.
        if (key.Key == Key.Left  || (key.Key == Key.Glyph && (key.Glyph is 'a' or 'A')))
        {
            if (IsAlive && IsStarted && !IsPaused && _playerX > _arenaX0 + SpriteHalfX) _playerX--;
            return;
        }
        if (key.Key == Key.Right || (key.Key == Key.Glyph && (key.Glyph is 'd' or 'D')))
        {
            if (IsAlive && IsStarted && !IsPaused && _playerX < _arenaX1 - SpriteHalfX) _playerX++;
            return;
        }

        if (key.Key != Key.Glyph) return;
        switch (key.Glyph)
        {
            case ' ':
                if (!IsStarted) { Start(); break; }
                if (IsAlive && !IsPaused) Fire();
                break;
            case 'p' or 'P':
                if (IsAlive && IsStarted) IsPaused = !IsPaused;
                break;
            case 'r' or 'R':
                if (!IsAlive) Restart();
                break;
            case 'h' or 'H' or '?':
                IsHelpOpen = true;
                break;
        }
    }

    private void Fire()
    {
        if (_fireCd > 0) return;
        for (var i = 0; i < MaxPlayerBullets; i++)
        {
            if (_pbY[i] >= 0) continue;
            _pbX[i] = _playerX;
            // Player sprite top is at arenaY1 - SpriteHeight + 1, so
            // the first cell *above* the nose is one further up.
            _pbY[i] = _arenaY1 - SpriteHeight;
            _fireCd = 4;
            return;
        }
    }

    public void Step()
    {
        if (!IsStarted || !IsAlive || IsPaused || IsHelpOpen) return;

        if (_fireCd > 0) _fireCd--;

        StepPlayerBullets();
        StepEnemyBullets();
        StepEnemies();
        MaybeEnemyFire();

        if (Lives <= 0)
        {
            IsAlive = false;
            return;
        }
        if (_aliveCount == 0)
        {
            // Wave cleared — bonus, then ramp. Bunkers persist across
            // waves on purpose (authentic SI behaviour); the formation
            // re-spawns full but the player has to make do with
            // whatever cover they didn't get destroyed.
            Score += 50;
            StartWave(Wave + 1);
        }
    }

    private void StepPlayerBullets()
    {
        for (var i = 0; i < MaxPlayerBullets; i++)
        {
            if (_pbY[i] < 0) continue;
            _pbY[i]--;
            if (_pbY[i] < _arenaY0) { _pbY[i] = -1; continue; }
            // Bunker block first — a shot lodged in your own cover
            // shouldn't pass through and wipe a row of squids.
            if (TryHitBunker(_pbX[i], _pbY[i])) { _pbY[i] = -1; continue; }
            if (TryHitEnemy(_pbX[i], _pbY[i]))  { _pbY[i] = -1; }
        }
    }

    private void StepEnemyBullets()
    {
        // Player sprite vertical span: [playerTopY .. arenaY1].
        var playerTopY = _arenaY1 - SpriteHeight + 1;
        for (var i = 0; i < MaxEnemyBullets; i++)
        {
            if (_ebY[i] < 0) continue;
            _ebY[i]++;
            if (_ebY[i] > _arenaY1) { _ebY[i] = -1; continue; }
            if (TryHitBunker(_ebX[i], _ebY[i])) { _ebY[i] = -1; continue; }
            if (_ebY[i] >= playerTopY && Math.Abs(_ebX[i] - _playerX) <= SpriteHalfX)
            {
                _ebY[i] = -1;
                Lives--;
            }
        }
    }

    private void StepEnemies()
    {
        _enemyStepCd--;
        if (_enemyStepCd > 0) return;

        // Would the next horizontal move push any alive enemy's sprite
        // past a wall? Sprite spans [x - SpriteHalfX .. x + SpriteHalfX]
        // around its center; check both edges so we account for the
        // full footprint, not just the anchor cell.
        var nextOffsetX = _enemyOffsetX + _enemyDir;
        var blocked = false;
        for (var r = 0; r < Rows && !blocked; r++)
        {
            for (var c = 0; c < Cols; c++)
            {
                if (!_alive[c, r]) continue;
                var x = nextOffsetX + c * ColSpacing;
                if (x - SpriteHalfX < _arenaX0 || x + SpriteHalfX > _arenaX1)
                {
                    blocked = true;
                    break;
                }
            }
        }

        if (blocked)
        {
            _enemyOffsetY++;
            _enemyDir = -_enemyDir;
        }
        else
        {
            _enemyOffsetX = nextOffsetX;
        }

        // Animation flips on every formation step — same cadence the
        // arcade used, so the player gets a visual pulse aligned with
        // each move (and a faster pulse as the formation thins out).
        _animFrame ^= 1;

        // Eat any bunker cells the enemies now overlap. Authentic SI:
        // descending invaders don't politely phase through your cover.
        ChewBunkersUnderEnemies();

        // Touching the player sprite is instant defeat. Enemy sprite
        // bottom row = enemy_y + SpriteHeight - 1; player sprite top
        // row = arenaY1 - SpriteHeight + 1. They touch when those
        // overlap, i.e. enemy_y >= arenaY1 - 2*SpriteHeight + 2.
        var defeatY = _arenaY1 - 2 * SpriteHeight + 2;
        for (var r = Rows - 1; r >= 0; r--)
        {
            for (var c = 0; c < Cols; c++)
            {
                if (!_alive[c, r]) continue;
                var y = _enemyOffsetY + r * RowSpacing;
                if (y >= defeatY) { IsAlive = false; return; }
            }
        }

        _enemyStepCd = _enemyStepPeriod;
    }

    private void ChewBunkersUnderEnemies()
    {
        for (var r = 0; r < Rows; r++)
        {
            var topY = _enemyOffsetY + r * RowSpacing;
            // Quick reject — if this row's sprite footprint is entirely
            // above the bunker band, no need to scan columns.
            if (topY + SpriteHeight - 1 < _bunkerTopY) continue;
            if (topY > _bunkerTopY + BunkerHeight - 1) continue;

            for (var c = 0; c < Cols; c++)
            {
                if (!_alive[c, r]) continue;
                var ex = _enemyOffsetX + c * ColSpacing;
                // For each cell of the enemy sprite footprint,
                // attempt to clear the matching bunker cell.
                for (var dy = 0; dy < SpriteHeight; dy++)
                    for (var dx = -SpriteHalfX; dx <= SpriteHalfX; dx++)
                        ClearBunkerCellAt(ex + dx, topY + dy);
            }
        }
    }

    private void MaybeEnemyFire()
    {
        // Cap concurrent enemy bullets so the screen never floods —
        // the array size IS the cap; bail out if every slot is busy.
        var freeIdx = -1;
        for (var i = 0; i < MaxEnemyBullets; i++)
        {
            if (_ebY[i] < 0) { freeIdx = i; break; }
        }
        if (freeIdx < 0) return;

        // Target rate scales with formation size — fewer enemies fire
        // less often per tick, but the *per-survivor* rate climbs so
        // late-wave play stays tense.
        if (_rng.Next(100) >= 5 + Wave) return;

        // Pick a random column with a living enemy; that column's
        // bottom-most alive cell is the shooter (occluded shots from
        // higher rows would clip through their own front line).
        var startCol = _rng.Next(Cols);
        for (var k = 0; k < Cols; k++)
        {
            var c = (startCol + k) % Cols;
            for (var r = Rows - 1; r >= 0; r--)
            {
                if (!_alive[c, r]) continue;
                _ebX[freeIdx] = _enemyOffsetX + c * ColSpacing;
                // Bullet appears just below the enemy sprite.
                _ebY[freeIdx] = _enemyOffsetY + r * RowSpacing + SpriteHeight;
                return;
            }
        }
    }

    private bool TryHitEnemy(int x, int y)
    {
        // Brute-force scan: cheap, and stays correct as sprite size or
        // anchor convention changes (the modulo math the cell-grid
        // version used silently broke for multi-cell sprites).
        for (var r = 0; r < Rows; r++)
        {
            var topY = _enemyOffsetY + r * RowSpacing;
            var dy   = y - topY;
            if (dy < 0 || dy >= SpriteHeight) continue;
            for (var c = 0; c < Cols; c++)
            {
                if (!_alive[c, r]) continue;
                var ex = _enemyOffsetX + c * ColSpacing;
                if (Math.Abs(x - ex) > SpriteHalfX) continue;

                _alive[c, r] = false;
                _aliveCount--;
                Score += ScoreForRow(r);
                RecomputeStepPeriod();
                return true;
            }
        }
        return false;
    }

    private bool TryHitBunker(int x, int y)
    {
        var dy = y - _bunkerTopY;
        if (dy < 0 || dy >= BunkerHeight) return false;
        for (var b = 0; b < NumBunkers; b++)
        {
            var dx = x - (_bunkerCenterX[b] - BunkerWidth / 2);
            if ((uint)dx >= (uint)BunkerWidth) continue;
            if (!_bunkerCells[b, dx, dy]) continue;
            _bunkerCells[b, dx, dy] = false;
            return true;
        }
        return false;
    }

    private void ClearBunkerCellAt(int x, int y)
    {
        var dy = y - _bunkerTopY;
        if (dy < 0 || dy >= BunkerHeight) return;
        for (var b = 0; b < NumBunkers; b++)
        {
            var dx = x - (_bunkerCenterX[b] - BunkerWidth / 2);
            if ((uint)dx >= (uint)BunkerWidth) continue;
            _bunkerCells[b, dx, dy] = false;
            return;
        }
    }

    private static int ScoreForRow(int r) => r switch
    {
        0 => 30,   // squid (top)    — fast, small, worth the most
        1 => 20,   // crab  (middle) — workhorse
        _ => 10,   // octopus (bot)  — chunky front line
    };

    private void RecomputeStepPeriod()
    {
        // Bigger formations move slowly; as enemies die the period
        // shrinks toward a wave-dependent floor. Linear in alive-count
        // for predictability.
        var basePeriod = Math.Max(4, 14 - (Wave - 1) * 2);
        var minPeriod  = Math.Max(2, basePeriod / 3);
        var ratio      = (double)_aliveCount / (Cols * Rows);
        _enemyStepPeriod = Math.Max(minPeriod, (int)(basePeriod * ratio));
        if (_enemyStepCd > _enemyStepPeriod) _enemyStepCd = _enemyStepPeriod;
    }

    public void Draw(ScreenBuffer screen)
    {
        screen.Clear(new Cell(' ', Color.LightGray, Color.Black));
        DrawHud(screen);
        DrawFooter(screen);
        DrawBorder(screen);
        DrawBunkers(screen);
        DrawEnemies(screen);
        DrawBullets(screen);
        DrawPlayer(screen);
        if (!IsStarted)  DrawIntro(screen);
        if (!IsAlive)    DrawGameOver(screen);
        if (IsHelpOpen)  DrawHelp(screen);
    }

    private void DrawHud(ScreenBuffer screen)
    {
        var label = $" Invaders  ·  Wave {Wave,2}  ·  Score {Score,5}  ·  Lives {Lives} ";
        var clipped = label.Length > _width ? label.AsSpan(0, _width) : label.AsSpan();
        screen.FillRect(0, 0, _width, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
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
                : (" Arrows / AD move  ·  SPACE fire  ·  P pause  ·  H help  ·  Esc quit ",
                   Color.LightGray, Color.DarkBlue);

        var y = _height - 1;
        screen.FillRect(0, y, _width, 1, new Cell(' ', fg, bg));
        var clipped = text.Length > _width ? text.AsSpan(0, _width) : text.AsSpan();
        screen.PutString(0, y, clipped, fg, bg, CellAttrs.Bold);
    }

    private void DrawBorder(ScreenBuffer screen)
    {
        var c = new Cell('░', Color.DarkGray, Color.Black);
        var topY    = 1;
        var bottomY = _height - 2;
        for (var x = 0; x < _width; x++)
        {
            screen[x, topY]    = c;
            screen[x, bottomY] = c;
        }
        for (var y = topY; y <= bottomY; y++)
        {
            screen[0, y]            = c;
            screen[_width - 1, y]   = c;
        }
    }

    private void DrawBunkers(ScreenBuffer screen)
    {
        var cell = new Cell(BunkerGlyph, Color.LightGreen, Color.Black, CellAttrs.Bold);
        for (var b = 0; b < NumBunkers; b++)
        {
            var startX = _bunkerCenterX[b] - BunkerWidth / 2;
            for (var c = 0; c < BunkerWidth; c++)
                for (var r = 0; r < BunkerHeight; r++)
                {
                    if (!_bunkerCells[b, c, r]) continue;
                    var x = startX + c;
                    var y = _bunkerTopY + r;
                    if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                        screen[x, y] = cell;
                }
        }
    }

    private void DrawEnemies(ScreenBuffer screen)
    {
        for (var r = 0; r < Rows; r++)
        {
            var (sprites, color) = EnemyLook(r);
            var frame = sprites[_animFrame % sprites.Length];
            for (var c = 0; c < Cols; c++)
            {
                if (!_alive[c, r]) continue;
                var x = _enemyOffsetX + c * ColSpacing;
                var y = _enemyOffsetY + r * RowSpacing;
                PutSprite(screen, x, y, frame, color);
            }
        }
    }

    private static (string[][] Frames, Color Color) EnemyLook(int row) => row switch
    {
        0 => (SquidSprite,   Color.LightRed),       // top    — squid
        1 => (CrabSprite,    Color.LightMagenta),   // middle — crab
        _ => (OctopusSprite, Color.LightCyan),      // bottom — octopus
    };

    private static void PutSprite(ScreenBuffer screen, int centerX, int topY, string[] rows, Color fg)
    {
        var startX = centerX - rows[0].Length / 2;
        for (var ry = 0; ry < rows.Length; ry++)
        {
            var y = topY + ry;
            if ((uint)y >= (uint)screen.Height) continue;
            var row = rows[ry];
            for (var i = 0; i < row.Length; i++)
            {
                var x = startX + i;
                if ((uint)x >= (uint)screen.Width) continue;
                if (row[i] == ' ') continue;  // transparent cells preserve background
                screen[x, y] = new Cell(row[i], fg, Color.Black, CellAttrs.Bold);
            }
        }
    }

    private void DrawBullets(ScreenBuffer screen)
    {
        for (var i = 0; i < MaxPlayerBullets; i++)
        {
            if (_pbY[i] < 0) continue;
            screen[_pbX[i], _pbY[i]] = new Cell('|', Color.Yellow, Color.Black, CellAttrs.Bold);
        }
        for (var i = 0; i < MaxEnemyBullets; i++)
        {
            if (_ebY[i] < 0) continue;
            screen[_ebX[i], _ebY[i]] = new Cell('*', Color.LightRed, Color.Black, CellAttrs.Bold);
        }
    }

    private void DrawPlayer(ScreenBuffer screen)
    {
        // Player anchor is the sprite's TOP-y; bottom row sits on the
        // arena floor at arenaY1.
        PutSprite(screen, _playerX, _arenaY1 - SpriteHeight + 1, PlayerSprite[0], Color.LightGreen);
    }

    private void DrawIntro(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                       ",
            "         I N V A D E R S               ",
            "         a Retro.Crt demo              ",
            "                                       ",
            "    Arrows  /  A D     move            ",
            "    SPACE              fire            ",
            "    P                  pause           ",
            "    H  ?               help            ",
            "    Esc                quit            ",
            "                                       ",
            "        SPACE  to launch               ",
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
            $"      Final score:  {Score,5}        ",
            $"      Wave reached: {Wave,5}        ",
            "                                  ",
            "      R   restart                 ",
            "      Esc quit                    ",
            "                                  ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkRed);
    }

    private void DrawHelp(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                              ",
            "             I N V A D E R S                  ",
            "                                              ",
            "    Arrows  /  A D       move ship            ",
            "    SPACE                fire                 ",
            "    P                    pause / resume       ",
            "    H  ?                 this help            ",
            "    Esc                  quit                 ",
            "                                              ",
            "    Squid     \\M/        30 pts               ",
            "    Crab      oWo        20 pts               ",
            "    Octopus   (@)        10 pts               ",
            "    Wave clear           +50 bonus            ",
            "                                              ",
            "    Bunkers absorb shots and erode where       ",
            "    enemies pass through them.                ",
            "                                              ",
            "         Press any key to close               ",
            "                                              ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
    }

    private void DrawCenteredBox(ScreenBuffer screen, string[] lines, Color fg, Color bg)
    {
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++) if (lines[i].Length > maxW) maxW = lines[i].Length;
        var w  = maxW + 4;
        var h  = lines.Length + 2;
        var x0 = (_width  - w) / 2;
        var y0 = (_height - h) / 2;

        screen.FillRect(x0, y0, w, h, new Cell(' ', fg, bg));
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            var lineX = x0 + (w - lines[i].Length) / 2;
            screen.PutString(lineX, y0 + 1 + i, lines[i].AsSpan(), fg, bg, CellAttrs.Bold);
        }
    }
}
