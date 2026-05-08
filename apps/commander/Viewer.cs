using System.Text;
using Retro.Crt.Input;

namespace Retro.Crt.Commander;

/// <summary>
/// F3 text viewer: opens the selected file in a centered modal panel,
/// scrollable with arrows / PgUp / PgDn / Home / End. Reads up to 256 KB
/// to keep the demo snappy on huge logs; a clean truncation indicator
/// says so. Files containing a NUL byte are flagged as binary and the
/// viewer renders a small "preview not supported" panel instead of
/// emitting noise into the terminal.
/// </summary>
internal sealed class Viewer
{
    private const int MaxBytes = 256 * 1024;

    private readonly string   _path;
    private readonly long     _fileSize;
    private readonly bool     _truncated;
    private readonly bool     _isBinary;
    private readonly string[] _lines;

    private readonly int _x;
    private readonly int _y;
    private readonly int _width;
    private readonly int _height;

    private int _scrollTop;
    private int _hScroll;

    public static Viewer? TryOpen(string path, int screenWidth, int screenHeight)
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || (info.Attributes & FileAttributes.Directory) != 0) return null;

            var len  = (int)Math.Min(info.Length, MaxBytes);
            var buf  = new byte[len];
            using (var fs = info.OpenRead())
                fs.ReadExactly(buf, 0, len);

            var isBinary = false;
            for (var i = 0; i < buf.Length; i++)
            {
                if (buf[i] == 0) { isBinary = true; break; }
            }

            string[] lines;
            if (isBinary)
            {
                lines = [];
            }
            else
            {
                var text = Encoding.UTF8.GetString(buf);
                lines    = text.Split('\n');
                // Strip trailing \r — keeps Windows files from showing a
                // stray ^M-equivalent at the end of every line.
                for (var i = 0; i < lines.Length; i++)
                    if (lines[i].Length > 0 && lines[i][^1] == '\r')
                        lines[i] = lines[i][..^1];
            }

            return new Viewer(path, info.Length, info.Length > MaxBytes, isBinary, lines, screenWidth, screenHeight);
        }
        catch
        {
            // Permission denied, in-use lock, transient I/O — treat any
            // failure as "no viewer" and let the user keep navigating.
            return null;
        }
    }

    private Viewer(string path, long fileSize, bool truncated, bool isBinary, string[] lines, int screenWidth, int screenHeight)
    {
        _path      = path;
        _fileSize  = fileSize;
        _truncated = truncated;
        _isBinary  = isBinary;
        _lines     = lines;

        // Take ~80% of the screen, but never less than 40×10 so a small
        // terminal still gets a usable preview.
        _width  = Math.Clamp(screenWidth  * 4 / 5, 40, screenWidth  - 4);
        _height = Math.Clamp(screenHeight * 4 / 5, 10, screenHeight - 4);
        _x      = (screenWidth  - _width)  / 2;
        _y      = (screenHeight - _height) / 2;
    }

    /// <summary>Returns true when the viewer should close.</summary>
    public bool HandleKey(KeyEvent key)
    {
        switch (key.Key)
        {
            case Key.Escape:   return true;
            case Key.F3:       return true;
            case Key.F10:      return true;
            case Key.Up:       _scrollTop = Math.Max(0, _scrollTop - 1);                return false;
            case Key.Down:     _scrollTop = Math.Min(MaxScroll, _scrollTop + 1);        return false;
            case Key.PageUp:   _scrollTop = Math.Max(0, _scrollTop - PageHeight);       return false;
            case Key.PageDown: _scrollTop = Math.Min(MaxScroll, _scrollTop + PageHeight); return false;
            case Key.Home:     _scrollTop = 0; _hScroll = 0;                            return false;
            case Key.End:      _scrollTop = MaxScroll;                                  return false;
            case Key.Left:     _hScroll   = Math.Max(0, _hScroll - 4);                  return false;
            case Key.Right:    _hScroll  += 4;                                          return false;
        }
        if (key.Key == Key.Glyph && (key.Glyph is 'q' or 'Q')) return true;
        return false;
    }

    private int PageHeight => Math.Max(1, _height - 4);
    private int MaxScroll  => Math.Max(0, _lines.Length - PageHeight);

    public void Draw(ScreenBuffer screen)
    {
        // Viewer uses a black/cyan terminal-CRT palette so it visually
        // detaches from the panes' DOS-blue. Title bar in DarkRed gives
        // a "you are now in viewer mode" cue you can read at a glance.
        var fg      = Color.LightCyan;
        var bg      = Color.Black;
        var titleBg = Color.DarkRed;
        var titleFg = Color.White;

        screen.FillRect(_x, _y, _width, _height, new Cell(' ', fg, bg));

        var rightX  = _x + _width  - 1;
        var bottomY = _y + _height - 1;
        for (var x = _x + 1; x < rightX; x++)
        {
            screen[x, _y]      = new Cell(' ', titleFg, titleBg);
            screen[x, bottomY] = new Cell('─', fg, bg);
        }
        for (var y = _y + 1; y < bottomY; y++)
        {
            screen[_x,     y] = new Cell('│', fg, bg);
            screen[rightX, y] = new Cell('│', fg, bg);
        }
        screen[_x,     _y]      = new Cell(' ', titleFg, titleBg);
        screen[rightX, _y]      = new Cell(' ', titleFg, titleBg);
        screen[_x,     bottomY] = new Cell('└', fg, bg);
        screen[rightX, bottomY] = new Cell('┘', fg, bg);

        var nameOnly = System.IO.Path.GetFileName(_path);
        var title    = _truncated
            ? $" {nameOnly}  (first {MaxBytes / 1024} KB of {FormatBytes(_fileSize)}) "
            : $" {nameOnly} ";
        if (title.Length > _width - 2) title = title[..(_width - 2)];
        screen.PutString(_x + 1, _y, title, titleFg, titleBg, CellAttrs.Bold);

        var indicator = _isBinary
            ? $" binary · {FormatBytes(_fileSize)} "
            : $" line {_scrollTop + 1}/{_lines.Length}  ·  col {_hScroll + 1} ";
        var closeHint = " Esc to close ";
        if (indicator.Length + closeHint.Length + 2 <= _width)
        {
            screen.PutString(_x + 2, bottomY, indicator, fg, bg);
            screen.PutString(rightX - 1 - closeHint.Length, bottomY, closeHint, fg, bg);
        }

        if (_isBinary)
        {
            DrawBinaryNotice(screen, fg, bg);
            return;
        }

        DrawText(screen, fg, bg);
    }

    private void DrawText(ScreenBuffer screen, Color fg, Color bg)
    {
        var bodyX = _x + 1;
        var bodyY = _y + 1;
        var bodyW = _width  - 2;
        var bodyH = _height - 2;

        for (var r = 0; r < bodyH; r++)
        {
            var line = _scrollTop + r;
            if (line >= _lines.Length) break;

            var s = _lines[line];
            if (_hScroll < s.Length)
            {
                var slice = s.AsSpan(_hScroll);
                if (slice.Length > bodyW) slice = slice[..bodyW];
                screen.PutString(bodyX, bodyY + r, slice, fg, bg);
            }
        }
    }

    private void DrawBinaryNotice(ScreenBuffer screen, Color fg, Color bg)
    {
        var msg1 = "Binary file — text preview not supported.";
        var msg2 = $"Size: {FormatBytes(_fileSize)}";
        var cx   = _x + _width  / 2;
        var cy   = _y + _height / 2;
        screen.PutString(cx - msg1.Length / 2, cy - 1, msg1, Color.Yellow, bg, CellAttrs.Bold);
        screen.PutString(cx - msg2.Length / 2, cy + 1, msg2, fg,           bg);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double v = bytes / 1024.0;
        if (v < 1024) return $"{v:0.0} KB";
        v /= 1024;
        if (v < 1024) return $"{v:0.0} MB";
        v /= 1024;
        return $"{v:0.0} GB";
    }
}
