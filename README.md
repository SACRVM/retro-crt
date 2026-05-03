# Retro.Crt

[![ci](https://github.com/chloe-dream/retro-crt/actions/workflows/ci.yml/badge.svg)](https://github.com/chloe-dream/retro-crt/actions/workflows/ci.yml)
[![docs](https://github.com/chloe-dream/retro-crt/actions/workflows/docs.yml/badge.svg)](https://chloe-dream.github.io/retro-crt)
[![nuget](https://img.shields.io/nuget/v/Retro.Crt.svg)](https://www.nuget.org/packages/Retro.Crt)
[![license](https://img.shields.io/github/license/chloe-dream/retro-crt.svg)](LICENSE)

**Tiny, zero-dep, AOT-clean Pascal-CRT charm for .NET CLIs.**

Pascal CRT-Unit verbs (`TextColor`, `GotoXY`, `ClrScr`, `ClrEol`),
truecolor with graceful 16-color and `NO_COLOR` fallback, and a small
set of curated output blocks — framed banners, in-place progress bars,
a five-level logger, and a typewriter that fades characters in.

```bash
dotnet add package Retro.Crt
```

```csharp
using Retro.Crt;

Crt.TextColor(Color.LightCyan);
Crt.WriteLine("system online.");

using (Crt.WithStyle(Color.Yellow, bold: true))
    Crt.WriteLine("> ready.");
```

> **Demos to run live** (cast recordings coming soon):
>
> ```bash
> dotnet run --project samples/Retro.Crt.Demo            # 25 s feature tour
> dotnet run --project samples/Retro.Crt.Themes.Demo     # all 6 themes side by side
> dotnet run --project samples/Retro.Crt.Matrix.Demo     # "Wake up, Neo" cinematic
> dotnet run --project samples/Retro.Crt.Boot.Demo       # fake AMIBIOS POST + DOS prompt
> ```
<!-- TODO: record asciinema casts via ./scripts/record-demo.{ps1,sh} — see CONTRIBUTING.md -->

API reference: <https://chloe-dream.github.io/retro-crt>.
Targets `net10.0`. No third-party dependencies.

## Why

Spectre.Console is great, but it does not trim or AOT cleanly, and a
launcher that ships as a 12 MB single binary cannot afford the runtime
weight. Retro.Crt is the small, opinionated alternative for tools that
want curated colored output, a banner, a progress bar, and nothing else.

### Comparison

|                        | Retro.Crt  | Spectre.Console | Pastel    | Crayon |
|------------------------|------------|-----------------|-----------|--------|
| Trim / AOT clean       | ✅          | ❌              | ✅        | ✅      |
| Runtime dependencies   | 0          | many            | 0         | 0      |
| Truecolor              | ✅          | ✅              | ✅        | ❌      |
| Pascal-flavoured verbs | ✅          | ❌              | ❌        | ❌      |
| Framed banner          | ✅          | ✅              | ❌        | ❌      |
| Progress bar           | ✅ (single)| ✅ (multi/live) | ❌        | ❌      |
| Tables / trees / forms | ❌          | ✅              | ❌        | ❌      |
| Live regions / layout  | ❌          | ✅              | ❌        | ❌      |
| Markup language        | ❌          | ✅              | ❌        | ❌      |
| Built-in logger        | ✅ (tiny)  | ❌              | ❌        | ❌      |

If you need tables, trees, forms, layouts, or a markup language —
**use Spectre.Console**. If you need a 12 MB AOT launcher with a
charming splash screen, four log levels, and a single progress bar —
this library.

## How to use

### Colors and styling

The 16 classic DOS palette names (`LightCyan`, `Brown`, …) map onto the
user's terminal theme via SGR codes — so themed terminals (Solarized,
Dracula, …) keep their identity. Use `Color.Rgb(r, g, b)` for truecolor.

```csharp
Crt.TextColor(Color.LightGreen);
Crt.Write("ok");
Crt.ResetColor();

using (Crt.WithStyle(fg: Color.Rgb(255, 140, 0), bold: true))
    Crt.WriteLine("warning, in orange");
```

`Crt.ColorEnabled` reflects whether escapes will actually reach the
terminal (false when output is redirected, `NO_COLOR` is set, or VT
enablement failed on Windows). `FORCE_COLOR=1` overrides redirection
detection.

`Color.TryParse`, `Color.TryFromHex`, and `Color.TryFromName` accept
hex strings (`#RRGGBB`, `#RGB`, with or without the leading hash) and
the canonical DOS palette names (`LightCyan`, `Brown`, …, case
insensitive). Useful for reading colors from config files.

```csharp
if (Color.TryParse(userInput, out var c))
    Crt.TextColor(c);
```

### Themes

Six built-in palettes that evoke specific eras, all in truecolor:

- `Themes.Dos` — classic IBM PC / DOS prompt
- `Themes.AmberCrt` — amber phosphor monochrome terminal
- `Themes.GreenCrt` — green phosphor monochrome terminal
- `Themes.Amiga` — Workbench 1.x orange-on-blue
- `Themes.C64` — Commodore 64 boot screen
- `Themes.NortonCommander` — deep blue with yellow highlights

Themes are **pure data** — pick the colors you want and pass them to
any color-accepting API. No global state, no theme manager.

```csharp
var t = Themes.AmberCrt;

Banner.Box(["RETRO TERMINAL", "v1.0"], fg: t.Accent);

using (Crt.WithStyle(fg: t.Foreground, bg: t.Background))
    Crt.WriteLine(" system online");

using (Crt.WithStyle(fg: t.Error, bold: true))
    Crt.WriteLine(" disk i/o failure");
```

Each theme exposes `Background`, `Foreground`, `Accent`, `Muted`,
`Success`, `Warn`, and `Error` slots. `Themes.All` returns the full
list — handy for theme pickers and demos.

Truecolor only. On Standard16-only terminals the closest SGR slot is
used, which means the *user's* terminal theme tints the result. For a
faithful retro look, viewers need a truecolor terminal (Windows
Terminal, iTerm2, modern xterm).

### Diagnostics

```csharp
var report = Diagnostics.Capture();
Console.WriteLine(report);
// ansi=on unicode=on redirected=no TERM=xterm-256color
//   enc=utf-8(65001) os=linux
```

Use this in a startup hook when a user reports "I don't see colors" — the
one-line summary usually contains the answer (`NO_COLOR=set`,
`redirected=stdout`, `enc=us-ascii`, …).

### Pascal CRT verbs

```csharp
Crt.ClrScr();
Crt.GotoXY(10, 5);     // 1-based, like the original CRT unit
Crt.Write("hi");
Crt.ClrEol();
```

### Banner

```csharp
Banner.Box("Retro.Crt 0.2", fg: Color.LightCyan);

Banner.Box(
    ["Retro.Crt 0.2", "Banner / Bar / Log / Typewriter"],
    fg: Color.LightCyan);

Banner.Gradient(
    asciiArtLines,
    from: Color.Rgb(80, 220, 255),
    to:   Color.Rgb(255, 120, 175));
```

`Box` uses unicode box-drawing glyphs when the terminal can render them,
and falls back to `+--+` on legacy ASCII code pages. `Gradient`
interpolates per line; both endpoints must be truecolor or it falls back
to `from`.

### ProgressBar

```csharp
using var bar = ProgressBar.Start(
    total: 4_500_000,
    width: 30,
    label: " download",
    color: Color.LightCyan);

for (var i = 0; i <= 100; i++)
{
    bar.Set(i * 45_000);
    Thread.Sleep(40);
}
```

The bar redraws in place on every `Set` / `Tick`, hides the terminal
cursor for its lifetime, and prints a single final frame on `Dispose`.
When ANSI is unavailable (output redirected, `NO_COLOR`, dumb terminal)
intermediate updates are suppressed and only the final frame is written —
so log files do not end up with sixty progress lines in a row.

### Spinner

Single-line animated spinner for "this might take a moment" gestures.

```csharp
using var s = Spinner.Show("connecting…");
// ... do work ...
s.Stop("connected", Color.LightGreen);
```

`SpinnerStyle` picks the frame set:

- `Pipe` — classic ASCII rotator `| / - \` (default; works everywhere)
- `Dots` — three trailing dots
- `Braille` — smooth 10-frame unicode spinner
- `Block` — rotating quarter blocks
- `Arc` — rotating quarter-circle arcs

The unicode styles silently fall back to `Pipe` on terminals without
unicode encoding support. Note: `Braille`, `Block`, and `Arc` also
require the **font** to ship the relevant unicode ranges — Cascadia
Code, JetBrains Mono, and Fira Code all do; many system defaults do
not. If you see `?` instead of glyphs, switch font or stick with
`Pipe` / `Dots`.

```csharp
using var s = Spinner.Show(
    "downloading",
    style: SpinnerStyle.Braille,
    color: Color.LightCyan,
    msPerFrame: 80);

s.Update("downloading… 50%");
// later:
s.Stop("downloaded 4.5 MB", Color.LightGreen);
```

The spinner owns its line for its lifetime — route writes through
`Update` / `Stop` rather than calling `Crt.Write` while it spins, or
your output gets clobbered. Without ANSI support (output redirected,
`NO_COLOR`, dumb terminal) the spinner does not animate: it writes the
label once and a newline on `Stop`, so log files stay clean.

### Table

Tiny aligned-column tables. Box-drawing borders by default, header in
bold, optional foreground for header and borders. One column auto-
resizes to its widest cell — no manual width configuration.

```csharp
Table.Print(
    headers: ["Demo",   "Time", "Vibe"],
    rows:    [
        ["tour",   "24s", "feature tour"],
        ["themes", "16s", "all 6 themes"],
        ["matrix", "25s", "wake up, neo"],
        ["boot",   "22s", "AMIBIOS POST"],
    ],
    headerColor: Color.LightCyan,
    borderColor: Color.DarkGray);
```

Renders as:

```
┌────────┬──────┬──────────────┐
│ Demo   │ Time │ Vibe         │
├────────┼──────┼──────────────┤
│ tour   │ 24s  │ feature tour │
│ themes │ 16s  │ all 6 themes │
│ matrix │ 25s  │ wake up, neo │
│ boot   │ 22s  │ AMIBIOS POST │
└────────┴──────┴──────────────┘
```

Pass `border: TableBorder.None` for a borderless variant — columns are
still aligned, header still bold, but no box glyphs:

```csharp
Table.Print(headers, rows, border: TableBorder.None);
```

ASCII fallback (`+`/`-`/`|`) on terminals without unicode. When ANSI
is unavailable the table is emitted as plain text — colors disappear,
structure stays intact.

Deliberately small surface: no row borders between body rows, no
alignment options, no multi-line cells. Reach for `Spectre.Console`
if you need any of that.

### Prompt

Interactive prompts that stay tiny and dependency-free.

```csharp
if (!Prompt.Confirm("Continue?", defaultYes: true))
    return;

var name = Prompt.Ask("Your name?", defaultValue: "guest");

var idx = Prompt.Select(
    "Pick a color:",
    ["Red", "Green", "Blue"],
    initialIndex: 1,
    color: Color.LightCyan);
```

- `Confirm` reads a single keystroke (no Enter required); `y` / `Y` →
  true, `n` / `N` → false, Enter → `defaultYes`. Other keys are
  ignored. The chosen letter is echoed before the line ends.
- `Ask` reads a full line. If `defaultValue` is set it appears in the
  prompt as `[default]` and is returned when the user presses Enter
  on empty input.
- `Select` is an arrow-key menu — Up/Down to move, Enter to choose.
  The active option is prefixed with `>` and rendered in `color`
  + bold. Without ANSI it falls back to a numbered list with
  `Console.ReadLine` so it works in pipes and dumb terminals.

`Confirm` and `Ask` work everywhere; `Select`'s animated mode requires
ANSI escape support.

### Log

```csharp
Log.Debug("loading config from /etc/retro");
Log.Info("system online");
Log.Success("checksum verified");
Log.Warn("disk usage at 84%");
Log.Error("failed to bind port 8080");
```

Format: `HH:MM:SS  LEVEL  message` with a fixed five-char level tag so
columns line up. `Warn` and `Error` go to `stderr`; everything else to
`stdout`.

### Typewriter

Reveals text one character at a time. Optional fake cursor between
characters and optional alpha fade-in (the final glyph appears in its
target color but ramps from dim to full brightness). The terminal's
native cursor is hidden for the whole reveal.

```csharp
Typewriter.TypeLine(
    "system online.",
    msPerChar: 25,
    fg: Color.LightCyan);

Typewriter.TypeLine(
    "with a fake cursor...",
    msPerChar: 30,
    fg: Color.LightGreen,
    cursor: TypewriterCursor.Block);

Typewriter.TypeLine(
    "alpha fade-in (truecolor)...",
    msPerChar: 50,
    fg: Color.Rgb(255, 120, 200),
    fade: TypewriterFade.Alpha);

Typewriter.TypeLine(
    "gradient + cursor + alpha fade",
    msPerChar: 40,
    cursor: TypewriterCursor.Block,
    fade: TypewriterFade.Alpha,
    gradient: (Color.Rgb(80, 220, 255), Color.Rgb(255, 120, 175)));
```

`Alpha` fade requires truecolor — on Standard16 it silently degrades to
no fade (Standard16 has no brightness scaling). When ANSI is off both
cursor and fade are skipped and the string is dumped at full speed —
still typed, but without animation residue in logs.

For Matrix-style "Wake up, Neo" beats, pair `TypewriterCursor.MatrixBlock`
(the chunky `█`) with `Typewriter.Blink` between phrases:

```csharp
Typewriter.Blink(800, TypewriterCursor.MatrixBlock, fg: Color.LightGreen);
Typewriter.Type("wake up, neo...", msPerChar: 70,
    fg: Color.LightGreen, cursor: TypewriterCursor.MatrixBlock);
Typewriter.Blink(900, TypewriterCursor.MatrixBlock, fg: Color.LightGreen);
```

`Blink` sits at the current cursor position and toggles the cursor
glyph on/off for the requested duration, then erases it. With ANSI
off it falls back to a plain sleep. `BlinkAsync` is the cancellable
variant.

The cursor and fade animations assume one terminal cell per `char`, so
emoji (surrogate pairs), combining marks, and wide CJK glyphs aren't
correctly tracked. Stick to BMP single-cell characters when animating.

`TypeAsync` / `TypeLineAsync` are the awaitable variants — same shape,
plus a `CancellationToken`. Cancellation aborts the reveal mid-string;
the `finally` block still erases the trailing cursor, restores color,
and re-shows the terminal cursor before the
`OperationCanceledException` propagates.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    await Typewriter.TypeLineAsync(
        "running...", msPerChar: 40, fg: Color.LightCyan,
        cancellationToken: cts.Token);
}
catch (OperationCanceledException) { /* terminal is in a clean state */ }
```

## Building from source

```bash
git clone https://github.com/chloe-dream/retro-crt
cd retro-crt
dotnet build
dotnet test
dotnet run --project samples/Retro.Crt.Demo
```

## NuGet package

Published on nuget.org:
[**Retro.Crt**](https://www.nuget.org/packages/Retro.Crt). Install with:

```bash
dotnet add package Retro.Crt
```

Symbols are shipped as a `.snupkg` so `Source Link` and step-into
debugging work out of the box.

For maintainers: the full release loop (version bump, changelog, tag,
automated publish) lives in [RELEASING.md](RELEASING.md). Tagging
`vX.Y.Z` triggers `.github/workflows/release.yml`, which packs, pushes
to nuget.org, and creates a GitHub Release with the artifacts attached.

## Roadmap

See [ROADMAP.md](ROADMAP.md). Stage 3 sketches a cell-grid screen buffer
with diff renderer for Turbo-Vision-style shadows, save/restore-rect, and
flicker-free repaint.

## Status

Pre-1.0. Public API may move between minor versions until 1.0.

## License

MIT
