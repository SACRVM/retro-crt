# CLAUDE.md — Working on Retro.Crt

You are working for the repo owner, who writes in **German**; reply in
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
- Status updates to the owner in German.

## Firepit-Inbox

At session start, read and process all `.firepit/inbox/*.md`. Mark each handled
message with `firepit_inbox_complete` (moves it to `inbox/processed/`). The tool
only loads after a Claude Code restart — if it's missing, restart. If the tool
is unavailable, move the file to `.firepit/inbox/processed/` by hand.

## When in doubt — ask in German

Specifics, not open questions. Suggest 2–3 options, recommend one with a
one-line reason.

🐺

## Firepit knowledge

Before researching something that may already be known, query the knowledge base with the `firepit_knowledge_search` MCP tool (scope `both` covers this project plus the global base). Save durable findings with `firepit_knowledge_add` — written in English, per the indexing convention. The created markdown files live under `.firepit/knowledge/` and are committed like any other file.

## Firepit pinned knowledge

@.firepit/knowledge-pinned.md

The import above auto-loads the knowledge docs marked `pin: true` in their frontmatter — always-on rules that apply every session without a search. Firepit regenerates the file from the pinned docs; don't edit it directly. Pin/unpin via the pinned flag on `firepit_knowledge_add` / `firepit_knowledge_update`, and keep the pinned set small — everything else stays reachable through `firepit_knowledge_search`.

## Firepit artifacts

When you produce a file the user will want to open — a report, screenshot, diagram, generated image, log excerpt, build output, or an executable you built for them to run — pin it with the `firepit_artifact_add` MCP tool so it appears in the project's paperclip pane. Do this as you produce it, not at the end of the session; a path buried in scrollback is a path the user has to hunt for. Pinning only links the file — it stays where it is, and `firepit_artifact_remove` never deletes it. Check `firepit_artifact_list` first so you update an existing entry instead of piling up near-duplicates, and unpin what has gone stale.

## Firepit conventions

<!-- claude-firepit-fragments -->

@../.firepit/projects/claude.md
@../.firepit/projects/claude-github-public.md

The two imports above are shared files in the Firepit central repo — edit them there and every project follows. They carry policy; the tools themselves are described by Firepit's MCP server at the handshake, so nothing is duplicated between the two.
