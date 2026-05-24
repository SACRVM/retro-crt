# Changelog

All notable changes to Retro.Crt are tracked here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
from `1.0` onward. Pre-1.0, the public API may change between minor
versions; breaking changes are called out below.

## [Unreleased]

### Added

- `Table.Print` per-cell foreground colors — a new overload takes a
  `Color?[][] cellColors` jagged array aligned to `rows`, so a status
  column can render green `Running` / red `No listener`. `null` entries
  (and short / missing rows) stay uncolored. Non-breaking: the plain
  `string[][]` overload is unchanged and collection-expression callers
  still resolve to it. Part of #26.
- `Rule.Print(title, color, width)` — a thin full-width horizontal rule
  with an optional centered title (`── THE FISHBOWL ──────`), the
  lightweight section-header companion to `Banner.Box`. Defaults to the
  terminal width; falls back to `-` without unicode and to a plain line
  without ANSI; color falls back to the theme's `Muted`. Part of #26.
- `Crt.Link(text, url)` / `Crt.WriteLink(text, url)` — OSC 8 terminal
  hyperlinks. `Link` returns the escape string (capability-aware: plain
  `text` when not a terminal) for composing into e.g. a `Table` cell;
  `WriteLink` writes it. Terminals without OSC 8 show just the label.
  Part of #26.
- `Retro.Crt.Status.Demo` — a self-hosted-app status panel sample tying
  the #26 features together: a titled `Rule`, a colored status column,
  and clickable endpoint URLs in table cells. Mirrors the Fishbowl
  migration that drove the issue.

### Changed

- `Table` column widths and padding are now ANSI/OSC-aware: cells
  measure by *visible* width, so a `Crt.Link` hyperlink (whose escape
  bytes are invisible) no longer inflates its column or breaks border
  alignment. Plain cells are unaffected. Part of #26.
- `Color.Default` (`ColorMode.Default`) — the terminal's own
  foreground / background. Emits SGR `39` / `49` instead of a concrete
  color, so a `Cell` / `ScreenBuffer` widget can inherit the terminal's
  configured background instead of forcing one (e.g. a `StickyFooter`
  over native scrollback on a non-black terminal). Mirrors
  `WithStyle(bg: null)` but is expressible inside a non-nullable `Cell`.
  Closes #24.
- `Crt.SetWindowTitle(title)` / `Crt.UseWindowTitle(title)` — set the
  terminal window title (OSC 2). The `Use*` scope sets the title and
  restores the user's previous one on dispose via the terminal's title
  stack (XTWINOPS push / pop), so the restore works even on Unix where
  `Console.Title` can't be read back. Nested scopes restore in LIFO
  order; lazily-registered `CancelKeyPress` / `ProcessExit` handlers
  restore on Ctrl-C. Control characters in the title are stripped so it
  can't break out of the escape. No-op when output is redirected or the
  host isn't a real terminal. Closes #25.
- `Crt.EnableUtf8()` — explicit opt-in that switches the console to
  BOM-less UTF-8 output (on Windows also flips the output code page to
  65001) so the box-drawing, shading, and spinner glyphs render instead
  of their ASCII fallbacks. Retro.Crt still never changes the encoding
  implicitly — it detects the active `Console.OutputEncoding` and
  degrades to ASCII when it can't represent the glyphs; this is the one
  explicit hook for the prettier output, mainly for fresh Windows
  consoles that boot on a legacy code page. Best-effort and wrapped in a
  try/catch: a harmless no-op when the host forbids the change
  (redirected stdout, no console). Returns `true` when UTF-8 glyphs will
  render after the call. Refreshes the cached capability detection so
  widgets and `Diagnostics` immediately see the new encoding.
- `Retro.Crt.StickyFooter` — persistent N-row region pinned to the
  bottom of the terminal. While alive, `Crt.Write` / `Crt.WriteLine`
  output scrolls in the region *above* it (native terminal scrollback,
  selection, and wheel intact); the footer rows are reserved out-of-
  band and repainted on demand. Mechanism is DECSTBM (`ESC[t;b r`) +
  `ESC[<n>S` to scroll existing content up at start, with a 250 ms
  background watcher that reflows the footer on terminal resize
  (SIGWINCH on Unix, polled width/height on Windows). Use once via
  `using var footer = StickyFooter.Start(2, Paint);`; `Refresh()` is
  thread-safe so worker threads can nudge it directly. Closes the gap
  between line-mode and the full alt-screen `Application`: a downstream
  app gets a fixed footer / status bar without giving up the terminal's
  own selection or scrollback. No-op fallback when
  `Crt.IsInteractive` is false or the terminal is too small to host
  both the scroll region and the reserved rows. Closes #21.
- `Application.Invalidate()` — thread-safe convenience for nudging
  the loop into repainting from a worker thread, semantically
  identical to `Root.MarkDirty()` but named for the cross-thread
  wake-up use case. The dirty-flag loop already polls every 16 ms,
  so a background `MarkDirty` reaches the screen on the next tick;
  the new entry point documents that and gives callers a single,
  obvious name for it. Addresses #22.

### Docs

- `Application.Run` XML — clarify that the loop polls
  `TerminalInput.WaitForEvent(16ms)` and is dirty-flag-driven, not
  blocked on input. Stale text from the pre-polling era claimed
  resizes only took effect on the next event; the actual loop has
  drained SIGWINCH and re-sampled the terminal size every 16 ms
  since `9ac9ddb`. Part of #22.

## [Retro.Crt.Tui 0.2.0] — 2026-05-11

### Added

- `Application.MouseCapture` (`MouseCaptureMode`) — opt-out switch for
  terminal mouse reporting. Default `Full` is the historic behaviour
  (`Crt.UseMouse()` enables xterm mode 1003+1006 so the alt-screen app
  gets click / drag / wheel events). Set to `None` to skip
  `UseMouse()` entirely: click-to-focus, scrollbar drag, and wheel
  scrolling stop working, but the terminal's native click-and-drag
  text selection stays alive inside the alt-screen viewport — so
  users can copy a single log line into chat instead of grabbing the
  whole buffer with the existing `[s] save logs` action. Set before
  `Run()`; changes after start are ignored. Closes #19.

### Docs

- README — note that the terminal's own scrollbar can stay visible
  over the alt-screen if the user's terminal profile pins it
  (`scrollbarState: "visible"` in Windows Terminal). The alt-screen
  entry sequence (`?1049h`) is shipped correctly; suppressing the
  outer scrollbar is a terminal-level setting, not a library knob.
  Addresses #20.

## [Retro.Crt.Tui 0.1.5] — 2026-05-09

### Added

- `Retro.Crt.Tui.Widgets.Separator` — one-cell-thick rule painted as
  a single repeating glyph across `Bounds`. Useful as a chrome band
  between header / content / footer rows. Default `Glyph` is U+2500
  (`─`); set to `'═'` / `'┄'` / `'·'` / `'-'` for different rule
  styles. Vertical rules are just `Bounds.Width = 1` instances with
  `Glyph = '│'` — no `Orientation` enum in v1.
- `LogViewer.UpdateLast(string text, Color? foreground = null)` —
  rewrites `Items[^1]` in place without growing the list. Foundation
  for spinner frames, download bars, graceful-shutdown countdown
  lines. Caller dispatches between `Append` (new line) and
  `UpdateLast` (rewrite tail); the widget does not model an
  "in-place entry is held" state. Documented thread-safety contract:
  call from the application thread.
- `LogViewer.MaxItems` — `int` ring cap, default `0` (= unbounded,
  historic behaviour). When `Append` would push the count past the
  cap, oldest entries are dropped from the head until the cap holds.
  Lowering `MaxItems` on a populated viewer does NOT retroactively
  trim; only `Append` enforces the cap. Sticky-tail (`IsPinnedToTail`,
  shipped in 0.1.2) survives across trims so a pinned viewport keeps
  following the tail.

## [Retro.Crt.Tui 0.1.2] — 2026-05-09

### Added

- `ScrollViewer.IsPinnedToTail` — bool getter exposing whether the
  most recent scroll write left the viewport at `MaxScrollOffset`.
  Recomputed on every `ScrollOffset` write so user input flips it
  through the public setter; auto-scroll consults it before
  following new content.
- `ScrollViewer.AutoScrollOnContentGrowth()` — protected hook for
  log-pane subclasses. Replaces the old
  `if (AutoScroll) ScrollToEnd()` pattern; only follows the tail
  when both `AutoScroll` and `IsPinnedToTail` are set.

### Fixed

- `LogViewer` (and any `ScrollViewer` subclass that follows new
  content) is now sticky-tail: once the user scrolls up to read past
  output, fresh entries no longer drag the viewport away. Pressing
  `End`, scrolling back down past the last row, or clicking the
  bottom of the scrollbar track all re-pin them to the live tail.
  Previously `AutoScroll` was a binary 'always follow' that made
  chatty log panes unreadable while scrolled up.
- `Application.DispatchMouse` drops mouse events whose modifiers
  include `Shift` so the terminal's native click-and-drag text
  selection wins on every backend. Modern terminals already swallow
  Shift+mouse client-side; the ones that forward it would otherwise
  rob the user of selection. README's TUI section documents the
  gesture.
- `Application.Run` opens a `Crt.UseHiddenCursor` scope alongside
  the alt-screen / raw / mouse / paste scopes so the real terminal
  cursor isn't left blinking at row 0/col 0 over the chrome. The
  scope tears down on `Run` exit so the user's shell gets its
  cursor back.

## [0.7.1] — 2026-05-08

### Added

- `Crt.UseHiddenCursor()` — `IDisposable` scope around the
  hide-cursor / show-cursor SGR pair. Reference-counted, with the
  same `CancelKeyPress` / `ProcessExit` safety net as
  `UseAlternateScreen` and `UseMouse` so a Ctrl-C never leaves the
  user's terminal cursorless. Pairs cleanly with `UseAlternateScreen`
  for full-screen game loops.
- `apps/Retro.Crt.Commander` — first entry in a new `apps/` tier.
  Norton Commander style two-pane file browser exercising the lib
  end-to-end: marquee-scrolling long filenames, color-coded
  entries, multi-select via `Ins` / `Shift+↑` / `Shift+↓`, file
  ops (F4 dup, F5 copy, F6 move, F8 delete) with a `█`/`░` progress
  modal mirroring `ProgressBar`'s glyphs, F3 viewer with binary
  detection. Auto-generates a fake workspace under `%TEMP%` so
  destructive ops can never escape into the real filesystem.

### Fixed

- `TerminalInput.WaitForEvent` commits a buffered lone `ESC` byte
  as a real `Escape` keypress when its wait elapses without
  follow-up bytes. The classic ESC-vs-CSI ambiguity left every
  Esc-bound shortcut (modal close, menu dismiss, app quit) feeling
  stuck until the next keystroke nudged the parser forward; the
  fix bounds Esc latency to the caller's own `timeoutMs`. New
  tests cover both the synth path and the no-regression case
  (CSI sequence split across two reads still surfaces as the
  right cursor key).
- Snake cursor flicker on Windows + glyph fallback on non-unicode
  hosts. The fix added the `Crt.UseHiddenCursor()` scope and a
  ASCII-fallback path in the renderer's glyph table.

### Performance

- `ScreenRenderer.Render` batches the whole frame's output into a
  single `Sink.Write` call instead of dribbling it cell-by-cell.
  Measurable improvement on busy frames (full-screen
  reflows, big diffs); the `Crt.Sink.Flush()` afterwards still
  pushes one syscall per frame.

## [Retro.Crt.Tui 0.1.1] — 2026-05-06

### Fixed

- Bumps the core dependency to `Retro.Crt >= 0.7.0`. The 0.1.0 nupkg
  shipped pinned to `>= 0.6.0`, but its compiled DLL referenced
  `WindowResize` (added on `main` between v0.6.0 and v0.7.0), so
  downstream consumers got a `TypeLoadException` at runtime. **Use
  this version, not 0.1.0.**

## [0.7.0] — 2026-05-06

### Added

- `Retro.Crt.Internals.WindowResize.Install()` — SIGWINCH handler
  on Unix, lightweight polling on Windows. Internal so only
  `Retro.Crt.Tui` consumes it (via `InternalsVisibleTo`), but the
  public surface is the smooth resize behavior in any
  `Retro.Crt.Tui.Application`-driven app.
- `InternalsVisibleTo("Retro.Crt.Tui")` — the Tui package can ride
  on core internals (currently `WindowResize`, `CursorState`,
  `Glyphs`) without forcing a wider public API.

## [Retro.Crt.Tui 0.1.0] — 2026-05-06

First release of the second package — Stages 3–5 of the staged
roadmap, all in one cut.

### Added

- `Retro.Crt.Tui.Layout` — `Rect`, `LayoutSize` (`Cells(int)` /
  `Star(double)`), `Split.Horizontal/Vertical`, `Dock.Peel` with
  `DockSide`. Pure geometry, span-based, zero-alloc.
- `Application` — sealed event loop on top of `ScreenBuffer` +
  `ScreenRenderer` + `TerminalInput`. Enters alt-screen, raw mode,
  mouse tracking, and bracketed paste; redraws via diff whenever a
  view marks itself dirty; tab/shift+tab cycles a focus tree; mouse
  capture between Press / Release; wheel events route to whatever
  view sits under the cursor. SIGWINCH integration on Unix, polling
  on Windows. Window size sampled *after* alt-screen + raw-mode are
  active so the first frame paints at the right dimensions.
- `View` base + `Container` base. `OnKey(KeyEvent, Application)`
  returns `bool` — `true` consumes the key and stops bubble-up.
  `OnDraw(ScreenBuffer)`, `OnMouse`, `OnPaste`, `IsFocusable`,
  `IsFocused`, `MarkDirty()`, `Bounds`.
- `Application.ShowModal(View)` / `CloseModal()` / `Modal` — single
  modal slot; while a modal is open, all input is restricted to its
  subtree and the background root sees nothing. Saves and restores
  focus across the modal lifetime.
- `Application.SetFocus(View?)` — direct focus, scope-validated, in
  addition to Tab traversal.
- Widgets: `Panel`, `Label` (with `TextAlign`), `Button` (with
  `Click` event), `LogViewer` (scrollable + scrollbar + drag thumb,
  `Append(string, Color?)` / `Clear()`), `TextBox` (single-line
  editor + `Submit` event + `TextChanged`, paste-aware), `Menu`
  (vertical list + disabled-row skipping + activate), `Dialog`
  (centered modal with title + content + buttons + `Closed`
  event), `StackPanel`.
- `ScrollViewer` — abstract base. Subclasses implement
  `ContentHeight` + `DrawContent(ScreenBuffer)`; the base provides
  scrollbar, thumb-drag, and key/wheel scrolling. `LogViewer` is
  the reference subclass.
- `Dialog.MessageBox(app, title, message)` — one-button modal
  helper for the common case.
- Bracketed paste — `Crt.UseBracketedPaste()`, `InputEventKind.Paste`,
  `View.OnPaste`, `TextBox` bulk-inserts printables atomically.
- `samples/Retro.Crt.Tui.Demo` — under-250-line widget tour
  (menu / log / textbox / button / modal dialog / paste).

## [0.6.0] — 2026-05-05

Big core release — Stages 1, 2a, and 2b of the staged roadmap.
Sets up everything `Retro.Crt.Tui` will need without taking a UI
dependency itself.

### Added

- `Retro.Crt.Input` namespace — pure parsers + types for terminal
  input. `KeyEvent` (`Key`, `Glyph`, `KeyModifiers`), `MouseEvent`
  (`MouseButton`, `MouseEventKind`), and the `InputEvent`
  tagged-union on top. `InputParser.TryParseKey` / `TryParseMouse`
  / `TryParseEvent` decode ANSI escape sequences (cursor keys,
  F1..F12 in SS3 *and* CSI forms, modifier-augmented variants),
  control bytes, Alt-prefixed printables, Ctrl+letter, and
  SGR-encoded mouse reports (xterm mode 1006). Stateless and
  zero-alloc; `InputParseStatus` separates complete from
  incomplete-buffer from invalid-sequence cases so input loops can
  buffer correctly.
- `Retro.Crt.Input.RawMode.Enter()` — per-OS raw mode scope.
  `[LibraryImport]`-backed termios on Linux + Darwin (split into
  `TermiosLinux` / `TermiosDarwin` because the `c_cc` array layouts
  differ), `SetConsoleMode` on Windows. `ISIG` is stripped by
  default; pass `keepSignals: true` to preserve Ctrl-C delivery.
  VT-input only on Windows.
- `Retro.Crt.Input.TerminalInput` — stdin reader that drives the
  parser with a small UTF-8 byte buffer + decoder state. Three
  read modes: blocking `ReadEvent`, non-blocking `TryReadEvent`,
  bounded-wait `WaitForEvent(timeoutMs, out ev)`. Bracketed-paste
  envelope handled before the regular parser so injected ESC
  sequences inside a paste body don't get re-interpreted as
  cursor keys.
- `Crt.UseMouse()` — `IDisposable` scope that turns on SGR mouse
  reporting (xterm modes 1006 + 1003) on entry and off on exit.
  Reference-counted; lazy `CancelKeyPress` / `ProcessExit` handlers
  shut tracking off if a Ctrl-C kills the process before the scope
  disposes.
- `ScreenBuffer` + `ScreenRenderer` — stateful cell grid + minimal
  diff renderer. Every cell carries a `Glyph`, foreground,
  background, and `CellAttrs` (`None` / `Bold` / `Underline`); the
  renderer walks two buffers and emits cursor moves + SGR + chars
  only for cells that actually changed. Pair with
  `Crt.UseAlternateScreen()` for flicker-free game loops or hand-
  rolled TUIs. Helpers: `Clear`, `PutString` (clipping), `FillRect`
  (clipping), per-cell indexer (throws on out-of-bounds). One cell
  == one terminal column; surrogate pairs / wide East-Asian glyphs
  are not modeled in v1.
- `samples/Retro.Crt.Input.Demo` — live event probe; prints every
  decoded `InputEvent` as it arrives, useful for verifying
  modifier-byte decoding on a given terminal.
- `samples/Retro.Crt.ScreenBuffer.Demo` — short bouncing-ball demo
  inside an alternate-screen scope. Two buffers ping-ponged, only
  the ball's old + new positions get repainted per frame (~30 fps,
  flicker-free).
- `games/` directory with five ASCII showcases: Snake, Conway's
  Life (with patterns + age coloring), Matrix Rain, Space Invaders,
  Tetris. All ride on the same core stack
  (`ScreenBuffer` + `ScreenRenderer` + `RawMode` + `TerminalInput`)
  with their own per-game tick loops.

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

[Unreleased]: https://github.com/chloe-dream/retro-crt/compare/tui-v0.1.5...HEAD
[Retro.Crt.Tui 0.1.5]: https://github.com/chloe-dream/retro-crt/releases/tag/tui-v0.1.5
[Retro.Crt.Tui 0.1.2]: https://github.com/chloe-dream/retro-crt/releases/tag/tui-v0.1.2
[0.7.1]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.7.1
[Retro.Crt.Tui 0.1.1]: https://github.com/chloe-dream/retro-crt/releases/tag/tui-v0.1.1
[0.7.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.7.0
[Retro.Crt.Tui 0.1.0]: https://github.com/chloe-dream/retro-crt/releases/tag/tui-v0.1.0
[0.6.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.6.0
[0.5.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.5.0
[0.4.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.4.0
[0.3.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.3.0
[0.2.1]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.2.1
[0.2.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.2.0
[0.1.0]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.1.0
