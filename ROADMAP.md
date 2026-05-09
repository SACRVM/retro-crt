# Retro.Crt Roadmap

Loose, opinionated. Order is roughly the order things landed in.
Stages 1–5 are all shipped to nuget.org as `Retro.Crt 0.7.1` plus
`Retro.Crt.Tui 0.1.2` (mono-repo, two packages, separate tag schemes).
Anything without a stage number is unscoped — interesting, but not
promised.

## Stage 1 — Core (shipped, `Retro.Crt 0.1.0` → `0.5.0`)

- ANSI emission with `NO_COLOR` / redirection / `FORCE_COLOR` detection.
- Truecolor + Standard16 palette (DOS names: `LightCyan`, `Brown`, …).
- Pascal CRT verbs: `TextColor`, `TextBackground`, `GotoXY`, `ClrScr`,
  `ClrEol`, `WithStyle` scope.
- Windows VT enablement via `LibraryImport` (`SetConsoleMode`).

## Stage 2 — Output building blocks (shipped, `Retro.Crt 0.2.0` → `0.5.0`)

- `Banner.Box` (Pascal frame, unicode + ASCII fallback) and
  `Banner.Gradient` (per-line truecolor interpolation).
- `ProgressBar` — `IDisposable` scope, throttled redraw, animation gated
  on ANSI availability (single final frame in non-interactive output).
- `Log` / `LogLevel` — `Info / Warn / Error / Debug / Success` with
  fixed-width tag, colored level, Warn/Error to `stderr`.
- `Typewriter` — character-by-character reveal with optional cursor
  (`Block` / `Underline` / `MatrixBlock`), optional fade (`Glyph` ramp /
  `AlphaRgb` truecolor brightness) and optional gradient.
- `Spinner` — animated single-line status with five frame styles
  (Pipe / Dots / Braille / Block / Arc), `IDisposable` scope, throttled
  redraw, single-frame fallback when not interactive.
- `Theme` + `Themes` presets — DOS, AmberCrt, GreenCrt, plus three
  modern dark themes (Midnight / Slate / Twilight) added in 0.5.0.
  Applied via `Crt.UseTheme` scope.
- `Prompt` — `Confirm` / `Ask` / `Select` with arrow-key menu;
  ESC-to-cancel, default highlighted, plays well with `NO_COLOR`.
- `Table` — aligned-column renderer with bold headers and optional
  Box / None border style.

## Stage 2.5 — Screen control (shipped, `Retro.Crt 0.4.0` → `0.7.1`)

- `Crt.Bell` — Pascal `Sound`-flavoured `\a` beep, gated on
  interactivity.
- `Crt.UseAlternateScreen` — alt-screen-buffer scope for vim/less/htop-
  style takeover. Cleans up on Ctrl-C and `ProcessExit`.
- `Crt.UseHiddenCursor` (added in 0.7.1) — IDisposable cursor-hidden
  scope, same Ctrl-C safety net as `UseAlternateScreen` and `UseMouse`.

## Stage 3 — Cell-grid screen buffer (shipped, `Retro.Crt 0.6.0`)

The DOS-style "cheat directly in video memory" trick — Pascal's
`mem[$B800:offset]` and friends — does not exist in modern terminals,
because terminals are byte streams, not memory-mapped char cells. But
the *effect* is reproducible by holding our own cell grid and emitting
only the diffs as ANSI, which is what `ScreenBuffer` + `ScreenRenderer`
do.

- `ScreenBuffer` — stateful cell grid (`Cell` per coordinate, each
  carrying `Glyph` + `Fg` + `Bg` + `CellAttrs`).
- `ScreenRenderer.Render(prev, current, sink)` — minimal-ANSI diff
  renderer. Walks both buffers, emits cursor moves + SGR + chars only
  for cells that actually changed. As of 0.7.1 the full frame is
  batched into one `Sink.Write` for fewer syscalls on busy frames.
- One cell = one terminal column; surrogate pairs and East-Asian wide
  glyphs are not modeled in v1.

## Stage 2a / 2b — Input (shipped, `Retro.Crt 0.6.0`)

- `Retro.Crt.Input` — `KeyEvent`, `MouseEvent`, `InputEvent` tagged
  union, `KeyModifiers` flags. `InputParser` decodes ANSI escape
  sequences (cursor keys, F1..F12 in SS3 *and* CSI forms,
  modifier-augmented variants), control bytes, Alt-prefixed printables,
  Ctrl+letter, SGR-encoded mouse reports (xterm mode 1006). Stateless
  + zero-alloc + `InputParseStatus` for buffered reads.
- `Crt.UseMouse()` — SGR mouse reporting scope (modes 1006 + 1003).
- `Crt.UseBracketedPaste()` — paste envelope so injected ESCs in a
  paste body don't masquerade as cursor keys.
- `Retro.Crt.Input.RawMode.Enter()` — per-OS termios on Linux/Darwin
  (`[LibraryImport]`), `SetConsoleMode` on Windows.
- `Retro.Crt.Input.TerminalInput` — stdin reader; `ReadEvent`
  (blocking), `TryReadEvent` (non-blocking), `WaitForEvent(timeoutMs)`
  (bounded). 0.7.1 added the lone-ESC commit so Esc-bound shortcuts
  feel snappy.

## Stage 4 — Tui Application (shipped, `Retro.Crt.Tui 0.1.0` → `0.1.2`)

The second package: `Retro.Crt.Tui` rides on the core's
`ScreenBuffer` + diff renderer + input parser to do full-screen
DOS-style UIs (Midnight Commander / Turbo Vision / Husky). Same
constraints as core: tiny, dependency-free, trim- and AOT-clean.

- `Retro.Crt.Tui.Layout` — `Rect`, `LayoutSize` (`Cells` / `Star`),
  `Split.Horizontal/Vertical`, `Dock.Peel`. Pure geometry, span-based,
  zero-alloc.
- `Application` — sealed event loop. Enters alt-screen, raw mode,
  mouse, bracketed paste; redraws via diff on dirty; tab/shift+tab
  cycles a focus tree; mouse capture between Press / Release; wheel
  routes to the view under the cursor. SIGWINCH on Unix, polling on
  Windows. Single modal slot via `ShowModal` / `CloseModal`. As of
  Tui 0.1.2 also opens a `Crt.UseHiddenCursor` scope, and drops any
  Shift+mouse event so the terminal's native text selection wins.
- `View` base + `Container` base. `OnKey -> bool` (`true` consumes,
  stops bubble-up).

## Stage 5 — Tui widgets (shipped, `Retro.Crt.Tui 0.1.0` → `0.1.2`)

- `Panel`, `Label`, `Button`, `LogViewer`, `TextBox`, `Menu`,
  `Dialog`, `StackPanel`.
- `ScrollViewer` — abstract base; subclasses implement `ContentHeight`
  + `DrawContent`. `LogViewer` is the reference subclass. Tui 0.1.2
  added sticky-tail semantics (`IsPinnedToTail` +
  `AutoScrollOnContentGrowth`) so chatty log panes no longer drag the
  viewport away while the user is reading past output.
- `Dialog.MessageBox(app, title, message)` — one-button modal
  helper.
- `Application.SetFocus(View?)` — direct focus, scope-validated.

## Showcases

Two tiers downstream of the published packages, both inside this
repo:

- `samples/` — small single-feature demos (one per stage, plus the
  Tui widget tour). Read these first when learning the lib.
- `games/` — five ASCII games (Snake, Conway's Life, Matrix Rain,
  Space Invaders, Tetris) ride directly on the core's
  ScreenBuffer + diff renderer + RawMode + TerminalInput, each with
  its own tick loop. Tui-free.
- `apps/` — substantial demo apps that exercise the lib end-to-end.
  First entry: `apps/commander/Retro.Crt.Commander` (NC-light file
  browser with marquee filenames, multi-select, copy / move / delete
  / duplicate with progress bar). Tui-free; pure-core because the
  panes are highly custom.

## Backlog (driver-led — nothing scheduled)

Nothing on this list is blocked. All four wait for a real downstream
ask before we build them; the current shipped surface covers every
observed consumer use case.

- **TextArea** — multi-line editor widget (sibling to `TextBox`).
  Husky might want this for compose / edit views.
- **`ScrollViewer` hosting an arbitrary child View** — current users
  either subclass `ScrollViewer` like `LogViewer` does, or don't need
  scrolling. A hosting variant with a declared `ContentHeight` is the
  obvious extension.
- **`Application` nesting** — stack of modal contexts instead of the
  current single slot. Single-slot has covered every use case so far.
- **Linux / macOS terminal verification** — the SIGWINCH path is
  shipped but only smoke-tested on Windows. A live demo run on each
  before any "full cross-platform" production claim.

## Things to think about, no commitment

- Sixel / Kitty graphics protocol for inline images. Charming, but
  tugs hard against the "small + boring" vibe.
- A second "tech demo" app (process viewer, hex editor, sysinfo
  dashboard) once Husky has shaped what real consumers need.
