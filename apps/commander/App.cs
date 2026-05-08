using System.Globalization;
using Retro.Crt.Input;

namespace Retro.Crt.Commander;

/// <summary>
/// Norton Commander style two-pane file browser. Header (1 row), two
/// panes side-by-side, footer hint (1 row). One pane is active at a
/// time; Tab swaps. Modals (F1 help, F3 viewer, delete confirm, op
/// progress) draw on top of the chrome and consume input until done.
/// File operations (F4 dup, F5 copy, F6 move, F8 delete) walk the
/// selection (or marked batch) and update an in-place progress bar.
/// </summary>
internal sealed class App
{
    private const int HeaderHeight = 1;
    private const int FooterHeight = 1;
    private const int CopyChunkBytes = 64 * 1024;

    private readonly int    _width;
    private readonly int    _height;
    private readonly Pane   _left;
    private readonly Pane   _right;
    private readonly Action _redraw;

    private bool     _leftActive = true;
    private Viewer?  _viewer;
    private bool     _helpOpen;
    private bool     _confirmDelete;
    private OpState? _op;

    public App(int width, int height, string leftPath, string rightPath, string? rootGuard, Action redraw)
    {
        _width  = width;
        _height = height;
        _redraw = redraw;

        var bodyY  = HeaderHeight;
        var bodyH  = height - HeaderHeight - FooterHeight;
        var leftW  = width / 2;
        var rightW = width - leftW;

        _left  = new Pane(leftPath,  rootGuard) { X = 0,     Y = bodyY, Width = leftW,  Height = bodyH, IsActive = true  };
        _right = new Pane(rightPath, rootGuard) { X = leftW, Y = bodyY, Width = rightW, Height = bodyH, IsActive = false };
    }

    private Pane Active => _leftActive ? _left  : _right;
    private Pane Other  => _leftActive ? _right : _left;

    /// <summary>Returns true when the app should exit.</summary>
    public bool HandleKey(KeyEvent key)
    {
        if (_helpOpen)
        {
            _helpOpen = false;
            return false;
        }

        if (_viewer is not null)
        {
            if (_viewer.HandleKey(key)) _viewer = null;
            return false;
        }

        if (_confirmDelete)
        {
            if (key.Key == Key.Glyph && (key.Glyph is 'y' or 'Y'))
            {
                _confirmDelete = false;
                DoDelete();
            }
            else if (key.Key == Key.Escape || key.Key == Key.Glyph)
            {
                _confirmDelete = false;
            }
            return false;
        }

        // Global keys
        if (key.Key == Key.F10 || key.Key == Key.Escape) return true;
        if (key.Key == Key.Glyph && (key.Glyph is 'q' or 'Q')) return true;

        if (key.Key == Key.F1) { _helpOpen = true;                       return false; }
        if (key.Key == Key.F3) { OpenViewer();                           return false; }
        if (key.Key == Key.F4) { RunCopyOp(removeSource: false, intoOther: false, "Duplicating", Color.DarkMagenta); return false; }
        if (key.Key == Key.F5) { RunCopyOp(removeSource: false, intoOther: true,  "Copying",     Color.DarkGreen);   return false; }
        if (key.Key == Key.F6) { RunCopyOp(removeSource: true,  intoOther: true,  "Moving",      Color.DarkBlue);    return false; }
        if (key.Key == Key.F8) { if (Active.SelectionForOperation().Count > 0) _confirmDelete = true; return false; }

        if (key.Key == Key.Tab) { SwapActive(); return false; }

        if (key.Key == Key.Insert)
        {
            Active.ToggleMarkAtCursor();
            Active.Move(+1);
            return false;
        }
        if (key.Key == Key.Down && (key.Modifiers & KeyModifiers.Shift) != 0)
        {
            Active.ToggleMarkAtCursor();
            Active.Move(+1);
            return false;
        }
        if (key.Key == Key.Up && (key.Modifiers & KeyModifiers.Shift) != 0)
        {
            Active.ToggleMarkAtCursor();
            Active.Move(-1);
            return false;
        }

        switch (key.Key)
        {
            case Key.Up:        Active.Move(-1);        break;
            case Key.Down:      Active.Move(+1);        break;
            case Key.PageUp:    Active.PageMove(-1);    break;
            case Key.PageDown:  Active.PageMove(+1);    break;
            case Key.Home:      Active.HomeEnd(true);   break;
            case Key.End:       Active.HomeEnd(false);  break;
            case Key.Enter:     Active.EnterSelected(); break;
            case Key.Backspace: Active.GoUp();          break;
        }
        return false;
    }

    public bool Tick() => Active.Tick();

    private void SwapActive()
    {
        _leftActive     = !_leftActive;
        _left.IsActive  =  _leftActive;
        _right.IsActive = !_leftActive;
        Active.ResetMarquee();
    }

    private void OpenViewer()
    {
        var path = Active.SelectedFullPath;
        if (path is not null && !Active.SelectedIsDirectory)
            _viewer = Viewer.TryOpen(path, _width, _height);
    }

    public void Draw(ScreenBuffer screen)
    {
        DrawHeader(screen);
        _left.Draw(screen);
        _right.Draw(screen);
        DrawFooter(screen);
        _viewer?.Draw(screen);
        if (_helpOpen)      DrawHelp(screen);
        if (_confirmDelete) DrawDeleteConfirm(screen);
        if (_op is not null) DrawOpModal(screen, _op);
    }

    private void DrawHeader(ScreenBuffer screen)
    {
        screen.FillRect(0, 0, _width, 1, new Cell(' ', Color.Black, Color.LightGray));
        var title = " Retro.Crt.Commander  ·  NC-light tech demo ";
        screen.PutString(0, 0, Truncate(title, _width), Color.Black, Color.LightGray, CellAttrs.Bold);

        var clock = " " + DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture) + " ";
        if (clock.Length + title.Length + 2 <= _width)
            screen.PutString(_width - clock.Length, 0, clock, Color.Black, Color.LightGray, CellAttrs.Bold);
    }

    private void DrawFooter(ScreenBuffer screen)
    {
        var y    = _height - 1;
        screen.FillRect(0, y, _width, 1, new Cell(' ', Color.LightGray, Color.DarkBlue));
        var hint = " F1 Help  F3 View  F4 Dup  F5 Copy  F6 Move  F8 Del  Ins mark  Tab swap  F10 Quit ";
        screen.PutString(0, y, Truncate(hint, _width), Color.LightGray, Color.DarkBlue, CellAttrs.Bold);
    }

    private void DrawHelp(ScreenBuffer screen)
    {
        var lines = new[] {
            "                                              ",
            "       R E T R O   C O M M A N D E R          ",
            "       a Retro.Crt tech demo                  ",
            "                                              ",
            "    Tab             swap active pane          ",
            "    Up / Down       move cursor               ",
            "    Shift+Up/Dn     mark + move               ",
            "    Ins             toggle mark + advance     ",
            "    PgUp / PgDn     move by page              ",
            "    Home / End      first / last entry        ",
            "    Enter           enter directory           ",
            "    Bksp            go up one directory       ",
            "                                              ",
            "    F1              this help                 ",
            "    F3              view selected file        ",
            "    F4              duplicate (in place)      ",
            "    F5              copy → other pane         ",
            "    F6              move → other pane         ",
            "    F8              delete selection / marks  ",
            "    F10 / Esc / q   quit                      ",
            "                                              ",
            "    Long filenames on the focused row scroll  ",
            "    horizontally so you can read them whole.  ",
            "                                              ",
            "          Press any key to close              ",
            "                                              ",
        };
        DrawCenteredBox(screen, lines, Color.White, Color.DarkBlue);
    }

    private void DrawDeleteConfirm(ScreenBuffer screen)
    {
        var count = Active.SelectionForOperation().Count;
        var what  = count == 1 ? "this item" : $"{count} items";
        var lines = new[] {
            "                                          ",
            $"        Delete {what}?",
            "                                          ",
            "    Y to confirm  ·  N / Esc to cancel    ",
            "                                          ",
        };
        // Title bar look: red header strip + body in DarkGray
        DrawTitledBox(screen, lines, " D E L E T E ", Color.White, Color.DarkRed, Color.LightGray, Color.DarkGray);
    }

    private void DrawOpModal(ScreenBuffer screen, OpState op)
    {
        var w  = Math.Min(64, _width  - 6);
        var h  = 9;
        var x0 = (_width  - w) / 2;
        var y0 = (_height - h) / 2;

        var bodyFg  = Color.LightGray;
        var bodyBg  = Color.DarkGray;
        var titleFg = Color.White;
        var titleBg = op.TitleBg;

        screen.FillRect(x0, y0, w, h, new Cell(' ', bodyFg, bodyBg));

        for (var i = 0; i < w; i++) screen[x0 + i, y0] = new Cell(' ', titleFg, titleBg);
        screen.PutString(x0 + 1, y0, Truncate(op.Title, w - 2), titleFg, titleBg, CellAttrs.Bold);

        var rightX  = x0 + w - 1;
        var bottomY = y0 + h - 1;
        for (var y = y0 + 1; y < bottomY; y++)
        {
            screen[x0,     y] = new Cell('│', bodyFg, bodyBg);
            screen[rightX, y] = new Cell('│', bodyFg, bodyBg);
        }
        for (var i = 1; i < w - 1; i++)
            screen[x0 + i, bottomY] = new Cell('─', bodyFg, bodyBg);
        screen[x0,     bottomY] = new Cell('└', bodyFg, bodyBg);
        screen[rightX, bottomY] = new Cell('┘', bodyFg, bodyBg);

        var item = op.CurrentItem;
        if (item.Length > w - 4) item = string.Concat(item.AsSpan(0, w - 5), "…");
        screen.PutString(x0 + 2, y0 + 2, item, Color.LightCyan, bodyBg, CellAttrs.Bold);

        const int PercentSuffixWidth = 5; // " ddd%"
        var barWidth = w - 4 - PercentSuffixWidth;
        var ratio    = (double)op.BytesDone / op.BytesTotal;
        if (ratio < 0) ratio = 0; if (ratio > 1) ratio = 1;
        DrawProgressBar(screen, x0 + 2, y0 + 4, barWidth, ratio, Color.LightGreen, bodyBg);

        var pct    = (int)(ratio * 100);
        var pctStr = string.Create(CultureInfo.InvariantCulture, $" {pct,3}%");
        screen.PutString(x0 + 2 + barWidth, y0 + 4, pctStr, bodyFg, bodyBg);

        var info = string.Create(CultureInfo.InvariantCulture,
            $"{op.ItemIndex} of {op.ItemTotal}  ·  {FormatBytes(op.BytesDone)} / {FormatBytes(op.BytesTotal)}");
        screen.PutString(x0 + 2, y0 + 6, Truncate(info, w - 4), bodyFg, bodyBg);
    }

    private static void DrawProgressBar(ScreenBuffer screen, int x, int y, int width, double ratio, Color fg, Color bg)
    {
        if (width <= 0) return;
        var filled = (int)(ratio * width);
        if (filled < 0)     filled = 0;
        if (filled > width) filled = width;
        // '█' / '░' mirrors Retro.Crt.ProgressBar's glyphs so the demo's
        // bar reads as the same component as the line-mode one.
        for (var i = 0; i < width; i++)
        {
            var ch = i < filled ? '█' : '░';
            screen[x + i, y] = new Cell(ch, fg, bg, CellAttrs.Bold);
        }
    }

    private void DrawCenteredBox(ScreenBuffer screen, string[] lines, Color fg, Color bg)
    {
        var maxW = 0;
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Length > maxW) maxW = lines[i].Length;

        var w  = maxW + 4;
        var h  = lines.Length + 2;
        var x0 = (_width  - w) / 2;
        var y0 = (_height - h) / 2;

        screen.FillRect(x0, y0, w, h, new Cell(' ', fg, bg));
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            var lx = x0 + (w - lines[i].Length) / 2;
            screen.PutString(lx, y0 + 1 + i, lines[i].AsSpan(), fg, bg, CellAttrs.Bold);
        }
    }

    private void DrawTitledBox(ScreenBuffer screen, string[] lines, string title, Color titleFg, Color titleBg, Color bodyFg, Color bodyBg)
    {
        var maxW = title.Length;
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Length > maxW) maxW = lines[i].Length;

        var w  = maxW + 4;
        var h  = lines.Length + 3; // +1 title row, +2 padding
        var x0 = (_width  - w) / 2;
        var y0 = (_height - h) / 2;

        screen.FillRect(x0, y0, w, h, new Cell(' ', bodyFg, bodyBg));
        for (var i = 0; i < w; i++) screen[x0 + i, y0] = new Cell(' ', titleFg, titleBg);
        var titleX = x0 + (w - title.Length) / 2;
        screen.PutString(titleX, y0, title, titleFg, titleBg, CellAttrs.Bold);

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            var lx = x0 + (w - lines[i].Length) / 2;
            screen.PutString(lx, y0 + 2 + i, lines[i].AsSpan(), bodyFg, bodyBg, CellAttrs.Bold);
        }
    }

    private void DoDelete()
    {
        var sources = Active.SelectionForOperation();
        if (sources.Count == 0) return;
        foreach (var s in sources)
        {
            try
            {
                if (Directory.Exists(s)) Directory.Delete(s, recursive: true);
                else if (File.Exists(s)) File.Delete(s);
            }
            catch
            {
                // Swallow — refresh below will reflect what actually got
                // deleted; a status line could surface failures, future
                // polish.
            }
        }
        Active.ClearMarks();
        _left.Refresh();
        _right.Refresh();
    }

    private void RunCopyOp(bool removeSource, bool intoOther, string opLabel, Color titleBg)
    {
        var sources = Active.SelectionForOperation();
        if (sources.Count == 0) return;

        var targetDir = intoOther ? Other.Path : Active.Path;

        // Tally bytes up front so the bar represents real progress, not
        // file-count progress (3 MB binary should dominate, not be one-
        // third of the bar regardless of size).
        long totalBytes = 0;
        foreach (var s in sources) totalBytes += TotalBytesOf(s);

        _op = new OpState
        {
            Title       = $" {opLabel} {sources.Count} item{(sources.Count == 1 ? "" : "s")} ",
            TitleBg     = titleBg,
            CurrentItem = string.Empty,
            BytesDone   = 0,
            BytesTotal  = Math.Max(1, totalBytes),
            ItemIndex   = 0,
            ItemTotal   = sources.Count,
        };
        _redraw();

        for (var i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            _op.CurrentItem = System.IO.Path.GetFileName(src);
            _op.ItemIndex   = i + 1;
            _redraw();

            try
            {
                var dstName = System.IO.Path.GetFileName(src);
                var dstPath = AvoidCollision(System.IO.Path.Combine(targetDir, dstName));
                CopyTreeWithProgress(src, dstPath, bytes =>
                {
                    _op.BytesDone += bytes;
                    _redraw();
                });
                if (removeSource)
                {
                    if (Directory.Exists(src)) Directory.Delete(src, recursive: true);
                    else if (File.Exists(src)) File.Delete(src);
                }
            }
            catch
            {
                // Skip on error — keeps the demo flowing.
            }
        }

        _op = null;
        Active.ClearMarks();
        _left.Refresh();
        _right.Refresh();
        _redraw();
    }

    private static void CopyTreeWithProgress(string src, string dst, Action<long> onBytes)
    {
        if (Directory.Exists(src))
        {
            // Top-level dst is already collision-free (caller picks it
            // via AvoidCollision), so children copy in by name without
            // a second uniqueness check.
            Directory.CreateDirectory(dst);
            foreach (var entry in Directory.EnumerateFileSystemEntries(src))
            {
                var name = System.IO.Path.GetFileName(entry);
                CopyTreeWithProgress(entry, System.IO.Path.Combine(dst, name), onBytes);
            }
            return;
        }
        if (!File.Exists(src)) return;

        using var srcFs = File.OpenRead(src);
        using var dstFs = File.Create(dst);
        var buf = new byte[CopyChunkBytes];
        int n;
        while ((n = srcFs.Read(buf, 0, buf.Length)) > 0)
        {
            dstFs.Write(buf, 0, n);
            onBytes(n);
        }
    }

    private static long TotalBytesOf(string path)
    {
        if (Directory.Exists(path))
        {
            long sum = 0;
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { sum += new FileInfo(f).Length; } catch { /* permission etc. — ignore */ }
            }
            return sum;
        }
        if (File.Exists(path))
        {
            try { return new FileInfo(path).Length; } catch { return 0; }
        }
        return 0;
    }

    /// <summary>
    /// Append " (2)", " (3)", … to the file name (before the extension)
    /// until the path is free. Keeps the demo collision-proof without
    /// pulling up an "overwrite?" dialog every copy.
    /// </summary>
    private static string AvoidCollision(string fullPath)
    {
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) return fullPath;
        var dir       = System.IO.Path.GetDirectoryName(fullPath) ?? string.Empty;
        var nameNoExt = System.IO.Path.GetFileNameWithoutExtension(fullPath);
        var ext       = System.IO.Path.GetExtension(fullPath);
        for (var n = 2; n < 1000; n++)
        {
            var candidate = System.IO.Path.Combine(dir, $"{nameNoExt} ({n}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
        return fullPath; // out of patience; let the caller's exception handler take it
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return string.Create(CultureInfo.InvariantCulture, $"{bytes} B");
        double v = bytes / 1024.0;
        if (v < 1024) return string.Create(CultureInfo.InvariantCulture, $"{v:0.0} KB");
        v /= 1024;
        if (v < 1024) return string.Create(CultureInfo.InvariantCulture, $"{v:0.0} MB");
        v /= 1024;
        return            string.Create(CultureInfo.InvariantCulture, $"{v:0.0} GB");
    }

    private static ReadOnlySpan<char> Truncate(string s, int width)
        => s.Length <= width ? s.AsSpan() : s.AsSpan(0, width);

    /// <summary>
    /// Mutable state for an in-flight copy / move / duplicate. The Draw
    /// pass reads it; the operation loop mutates it between chunks.
    /// </summary>
    private sealed class OpState
    {
        public string Title       { get; init; } = string.Empty;
        public Color  TitleBg     { get; init; }
        public string CurrentItem { get; set;  } = string.Empty;
        public long   BytesDone   { get; set;  }
        public long   BytesTotal  { get; init; }
        public int    ItemIndex   { get; set;  }
        public int    ItemTotal   { get; init; }
    }
}
