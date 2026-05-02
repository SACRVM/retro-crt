using Retro.Crt.Internals;

namespace Retro.Crt.Tests;

public class BoxBuilderTests
{
    [Fact]
    public void Single_line_box_has_three_rows()
    {
        var box = BoxBuilder.Build(["hello"]);

        Assert.Equal(3, box.Length);
        Assert.Equal("+-------+", box[0]);
        Assert.Equal("| hello |", box[1]);
        Assert.Equal("+-------+", box[2]);
    }

    [Fact]
    public void Multi_line_box_pads_to_longest_line()
    {
        var box = BoxBuilder.Build(["hi", "world"]);

        Assert.Equal(4, box.Length);
        Assert.Equal("+-------+", box[0]);
        Assert.Equal("| hi    |", box[1]);
        Assert.Equal("| world |", box[2]);
        Assert.Equal("+-------+", box[3]);
    }

    [Fact]
    public void Padding_zero_hugs_the_text()
    {
        var box = BoxBuilder.Build(["hi"], padding: 0);

        Assert.Equal("+--+", box[0]);
        Assert.Equal("|hi|", box[1]);
        Assert.Equal("+--+", box[2]);
    }

    [Fact]
    public void Custom_glyphs_compose_into_unicode_box()
    {
        var box = BoxBuilder.Build(
            ["X"],
            tl: '┌', tr: '┐', bl: '└', br: '┘',
            horizontal: '─', vertical: '│');

        Assert.Equal("┌───┐", box[0]);
        Assert.Equal("│ X │", box[1]);
        Assert.Equal("└───┘", box[2]);
    }

    [Fact]
    public void Empty_input_still_yields_a_box()
    {
        var box = BoxBuilder.Build([]);

        Assert.Equal(3, box.Length);
        Assert.Equal("+--+", box[0]);
        Assert.Equal("|  |", box[1]);
        Assert.Equal("+--+", box[2]);
    }
}
