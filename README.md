# Retro.Crt

[![ci](https://github.com/chloe-dream/retro-crt/actions/workflows/ci.yml/badge.svg)](https://github.com/chloe-dream/retro-crt/actions/workflows/ci.yml)
[![docs](https://github.com/chloe-dream/retro-crt/actions/workflows/docs.yml/badge.svg)](https://chloe-dream.github.io/retro-crt)
[![nuget](https://img.shields.io/nuget/v/Retro.Crt.svg)](https://www.nuget.org/packages/Retro.Crt)
[![license](https://img.shields.io/github/license/chloe-dream/retro-crt.svg)](LICENSE)

**Tiny, zero-dep, AOT-clean Pascal-CRT charm for .NET CLIs.**

Pascal CRT-Unit verbs (`TextColor`, `GotoXY`, `ClrScr`, `ClrEol`,
`Bell`), truecolor with graceful 256-color, 16-color, and `NO_COLOR`
fallback, six era-faithful themes, and a small set of curated output
blocks — framed banners, in-place progress bars, animated spinners,
aligned tables, interactive prompts, a five-level logger, and a
typewriter that fades characters in. Plus alt-screen takeover for
vim/less-style apps.

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
> dotnet run --project samples/Retro.Crt.Capabilities.Demo   # color-depth fallback (FORCE_COLOR=3/2/1, NO_COLOR)
> dotnet run --project samples/Retro.Crt.AltScreen.Demo  # alt-screen takeover, restores your shell
> ```
>
> The Capabilities demo prints the same scene under whatever depth the
> host can render — set `FORCE_COLOR=3/2/1` or `NO_COLOR=1` before
> running to record the truecolor / 256 / 16 / no-color tiers as four
> separate casts via `scripts/record-fallback.ps1`.
<!-- TODO: record asciinema casts via ./scripts/record-demo.{ps1,sh} — see CONTRIBUTING.md -->

API reference: <https://chloe-dream.github.io/retro-crt>.
Targets `net10.0`. No third-party dependencies.

## Why

Spectre.Console is great, but it does not trim or AOT cleanly. For a
small CLI that publishes trim- or AOT-safe — a launcher, a build tool,
a one-shot installer — pulling Spectre in noticeably bloats the output
and breaks the trim pass. Retro.Crt is the small, opinionated
alternative for tools that want curated colored output, themed
widgets, and nothing more elaborate than a table.

### Comparison

|                        | Retro.Crt  | Spectre.Console | Pastel    | Crayon |
|------------------------|------------|-----------------|-----------|--------|
| Trim / AOT clean       | ✅          | ❌              | ✅        | ✅      |
| Runtime dependencies   | 0          | many            | 0         | 0      |
| Truecolor              | ✅          | ✅              | ✅        | ❌      |
| 256-color quantization | ✅          | ✅              | ❌        | ❌      |
| Pascal-flavoured verbs | ✅          | ❌              | ❌        | ❌      |
| Framed banner          | ✅          | ✅              | ❌        | ❌      |
| Progress bar           | ✅ (single)| ✅ (multi/live) | ❌        | ❌      |
| Spinner                | ✅          | ✅              | ❌        | ❌      |
| Aligned tables         | ✅ (basic) | ✅ (rich)       | ❌        | ❌      |
| Interactive prompts    | ✅ (3 verbs)| ✅ (rich)      | ❌        | ❌      |
| Themes                 | ✅ (6 presets)| ✅          | ❌        | ❌      |
| Trees / forms / panels | ❌          | ✅              | ❌        | ❌      |
| Live regions / layout  | ❌          | ✅              | ❌        | ❌      |
| Markup language        | ❌          | ✅              | ❌        | ❌      |
| Alt-screen takeover    | ✅          | ❌              | ❌        | ❌      |
| Built-in logger        | ✅ (tiny)  | ❌              | ❌        | ❌      |

If you need trees, forms, panels, live layouts, or a markup language —
**use Spectre.Console**. If your CLI publishes trim- or AOT-safe and
you don't want a single console UI library to be the thing that breaks
that — and you'd settle for a charming splash screen, themed output, a
few log levels, a progress bar, a spinner, simple tables, and three
flavours of prompt — this library.

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
enablement failed on Windows). `Crt.Depth` reports the actual color
depth: `Truecolor`, `Xterm256`, `Standard16`, or `None`. Truecolor
values are quantized to whatever the terminal can render — a
`Color.Rgb(255, 140, 0)` becomes a 256-cube entry on a `xterm-256color`
terminal and the closest BIOS-16 anchor on a basic VT.

Detection follows the npm convention:

- `NO_COLOR` (any value) → `None`
- `TERM=dumb` → `None`
- `FORCE_COLOR=0` → `None`; `=1` → at least 16; `=2` → 256; `=3` → truecolor
- `COLORTERM=truecolor` / `=24bit` → `Truecolor`
- `WT_SESSION` set (Windows Terminal) → `Truecolor`
- `TERM` contains `truecolor` / `direct` → `Truecolor`
- `TERM` contains `256` → `Xterm256`
- otherwise (with ANSI on) → `Standard16`

`Color.TryParse`, `Color.TryFromHex`, and `Color.TryFromName` accept
hex strings (`#RRGGBB`, `#RGB`, with or without the leading hash) and
the canonical DOS palette names (`LightCyan`, `Brown`, …, case
insensitive). Useful for reading colors from config files.

```csharp
if (Color.TryParse(userInput, out var c))
    Crt.TextColor(c);
```

### Themes

Six built-in palettes — three era-faithful retro presets and three
modern dark themes — all in truecolor:

Retro:

- `Themes.Dos` — classic IBM PC / DOS prompt
- `Themes.AmberCrt` — amber phosphor monochrome terminal
- `Themes.GreenCrt` — green phosphor monochrome terminal

Modern dark:

- `Themes.Midnight` — deep blue-charcoal, periwinkle/mint/coral pastels
- `Themes.Slate` — neutral charcoal, cool cyan-leaning pastels
- `Themes.Twilight` — deep aubergine, magenta/orchid pastels

Themes are **pure data** — pick the colors you want and pass them to
any color-accepting API, or activate one with `Crt.UseTheme(...)` so
widgets pick their colors from it automatically.

```csharp
using (Crt.UseTheme(Themes.AmberCrt))
{
    Banner.Box(["RETRO TERMINAL", "v1.0"]);   // uses theme.Accent
    Crt.WriteLine(" system online");          // theme.Foreground
    Log.Warn("disk usage at 84%");            // theme.Warn
    Log.Error("disk i/o failure");            // theme.Error
}
```

Inside the scope:

- `Crt.Write` / `Crt.WriteLine` render in `theme.Foreground` (the SGR
  is emitted on entry, `RESET` on exit).
- `Banner.Box`, `ProgressBar.Start`, `Spinner.Show`, `Prompt.*`, and
  `Typewriter.Type` fall back to `theme.Accent` (or `theme.Foreground`
  for typed text) when the caller doesn't pass a color.
- `Table.Print` picks `theme.Accent` for headers and `theme.Muted` for
  borders.
- `Log` levels route to `Foreground` / `Warn` / `Error` / `Muted` /
  `Success`.
- An explicit `fg` / `color` parameter always wins over the theme.

Themes nest cleanly — disposing the inner scope restores the outer
theme's SGR. `Crt.CurrentTheme` reflects the active theme (or `null`
when none is in effect). Without a `UseTheme` scope, the public API is
unchanged: pure data, you still pass colors yourself.

Each theme exposes `Foreground`, `Accent`, `Muted`, `Success`, `Warn`,
and `Error` slots. **Themes do not own a background** — the terminal's
own background shows through. If a specific build needs a colored
background, set it explicitly per call via `Crt.WithStyle(bg: …)` or
`Crt.TextBackground(…)`. `Themes.All` returns the full list — handy
for theme pickers and demos.

**Painting the full screen with a background color.** A `bg` SGR
only colors cells you actually write to — empty viewport space keeps
the terminal's native background. ECMA-48 says `Crt.ClrScr()` should
erase using the active SGR background and most modern terminals
(Windows Terminal, iTerm2, kitty, alacritty, gnome-terminal) honor
that, but real-world compliance varies. `Crt.PaintBackground()` is
the bulletproof variant: it fills every visible cell with spaces
under the **currently active** SGR background, so set one first via
`WithStyle(bg: …)` or `TextBackground(…)`. Pairs naturally with
`Crt.UseAlternateScreen()` for a vim/less-style fullscreen takeover
that doesn't pollute the user's shell:

```csharp
using var alt = Crt.UseAlternateScreen();
using var bg  = Crt.WithStyle(bg: Color.Rgb(20, 22, 32));
Crt.PaintBackground();    // entire alt-screen now reads as charcoal
Crt.GotoXY(1, 1);
using var theme = Crt.UseTheme(Themes.Midnight);
Banner.Box("system online", fg: Themes.Midnight.Accent);
// ... your TUI here ...
// On dispose: alt-screen restores the user's shell, theme + bg gone
```

Truecolor at the source — Retro.Crt quantizes down to 256-color or
Standard16 if the terminal can't render the full 24 bits. On
Standard16 the closest SGR slot is used, which means the *user's*
terminal theme tints the result. For a faithful retro look, viewers
need a truecolor terminal (Windows Terminal, iTerm2, modern xterm).

### Routing all output to a TextWriter

`Crt.WithSink(writer)` redirects every Retro.Crt write — Crt itself,
Banner, ProgressBar, Spinner, Prompt, Table, Typewriter, *and* Log —
to a single `TextWriter` for the duration of the scope. Useful for
capturing output in tests, writing to log files, or piping into a
side-channel without touching `Console.SetOut`.

```csharp
using var sink = new StringWriter();
using (Crt.WithSink(sink))
{
    Banner.Box("hi");
    Log.Info("captured");
    using var bar = ProgressBar.Start(total: 100, width: 20);
    for (var i = 0; i <= 100; i++) bar.Set(i);
}
// `sink` now contains the full ANSI-coloured transcript; Console.Out
// and Console.Error were never touched.
```

`Crt.Sink` exposes the active writer (override or `Console.Out`).
Scopes nest cleanly. Without an active `WithSink`, all output follows
`Console.Out` / `Console.Error` as before — `Console.SetOut` keeps
working transparently.

### Diagnostics

```csharp
var report = Diagnostics.Capture();
Console.WriteLine(report);
// ansi=on depth=truecolor unicode=on redirected=no
//   TERM=xterm-256color COLORTERM=truecolor
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
Crt.Bell();            // BEL — Pascal Sound nostalgia
```

`Bell` emits `\a` and flushes so the terminal actually rings; gated on
`Crt.IsInteractive` so piped output stays quiet. Works under `NO_COLOR`
and on legacy hosts that never enabled VT (BEL predates ANSI).

### Alternate screen

`Crt.UseAlternateScreen()` takes over the terminal with the alternate
screen buffer for the duration of the returned scope — the user's
previous shell content is preserved by the terminal itself and restored
verbatim when the scope disposes. Same trick `vim` / `less` / `htop` use:
no scrollback leak, no cleared shell, no "where did my prompt go?".

```csharp
using (Crt.UseAlternateScreen())
{
    Crt.ClrScr();
    Banner.Box("Demo", fg: Color.LightCyan);
    Typewriter.TypeLine("hello from a clean screen…");
    Prompt.Confirm("quit?");
}
// Shell content above looks exactly as the user left it.
```

Reference-counted so nesting only flips the buffer on the outermost
transition. Lazily registers `Console.CancelKeyPress` and
`AppDomain.ProcessExit` handlers on first use — if the process is
Ctrl-C'd or killed before the scope disposes, those handlers still emit
the leave sequence so the user's shell isn't left stuck on the alt
screen. No-op when output is redirected or the host isn't a real
terminal.

### Anchoring widgets to a cursor position

`GotoXY` followed by `Banner.Box`, `ProgressBar.Start`, or
`Spinner.Show` keeps the widget anchored to that column for its entire
lifetime — multi-line frames stay vertically aligned, and in-place
redraws (CR-driven) jump back to the anchor instead of column 1.

```csharp
Crt.GotoXY(20, 5);
Banner.Box(["RETRO TERMINAL", "v1.0"]);   // both lines start at col 20

Crt.GotoXY(20, 8);
using var bar = ProgressBar.Start(total: 100, width: 30);
for (var i = 0; i <= 100; i++) { bar.Tick(); Thread.Sleep(20); }
// bar redraws stay at column 20 across all frames
```

`Crt.CursorLeft` exposes the current column (0-based, defaults to 0
when the host can't report). The widget reads it once at construction;
later `GotoXY` calls have no effect on its anchor. Caveat: the anchor
follows the column, not the row — if your terminal scrolls during the
widget's lifetime, the widget's row drifts (a cell-grid screen buffer
on the roadmap will fix that).

### Banner

```csharp
Banner.Box("Retro.Crt", fg: Color.LightCyan);

Banner.Box(
    ["Retro.Crt", "Banner / Bar / Spinner / Table / Prompt / Log / Typewriter"],
    fg: Color.LightCyan);

// Stretch the frame to the current terminal width:
Banner.Box("system online", fg: Color.LightGreen, width: Crt.FillWidth);

// Centre or right-align lines that are shorter than the inner width:
Banner.Box(
    ["RETRO TERMINAL", "v1.0"],
    fg: Color.LightCyan, width: 40, align: BoxAlign.Center);

Banner.Gradient(
    asciiArtLines,
    from: Color.Rgb(80, 220, 255),
    to:   Color.Rgb(255, 120, 175));
```

`Box` uses unicode box-drawing glyphs when the terminal can render them,
and falls back to `+--+` on legacy ASCII code pages. The `width`
parameter sizes the total frame in cells; values smaller than the
longest line are clamped to fit. `Crt.FillWidth` (-1) sizes to the
current terminal. `align` picks how short lines sit inside the inner
width — `BoxAlign.Left` (default), `Center`, or `Right`. `Gradient`
interpolates per line; both endpoints must be truecolor or it falls
back to `from`.

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

Pass `width: Crt.FillWidth` to size the bar to the current terminal —
label, percent suffix, and a one-cell safety margin are subtracted
automatically.

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

`Log.MinLevel` filters lines below the threshold:

```csharp
Log.MinLevel = LogLevel.Warn;
Log.Info("won't appear");
Log.Warn("will appear");
```

Use `Log.IsEnabled(level)` to gate expensive formatting.

Routing log output elsewhere — useful for tests, log files, or
side-channels — goes through `Log.OutSink` / `Log.ErrSink` (raw
properties) or `Log.UseSink(writer)` (scoped):

```csharp
using var sink = new StringWriter();
using (Log.UseSink(sink))
{
    Log.Info("captured");
    Log.Error("also captured");
}
// Both lines landed in `sink`; Console.Out / Console.Error untouched.
```

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
