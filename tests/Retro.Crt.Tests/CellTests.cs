namespace Retro.Crt.Tests;

public class CellTests
{
    [Fact]
    public void Empty_is_space_on_lightgray_on_black_with_no_attrs()
    {
        var c = Cell.Empty;
        Assert.Equal(' ', c.Glyph);
        Assert.Equal(Color.LightGray, c.Fg);
        Assert.Equal(Color.Black, c.Bg);
        Assert.Equal(CellAttrs.None, c.Attrs);
    }

    [Fact]
    public void Cells_with_same_fields_compare_equal()
    {
        var a = new Cell('X', Color.LightCyan, Color.DarkBlue, CellAttrs.Bold);
        var b = new Cell('X', Color.LightCyan, Color.DarkBlue, CellAttrs.Bold);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Cells_with_different_attrs_are_not_equal()
    {
        var a = new Cell('X', Color.LightCyan, Color.DarkBlue, CellAttrs.Bold);
        var b = new Cell('X', Color.LightCyan, Color.DarkBlue, CellAttrs.Underline);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Cell_attrs_combine_via_flags()
    {
        var combined = CellAttrs.Bold | CellAttrs.Underline;
        Assert.NotEqual(CellAttrs.None, combined & CellAttrs.Bold);
        Assert.NotEqual(CellAttrs.None, combined & CellAttrs.Underline);
    }
}
