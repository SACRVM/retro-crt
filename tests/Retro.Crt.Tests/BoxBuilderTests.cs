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

    [Fact]
    public void MinContentWidth_pads_short_lines_to_that_width()
    {
        var box = BoxBuilder.Build(["hi"], minContentWidth: 10);

        // Total inner = 10 + 2*padding(default 1) = 12, plus 2 corners = 14.
        Assert.Equal(14, box[0].Length);
        Assert.Equal("+------------+", box[0]);
        Assert.Equal("| hi         |", box[1]);
        Assert.Equal("+------------+", box[2]);
    }

    [Fact]
    public void MinContentWidth_below_longest_line_is_ignored()
    {
        // "hello" is 5 wide, minContentWidth=2 should not shrink it.
        var box = BoxBuilder.Build(["hello"], minContentWidth: 2);

        Assert.Equal("+-------+", box[0]);
        Assert.Equal("| hello |", box[1]);
        Assert.Equal("+-------+", box[2]);
    }

    [Fact]
    public void Align_left_pads_trailing_space()
    {
        var box = BoxBuilder.Build(["hi"], minContentWidth: 8, align: BoxAlign.Left);

        Assert.Equal("| hi       |", box[1]);
    }

    [Fact]
    public void Align_right_pads_leading_space()
    {
        var box = BoxBuilder.Build(["hi"], minContentWidth: 8, align: BoxAlign.Right);

        Assert.Equal("|       hi |", box[1]);
    }

    [Fact]
    public void Align_center_balances_space_with_odd_leftover_to_the_right()
    {
        // slack = 8 - 2 = 6, even — perfect split, three left, three right.
        var box = BoxBuilder.Build(["hi"], minContentWidth: 8, align: BoxAlign.Center);

        Assert.Equal("|    hi    |", box[1]);
    }

    [Fact]
    public void Align_center_with_odd_slack_sends_extra_to_the_right()
    {
        // slack = 7 - 2 = 5, odd — 2 left, 3 right.
        var box = BoxBuilder.Build(["hi"], minContentWidth: 7, align: BoxAlign.Center);

        Assert.Equal("|   hi    |", box[1]);
    }

    [Fact]
    public void Align_center_keeps_multi_line_layout_anchored()
    {
        var box = BoxBuilder.Build(["hi", "longer"], align: BoxAlign.Center);

        // width = 6 (longer); shorter line gets centred inside that.
        Assert.Equal("|   hi   |", box[1]);
        Assert.Equal("| longer |", box[2]);
    }
}
