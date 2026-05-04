# Retro.Crt

**Tiny, zero-dep, AOT-clean Pascal-CRT charm for .NET CLIs.**

Pascal CRT-Unit verbs (`TextColor`, `GotoXY`, `ClrScr`, `ClrEol`,
`Bell`), truecolor with graceful 256-color, 16-color, and `NO_COLOR`
fallback, nine built-in themes (six era-faithful retro presets and
three modern dark themes), and a small set of curated output blocks —
framed banners, in-place progress bars, animated spinners, aligned
tables, interactive prompts, a five-level logger, and a typewriter
that fades characters in. Plus alt-screen takeover for vim/less-style
apps.

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
| Themes                 | ✅ (9 presets)| ✅          | ❌        | ❌      |
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

## Demos

Short live samples under `samples/` on GitHub — clone the repo and run
any of them:

```bash
dotnet run --project samples/Retro.Crt.Demo            # 25 s feature tour
dotnet run --project samples/Retro.Crt.Themes.Demo     # all 9 themes side by side
dotnet run --project samples/Retro.Crt.Matrix.Demo     # "Wake up, Neo" cinematic
dotnet run --project samples/Retro.Crt.Boot.Demo       # fake AMIBIOS POST + DOS prompt
dotnet run --project samples/Retro.Crt.Capabilities.Demo   # color-depth fallback
dotnet run --project samples/Retro.Crt.AltScreen.Demo  # alt-screen takeover, restores your shell
```

## Public surface at a glance

### Core

- [`Crt`](api/Retro.Crt.Crt.html) — Pascal verbs (`TextColor`, `GotoXY`,
  `ClrScr`, `ClrEol`, `Bell`, `PaintBackground`),
  `WithStyle` / `UseTheme` / `WithSink` / `UseAlternateScreen` scopes,
  capability accessors (`ColorEnabled`, `Depth`, `IsInteractive`,
  `WindowWidth`, `CursorLeft`, `Sink`).
- [`Color`](api/Retro.Crt.Color.html) — DOS palette, truecolor, xterm-256
  via `Color.Indexed256`, depth-aware quantization through `Color.For`,
  hex / name parsing.
- [`ColorMode`](api/Retro.Crt.ColorMode.html) — `Truecolor` / `Standard16`
  / `Xterm256`.
- [`ColorDepth`](api/Retro.Crt.ColorDepth.html) — what the terminal can
  render: `None` / `Standard16` / `Xterm256` / `Truecolor`.

### Output blocks

- [`Banner`](api/Retro.Crt.Banner.html) — framed `Box` (with
  [`BoxAlign`](api/Retro.Crt.BoxAlign.html)) and per-line `Gradient`.
- [`ProgressBar`](api/Retro.Crt.ProgressBar.html) — single-line redraw,
  cursor-anchored, `Crt.FillWidth` for terminal-width sizing.
- [`Spinner`](api/Retro.Crt.Spinner.html) — animated single-line spinner;
  pick a frame set via [`SpinnerStyle`](api/Retro.Crt.SpinnerStyle.html).
- [`Table`](api/Retro.Crt.Table.html) — aligned-column tables with
  [`TableBorder`](api/Retro.Crt.TableBorder.html) box / borderless modes.
- [`Typewriter`](api/Retro.Crt.Typewriter.html) — character-by-character
  reveal with optional cursor
  ([`TypewriterCursor`](api/Retro.Crt.TypewriterCursor.html)) and fade
  ([`TypewriterFade`](api/Retro.Crt.TypewriterFade.html)). `Blink` for
  pause beats, async + cancellation supported.
- [`Prompt`](api/Retro.Crt.Prompt.html) — `Confirm`, `Ask`, arrow-key
  `Select`.
- [`Log`](api/Retro.Crt.Log.html) — semantic logger with
  [`LogLevel`](api/Retro.Crt.LogLevel.html) tags, `MinLevel` filter,
  `OutSink` / `ErrSink` overrides, scoped `UseSink`.

### Theming

- [`Theme`](api/Retro.Crt.Theme.html) — record struct with six slots
  (Foreground, Accent, Muted, Success, Warn, Error). Themes do not own
  a background — set one explicitly per call when you need it.
- [`Themes`](api/Retro.Crt.Themes.html) — built-in palettes: retro
  (`Dos`, `AmberCrt`, `GreenCrt`) and modern dark (`Midnight`, `Slate`,
  `Twilight`).
- Apply with `Crt.UseTheme(Themes.AmberCrt)` — widgets fall back to the
  theme's slots when no explicit color is passed.

### Diagnostics

- [`Diagnostics`](api/Retro.Crt.Diagnostics.html) — `Capture()` returns
  a [`TerminalReport`](api/Retro.Crt.TerminalReport.html) with
  `ColorDepth`, `IsInteractive`, encoding, env vars; `ToString()` gives
  a one-line dense summary for support tickets.

Browse the full namespace in the [API reference](api/Retro.Crt.html).

## More

- [Full README on GitHub](https://github.com/chloe-dream/retro-crt) —
  narrative how-to-use guide with code samples for every feature.
- [Roadmap](https://github.com/chloe-dream/retro-crt/blob/main/ROADMAP.md)
  — what's shipped, planned, and parked.
- [Changelog](https://github.com/chloe-dream/retro-crt/blob/main/CHANGELOG.md)
  — release-by-release diff.
- [Benchmarks](https://github.com/chloe-dream/retro-crt/blob/main/bench/BENCHMARKS.md)
  — BenchmarkDotNet baseline numbers.
- [Contributing](https://github.com/chloe-dream/retro-crt/blob/main/CONTRIBUTING.md)
  — how to file issues, propose features, and submit PRs.
- [NuGet package](https://www.nuget.org/packages/Retro.Crt) — released
  on tag pushes via `.github/workflows/release.yml`.

## License

MIT.
