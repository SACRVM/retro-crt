namespace Retro.Crt.Tests;

public class ScreenBufferTests
{
    [Fact]
    public void New_buffer_is_filled_with_Cell_Empty()
    {
        var b = new ScreenBuffer(4, 3);
        for (var y = 0; y < b.Height; y++)
            for (var x = 0; x < b.Width; x++)
                Assert.Equal(Cell.Empty, b[x, y]);
    }

    [Fact]
    public void Width_and_height_below_one_throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenBuffer(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenBuffer(5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenBuffer(-1, 5));
    }

    [Fact]
    public void Indexer_get_set_round_trips()
    {
        var b = new ScreenBuffer(3, 2);
        var c = new Cell('Z', Color.Yellow, Color.DarkBlue);
        b[1, 1] = c;
        Assert.Equal(c, b[1, 1]);
        // Untouched cells remain Empty.
        Assert.Equal(Cell.Empty, b[0, 0]);
        Assert.Equal(Cell.Empty, b[2, 0]);
    }

    [Fact]
    public void Indexer_throws_on_out_of_bounds()
    {
        var b = new ScreenBuffer(2, 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => b[2, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => b[0, 2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => b[-1, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => b[0, -1]);
    }

    [Fact]
    public void Clear_with_fill_replaces_every_cell()
    {
        var b = new ScreenBuffer(3, 2);
        var c = new Cell('.', Color.DarkGreen, Color.Black);
        b.Clear(c);
        for (var y = 0; y < b.Height; y++)
            for (var x = 0; x < b.Width; x++)
                Assert.Equal(c, b[x, y]);
    }

    [Fact]
    public void PutString_writes_chars_with_given_style()
    {
        var b = new ScreenBuffer(10, 1);
        b.PutString(2, 0, "OK", Color.LightGreen, Color.Black, CellAttrs.Bold);
        Assert.Equal(Cell.Empty, b[0, 0]);
        Assert.Equal(Cell.Empty, b[1, 0]);
        Assert.Equal(new Cell('O', Color.LightGreen, Color.Black, CellAttrs.Bold), b[2, 0]);
        Assert.Equal(new Cell('K', Color.LightGreen, Color.Black, CellAttrs.Bold), b[3, 0]);
        Assert.Equal(Cell.Empty, b[4, 0]);
    }

    [Fact]
    public void PutString_clips_at_right_edge()
    {
        var b = new ScreenBuffer(5, 1);
        b.PutString(3, 0, "ABCDEF", Color.White, Color.Black);
        Assert.Equal('A', b[3, 0].Glyph);
        Assert.Equal('B', b[4, 0].Glyph);
        // Nothing thrown, no overflow into next row (there is none).
    }

    [Fact]
    public void PutString_skips_negative_x_prefix_and_writes_the_rest()
    {
        var b = new ScreenBuffer(5, 1);
        b.PutString(-2, 0, "ABCDE", Color.White, Color.Black);
        // First two chars (A, B) clipped, C lands at column 0.
        Assert.Equal('C', b[0, 0].Glyph);
        Assert.Equal('D', b[1, 0].Glyph);
        Assert.Equal('E', b[2, 0].Glyph);
        Assert.Equal(Cell.Empty, b[3, 0]);
    }

    [Fact]
    public void PutString_off_screen_y_is_a_noop()
    {
        var b = new ScreenBuffer(5, 2);
        b.PutString(0,  2, "X", Color.White, Color.Black);
        b.PutString(0, -1, "X", Color.White, Color.Black);
        Assert.Equal(Cell.Empty, b[0, 0]);
        Assert.Equal(Cell.Empty, b[0, 1]);
    }

    [Fact]
    public void FillRect_clips_to_buffer_bounds()
    {
        var b = new ScreenBuffer(4, 4);
        var c = new Cell('#', Color.LightRed, Color.Black);
        // Rect that extends past every edge — only the overlapping
        // 2×2 subrect inside the buffer should be filled.
        b.FillRect(-1, -1, 3, 3, c);
        Assert.Equal(c, b[0, 0]);
        Assert.Equal(c, b[1, 0]);
        Assert.Equal(c, b[0, 1]);
        Assert.Equal(c, b[1, 1]);
        Assert.Equal(Cell.Empty, b[2, 2]);
    }

    [Fact]
    public void FillRect_zero_or_negative_size_is_a_noop()
    {
        var b = new ScreenBuffer(3, 3);
        b.FillRect(0, 0, 0,  3, new Cell('X', Color.White, Color.Black));
        b.FillRect(0, 0, 3,  0, new Cell('X', Color.White, Color.Black));
        b.FillRect(0, 0, -5, 5, new Cell('X', Color.White, Color.Black));
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
                Assert.Equal(Cell.Empty, b[x, y]);
    }
}
