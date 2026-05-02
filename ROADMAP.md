# Retro.Crt Roadmap

Loose, opinionated. Order is roughly the order things should land in. Anything
without a stage number is unscoped — interesting, but not promised.

## Stage 1 — Core (shipped)

- ANSI emission with `NO_COLOR` / redirection / `FORCE_COLOR` detection.
- Truecolor + Standard16 palette (DOS names: `LightCyan`, `Brown`, …).
- Pascal CRT verbs: `TextColor`, `TextBackground`, `GotoXY`, `ClrScr`,
  `ClrEol`, `WithStyle` scope.
- Windows VT enablement via `LibraryImport` (`SetConsoleMode`).

## Stage 2 — Output building blocks (shipped)

- `Banner.Box` (Pascal frame, unicode + ASCII fallback) and
  `Banner.Gradient` (per-line truecolor interpolation).
- `ProgressBar` — `IDisposable` scope, throttled redraw, animation gated on
  ANSI availability (single final frame in non-interactive output).
- `Log` / `LogLevel` — `Info / Warn / Error / Debug / Success` with
  fixed-width tag, colored level, Warn/Error to `stderr`.
- `Typewriter` — character-by-character reveal with optional cursor
  (`Block` / `Underline`), optional fade (`Glyph` ramp / `AlphaRgb`
  truecolor brightness) and optional gradient. CSI cursor-left for
  in-place overwrite; animations auto-disabled when ANSI is off.

## Stage 3 — Screen buffer (planned)

The DOS-style "cheat directly in video memory" trick — Pascal's
`mem[$B800:offset]` and friends — does not exist in modern terminals,
because terminals are byte streams, not memory-mapped char cells. But the
*effect* is reproducible by holding our own cell grid and emitting only the
diffs as ANSI.

Goal: make Turbo-Vision-flavoured tricks possible inside a Retro.Crt app
without the user having to manage cursor positioning by hand.

Sketch:

```csharp
var screen = new Screen(80, 25);
screen.At(10, 5).PutString("Hello", fg: Color.LightCyan, bg: Color.DarkBlue);
screen.At(11, 6).Shadow();          // dim attr only, char stays
var saved = screen.SaveRect(20, 5, 40, 10);
screen.DrawBox(20, 5, 40, 10, fg: Color.Yellow);
// ... popup interaction ...
saved.Restore();
screen.Flush();                     // emits only changed cells
```

What this unlocks (all the things DOS apps used to do):

- **Shadow-wurf** without mutating the underlying text — set attr only.
- **Save/restore rectangles** for popups, dialogs, command palettes.
- **Flicker-free repaint** via dirty-cell diffing instead of full redraws.
- **Double buffer / backbuffer flip** semantics with explicit `Flush()`.
- A foundation for boxes, menus, modal dialogs (Turbo Vision lite).

Still in scope of Retro.Crt's "tiny + AOT-clean" rule — no third-party
deps, no reflection, just a `Cell[width, height]` plus a diff renderer.

## Things to think about, no commitment

- Optional alternate-screen-buffer mode (`\x1b[?1049h`) so a Retro.Crt app
  can take over the screen and restore the user's shell on exit (vim,
  less, htop style).
- Mouse events (`\x1b[?1003h`) — only if Stage 3 lands and a real consumer
  asks. Nothing worse than half-baked input handling.
- Sixel / Kitty graphics protocol for inline images. Charming, but tugs
  hard against the "small + boring" vibe.
- Bell helper (`\a`) — single line, fits Pascal `Sound`-ish nostalgia
  without the cross-platform pain of real audio.
- Spinners (`/ - \ |`, dots, braille). Probably fits ProgressBar, not its
  own type.
