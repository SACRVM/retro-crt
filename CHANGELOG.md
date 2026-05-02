# Changelog

All notable changes to Retro.Crt are tracked here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
from `1.0` onward. Pre-1.0, the public API may change between minor
versions; breaking changes are called out below.

## [Unreleased]

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
- Issue and PR templates, security policy, contributing guide,
  dependabot config.

### Changed

- `ProgressBarRenderer.RenderFrame` rewritten as a single
  `string.Create` call. Allocations dropped from ~300 B to ~110 B per
  frame (-65 %), wall-clock from ~84 ns to ~29 ns (-66 %).
- `AnsiCodes.Foreground` / `Background` for the 16 standard palette
  slots are now zero-alloc (precomputed strings).
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

[Unreleased]: https://github.com/chloe-dream/retro-crt/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.2.0
[0.1.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.1.0
