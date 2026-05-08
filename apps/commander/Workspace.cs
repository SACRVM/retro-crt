using System.Text;

namespace Retro.Crt.Commander;

/// <summary>
/// Builds a fake demo workspace under the OS temp directory: two
/// side-by-side folders ("left" and "right") seeded with a curated mix
/// of names (short, medium, very long for the marquee), sizes (zero
/// bytes, KB, multi-MB for the copy/move progress bar), and types
/// (text, hidden, executable, binary). Lets the demo show off without
/// touching the user's real files; <see cref="Cleanup"/> drops the
/// whole tree on shutdown.
/// </summary>
internal static class Workspace
{
    public static (string Root, string Left, string Right) Create()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"retro-crt-commander-{Environment.ProcessId}");
        var left  = System.IO.Path.Combine(root, "left");
        var right = System.IO.Path.Combine(root, "right");

        // Fresh start every run. If a previous process crashed and left
        // its temp dir behind, blow it away rather than reuse — the
        // demo's whole point is repeatable seed state.
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);

        Directory.CreateDirectory(left);
        Directory.CreateDirectory(right);
        Directory.CreateDirectory(System.IO.Path.Combine(left,  "nested"));
        Directory.CreateDirectory(System.IO.Path.Combine(right, "archive"));

        SeedLeft(left);
        SeedRight(right);

        return (root, left, right);
    }

    public static void Cleanup(string root)
    {
        try
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
        catch
        {
            // Last-ditch cleanup: if a viewer still holds a handle on
            // Windows, the OS will sweep the temp dir eventually. We
            // don't want a noisy stderr line on quit.
        }
    }

    private static void SeedLeft(string dir)
    {
        WriteText(dir, "readme.md",
            "# Retro.Crt.Commander\n\nNorton-Commander-Lite über `Retro.Crt`.\n\n" +
            "## Features\n- zwei Panes, Tab swap\n- Marquee auf langen Namen\n" +
            "- F3 Viewer · F4 Dup · F5 Copy · F6 Move · F8 Delete\n- Multi-Select via Ins / Shift+Down\n");
        WriteText(dir, "notes.txt",
            "Quick notes:\n* CLAUDE.md keeps the vibe\n* zero deps\n* AOT/trim-clean\n");
        WriteText(dir, "config.json",
            "{\n  \"theme\": \"dos-classic\",\n  \"shortcuts\": { \"quit\": \"F10\", \"help\": \"F1\" }\n}\n");
        WriteText(dir, "hello.cs",
            "using System;\n\nConsole.WriteLine(\"hello, retro\");\n");
        WriteText(dir, "data.csv", BuildCsv(rows: 80));
        WriteText(dir, "very_long_filename_to_demonstrate_horizontal_marquee_scrolling.txt",
            "If you can read this, the marquee animation worked: long names\n" +
            "scroll horizontally on the focused row so you never lose the tail.\n");
        WriteText(dir, "empty.log", string.Empty);

        // Executables: extension is what the demo color-codes on. Real
        // shebang / +x detection is out of scope.
        WriteText(dir, "deploy.sh",
            "#!/usr/bin/env bash\nset -euo pipefail\necho \"shipping…\"\n");
        WriteText(dir, "build.cmd",
            "@echo off\r\nrem windows side, just for the green color\r\necho building...\r\n");

        // Hidden file (UNIX convention — leading dot. Windows-Hidden
        // attribute would also colorize but is fiddly to set portably).
        WriteText(dir, ".hidden_secrets",
            "Pretend this is a config dotfile.\n");

        // Binary blobs, large enough to make the copy/move progress bar
        // tick visibly. Deterministic seed so the same demo every run.
        WriteBinary(dir, "image.png",                    sizeBytes: 220 * 1024,        seed: 1);
        WriteBinary(dir, "archive.zip",                  sizeBytes: 480 * 1024,        seed: 2);
        WriteBinary(dir, "large_file_for_progress.bin",  sizeBytes: 3 * 1024 * 1024,   seed: 3);

        // A nested directory so users can dive in and back out.
        var nested = System.IO.Path.Combine(dir, "nested");
        WriteText(nested, "deep_thoughts.md",
            "# nested\n\nDirectories can hold directories. Wild.\n");
        WriteText(nested, "todo.txt",
            "- [ ] add Linux/macOS verification\n- [x] ship the commander demo\n");
    }

    private static void SeedRight(string dir)
    {
        WriteText(dir, "manual.md",
            "# Manual\n\nKeys:\n- Tab swap pane\n- F1 help\n- F3 view\n- F4 duplicate\n" +
            "- F5 copy → other pane\n- F6 move → other pane\n- F8 delete\n- F10 / Esc quit\n");
        WriteText(dir, "TODO.md",
            "- [ ] copy something from the left side\n- [ ] mark with Ins or Shift+↓\n" +
            "- [ ] delete a marked batch\n- [ ] watch the marquee scroll on a long name\n");

        WriteBinary(dir, "old_archive.zip", sizeBytes: 320 * 1024, seed: 4);

        var archive = System.IO.Path.Combine(dir, "archive");
        WriteText(archive, "ledger_2025.txt", BuildLedger(months: 12));
        WriteText(archive, "summary.csv",     BuildCsv(rows: 24));
    }

    private static void WriteText(string dir, string name, string content)
    {
        var path = System.IO.Path.Combine(dir, name);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private static void WriteBinary(string dir, string name, int sizeBytes, int seed)
    {
        var path = System.IO.Path.Combine(dir, name);
        var rng  = new Random(seed);
        var buf  = new byte[64 * 1024];
        using var fs = File.Create(path);
        var remaining = sizeBytes;
        while (remaining > 0)
        {
            var chunk = Math.Min(buf.Length, remaining);
            rng.NextBytes(buf.AsSpan(0, chunk));
            // Force a few NUL bytes so the viewer's binary-detection
            // confidently treats this as not-text.
            buf[0] = 0;
            fs.Write(buf, 0, chunk);
            remaining -= chunk;
        }
    }

    private static string BuildCsv(int rows)
    {
        var sb = new StringBuilder(rows * 32);
        sb.AppendLine("id,name,score,active");
        for (var i = 1; i <= rows; i++)
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{i},row-{i:000},{(i * 17) % 100},{(i % 2 == 0).ToString().ToLowerInvariant()}");
        return sb.ToString();
    }

    private static string BuildLedger(int months)
    {
        var sb = new StringBuilder();
        sb.AppendLine("date         debit    credit   balance");
        var balance = 1000m;
        for (var m = 1; m <= months; m++)
        {
            var debit  = (m * 37m) % 200m;
            var credit = (m * 53m) % 250m;
            balance += credit - debit;
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"2025-{m:00}-01    {debit,7:0.00}  {credit,7:0.00}  {balance,8:0.00}");
        }
        return sb.ToString();
    }
}
