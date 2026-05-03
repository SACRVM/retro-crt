using Retro.Crt.Tests.Support;

namespace Retro.Crt.Tests.Integration;

[Collection(EnvMutatingCollection.Name)]
public class PromptIntegrationTests
{
    // ─── Confirm ─────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_y_returns_true_and_echoes_y()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('y', ConsoleKey.Y)]);

        var result = Prompt.Confirm("Continue?", defaultYes: false, color: null, keys.Dequeue);

        Assert.True(result);
        Assert.Contains("Continue? [y/N]", c.Out);
        Assert.EndsWith("y\n", c.Out);
    }

    [Fact]
    public void Confirm_capital_N_returns_false()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('N', ConsoleKey.N)]);

        var result = Prompt.Confirm("Wipe disk?", defaultYes: true, color: null, keys.Dequeue);

        Assert.False(result);
        Assert.Contains("[Y/n]", c.Out);
        Assert.EndsWith("n\n", c.Out);
    }

    [Fact]
    public void Confirm_enter_takes_default_yes()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('\r', ConsoleKey.Enter)]);

        var result = Prompt.Confirm("ok?", defaultYes: true, color: null, keys.Dequeue);

        Assert.True(result);
        Assert.EndsWith("y\n", c.Out);
    }

    [Fact]
    public void Confirm_enter_takes_default_no()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('\r', ConsoleKey.Enter)]);

        var result = Prompt.Confirm("ok?", defaultYes: false, color: null, keys.Dequeue);

        Assert.False(result);
        Assert.EndsWith("n\n", c.Out);
    }

    [Fact]
    public void Confirm_ignores_unrelated_keys_until_valid()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([
            Key('a', ConsoleKey.A),
            Key('z', ConsoleKey.Z),
            Key(' ', ConsoleKey.Spacebar),
            Key('y', ConsoleKey.Y),
        ]);

        var result = Prompt.Confirm("?", defaultYes: false, color: null, keys.Dequeue);

        Assert.True(result);
    }

    // ─── Ask ─────────────────────────────────────────────────────────────

    [Fact]
    public void Ask_returns_typed_input()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        Console.SetIn(new StringReader("Chloe\n"));

        var name = Prompt.Ask("Name?");

        Assert.Equal("Chloe", name);
        Assert.Contains("Name?", c.Out);
    }

    [Fact]
    public void Ask_returns_default_on_empty_input()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        Console.SetIn(new StringReader("\n"));

        var name = Prompt.Ask("Name?", defaultValue: "guest");

        Assert.Equal("guest", name);
        Assert.Contains("Name? [guest]", c.Out);
    }

    [Fact]
    public void Ask_returns_empty_string_when_no_input_and_no_default()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        Console.SetIn(new StringReader(""));

        var result = Prompt.Ask("Type:");

        Assert.Equal(string.Empty, result);
    }

    // ─── Select (animated) ───────────────────────────────────────────────

    [Fact]
    public void Select_enter_returns_initial_when_no_movement()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('\r', ConsoleKey.Enter)]);

        var idx = Prompt.Select(
            "Pick:",
            ["Red", "Green", "Blue"],
            initialIndex: 1,
            color: null,
            keys.Dequeue);

        Assert.Equal(1, idx);
        Assert.Contains("Pick:", c.Out);
        Assert.Contains("> Green", c.Out);
    }

    [Fact]
    public void Select_down_arrow_moves_active_one_step()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([
            Key('\0', ConsoleKey.DownArrow),
            Key('\r', ConsoleKey.Enter),
        ]);

        var idx = Prompt.Select(
            "Pick:",
            ["Red", "Green", "Blue"],
            initialIndex: 0,
            color: null,
            keys.Dequeue);

        Assert.Equal(1, idx);
    }

    [Fact]
    public void Select_up_arrow_clamps_at_zero()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([
            Key('\0', ConsoleKey.UpArrow),
            Key('\0', ConsoleKey.UpArrow),
            Key('\r', ConsoleKey.Enter),
        ]);

        var idx = Prompt.Select(
            "Pick:",
            ["A", "B"],
            initialIndex: 0,
            color: null,
            keys.Dequeue);

        Assert.Equal(0, idx);
    }

    [Fact]
    public void Select_down_arrow_clamps_at_last()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([
            Key('\0', ConsoleKey.DownArrow),
            Key('\0', ConsoleKey.DownArrow),
            Key('\0', ConsoleKey.DownArrow),
            Key('\r', ConsoleKey.Enter),
        ]);

        var idx = Prompt.Select(
            "Pick:",
            ["A", "B"],
            initialIndex: 0,
            color: null,
            keys.Dequeue);

        Assert.Equal(1, idx);
    }

    [Fact]
    public void Select_initialIndex_is_clamped_to_options_range()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('\r', ConsoleKey.Enter)]);

        var idx = Prompt.Select(
            "Pick:",
            ["A", "B", "C"],
            initialIndex: 99,
            color: null,
            keys.Dequeue);

        Assert.Equal(2, idx);
    }

    [Fact]
    public void Select_emits_hide_and_show_cursor_around_interaction()
    {
        using var c = ConsoleCapture.Start(ansi: true);
        var keys = new Queue<ConsoleKeyInfo>([Key('\r', ConsoleKey.Enter)]);

        Prompt.Select("Pick:", ["A", "B"], 0, null, keys.Dequeue);

        Assert.Contains("\x1b[?25l", c.Out);
        Assert.Contains("\x1b[?25h", c.Out);
    }

    [Fact]
    public void Select_throws_on_empty_options()
    {
        Assert.Throws<ArgumentException>(() =>
            Prompt.Select("Pick:", []));
    }

    // ─── Select (fallback) ───────────────────────────────────────────────

    [Fact]
    public void Select_fallback_uses_numbered_list_without_ansi()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        Console.SetIn(new StringReader("2\n"));

        var idx = Prompt.Select("Pick:", ["A", "B", "C"]);

        Assert.Equal(1, idx);
        Assert.Contains("1) A", c.Out);
        Assert.Contains("2) B", c.Out);
        Assert.Contains("3) C", c.Out);
    }

    [Fact]
    public void Select_fallback_uses_default_on_empty_input()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        Console.SetIn(new StringReader("\n"));

        var idx = Prompt.Select("Pick:", ["A", "B", "C"], initialIndex: 2);

        Assert.Equal(2, idx);
        Assert.Contains("default 3", c.Out);
    }

    [Fact]
    public void Select_fallback_re_prompts_on_invalid_input()
    {
        using var c = ConsoleCapture.Start(ansi: false);
        Console.SetIn(new StringReader("x\n0\n99\n2\n"));

        var idx = Prompt.Select("Pick:", ["A", "B", "C"]);

        Assert.Equal(1, idx);
    }

    // ─── helpers ─────────────────────────────────────────────────────────

    private static ConsoleKeyInfo Key(char ch, ConsoleKey key)
        => new(ch, key, shift: false, alt: false, control: false);
}
