using Retro.Crt.Internals;

namespace Retro.Crt;

/// <summary>
/// Tiny interactive prompts: yes/no <c>Confirm</c>, free-text
/// <c>Ask</c>, arrow-key <c>Select</c>. All zero-dependency, AOT-clean.
/// Without ANSI, <c>Select</c> degrades to a numbered list with
/// <see cref="Console.ReadLine"/>.
/// </summary>
public static class Prompt
{
    /// <summary>
    /// Yes/no question. Reads a single keystroke (no Enter required).
    /// Pressing Enter accepts <paramref name="defaultYes"/>; any key
    /// other than y/Y/n/N is ignored.
    /// </summary>
    public static bool Confirm(string question, bool defaultYes = false, Color? color = null)
        => Confirm(question, defaultYes, color, () => Console.ReadKey(intercept: true));

    internal static bool Confirm(
        string question,
        bool defaultYes,
        Color? color,
        Func<ConsoleKeyInfo> readKey)
    {
        color ??= Crt.CurrentTheme?.Accent;

        var hint = defaultYes ? "[Y/n]" : "[y/N]";
        WritePrompt($"{question} {hint} ", color);

        while (true)
        {
            var key = readKey();
            if (key.Key == ConsoleKey.Enter)
            {
                Crt.Sink.WriteLine(defaultYes ? "y" : "n");
                return defaultYes;
            }
            if (key.KeyChar is 'y' or 'Y')
            {
                Crt.Sink.WriteLine("y");
                return true;
            }
            if (key.KeyChar is 'n' or 'N')
            {
                Crt.Sink.WriteLine("n");
                return false;
            }
            // Anything else — keep waiting.
        }
    }

    /// <summary>
    /// Free-text prompt. Reads a full line. If <paramref name="defaultValue"/>
    /// is set it appears in the prompt as <c>[default]</c> and is returned
    /// when the user presses Enter on an empty input.
    /// </summary>
    public static string Ask(string question, string? defaultValue = null, Color? color = null)
    {
        color ??= Crt.CurrentTheme?.Accent;

        var prompt = defaultValue is null
            ? $"{question} "
            : $"{question} [{defaultValue}] ";
        WritePrompt(prompt, color);

        var input = Console.In.ReadLine();
        if (string.IsNullOrEmpty(input))
            return defaultValue ?? "";
        return input;
    }

    /// <summary>
    /// Arrow-key menu. Returns the index of the chosen option. Up/Down
    /// to move; Enter to select. The active option is prefixed with
    /// <c>&gt;</c> and rendered in <paramref name="color"/> + bold.
    /// Without ANSI, falls back to a numbered list read via
    /// <see cref="Console.ReadLine"/>.
    /// </summary>
    public static int Select(
        string question,
        string[] options,
        int initialIndex = 0,
        Color? color = null)
        => Select(question, options, initialIndex, color, () => Console.ReadKey(intercept: true));

    internal static int Select(
        string question,
        string[] options,
        int initialIndex,
        Color? color,
        Func<ConsoleKeyInfo> readKey)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Length == 0)
            throw new ArgumentException("options must not be empty", nameof(options));

        if (!Crt.ColorEnabled)
            return SelectFallback(question, options, initialIndex);

        color ??= Crt.CurrentTheme?.Accent;

        var active = Math.Clamp(initialIndex, 0, options.Length - 1);

        Crt.Sink.WriteLine(question);
        for (var i = 0; i < options.Length; i++)
            WriteOption(options[i], i == active, color);

        Crt.Sink.Write(AnsiCodes.HideCursor);
        try
        {
            while (true)
            {
                var key = readKey();

                if (key.Key == ConsoleKey.Enter) break;

                if (key.Key == ConsoleKey.UpArrow && active > 0)
                {
                    active--;
                    Redraw(options, active, color);
                }
                else if (key.Key == ConsoleKey.DownArrow && active < options.Length - 1)
                {
                    active++;
                    Redraw(options, active, color);
                }
                // Anything else — ignored.
            }
        }
        finally
        {
            Crt.Sink.Write(AnsiCodes.ShowCursor);
        }

        return active;
    }

    private static int SelectFallback(string question, string[] options, int initialIndex)
    {
        Crt.Sink.WriteLine(question);
        for (var i = 0; i < options.Length; i++)
            Crt.Sink.WriteLine($"  {i + 1}) {options[i]}");

        var fallbackInitial = Math.Clamp(initialIndex, 0, options.Length - 1);
        while (true)
        {
            Crt.Sink.Write($"choose [1-{options.Length}, default {fallbackInitial + 1}]: ");
            var input = Console.In.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) return fallbackInitial;
            if (int.TryParse(input, out var n) && n >= 1 && n <= options.Length)
                return n - 1;
        }
    }

    private static void Redraw(string[] options, int active, Color? color)
    {
        // Cursor is currently below the last option (after the trailing
        // newline). Move back to the top of the menu and rewrite each
        // option line with ClearToEol so trailing chars from a longer
        // previous label don't survive.
        Crt.Sink.Write($"\r{AnsiCodes.Csi}{options.Length}A");
        for (var i = 0; i < options.Length; i++)
        {
            Crt.Sink.Write(AnsiCodes.ClearToEol);
            WriteOption(options[i], i == active, color);
        }
    }

    private static void WriteOption(string text, bool active, Color? color)
    {
        var prefix = active ? "> " : "  ";

        if (active)
        {
            if (color is { } c) Crt.Sink.Write(Emit.Fg(c));
            Crt.Sink.Write(AnsiCodes.Bold);
            Crt.Sink.Write(prefix);
            Crt.Sink.Write(text);
            Crt.Sink.Write(AnsiCodes.Reset);
        }
        else
        {
            Crt.Sink.Write(prefix);
            Crt.Sink.Write(text);
        }
        Crt.Sink.WriteLine();
    }

    private static void WritePrompt(string prompt, Color? color)
    {
        if (color is { } c)
        {
            using (Crt.WithStyle(fg: c))
                Crt.Sink.Write(prompt);
        }
        else
        {
            Crt.Sink.Write(prompt);
        }
    }
}
