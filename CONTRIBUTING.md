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
- Re-implementing TUI features from Spectre.Console / Terminal.Gui.
  They are complete and excellent — pick them instead if you want
  trees, tables, prompts, or live regions.
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

- New behaviour requires a test. Pure logic goes in
  `tests/Retro.Crt.Tests/` directly; anything that touches `Console.Out`
  goes in `tests/Retro.Crt.Tests/Integration/` and uses the
  `ConsoleCapture` helper.
- Tests that mutate environment variables, `Console.Out`, or the
  `TerminalCapabilities` cache must declare
  `[Collection(EnvMutatingCollection.Name)]`.

## Benchmarks

- If your change touches a hot path (anything in `Internals/`, `Crt`,
  or per-character / per-frame code in `ProgressBar` / `Typewriter` /
  `Log`), include a before/after table from
  `bench/Retro.Crt.Bench` in the PR description.

## Recording the demo cast

The demo is shipped as an asciinema cast in `docs/images/demo.cast`.
To re-record after API changes:

```bash
asciinema rec docs/images/demo.cast \
    -c "dotnet run --project samples/Retro.Crt.Demo -c Release" \
    --overwrite \
    --idle-time-limit 1
```

The README embeds the cast via `asciinema-player`. Keep the cast under
60 s — anything longer reads as bloat.

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

## Code of conduct

By participating you agree to follow the
[Code of Conduct](CODE_OF_CONDUCT.md).
