using Retro.Crt.Input;

namespace Retro.Crt.MatrixRain;

/// <summary>
/// The classic "digital rain" effect: each terminal column is a falling
/// stream of glyphs whose head glows white and tail fades through green.
/// Per-column cadence (1..3 ticks per cell-step) gives the rain its
/// depth feel without per-cell float math; occasional in-place glyph
/// swaps reproduce the iconic shimmer of characters changing as they
/// fall.
/// </summary>
/// <remarks>
/// Glyph alphabet is half-width katakana (U+FF66..U+FF9D) plus ASCII
/// letters / digits / symbols. Half-width katakana renders one column
/// wide in mainstream terminals — full-width katakana (U+30A0..U+30FF)
/// would split the layout.
/// </remarks>
internal sealed class Rain
{
    private static readonly char[] Alphabet = BuildAlphabet();

    private static char[] BuildAlphabet()
    {
        const string symbols = "@#$%&*+=-:;.<>/?";
        // Half-width katakana block: U+FF66..U+FF9D (56 glyphs).
        // Numeric bounds (not char literals) keep the source pure-ASCII
        // so encoding round-trips on Win/Linux/macOS toolchains can't
        // corrupt the alphabet.
        const int katFirst = 0xFF66;
        const int katLast  = 0xFF9D;
        var katCount = katLast - katFirst + 1;
        var arr = new char[katCount + 26 + 10 + symbols.Length];
        var i = 0;
        for (var c = katFirst; c <= katLast; c++) arr[i++] = (char)c;
        for (var c = 'A';      c <= 'Z';     c++) arr[i++] = c;
        for (var c = '0';      c <= '9';     c++) arr[i++] = c;
        for (var k = 0; k < symbols.Length; k++)  arr[i++] = symbols[k];
        return arr;
    }

    private readonly int _width;
    private readonly int _height;
    private readonly Random _rng;

    private readonly int[]  _head;          // head row; can be < 0 or >= height
    private readonly int[]  _tailLen;       // tail length per column
    private readonly int[]  _ticksPerStep;  // 1 = fast column, 3 = slow column
    private readonly int[]  _ticksLeft;     // countdown to next step
    private readonly char[,] _glyph;        // [col, row] — current glyph at each cell

    private int _tickMs = 60;

    public bool IsPaused   { get; private set; }
    public bool IsHelpOpen { get; private set; }
    public int  TickMs => _tickMs;

    public Rain(int width, int height, int? seed = null)
    {
        _width        = width;
        _height       = height;
        _rng          = seed is { } s ? new Random(s) : new Random();
        _head         = new int[width];
        _tailLen      = new int[width];
        _ticksPerStep = new int[width];
        _ticksLeft    = new int[width];
        _glyph        = new char[width, height];

        for (var x = 0; x < width; x++) ResetColumn(x, initial: true);

        // Open with the help overlay so first-time viewers see the
        // controls without hunting. Any key dismisses it and the rain
        // starts running.
        IsHelpOpen = true;
    }

    private void ResetColumn(int x, bool initial)
    {
        // Initial heads stagger across a wide negative range so columns
        // don't all bloom at once. On replay we restart just above the
        // top so a fresh head streams in immediately.
        _head[x]         = initial
            ? _rng.Next(-_height * 2, _height)
            : -_rng.Next(1, _height + 1);
        _tailLen[x]      = _rng.Next(6, 22);
        _ticksPerStep[x] = _rng.Next(1, 4);
        _ticksLeft[x]    = _rng.Next(1, _ticksPerStep[x] + 1);
        for (var y = 0; y < _height; y++)
            _glyph[x, y] = Alphabet[_rng.Next(Alphabet.Length)];
    }

    public void Step()
    {
        // Help overlay does NOT freeze the rain — the animation keeps
        // streaming behind the box, which is the whole point of the
        // demo. Pause does freeze, intentionally.
        if (IsPaused) return;

        for (var x = 0; x < _width; x++)
        {
            _ticksLeft[x]--;
            if (_ticksLeft[x] > 0) continue;
            _ticksLeft[x] = _ticksPerStep[x];

            _head[x]++;
            if (_head[x] - _tailLen[x] >= _height)
            {
                ResetColumn(x, initial: false);
                continue;
            }

            // Mutate one random on-screen glyph ~25% of column-steps —
            // the "characters keep changing as they fall" shimmer.
            if (_rng.Next(100) < 25)
            {
                var y = _rng.Next(0, _height);
                _glyph[x, y] = Alphabet[_rng.Next(Alphabet.Length)];
            }
        }
    }

    public void HandleKey(KeyEvent key)
    {
        // Help is modal: any key dismisses it and nothing else fires.
        if (IsHelpOpen)
        {
            IsHelpOpen = false;
            return;
        }
        if (key.Key != Key.Glyph) return;

        switch (key.Glyph)
        {
            case ' ':
                IsPaused = !IsPaused;
                break;
            case 'r' or 'R':
                for (var x = 0; x < _width; x++) ResetColumn(x, initial: true);
                IsPaused = false;
                break;
            case '+':
                _tickMs = Math.Max(20, _tickMs - 10);
                break;
            case '-':
                _tickMs = Math.Min(300, _tickMs + 10);
                break;
            case 'h' or 'H' or '?':
                IsHelpOpen = true;
                break;
        }
    }

    public void Draw(ScreenBuffer screen)
    {
        screen.Clear(new Cell(' ', Color.LightGreen, Color.Black));

        for (var x = 0; x < _width; x++)
        {
            var head = _head[x];
            var len  = _tailLen[x];
            var topY = head - len + 1;
            var y0   = topY < 0 ? 0 : topY;
            var y1   = head >= _height ? _height - 1 : head;
            for (var y = y0; y <= y1; y++)
            {
                var distance   = head - y;
                var (fg, attr) = ColorForDistance(distance);
                screen[x, y]   = new Cell(_glyph[x, y], fg, Color.Black, attr);
            }
        }

        if (IsPaused)   DrawCenteredOverlay(screen, PauseLines);
        if (IsHelpOpen) DrawCenteredOverlay(screen, HelpLines);
    }

    private static (Color Fg, CellAttrs Attr) ColorForDistance(int d)
    {
        // Standard16 only — no truecolor fading, but the four-step
        // pyramid (White → LightGreen+bold → LightGreen → DarkGreen)
        // reads as "glow → bright → dim" and matches CRT phosphor.
        if (d == 0) return (Color.White,      CellAttrs.Bold);
        if (d == 1) return (Color.LightGreen, CellAttrs.Bold);
        if (d <= 4) return (Color.LightGreen, CellAttrs.None);
        return (Color.DarkGreen, CellAttrs.None);
    }

    private static readonly string[] PauseLines = {
        "                              ",
        "          P A U S E           ",
        "                              ",
        "    SPACE  resume             ",
        "    H      help               ",
        "    Esc    quit               ",
        "                              ",
    };

    private static readonly string[] HelpLines = {
        "                                          ",
        "         M A T R I X   R A I N            ",
        "                                          ",
        "    SPACE  pause / resume                 ",
        "    R      reseed all columns             ",
        "    +/-    faster / slower                ",
        "    H ?    this help                      ",
        "    Esc    quit                           ",
        "                                          ",
        "          Press any key to close          ",
        "                                          ",
    };

    private void DrawCenteredOverlay(ScreenBuffer screen, string[] lines)
    {
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Length > maxW) maxW = lines[i].Length;

        var w  = maxW + 4;
        var h  = lines.Length + 2;
        var x0 = (_width  - w) / 2;
        var y0 = (_height - h) / 2;

        screen.FillRect(x0, y0, w, h, new Cell(' ', Color.LightGreen, Color.Black));

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            var lineX = x0 + (w - lines[i].Length) / 2;
            screen.PutString(lineX, y0 + 1 + i, lines[i].AsSpan(),
                Color.LightGreen, Color.Black, CellAttrs.Bold);
        }

        // Faint border so the overlay reads against the busy rain.
        var border = new Cell('░', Color.DarkGreen, Color.Black);
        var x1 = x0 + w - 1;
        var y1 = y0 + h - 1;
        for (var x = x0; x <= x1; x++)
        {
            if ((uint)x >= (uint)_width) continue;
            if ((uint)y0 < (uint)_height) screen[x, y0] = border;
            if ((uint)y1 < (uint)_height) screen[x, y1] = border;
        }
        for (var y = y0; y <= y1; y++)
        {
            if ((uint)y >= (uint)_height) continue;
            if ((uint)x0 < (uint)_width) screen[x0, y] = border;
            if ((uint)x1 < (uint)_width) screen[x1, y] = border;
        }
    }
}
