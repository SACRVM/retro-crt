namespace Retro.Crt.Tests;

public class ScreenRendererTests
{
    [Fact]
    public void Render_with_null_previous_paints_every_cell()
    {
        // First-frame render: no previous frame, so every cell is
        // dirty. The cursor naturally advances after each glyph write,
        // so only the FIRST dirty cell of a contiguous run needs an
        // explicit goto — the rest ride the auto-advance. Verify the
        // initial goto and that the X actually shows up at all.
        var current = new ScreenBuffer(3, 1);
        current[1, 0] = new Cell('X', Color.White, Color.Black);

        var sw = new StringWriter();
        ScreenRenderer.Render(previous: null, current, sw);
        var output = sw.ToString();

        // Initial goto to (1, 1) for the first dirty cell.
        Assert.Contains("\x1b[1;1H", output);
        // The non-default glyph is somewhere in the stream.
        Assert.Contains("X", output);

        // Final reset + park-cursor at origin so the caller doesn't
        // inherit our pen / position.
        Assert.EndsWith("\x1b[0m\x1b[1;1H", output);
    }

    [Fact]
    public void Render_with_identical_buffers_emits_nothing()
    {
        var prev    = new ScreenBuffer(5, 2);
        var current = new ScreenBuffer(5, 2);
        // Both default-filled → no diff, nothing to emit.
        var sw = new StringWriter();
        ScreenRenderer.Render(prev, current, sw);
        Assert.Equal(string.Empty, sw.ToString());
    }

    [Fact]
    public void Render_only_repaints_cells_that_changed()
    {
        var prev    = new ScreenBuffer(3, 1);
        var current = new ScreenBuffer(3, 1);
        current[1, 0] = new Cell('X', Color.White, Color.Black);

        var sw = new StringWriter();
        ScreenRenderer.Render(prev, current, sw);
        var output = sw.ToString();

        // Only column 1 changed — goto must target column 2 (1-based).
        Assert.Contains("\x1b[1;2H", output);
        // Other columns must NOT have been visited.
        Assert.DoesNotContain("\x1b[1;1H", output[..output.IndexOf('X')]);
        Assert.DoesNotContain("\x1b[1;3H", output[..output.IndexOf('X')]);
        Assert.Contains("X", output);
    }

    [Fact]
    public void Pen_state_survives_within_a_run_of_same_styled_cells()
    {
        // Three adjacent cells in the same fg/bg → the renderer should
        // emit Reset+Fg+Bg ONCE for the run, not three times.
        var prev    = new ScreenBuffer(3, 1);
        var current = new ScreenBuffer(3, 1);
        var c = new Cell(' ', Color.LightCyan, Color.DarkBlue);
        current[0, 0] = c with { Glyph = 'A' };
        current[1, 0] = c with { Glyph = 'B' };
        current[2, 0] = c with { Glyph = 'C' };

        var sw = new StringWriter();
        ScreenRenderer.Render(prev, current, sw);
        var output = sw.ToString();

        // Reset appears for the first cell + the final park-cursor reset.
        var resetCount = CountOccurrences(output, "\x1b[0m");
        Assert.Equal(2, resetCount);

        // Chars in order, contiguous (no goto between B and C).
        Assert.Contains("ABC", output);
    }

    [Fact]
    public void Pen_changes_when_attrs_differ_between_neighbouring_cells()
    {
        var prev    = new ScreenBuffer(2, 1);
        var current = new ScreenBuffer(2, 1);
        current[0, 0] = new Cell('A', Color.White, Color.Black, CellAttrs.None);
        current[1, 0] = new Cell('B', Color.White, Color.Black, CellAttrs.Bold);

        var sw = new StringWriter();
        ScreenRenderer.Render(prev, current, sw);
        var output = sw.ToString();

        // The bold attribute shows up between A and B — there must be
        // a Reset followed by a bold escape between the two characters.
        var aPos    = output.IndexOf('A');
        var bPos    = output.IndexOf('B');
        var middle  = output[(aPos + 1)..bPos];
        Assert.Contains("\x1b[1m", middle); // SGR bold
    }

    [Fact]
    public void Render_with_different_dimensions_repaints_everything()
    {
        // Old buffer is 2×2, new buffer is 3×1 — geometry mismatch
        // means we treat prev as "absent" and repaint every cell.
        var prev    = new ScreenBuffer(2, 2);
        var current = new ScreenBuffer(3, 1);
        current[0, 0] = new Cell('A', Color.White, Color.Black);
        current[1, 0] = new Cell('B', Color.White, Color.Black);
        current[2, 0] = new Cell('C', Color.White, Color.Black);

        var sw = new StringWriter();
        ScreenRenderer.Render(prev, current, sw);
        var output = sw.ToString();

        Assert.Contains("A", output);
        Assert.Contains("B", output);
        Assert.Contains("C", output);
    }

    [Fact]
    public void Final_reset_only_emitted_when_at_least_one_cell_painted()
    {
        // Identical buffers — nothing painted, so no trailing reset
        // either. (Tested implicitly by the empty-output test, but
        // assert explicitly here too.)
        var prev    = new ScreenBuffer(2, 2);
        var current = new ScreenBuffer(2, 2);

        var sw = new StringWriter();
        ScreenRenderer.Render(prev, current, sw);
        Assert.DoesNotContain("\x1b[0m", sw.ToString());
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
