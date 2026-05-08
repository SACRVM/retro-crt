using System.Globalization;

namespace Retro.Crt.Commander;

/// <summary>
/// One Norton Commander style file pane: a bordered box with the current
/// directory in the top frame, a vertically scrolling listing in the
/// body, and an item-count + total-size summary in the bottom frame.
/// Active panes get a double-line border + a highlighted selection bar;
/// the inactive pane reads as quiet single-line chrome. Long filenames
/// on the selected row scroll horizontally (marquee). A multi-select
/// "marks" overlay (yellow text) lets a batch of files participate in
/// copy / move / delete operations.
/// </summary>
internal sealed class Pane
{
    private static readonly char[] PathSeparators = ['/', '\\'];

    /// <summary>Tape separator that loops between repetitions of the marquee'd name.</summary>
    private const string MarqueeSeparator = "   ·   ";

    /// <summary>50 ms tick × 20 = ~1 s pause at offset 0 each lap so the head of the name stays readable.</summary>
    private const int MarqueeHoldFrames = 20;

    /// <summary>50 ms tick × 3 = ~150 ms per character step — readable but not lazy.</summary>
    private const int MarqueeFramesPerChar = 3;

    /// <summary>Right column reserved for "&lt;DIR&gt;" / size text. 5 chars covers "&lt;DIR&gt;", "1.2K", "12.3K", "234K", "1.2M".</summary>
    private const int SizeColumnWidth = 5;

    /// <summary>"MMM dd HH:mm" → "May 08 14:32".</summary>
    private const int DateColumnWidth = 12;

    /// <summary>Below this interior width the date column is dropped to keep the name readable.</summary>
    /// <remarks>36 cells = 12 date + 1 gap + 5 size + 2 pads + 1 gap = 21 fixed, leaves 15 for the name.</remarks>
    private const int DateColumnMinInteriorWidth = 36;

    public int  X      { get; set; }
    public int  Y      { get; set; }
    public int  Width  { get; set; }
    public int  Height { get; set; }

    public bool IsActive { get; set; }

    private readonly List<FileEntry> _entries = [];
    private readonly HashSet<int>    _marked  = [];
    private readonly string?         _rootGuard;
    private string  _path        = string.Empty;
    private string? _loadError;
    private long    _totalSize;
    private int     _selected;
    private int     _scrollTop;

    private int _marqueeFrame;
    private int _marqueeOffset;

    public Pane(string path, string? rootGuard = null)
    {
        _rootGuard = rootGuard is null ? null : System.IO.Path.GetFullPath(rootGuard).TrimEnd(PathSeparators);
        Navigate(path);
    }

    public string Path => _path;

    /// <summary>Path of the currently highlighted entry, or <c>null</c> if the listing is empty / errored / on the parent link.</summary>
    public string? SelectedFullPath
    {
        get
        {
            if (_selected < 0 || _selected >= _entries.Count) return null;
            var e = _entries[_selected];
            if (e.IsParentLink) return null;
            return System.IO.Path.Combine(_path, e.Name);
        }
    }

    public bool SelectedIsDirectory
        => _selected >= 0 && _selected < _entries.Count && _entries[_selected].IsDirectory;

    /// <summary>Number of marked entries (excluding the parent link, which can never be marked).</summary>
    public int MarkedCount => _marked.Count;

    /// <summary>
    /// Resolved full paths of every marked entry, in listing order. If
    /// no marks exist, returns the single selected entry's path (or an
    /// empty span when the cursor is on ".." or the listing is empty).
    /// Operations call this so they can treat "1 selection" and "N
    /// marked" symmetrically.
    /// </summary>
    public IReadOnlyList<string> SelectionForOperation()
    {
        if (_marked.Count > 0)
        {
            var paths = new List<string>(_marked.Count);
            // Listing-order iteration keeps copy/move output stable
            // across runs (HashSet enumeration is not).
            for (var i = 0; i < _entries.Count; i++)
            {
                if (!_marked.Contains(i)) continue;
                var e = _entries[i];
                if (e.IsParentLink) continue;
                paths.Add(System.IO.Path.Combine(_path, e.Name));
            }
            return paths;
        }
        var single = SelectedFullPath;
        return single is null ? [] : [single];
    }

    public void ToggleMarkAtCursor()
    {
        if (_selected < 0 || _selected >= _entries.Count) return;
        if (_entries[_selected].IsParentLink) return;
        if (!_marked.Add(_selected)) _marked.Remove(_selected);
    }

    public void ClearMarks() => _marked.Clear();

    private int InteriorX     => X + 1;
    private int InteriorY     => Y + 1;
    private int InteriorWidth => Math.Max(0, Width - 2);
    private int ListHeight    => Math.Max(0, Height - 2);

    private bool ShowDateColumn => InteriorWidth >= DateColumnMinInteriorWidth;

    /// <summary>
    /// Layout per row: " name  date  size " — 1 left pad, name col,
    /// 1 gap, optional date(12)+gap, size col, 1 right pad.
    /// </summary>
    private int NameColumnWidth
    {
        get
        {
            var fixedPart = 1 + 1 + SizeColumnWidth + 1; // pads + 1 gap + size
            if (ShowDateColumn) fixedPart += DateColumnWidth + 1; // date + 1 more gap
            return Math.Max(1, InteriorWidth - fixedPart);
        }
    }

    public void Navigate(string path)
    {
        _entries.Clear();
        _marked.Clear();
        _loadError  = null;
        _totalSize  = 0;
        _selected   = 0;
        _scrollTop  = 0;
        ResetMarquee();

        try
        {
            var full = System.IO.Path.GetFullPath(path);
            _path = full;

            // Synthetic ".." link unless going up would either run out
            // of FS root or escape the demo's root guard.
            if (CanGoUpFrom(full))
                _entries.Add(FileEntry.ParentLink());

            var dirs  = new List<FileEntry>();
            var files = new List<FileEntry>();
            foreach (var sub in Directory.EnumerateDirectories(full))
            {
                var info = new DirectoryInfo(sub);
                dirs.Add(FileEntry.FromDirectory(info));
            }
            foreach (var f in Directory.EnumerateFiles(full))
            {
                var info = new FileInfo(f);
                files.Add(FileEntry.FromFile(info));
                _totalSize += info.Length;
            }
            dirs.Sort(static (a, b)  => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            files.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            _entries.AddRange(dirs);
            _entries.AddRange(files);
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
    }

    /// <summary>
    /// Reload the current directory in place, preserving cursor position
    /// by name when possible. Used after copy / move / delete so the
    /// view reflects the new state without losing the user's place.
    /// </summary>
    public void Refresh()
    {
        var keepName = (_selected >= 0 && _selected < _entries.Count)
            ? _entries[_selected].Name
            : null;
        Navigate(_path);
        if (keepName is not null)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Name, keepName, StringComparison.OrdinalIgnoreCase))
                {
                    _selected = i;
                    EnsureVisible();
                    ResetMarquee();
                    break;
                }
            }
        }
    }

    public void Move(int delta)
    {
        if (_entries.Count == 0) return;
        var n = _selected + delta;
        if (n < 0)               n = 0;
        if (n >= _entries.Count) n = _entries.Count - 1;
        if (n != _selected)
        {
            _selected = n;
            EnsureVisible();
            ResetMarquee();
        }
    }

    public void PageMove(int direction)
    {
        var page = Math.Max(1, ListHeight - 1);
        Move(direction * page);
    }

    public void HomeEnd(bool home)
        => Move(home ? -_entries.Count : _entries.Count);

    public void EnterSelected()
    {
        if (_selected >= _entries.Count) return;
        var e = _entries[_selected];
        if (!e.IsDirectory) return;
        if (e.IsParentLink)
        {
            GoUp();
            return;
        }
        Navigate(System.IO.Path.Combine(_path, e.Name));
    }

    public void GoUp()
    {
        if (!CanGoUpFrom(_path)) return;
        var parent = System.IO.Path.GetDirectoryName(_path.TrimEnd(PathSeparators));
        if (string.IsNullOrEmpty(parent)) return;

        var leaving = new DirectoryInfo(_path).Name;
        Navigate(parent);
        for (var i = 0; i < _entries.Count; i++)
        {
            if (string.Equals(_entries[i].Name, leaving, StringComparison.OrdinalIgnoreCase))
            {
                _selected = i;
                EnsureVisible();
                ResetMarquee();
                break;
            }
        }
    }

    private bool CanGoUpFrom(string fromPath)
    {
        var parent = System.IO.Path.GetDirectoryName(fromPath.TrimEnd(PathSeparators));
        if (string.IsNullOrEmpty(parent)) return false;
        if (string.Equals(parent, fromPath, StringComparison.OrdinalIgnoreCase)) return false;

        if (_rootGuard is null) return true;

        // Demo mode: refuse to leave the workspace. Comparison is case-
        // insensitive so Windows path-casing inconsistencies don't lock
        // the user inside their own working dir on the round trip.
        var pNorm = parent.TrimEnd(PathSeparators);
        if (string.Equals(pNorm, _rootGuard, StringComparison.OrdinalIgnoreCase)) return true;
        return pNorm.StartsWith(_rootGuard + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || pNorm.StartsWith(_rootGuard + System.IO.Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Advance the marquee one frame. Returns true when the visible row changed and the screen needs a redraw.</summary>
    public bool Tick()
    {
        if (!IsActive)                                    return false;
        if (_selected < 0 || _selected >= _entries.Count) return false;

        var name  = _entries[_selected].Name;
        var nameW = NameColumnWidth;
        if (name.Length <= nameW) return false;

        _marqueeFrame++;
        if (_marqueeFrame < MarqueeHoldFrames) return false;

        var step   = (_marqueeFrame - MarqueeHoldFrames) / MarqueeFramesPerChar;
        var period = name.Length + MarqueeSeparator.Length;

        if (step >= period)
        {
            _marqueeFrame = 0;
            if (_marqueeOffset != 0) { _marqueeOffset = 0; return true; }
            return false;
        }

        if (step != _marqueeOffset)
        {
            _marqueeOffset = step;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reset the marquee back to the head of the selected name. Called
    /// on selection change and on Tab-swap so the user always sees the
    /// start of a long filename when their attention lands on a row.
    /// </summary>
    public void ResetMarquee()
    {
        _marqueeFrame  = 0;
        _marqueeOffset = 0;
    }

    private void EnsureVisible()
    {
        if (_selected < _scrollTop)                    _scrollTop = _selected;
        else if (_selected >= _scrollTop + ListHeight) _scrollTop = _selected - ListHeight + 1;
        if (_scrollTop < 0) _scrollTop = 0;
    }

    public void Draw(ScreenBuffer screen)
    {
        DrawFrame(screen);
        if (_loadError is not null)
        {
            DrawError(screen);
            return;
        }
        DrawList(screen);
    }

    private void DrawFrame(ScreenBuffer screen)
    {
        var (h, v, tl, tr, bl, br) = IsActive
            ? ('═', '║', '╔', '╗', '╚', '╝')
            : ('─', '│', '┌', '┐', '└', '┘');

        var fg = IsActive ? Color.LightCyan : Color.LightGray;
        var bg = Color.DarkBlue;

        screen.FillRect(X, Y, Width, Height, new Cell(' ', fg, bg));

        var rightX  = X + Width  - 1;
        var bottomY = Y + Height - 1;

        for (var x = X + 1; x < rightX; x++)
        {
            screen[x, Y]       = new Cell(h, fg, bg);
            screen[x, bottomY] = new Cell(h, fg, bg);
        }
        for (var y = Y + 1; y < bottomY; y++)
        {
            screen[X,      y] = new Cell(v, fg, bg);
            screen[rightX, y] = new Cell(v, fg, bg);
        }
        screen[X,      Y]       = new Cell(tl, fg, bg);
        screen[rightX, Y]       = new Cell(tr, fg, bg);
        screen[X,      bottomY] = new Cell(bl, fg, bg);
        screen[rightX, bottomY] = new Cell(br, fg, bg);

        var available = InteriorWidth - 4;
        if (available > 0)
        {
            var displayPath = DisplayPath(_path);
            var title       = " " + Truncate(displayPath, available - 2) + " ";
            var attrs       = IsActive ? CellAttrs.Bold : CellAttrs.None;
            screen.PutString(X + 2, Y, title, fg, bg, attrs);
        }

        // Bottom-line summary: "5 items · 1.2 MB · 2 marked" or load error.
        var summary = _loadError is not null
            ? " (error) "
            : _marked.Count > 0
                ? $" {_entries.Count(static e => !e.IsParentLink)} items · {FormatSize(_totalSize)} · {_marked.Count} marked "
                : $" {_entries.Count(static e => !e.IsParentLink)} items · {FormatSize(_totalSize)} ";
        if (summary.Length <= InteriorWidth - 2)
            screen.PutString(X + 2, bottomY, summary, fg, bg);
    }

    private string DisplayPath(string p)
    {
        // Show paths relative to the root guard when one is set so the
        // demo's temp-dir path doesn't dominate the title bar. "/" for
        // the workspace root, "/nested" for subdirs.
        if (_rootGuard is null) return p;
        var norm = p.TrimEnd(PathSeparators);
        if (string.Equals(norm, _rootGuard, StringComparison.OrdinalIgnoreCase)) return "/";
        if (norm.StartsWith(_rootGuard + System.IO.Path.DirectorySeparatorChar,    StringComparison.OrdinalIgnoreCase) ||
            norm.StartsWith(_rootGuard + System.IO.Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var rel = norm[(_rootGuard.Length + 1)..].Replace('\\', '/');
            return "/" + rel;
        }
        return p;
    }

    private void DrawError(ScreenBuffer screen)
    {
        if (_loadError is null) return;
        var ix = InteriorX + 1;
        var iy = InteriorY;
        var iw = InteriorWidth - 2;
        if (iw <= 0) return;
        screen.PutString(ix, iy,     "Cannot read directory:".AsSpan(), Color.Yellow,    Color.DarkBlue, CellAttrs.Bold);
        screen.PutString(ix, iy + 2, Truncate(_loadError, iw),          Color.LightGray, Color.DarkBlue);
    }

    private void DrawList(ScreenBuffer screen)
    {
        var rows = ListHeight;
        for (var r = 0; r < rows; r++)
        {
            var idx = _scrollTop + r;
            if (idx >= _entries.Count) break;
            DrawRow(screen, r, idx);
        }
    }

    private void DrawRow(ScreenBuffer screen, int row, int entryIndex)
    {
        var entry      = _entries[entryIndex];
        var isSelected = entryIndex == _selected;
        var isMarked   = _marked.Contains(entryIndex);
        var (fg, bg, attrs) = ChooseColors(entry, isSelected, isMarked, IsActive);

        var ix = InteriorX;
        var iy = InteriorY + row;
        var iw = InteriorWidth;

        for (var i = 0; i < iw; i++)
            screen[ix + i, iy] = new Cell(' ', fg, bg, attrs);

        // Mark sigil in the left pad — yellow '*' against the row's bg.
        // Belt-and-suspenders with the bg-color swap below: terminals
        // with re-themed Yellow slots still get a glyph, themes that
        // re-paint DarkRed still get the glyph, and anyone with a normal
        // palette gets both.
        if (isMarked)
            screen[ix, iy] = new Cell('*', Color.Yellow, bg, CellAttrs.Bold);

        var nameX = ix + 1;
        var nameW = NameColumnWidth;

        if (isSelected && IsActive && entry.Name.Length > nameW)
        {
            DrawMarquee(screen, nameX, iy, entry.Name, _marqueeOffset, nameW, fg, bg, attrs);
        }
        else
        {
            screen.PutString(nameX, iy, Truncate(entry.Name, nameW), fg, bg, attrs);
        }

        if (ShowDateColumn)
        {
            var dateText = FormatEntryDate(entry);
            var dateX    = nameX + nameW + 1;
            screen.PutString(dateX, iy, dateText, fg, bg, attrs);
        }

        var sizeText = FormatEntrySize(entry);
        var sizeX    = ix + iw - 1 - sizeText.Length;
        screen.PutString(sizeX, iy, sizeText, fg, bg, attrs);
    }

    private static void DrawMarquee(
        ScreenBuffer screen, int x, int y,
        string name, int offset, int width,
        Color fg, Color bg, CellAttrs attrs)
    {
        var sep    = MarqueeSeparator;
        var period = name.Length + sep.Length;
        for (var i = 0; i < width; i++)
        {
            var pos = (offset + i) % period;
            var ch  = pos < name.Length ? name[pos] : sep[pos - name.Length];
            screen[x + i, y] = new Cell(ch, fg, bg, attrs);
        }
    }

    private static (Color fg, Color bg, CellAttrs attrs) ChooseColors(FileEntry e, bool isSelected, bool isMarked, bool paneActive)
    {
        // Bg only ever signals the cursor — keeps the pane reading like
        // a calm panel even with a half-dozen marks. The mark itself is
        // carried by the bright-yellow fg + the '*' sigil drawn in the
        // left pad of the row.
        var bg = isSelected
            ? (paneActive ? Color.DarkCyan : Color.DarkGray)
            : Color.DarkBlue;

        Color fg;
        if (isMarked)              fg = Color.Yellow;
        else if (e.IsParentLink)   fg = Color.LightGray;
        else if (e.IsDirectory)    fg = Color.LightCyan;
        else if (e.IsHidden)       fg = Color.DarkGray;
        else if (e.IsExecutable)   fg = Color.LightGreen;
        else                       fg = Color.LightGray;

        var attrs = (isSelected || isMarked) ? CellAttrs.Bold : CellAttrs.None;
        if (isSelected && !isMarked && fg == Color.LightGray) fg = Color.White;

        return (fg, bg, attrs);
    }

    private static string FormatEntrySize(FileEntry e)
    {
        if (e.IsParentLink) return "  UP";
        if (e.IsDirectory)  return "<DIR>";
        return FormatSize(e.Size);
    }

    private static string FormatEntryDate(FileEntry e)
    {
        if (e.IsParentLink) return new string(' ', DateColumnWidth);
        return e.Modified.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Compact IEC-ish size: "999", "1.2K", "12K", "234K", "1.2M". Always ≤ 5 chars.</summary>
    private static string FormatSize(long bytes)
    {
        if (bytes < 1000) return bytes.ToString(CultureInfo.InvariantCulture);
        double v   = bytes / 1024.0;
        char  unit = 'K';
        if (v >= 1000) { v /= 1024; unit = 'M'; }
        if (v >= 1000) { v /= 1024; unit = 'G'; }
        if (v >= 1000) { v /= 1024; unit = 'T'; }
        if (v >= 100)  return string.Create(CultureInfo.InvariantCulture, $"{v:0}{unit}");
        if (v >= 10)   return string.Create(CultureInfo.InvariantCulture, $"{v:0.0}{unit}");
        return            string.Create(CultureInfo.InvariantCulture, $"{v:0.0}{unit}");
    }

    private static string Truncate(string s, int width)
    {
        if (width <= 0)        return string.Empty;
        if (s.Length <= width) return s;
        if (width == 1)        return "…";
        return string.Concat(s.AsSpan(0, width - 1), "…");
    }
}

/// <summary>One row in a pane's listing — name, size, modified time, and a few flags used to colorize it.</summary>
internal readonly record struct FileEntry(
    string   Name,
    long     Size,
    DateTime Modified,
    bool     IsDirectory,
    bool     IsHidden,
    bool     IsExecutable,
    bool     IsParentLink)
{
    public static FileEntry ParentLink() => new("..", 0, DateTime.MinValue, IsDirectory: true, IsHidden: false, IsExecutable: false, IsParentLink: true);

    public static FileEntry FromDirectory(DirectoryInfo info)
    {
        var hidden = info.Name.StartsWith('.') || (info.Attributes & FileAttributes.Hidden) != 0;
        return new FileEntry(info.Name, 0, info.LastWriteTime, IsDirectory: true, hidden, IsExecutable: false, IsParentLink: false);
    }

    public static FileEntry FromFile(FileInfo info)
    {
        var hidden = info.Name.StartsWith('.') || (info.Attributes & FileAttributes.Hidden) != 0;
        var exec   = IsExecutableExtension(info.Extension);
        return new FileEntry(info.Name, info.Length, info.LastWriteTime, IsDirectory: false, hidden, exec, IsParentLink: false);
    }

    private static bool IsExecutableExtension(string ext)
    {
        if (ext.Length == 0) return false;
        return ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".com", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".ps1", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".sh",  StringComparison.OrdinalIgnoreCase);
    }
}
