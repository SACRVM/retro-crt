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
    public int Length => _body.Count;
    public bool IsAlive { get; private set; } = true;
    public bool IsPaused { get; private set; }

    public Game(int width, int height, int? seed = null)
    {
        _width  = width;
        _height = height;
        _rng    = seed is { } s ? new Random(s) : new Random();

        _arenaX0 = 1;
        _arenaY0 = 2;
        _arenaX1 = width  - 2;
        _arenaY1 = height - 2;

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
        switch (key.Key)
        {
            case Key.Up:    Queue(Direction.Up);    break;
            case Key.Down:  Queue(Direction.Down);  break;
            case Key.Left:  Queue(Direction.Left);  break;
            case Key.Right: Queue(Direction.Right); break;
            case Key.Glyph when key.Glyph is 'p' or 'P':
                if (IsAlive) IsPaused = !IsPaused;
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
        if (!IsAlive || IsPaused) return;

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
        DrawBorder(screen);
        DrawApple(screen);
        DrawSnake(screen);
        if (IsPaused) DrawCenterBanner(screen, "  PAUSE  ", Color.Black, Color.Yellow);
        if (!IsAlive) DrawGameOver(screen);
    }

    private void DrawHud(ScreenBuffer screen)
    {
        var label = $" Snake  ·  Score {Score}  ·  Length {_body.Count}  ·  ↑↓←→ move  ·  P pause  ·  Esc quit ";
        var clipped = label.Length > _width ? label.AsSpan(0, _width) : label.AsSpan();
        screen.FillRect(0, 0, _width, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
        screen.PutString(0, 0, clipped, Color.LightGray, Color.DarkBlue, CellAttrs.Bold);
    }

    private void DrawBorder(ScreenBuffer screen)
    {
        var c = new Cell('░', Color.DarkGray, Color.Black);
        for (var x = 0; x < _width; x++)
        {
            screen[x, 1]            = c;
            screen[x, _height - 1]  = c;
        }
        for (var y = 1; y < _height; y++)
        {
            screen[0, y]            = c;
            screen[_width - 1, y]   = c;
        }
    }

    private void DrawApple(ScreenBuffer screen)
    {
        screen[_apple.X, _apple.Y] = new Cell('*', Color.LightRed, Color.Black, CellAttrs.Bold);
    }

    private void DrawSnake(ScreenBuffer screen)
    {
        var first = true;
        foreach (var seg in _body)
        {
            // Head is '@' (classic roguelike, ASCII-safe everywhere);
            // body is '█' (CP437 full block, fine in every console
            // font Chloe tested). Avoiding U+25C9/U+25CF made the
            // glyphs survive on fonts that don't ship with the
            // miscellaneous-symbols block.
            var glyph = first ? '@' : '█';
            var fg    = first ? Color.LightGreen : Color.DarkGreen;
            var attr  = first ? CellAttrs.Bold : CellAttrs.None;
            screen[seg.X, seg.Y] = new Cell(glyph, fg, Color.Black, attr);
            first = false;
        }
    }

    private void DrawCenterBanner(ScreenBuffer screen, string text, Color fg, Color bg)
    {
        var x = (_width  - text.Length) / 2;
        var y = _height / 2;
        screen.PutString(x, y, text.AsSpan(), fg, bg, CellAttrs.Bold);
    }

    private void DrawGameOver(ScreenBuffer screen)
    {
        var lines = new[] {
            "  GAME OVER  ",
            $"  Final Score: {Score}  ",
            $"  Length: {_body.Count}  ",
            "",
            "  R restart  ·  Esc quit  ",
        };
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++) if (lines[i].Length > maxW) maxW = lines[i].Length;
        var w = maxW + 4;
        var h = lines.Length + 2;
        var x0 = (_width  - w) / 2;
        var y0 = (_height - h) / 2;

        screen.FillRect(x0, y0, w, h, new Cell(' ', Color.White, Color.DarkRed));
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            var lineX = x0 + (w - lines[i].Length) / 2;
            screen.PutString(lineX, y0 + 1 + i, lines[i].AsSpan(), Color.White, Color.DarkRed, CellAttrs.Bold);
        }
    }
}
