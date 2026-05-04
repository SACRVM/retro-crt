# Retro.Crt

A Pascal CRT-Unit-style console library for modern .NET. Tiny,
dependency-free, trim- and AOT-clean.

This site hosts the API reference. The narrative documentation lives in
the [README on GitHub](https://github.com/chloe-dream/retro-crt) — quick
links:

- [Install + how to use](https://github.com/chloe-dream/retro-crt#how-to-use)
- [Roadmap](https://github.com/chloe-dream/retro-crt/blob/main/ROADMAP.md)
- [Benchmarks](https://github.com/chloe-dream/retro-crt/blob/main/bench/BENCHMARKS.md)
- [Contributing](https://github.com/chloe-dream/retro-crt/blob/main/CONTRIBUTING.md)

## Public surface at a glance

### Core

- [`Crt`](api/Retro.Crt.Crt.html) — Pascal verbs (`TextColor`, `GotoXY`,
  `ClrScr`, `ClrEol`, `Bell`), `WithStyle` / `UseTheme` / `WithSink` /
  `UseAlternateScreen` scopes, capability accessors (`ColorEnabled`,
  `Depth`, `IsInteractive`, `WindowWidth`, `CursorLeft`, `Sink`).
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

- [`Theme`](api/Retro.Crt.Theme.html) — record struct with seven slots
  (Background, Foreground, Accent, Muted, Success, Warn, Error).
- [`Themes`](api/Retro.Crt.Themes.html) — built-in palettes (`Dos`,
  `AmberCrt`, `GreenCrt`, `Amiga`, `C64`, `NortonCommander`).
- Apply with `Crt.UseTheme(Themes.AmberCrt)` — widgets fall back to the
  theme's slots when no explicit color is passed.

### Diagnostics

- [`Diagnostics`](api/Retro.Crt.Diagnostics.html) — `Capture()` returns
  a [`TerminalReport`](api/Retro.Crt.TerminalReport.html) with
  `ColorDepth`, `IsInteractive`, encoding, env vars; `ToString()` gives
  a one-line dense summary for support tickets.

Browse the full namespace in the [API reference](api/Retro.Crt.html).
