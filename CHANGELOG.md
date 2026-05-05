# Changelog

All notable changes to Retro.Crt are tracked here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
from `1.0` onward. Pre-1.0, the public API may change between minor
versions; breaking changes are called out below.

## [Unreleased]

### Added

- `Retro.Crt.Input` namespace — pure parsers + types for terminal input.
  `KeyEvent` (with `Key`, `Glyph`, `KeyModifiers`), `MouseEvent` (with
  `MouseButton`, `MouseEventKind`), and the `InputEvent` tagged-union
  on top. `InputParser.TryParseKey` / `TryParseMouse` / `TryParseEvent`
  decode ANSI escape sequences (cursor keys, F1..F12 in SS3 *and* CSI
  forms, modifier-augmented variants), control bytes, Alt-prefixed
  printables, Ctrl+letter, and SGR-encoded mouse reports (xterm mode
  1006). Stateless and zero-alloc; `InputParseStatus` separates
  complete from incomplete-buffer from invalid-sequence cases so input
  loops can buffer correctly. Reading stdin in raw mode and dispatching
  parsed events is a follow-up (Stage 2b — needs the OS-specific
  termios / SetConsoleMode work).
- `Crt.UseMouse()` — `IDisposable` scope that turns on SGR mouse
  reporting (xterm modes 1006 + 1003) on entry and disables them on
  exit. Reference-counted so nesting works; lazy `CancelKeyPress` /
  `ProcessExit` handlers shut tracking off if a Ctrl-C kills the
  process before the scope disposes. Emit-side only — to actually
  receive the events you need a stdin reader in raw mode.
- `ScreenBuffer` + `ScreenRenderer` — a stateful cell grid plus a
  minimal-ANSI diff renderer. Every cell carries a `Glyph`, foreground,
  background, and `CellAttrs` (`None` / `Bold` / `Underline`); the
  renderer walks two buffers and emits cursor moves + SGR + chars only
  for cells that actually changed. Pair with
  `Crt.UseAlternateScreen()` to drive flicker-free game loops or
  manual TUIs. Helpers: `Clear`, `PutString` (clipping), `FillRect`
  (clipping), per-cell indexer (throws on out-of-bounds). One cell ==
  one terminal column; surrogate pairs / wide East-Asian glyphs are
  not modeled in v1.
- `samples/Retro.Crt.ScreenBuffer.Demo` — short bouncing-ball demo
  inside an alternate-screen scope. Two buffers ping-ponged, only the
  ball's old + new positions get repainted per frame (~30 fps,
  flicker-free).

## [0.5.0] — 2026-05-05

### Added

- `Crt.PaintBackground()` — fills every visible viewport cell with the
  active SGR background by writing spaces. ECMA-48 says
  `Crt.ClrScr()` erases with the current bg, but real-world `bce`
  compliance varies; this is the bulletproof variant. Set the bg
  first via `WithStyle(bg: …)` or `TextBackground(…)`. Pairs with
  `UseAlternateScreen` for full-screen retro takeovers. Cursor
  returns to (1, 1).
- Three modern dark themes alongside the existing retro presets:
  `Themes.Midnight` (deep blue-charcoal, periwinkle/mint/coral
  pastels), `Themes.Slate` (neutral charcoal, cool cyan-leaning
  pastels), `Themes.Twilight` (deep aubergine, magenta/orchid
  pastels). All truecolor, all picked up automatically by
  `Themes.All`.
- `samples/Retro.Crt.AltScreen.Demo` — short showcase for
  `UseAlternateScreen`: takes over the terminal under the AmberCrt
  theme, prints a themed banner, beeps once, then leaves — and the
  user's shell content above the demo prompt is exactly as they
  left it.

### Changed

- **Breaking:** `Theme.Background` removed. Themes now carry
  foreground / accent / muted / status colors only — the terminal's
  own background shows through. Cell-by-cell space-fill tricks for
  painting a colored background were never reliable enough to ship
  as part of a preset and only set expectations the library couldn't
  keep. If a build genuinely needs a colored background, set it
  explicitly per call via `Crt.WithStyle(bg: …)` or
  `Crt.TextBackground(…)`. `UseTheme` now emits only the theme
  foreground on entry and `RESET` on exit.
- **Breaking:** `Themes.Amiga`, `Themes.C64`, and
  `Themes.NortonCommander` removed. Their identity hinged on a
  bright colored background that themes no longer own; without it
  they no longer reproduced the era they were named for. The retro
  preset list is now `Dos`, `AmberCrt`, `GreenCrt`.
- `samples/Retro.Crt.Boot.Demo` no longer paints a background under
  `UseAlternateScreen`; it just clears the alt-screen with `ClrScr`
  and runs the themed boot sequence on the terminal's native bg.
- `samples/Retro.Crt.Themes.Demo` drops the per-row padding that was
  only needed to make `theme.Background` render as a visible band.
  Each preset is now shown as a small banner + body / status / footer
  scene in its own foreground colors.

## [0.4.0] — 2026-05-04

### Added

- `Crt.Bell()` — Pascal `Sound`-flavoured beep. Emits `BEL` (`\a`) and
  flushes so the terminal actually rings; gated on `IsInteractive` so
  piped output stays quiet. Predates ANSI, so it works under `NO_COLOR`
  and on legacy hosts that never enabled VT.
- `Crt.UseAlternateScreen()` — `IDisposable` scope around the alternate
  screen buffer (`\x1b[?1049h/l`), the same pair `vim` / `less` / `htop`
  use: the user's previous shell content is preserved by the terminal
  and restored verbatim on dispose, no scrollback leak. Reference-counted
  so nesting only flips the buffer on the outermost transition.
  `CancelKeyPress` and `ProcessExit` handlers register lazily on first
  use to restore the normal screen if the process is Ctrl-C'd or killed
  before the scope disposes.

## [0.3.0] — 2026-05-04

### Added

- `Theme` record + `Themes` static class with six era-faithful presets:
  `Dos`, `AmberCrt`, `GreenCrt`, `Amiga`, `C64`, `NortonCommander`. Pure
  data (no global state); compose with any color-accepting API. All
  truecolor, with documented graceful fallback on Standard16-only
  terminals. `Themes.All` exposes the full list for pickers and demos.
- `Crt.UseTheme(theme)` — applies a theme's foreground / background as
  SGR for the duration of the scope. Widgets with an optional
  `color` / `fg` parameter (Banner, Log, ProgressBar, Spinner, Table,
  Prompt, Typewriter) fall back to the matching theme slot when the
  caller doesn't supply one, so themed output stays consistent without
  threading colors through every call site.
- `Crt.WithSink(sink)` — routes all Retro.Crt output (and `Log` to both
  out and err) to a custom `TextWriter` for the duration of the scope.
- `Crt.Depth` / `Crt.ColorEnabled` / `Crt.IsInteractive` /
  `Crt.CursorLeft` / `Crt.WindowWidth` / `Crt.WindowHeight` /
  `Crt.CurrentTheme` / `Crt.Sink` — public accessors covering capability,
  layout, and active scope state, so consumers can branch on terminal
  capability without poking at internals.
- `ColorDepth` enum (`None` / `Standard16` / `Xterm256` / `Truecolor`)
  and `Color.For(depth)` quantizer, matching what
  `TerminalCapabilities` actually picks per host.
- `Spinner` — single-line animated spinner with five frame styles
  (`Pipe`, `Dots`, `Braille`, `Block`, `Arc`). `using var s =
  Spinner.Show("…")` ergonomics; `Update` to change the label
  mid-spin; `Stop(finalLabel, finalColor)` to leave a closing state in
  place. Unicode styles fall back to `Pipe` on non-unicode terminals.
  Without ANSI the label is written once and the spinner does not
  animate, keeping log files clean.
- `Prompt` — tiny interactive prompts: `Confirm` (yes/no, single
  keystroke), `Ask` (full line with optional default echoed in
  brackets), `Select` (arrow-key menu, returns the chosen index).
  All zero-dependency. `Confirm` and `Ask` work everywhere; `Select`
  uses ANSI cursor moves to redraw the active option in place and
  silently falls back to a numbered list with `Console.ReadLine`
  when ANSI is unavailable, so it stays useful in pipes and dumb
  terminals.
- `Table.Print(headers, rows, border, headerColor, borderColor)` —
  tiny aligned-column table renderer. Box-drawing borders by default
  (`TableBorder.Box`), borderless variant (`TableBorder.None`), header
  rendered bold, optional foreground colors for header and borders.
  Columns auto-resize to their widest cell. ASCII fallback
  (`+`/`-`/`|`) on non-unicode terminals; plain-text emission when
  ANSI is off so the table survives redirection. Deliberately small
  surface: no row borders between body rows, no alignment options, no
  multi-line cells.
- `TypewriterCursor.MatrixBlock` — chunky full-block (`█`) cursor for
  the "Wake up, Neo" aesthetic. ASCII fallback `#` on non-unicode
  terminals.
- `Typewriter.Blink(totalMs, cursor, fg, blinkRateMs)` and
  `Typewriter.BlinkAsync(...)` — sit at the current cursor position
  and toggle a fake cursor on/off for the requested duration, then
  leave a clean cell. Useful for Matrix-style pause-and-blink beats
  between typed phrases. Without ANSI it degrades to a plain sleep.
- Three new sample showcases under `samples/`:
  - `Retro.Crt.Themes.Demo` — walks through every built-in theme
    side by side so the palette differences are immediately visible.
  - `Retro.Crt.Matrix.Demo` — the iconic "Wake up, Neo" cinematic
    using `MatrixBlock` cursor + `Blink` + `GreenCrt` theme.
  - `Retro.Crt.Boot.Demo` — fake AMIBIOS POST sequence with
    Banner, Typewriter, Spinner, ProgressBar, Log, and a blinking
    `C:\>` shell prompt. Comprehensive feature usage in one
    nostalgia-bath.
  - `Retro.Crt.Capabilities.Demo` — runs the same scene under
    every color depth so the four-tier fallback is visible side by
    side.
- `scripts/record-demo.ps1` and `record-demo.sh` now accept a `-Demo`
  parameter (`tour` / `themes` / `matrix` / `boot`) to record any of
  the showcases as `docs/images/<demo>.cast` plus an animated GIF.
- `scripts/record-fallback.ps1` records the four color-depth tiers
  (truecolor / 256 / 16 / no-color) as separate casts.
- Public-API lock: `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`
  via the Microsoft.CodeAnalysis.PublicApiAnalyzers, so unintended
  surface drift fails the build.

### Fixed

- `TypewriterFade.Alpha` ramp now starts at 0 (invisible against dark
  terminal backgrounds) instead of 25%, so the fade-in is actually
  perceivable at typical per-char paces. The narrow 25%→100% band
  often read as "no fade" on common monitors. Final frame still
  lands on the exact target color so subsequent styling matches.
- `Typewriter.Type` cursor visibility: the fake cursor (`Block`,
  `Underline`, `MatrixBlock`) was emitted *after* the per-char dwell,
  not before, which meant it flashed for ~0 ms before the next
  iteration overwrote it via cursor-left. The per-char `Sleep` /
  `Task.Delay` now happens *after* the cursor glyph is written, so
  it's actually visible during the dwell and visibly trails each
  character as expected. Alpha-fade chars still consume their own time
  internally and are excluded from the extra sleep.
- Demo typewriter uses `Underline` cursor + restores a dedicated alpha
  line so the recorded cast doesn't leave a stray block glyph behind.
- Demo spinner uses ASCII `Pipe` glyphs by default so the recorded
  cast renders identically across font setups.

### Changed

- GitHub Actions workflows bumped to Node 24-compatible action versions
  to keep CI on currently-supported runners.

## [0.2.1] — 2026-05-03

### Added

- `Color.TryParse`, `TryFromHex`, `TryFromName`, `FromHex` — parse
  `#RRGGBB`, `#RGB`, and DOS palette names. Configuration-friendly,
  AOT-clean (no reflection / dictionary).
- `Diagnostics.Capture()` returning a `TerminalReport` snapshot with
  `ToString()` for one-line dense logging.
- `Typewriter.TypeAsync` / `TypeLineAsync` with full `CancellationToken`
  support — cancellation aborts mid-string and the cleanup path still
  erases the trailing cursor, resets color, and re-shows the terminal
  cursor before propagating `OperationCanceledException`.
- `bench/Retro.Crt.Bench` BenchmarkDotNet project + `BENCHMARKS.md`
  baseline numbers.
- DocFX site under `docs/`, deployed to GitHub Pages via
  `.github/workflows/docs.yml`.
- Stryker mutation-testing config + scheduled CI workflow.
- Release pipeline: `.github/workflows/release.yml` packs and publishes
  to nuget.org on `v*.*.*` tag pushes, with tag↔csproj version check
  and a GitHub Release artifact.
- Issue and PR templates, security policy, contributing guide,
  dependabot config.

### Changed

- `ProgressBarRenderer.RenderFrame` rewritten as a single
  `string.Create` call. Allocations dropped from ~300 B to ~110 B per
  frame (-65 %), wall-clock from ~84 ns to ~29 ns (-66 %).
- `AnsiCodes.Foreground` / `Background` for the 16 standard palette
  slots are now zero-alloc (precomputed strings).
- `Diagnostics.Capture` redirection check simplified
  (`SafeIsOutRedirected` mirrors `SafeIsErrRedirected`); `TerminalReport`
  uses property initializers instead of duplicating defaults in the
  parameterless constructor.
- CI workflow extended with trim-publish and AOT-publish smoke jobs on
  Linux + macOS, plus per-OS coverage upload.

### Fixed

- `ProgressBarRenderer.MaxWidth` capped at 200 (was 1024) to avoid
  pathological allocations.
- Dead `AnsiCodes.CarriageReturnAndClear` constant removed.
- `Typewriter` skips the fake-cursor write after `\r` / `\n` so
  multi-line reveals no longer clobber the new line.

## [0.2.0] — 2026-05-02

### Added

- `Banner.Box` (framed, unicode + ASCII fallback) and `Banner.Gradient`
  (per-line truecolor interpolation).
- `ProgressBar` — `IDisposable` redraw scope with throttled in-place
  updates, hidden cursor for the lifetime, single final frame when
  ANSI is unavailable.
- `Log` / `LogLevel` — `Info / Warn / Error / Debug / Success` with
  fixed-width level tag, `Warn` and `Error` routed to `stderr`.
- `Typewriter` / `TypewriterCursor` / `TypewriterFade` —
  character-by-character reveal with optional fake cursor and optional
  alpha fade-in (truecolor brightness ramp on the final glyph). Native
  cursor hidden for the whole reveal.
- `ROADMAP.md` capturing the planned Stage 3 cell-grid screen buffer.

### Fixed

- BIOS-vs-SGR Standard16 color mapping: BIOS swaps Blue/Red and
  Cyan/Yellow against ANSI SGR. Now via `BiosToSgr` lookup, with full
  coverage in `AnsiCodesTests`.

## [0.1.0] — 2026-04-XX

### Added

- Initial release: ANSI emission with `NO_COLOR` / redirection /
  `FORCE_COLOR` detection, truecolor + Standard16 palette, Pascal CRT
  verbs (`TextColor`, `TextBackground`, `GotoXY`, `ClrScr`, `ClrEol`,
  `WithStyle`), Windows VT enablement via `LibraryImport`.

[Unreleased]: https://github.com/chloe-dream/retro-crt/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.4.0
[0.3.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.3.0
[0.2.1]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.2.1
[0.2.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.2.0
[0.1.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.1.0
