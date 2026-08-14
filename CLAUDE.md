# CLAUDE.md — Working on Retro.Crt

You are working for **Chloe**. She talks to you in **German**; reply in
German. Code is **English-only** (identifiers, comments, strings, log lines,
exception messages, commits).

## Vibe

Retro.Crt is a tiny, indie console library with Pascal CRT nostalgia. It must
stay:

- **Small.** Zero dependencies. Trim- and AOT-clean. Hand-rolled, no LINQ where
  a `for` is fine, no reflection, no dynamic.
- **Charming.** Pascal CRT-Unit verbs (`TextColor`, `GotoXY`, `ClrScr`,
  `ClrEol`) where they fit. The DOS palette by name (`LightCyan`, `Brown`).
- **Modern .NET.** .NET 10, C# 14. Records, primary constructors, file-scoped
  namespaces, collection expressions, pattern matching, `LibraryImport` for
  PInvoke (never `DllImport`).
- **Cross-platform.** Linux + macOS terminals first-class; Windows console
  via `SetConsoleMode(ENABLE_VIRTUAL_TERMINAL_PROCESSING)`.

## Rules

- Never add a NuGet dependency without asking first.
- Public surface stays minimal. Add API only when a real consumer needs it.
- Trim/AOT discipline:
  - No `JsonSerializer.Serialize<T>(...)` without a source-gen context.
  - No reflection, `Activator.CreateInstance`, `Type.GetType` by string.
  - All PInvoke through `[LibraryImport]`.
  - Mark trim-unsafe code with `[RequiresUnreferencedCode]` if it ever exists.
- Commits in English, format `<type>: <summary>` (`feat | fix | refactor |
  test | docs | chore | build`).
- Status updates to Chloe in German.

## Firepit-Inbox

At session start, read and process all `.firepit/inbox/*.md`. Mark each handled
message with `firepit_inbox_complete` (moves it to `inbox/processed/`). The tool
only loads after a Claude Code restart — if it's missing, restart. If the tool
is unavailable, move the file to `.firepit/inbox/processed/` by hand.

## When in doubt — ask in German

Specifics, not open questions. Suggest 2–3 options, recommend one with a
one-line reason.

🐺
