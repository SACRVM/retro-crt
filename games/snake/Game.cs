using Retro.Crt.Input;

namespace Retro.Crt.Snake;

internal enum Direction { Up, Down, Left, Right }

/// <summary>
/// Snake game state + tick stepper. Owns the play arena rectangle, the
/// snake body (head at <c>First</c>), the apple, and the score. Pure
/// model — render is read-only via <see cref="Draw"/>; input feeds
/// through <see cref="HandleKey"/> which only queues a pending
/// direction. Movement happens on <see cref="Step"/> at whatever cadence
/// the host loop chooses.
/// </summary>
internal sealed class Game
{
    private readonly int _width;
    private readonly int _height;
    private readonly Random _rng;

    // Arena coords are inclusive bounds inside the border. Row 0 is the
    // HUD strip; row 1 is the top border; rows _arenaY0.._arenaY1 are
    // playable. Same idea for X with a 1-cell border each side.
    private readonly int _arenaX0, _arenaY0, _arenaX1, _arenaY1;

    private readonly LinkedList<(int X, int Y)> _body = new();
    private Direction _direction;
    private Direction _pending;
    private (int X, int Y) _apple;

    public int Score { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public bool IsPaused { get; private set; }
    public bool IsStarted { get; private set; }

    /// <summary>Begin ticking. Called by the host once the player presses Space on the intro screen.</summary>
    public void Start() => IsStarted = true;

    public Game(int width, int height, int? seed = null)
    {
        _width  = width;
        _height = height;
        _rng    = seed is { } s ? new Random(s) : new Random();

        // Layout rows: 0 HUD, 1 top border, 2..h-3 arena, h-2 footer,
        // h-1 bottom border.
        _arenaX0 = 1;
        _arenaY0 = 2;
        _arenaX1 = width  - 2;
        _arenaY1 = height - 3;

        // Spawn 4-segment snake centered, moving right. Tail behind head
        // so the first Step doesn't collide with itself.
        var cx = (_arenaX0 + _arenaX1) / 2;
        var cy = (_arenaY0 + _arenaY1) / 2;
        for (var i = 0; i < 4; i++) _body.AddLast((cx - i, cy));
        _direction = Direction.Right;
        _pending   = Direction.Right;

        _apple = SpawnApple();
    }

    /// <summary>
    /// Tick interval in milliseconds. Speeds up as the snake grows so
    /// late game has bite. Floor at 60 ms (~16 ticks/sec).
    /// </summary>
    public int TickMs
    {
        get
        {
            var ms = 130 - Score * 4;
            return ms < 60 ? 60 : ms;
        }
    }

    public void HandleKey(KeyEvent key)
    {
        if (key.Key != Key.Glyph) return;

        switch (key.Glyph)
        {
            case 'w' or 'W': Queue(Direction.Up);    break;
            case 's' or 'S': Queue(Direction.Down);  break;
            case 'a' or 'A': Queue(Direction.Left);  break;
            case 'd' or 'D': Queue(Direction.Right); break;
            case 'p' or 'P':
                if (IsAlive && IsStarted) IsPaused = !IsPaused;
                break;
        }
    }

    private void Queue(Direction d)
    {
        // Block 180° reversal — it would self-collide on the next Step
        // and the player would just have lost without intending to.
        if ((_direction == Direction.Up    && d == Direction.Down)  ||
            (_direction == Direction.Down  && d == Direction.Up)    ||
            (_direction == Direction.Left  && d == Direction.Right) ||
            (_direction == Direction.Right && d == Direction.Left))
            return;
        _pending = d;
    }

    public void Step()
    {
        if (!IsStarted || !IsAlive || IsPaused) return;

        _direction = _pending;
        var head = _body.First!.Value;
        (int X, int Y) next = _direction switch
        {
            Direction.Up    => (head.X,     head.Y - 1),
            Direction.Down  => (head.X,     head.Y + 1),
            Direction.Left  => (head.X - 1, head.Y),
            Direction.Right => (head.X + 1, head.Y),
            _               => head,
        };

        if (next.X < _arenaX0 || next.X > _arenaX1 ||
            next.Y < _arenaY0 || next.Y > _arenaY1)
        {
            IsAlive = false;
            return;
        }

        // Self collision. The tail vacates this tick *unless* we ate an
        // apple — so when not growing, the last segment is fair game.
        var ate = next == _apple;
        var tail = _body.Last!.Value;
        foreach (var seg in _body)
        {
            if (!ate && seg == tail) continue;
            if (seg == next) { IsAlive = false; return; }
        }

        _body.AddFirst(next);
        if (ate)
        {
            Score++;
            _apple = SpawnApple();
        }
        else
        {
            _body.RemoveLast();
        }
    }

    private (int X, int Y) SpawnApple()
    {
        // Naive rejection sampling. Fine until the snake covers most of
        // the arena, which is "you basically won" territory anyway.
        while (true)
        {
            var x = _rng.Next(_arenaX0, _arenaX1 + 1);
            var y = _rng.Next(_arenaY0, _arenaY1 + 1);
            var pt = (x, y);
            var clash = false;
            foreach (var seg in _body) if (seg == pt) { clash = true; break; }
            if (!clash) return pt;
        }
    }

    public void Draw(ScreenBuffer screen)
    {
        screen.Clear(new Cell(' ', Color.LightGray, Color.Black));
        DrawHud(screen);
        DrawFooter(screen);
        DrawBorder(screen);
        DrawApple(screen);
        DrawSnake(screen);
        if (!IsStarted) DrawIntro(screen);
        if (!IsAlive)   DrawGameOver(screen);
    }

    private void DrawHud(ScreenBuffer screen)
    {
        // Right-pad Score so the row width is stable across digit
        // roll-overs (e.g., 9 → 10 → 100).
        var label = $" Snake  ·  Score {Score,4} ";
        var clipped = label.Length > _width ? label.AsSpan(0, _width) : label.AsSpan();
        screen.FillRect(0, 0, _width, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
        screen.PutString(0, 0, clipped, Color.LightGray, Color.DarkBlue, CellAttrs.Bold);
    }

    private void DrawFooter(ScreenBuffer screen)
    {
        // Footer at row h-1 (very bottom, mirrors HUD at y=0).
        var (text, fg, bg) = !IsAlive
            ? ("  R restart  ·  Esc quit  ",
               Color.White, Color.DarkRed)
            : IsPaused
                ? ("  PAUSE  ·  P resume  ·  Esc quit  ",
                   Color.Black, Color.Yellow)
                : (" WASD move  ·  P pause  ·  Esc quit ",
                   Color.LightGray, Color.DarkBlue);

        var y = _height - 1;
        screen.FillRect(0, y, _width, 1, new Cell(' ', fg, bg));
        var clipped = text.Length > _width ? text.AsSpan(0, _width) : text.AsSpan();
        screen.PutString(0, y, clipped, fg, bg, CellAttrs.Bold);
    }

    private void DrawBorder(ScreenBuffer screen)
    {
        // Top border at y=1, bottom border at y=h-2. Side columns run
        // between them only — y=h-1 is the footer (no border), y=0 is
        // the HUD (also no border above). Symmetric framing.
        var c = new Cell('░', Color.DarkGray, Color.Black);
        var bottomY = _height - 2;
        for (var x = 0; x < _width; x++)
        {
            screen[x, 1]       = c;
            screen[x, bottomY] = c;
        }
        for (var y = 1; y <= bottomY; y++)
        {
            screen[0, y]            = c;
            screen[_width - 1, y]   = c;
        }
    }

    private void DrawApple(ScreenBuffer screen)
    {
        screen[_apple.X, _apple.Y] = new Cell('@', Color.LightRed, Color.Black, CellAttrs.Bold);
    }

    private void DrawSnake(ScreenBuffer screen)
    {
        var first = true;
        foreach (var seg in _body)
        {
            // Head is a yellow block; body is a green block. Same glyph
            // ('█', CP437 full block) for both — color carries the
            // distinction. Bold on the head bumps the yellow toward
            // bright yellow on terminals that distinguish.
            var fg = first ? Color.Yellow : Color.DarkGreen;
            var attr = first ? CellAttrs.Bold : CellAttrs.None;
            screen[seg.X, seg.Y] = new Cell('█', fg, Color.Black, attr);
            first = false;
        }
    }

    private void DrawGameOver(ScreenBuffer screen)
    {
        var lines = new[] {
            "  GAME OVER  ",
            $"  Final Score: {Score}  ",
            "",
            "  R restart  ·  Esc quit  ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkRed);
    }

    private void DrawIntro(ScreenBuffer screen)
    {
        var lines = new[] {
            "                            ",
            "          S N A K E         ",
            "                            ",
            "   Press SPACE to start     ",
            "                            ",
            "   WASD move · P pause      ",
            "   Esc quit                 ",
            "                            ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
    }

    private void DrawCenteredBox(ScreenBuffer screen, string[] lines, Color fg, Color bg)
    {
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++) if (lines[i].Length > maxW) maxW = lines[i].Length;
        var w = maxW + 4;
        var h = lines.Length + 2;
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
