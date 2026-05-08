# Contributing to Retro.Crt

Thanks for thinking about contributing — Retro.Crt is intentionally
small, opinionated, and AOT-clean. Please read the following before
opening a PR so we can save each other a round-trip.

## What fits

Yes:

- Bug fixes with a test that demonstrates the bug.
- Cross-platform compatibility fixes (Linux/macOS/Windows terminals).
- Documentation improvements (README, ROADMAP, BENCHMARKS, XML doc).
- Performance work backed by `BenchmarkDotNet` numbers (before/after).
- New features that fit the **Pascal CRT-Unit + tiny + AOT-clean**
  vibe and that have a clear consumer story.

Probably no:

- Anything that requires a NuGet dependency on the library project.
- Reflection, `Activator.CreateInstance`, JSON serialisation without a
  source-gen context, or anything else that breaks `IsTrimmable=true`.
- Tui widgets without a real consumer driving them. The Tui surface
  is deliberately small (Stage 5 + ScrollViewer abstract). New
  widgets are driver-led — when something downstream actually wants a
  TextArea or a ScrollViewer-as-host, we build it.
- Sound, audio, or process spawning of any kind.

If you're unsure, open a draft issue first and we'll talk.

## Local setup

Requires the .NET 10 SDK.

```bash
git clone https://github.com/chloe-dream/retro-crt
cd retro-crt
dotnet restore
dotnet build
dotnet test
```

Demo:

```bash
dotnet run --project samples/Retro.Crt.Demo
```

Trim / AOT smoke (recommended before any PR that touches `src/`):

```bash
dotnet publish samples/Retro.Crt.Demo \
    -c Release -p:PublishTrimmed=true -p:IsAotCompatible=true
dotnet publish samples/Retro.Crt.Demo \
    -c Release -p:PublishAot=true        # needs C/C++ build tools locally
```

## Coding rules

- C# 14 / .NET 10. Records, primary constructors, file-scoped namespaces,
  collection expressions, pattern matching.
- All P/Invoke through `[LibraryImport]`, never `[DllImport]`.
- No reflection, no `JsonSerializer.Serialize<T>` without a context, no
  `Type.GetType` by string.
- Mark trim-unsafe code (if any ever exists) with
  `[RequiresUnreferencedCode]`.
- Public surface stays minimal — add API only when a real consumer
  needs it.
- Every new public member needs an XML doc comment.
- Identifiers, comments, strings, log lines, exception messages, commit
  messages — all English.

## Tests

- Two test projects, one per package:
  - `tests/Retro.Crt.Tests/` — core. Pure logic at the root, anything
    that touches `Console.Out` under `Integration/` using the
    `ConsoleCapture` helper.
  - `tests/Retro.Crt.Tui.Tests/` — Tui (Application dispatch, focus,
    layouts, widgets, package-level smoke).
- New behaviour requires a test. The combined suite is **558+** at
  the time of writing and stays AOT/Trim-clean.
- Tests that mutate environment variables, `Console.Out`, or the
  `TerminalCapabilities` cache must declare
  `[Collection(EnvMutatingCollection.Name)]`.

## Benchmarks

- If your change touches a hot path (anything in `Internals/`, `Crt`,
  or per-character / per-frame code in `ProgressBar` / `Typewriter` /
  `Log`), include a before/after table from
  `bench/Retro.Crt.Bench` in the PR description.

## Recording the demo casts

Retro.Crt has three demo tiers in the repo: small single-feature
samples under `samples/`, five ASCII games under `games/`, and one
substantial app under `apps/`. The `record-demo` script targets the
samples (the games and apps are interactive — record those by hand
with `asciinema rec` if you want a cast).

| `-Demo`  | Project                         | Vibe                                      |
|----------|---------------------------------|-------------------------------------------|
| `tour`   | `Retro.Crt.Demo`                | The 25-second feature tour (default)      |
| `themes` | `Retro.Crt.Themes.Demo`         | All nine built-in themes side by side     |
| `matrix` | `Retro.Crt.Matrix.Demo`         | "Wake up, Neo" cinematic                  |
| `boot`   | `Retro.Crt.Boot.Demo`           | Fake AMIBIOS POST + DOS prompt            |

Use the helper scripts (they build the chosen demo, record at 80×24
to match DOS-era terminals, and render the GIF if `agg` is on PATH):

```powershell
pwsh ./scripts/record-demo.ps1                      # default: tour
pwsh ./scripts/record-demo.ps1 -Demo matrix
pwsh ./scripts/record-demo.ps1 -Demo themes
pwsh ./scripts/record-demo.ps1 -Demo boot
```

```bash
./scripts/record-demo.sh                            # default: tour
./scripts/record-demo.sh matrix
./scripts/record-demo.sh themes
./scripts/record-demo.sh boot
```

Prereqs (install once):

- `asciinema` — `winget install asciinema` on Windows, package manager
  on Linux/macOS.
- `agg` — `cargo install --git https://github.com/asciinema/agg`.
  Without it the script still records the cast; only the GIF is
  skipped.

Tuning knobs are environment variables (POSIX) or parameters
(PowerShell): `COLS`, `ROWS`, `THEME`, `SPEED`, `FONT_SIZE`. Keep the
cast under 60 s — anything longer reads as bloat. The current demo
program is tuned to ~20 s end-to-end.

## Commit messages

`<type>: <summary>` with one of:
`feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `build`, `bench`.

Body wrapped at ~72 chars. Explain the *why*, not the *what* — the diff
already has the *what*.

Example:

```
fix: erase trailing typewriter cursor before color reset

Without this the cursor glyph survives the reveal under terminals that
queue the SGR escape and apply it retroactively. Repro in
tests/.../TypewriterIntegrationTests.cs.
```

## Pull request flow

1. Open a PR against `main`.
2. CI runs build + test on Windows / Linux / macOS, plus trim and AOT
   publish smoke on Linux + macOS, plus a NuGet pack sanity check.
3. PRs that fail CI are not reviewed until green.
4. One approval is enough; please squash-merge so `main` stays linear.

## Releasing

Maintainers only. The full release loop — version bump, changelog,
tag, automated publish to nuget.org and GitHub Releases — is documented
in [RELEASING.md](RELEASING.md).

## Code of conduct

By participating you agree to follow the
[Code of Conduct](CODE_OF_CONDUCT.md).
