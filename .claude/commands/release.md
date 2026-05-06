---
description: Bump Retro.Crt / Retro.Crt.Tui version, promote PublicAPI, tag, push — release.yml then publishes to nuget.org.
argument-hint: [core|tui|all] [test|patch|minor|major|<x.y.z>]
---

# /release — cut a Retro.Crt release

You are cutting a Retro.Crt release. Follow these steps **exactly**, in order. **Never skip the confirmation step.** Speak to Chloe in German; commit messages and tag names stay English.

## Argument

`$ARGUMENTS` is **two tokens**, either of which may be omitted:

**Token 1 — package selector** (default `core` when missing):
- `core` — release `Retro.Crt` (tags `v*.*.*`, csproj `src/Retro.Crt/Retro.Crt.csproj`)
- `tui`  — release `Retro.Crt.Tui` (tags `tui-v*.*.*`, csproj `src/Retro.Crt.Tui/Retro.Crt.Tui.csproj`)
- `all`  — run the whole flow for `core` first, then for `tui`. Each package classifies its own commits and gets its own tag/push. Plan + confirmation cover both at once.

**Token 2 — bump kind** (default empty):
- (empty) — auto-detect bump from commits since the *package's* last tag
- `test` — dry-run: print the plan, do nothing
- `patch` / `minor` / `major` — force that bump
- `1.0.0` (or any `X.Y.Z`) — set this exact version

Examples: `/release` (= core auto-detect), `/release tui`, `/release tui minor`, `/release all`, `/release core 0.7.0`, `/release all test`.

## Per-package config

Resolve once based on the selected package; everything below is per-package.

| Field          | core                                    | tui                                                  |
|----------------|-----------------------------------------|------------------------------------------------------|
| csproj         | `src/Retro.Crt/Retro.Crt.csproj`        | `src/Retro.Crt.Tui/Retro.Crt.Tui.csproj`             |
| PublicAPI dir  | `src/Retro.Crt/`                        | `src/Retro.Crt.Tui/`                                 |
| Tag prefix     | `v`                                     | `tui-v`                                              |
| Tag pattern    | `v*.*.*` (`git describe --match 'v*'`)  | `tui-v*.*.*` (`git describe --match 'tui-v*'`)       |
| Package name   | `Retro.Crt`                             | `Retro.Crt.Tui`                                      |

For `all`: run Steps 1–9 fully for `core`, then start Steps 1–9 again for `tui`. Show the combined plan in Step 4 (one block per package) and a single confirmation prompt for both. If the user answers no, change nothing for either package.

## Step 1 — Sanity checks

Run all four. If any fails, stop and report to Chloe — do not proceed.

```bash
git rev-parse --abbrev-ref HEAD                                    # must be 'main'
test -z "$(git status --porcelain)" && echo clean || echo dirty    # must be 'clean'
git fetch origin main --quiet
test "$(git rev-parse HEAD)" = "$(git rev-parse origin/main)" && echo synced || echo behind  # must be 'synced'
test -f <CSPROJ> && echo found || echo missing  # must be 'found'
```

## Step 2 — Determine current state

```bash
git describe --tags --match '<TAG_PATTERN>' --abbrev=0          # last tag for this package
grep -oP '(?<=<Version>)[^<]+' <CSPROJ>                         # csproj version
git log $(git describe --tags --match '<TAG_PATTERN>' --abbrev=0)..HEAD --format='%s'  # commits since last tag
```

If `git describe` finds no tag for this package's pattern (typical first `tui` release), treat last tag as `<TAG_PREFIX>0.0.0` and analyze all commits.

**Path filter for `all`:** when classifying commits per package, filter `git log` to that package's source tree to avoid double-counting:

- core: `git log <range> -- src/Retro.Crt/ tests/Retro.Crt.Tests/`
- tui:  `git log <range> -- src/Retro.Crt.Tui/ tests/Retro.Crt.Tui.Tests/`

A commit that touches only `src/Retro.Crt.Tui/` shouldn't trigger a core bump.

## Step 3 — Decide the bump

Parse `$ARGUMENTS` token 2:

| Arg | Action |
|---|---|
| empty | classify commits: any `feat:` → minor; only `fix:` → patch; only `refactor:`/`test:`/`docs:`/`chore:`/`build:` → **nothing to release** for this package, skip it |
| `test` | same classification as empty, but stop after the plan in Step 4 |
| `patch` | force Z+1 |
| `minor` | force Y+1, Z=0 |
| `major` | force X+1, Y=0, Z=0 (unusual pre-1.0 — confirm twice) |
| `X.Y.Z` | use literally — must be greater than current |

Compute `new_version` from the current csproj version (NOT from the tag, in case they differ).

For `all` with auto-detect: each package classifies independently; one may bump while the other has nothing to release. Skip silently for the package with no changes.

## Step 4 — Show the plan, ask for confirmation

Print to Chloe in German, one block per package being released, exactly like:

```
Paket: Retro.Crt (core)
  Letzter Tag:       v0.6.0
  csproj <Version>:  0.6.0
  Commits seit Tag:  19 (8 feat, 5 fix, 6 chore/docs/etc.)
  Vorgeschlagener Bump: minor (weil 8 feats drin)
  Neue Version:      0.7.0
  Neuer Tag:         v0.7.0
  PublicAPI promote: 47 Einträge aus Unshipped.txt → Shipped.txt
```

Then list the commits since the package's last tag, grouped by prefix (feat / fix / chore / docs / refactor / test / build / other), as a sanity check.

**If `$ARGUMENTS` token 2 is `test`: stop here. Do not change any files. Tell Chloe „Trockenlauf — nichts geändert."**

Otherwise: ask **„OK so? [j/n]"** and wait for her answer.
- `j` / `ja` / `y` / `yes` → continue to Step 5 for each package in order
- anything else → abort, change nothing

## Step 5 — Promote PublicAPI

For each package being released, move all entries from `<PUBLIC_API_DIR>/PublicAPI.Unshipped.txt` into `<PUBLIC_API_DIR>/PublicAPI.Shipped.txt`:

1. Read both files.
2. Strip the `#nullable enable` header from each (track that it should be the first line of the result).
3. Concat the bodies, dedupe, sort alphabetically.
4. Write `Shipped.txt` as: `#nullable enable\n` + sorted unique body + trailing newline.
5. Write `Unshipped.txt` as: `#nullable enable\n` only (an empty unshipped list).

Do this with the Edit / Write tools — not with sed/awk pipelines that risk losing trailing newlines.

## Step 6 — Bump csproj version

Edit `<CSPROJ>` line ~9: change `<Version>OLD</Version>` to `<Version>NEW</Version>`. Use the Edit tool with the full surrounding `<Version>…</Version>` string for uniqueness.

## Step 7 — Sanity build

```bash
dotnet build <CSPROJ> --configuration Release --nologo
```

This catches PublicAPI drift errors locally before the tag goes out. If it fails, **stop**, show the error to Chloe, leave the working tree as-is so she can inspect — do not commit, do not tag, do not push.

## Step 8 — Commit, tag, push

```bash
git add <CSPROJ> <PUBLIC_API_DIR>/PublicAPI.Shipped.txt <PUBLIC_API_DIR>/PublicAPI.Unshipped.txt
git commit -m "release: <TAG_PREFIX><NEW>"
git tag <TAG_PREFIX><NEW>
git push origin main
git push origin <TAG_PREFIX><NEW>
```

Use the standard commit-message HEREDOC pattern; respect the repo's commit attribution settings (do not add a Co-Authored-By trailer if the repo's .claude config disables it).

For `all`: do core's commit + tag + push first, then start tui's flow. Two separate commits, two tags. The first push of `main` carries the core release commit; the second push of `main` is the tui release commit (fast-forward from origin's perspective).

## Step 9 — Hand off

Tell Chloe in German, one paragraph per released package:

```
Release v0.7.0 (core) ist raus.
- Tag gepusht → release.yml läuft
- nuget.org bekommt das Paket in ~3-5 min
- GitHub Release wird automatisch erstellt mit generated release notes
- CI-Status: https://github.com/chloe-dream/retro-crt/actions
```

For tui, replace `v0.7.0 (core)` with `tui-v0.1.0 (tui)` etc.

Do **not** poll or wait for the CI run — just hand off.

## Hard rules

- Never push tags without Step 4 confirmation.
- Never skip Step 5 (PublicAPI promote) — releases without it are guaranteed to break the next build.
- Never use `--no-verify` or `--force` on any git command.
- Never touch `.csproj` fields other than `<Version>`.
- Never mix tag schemes: `v*` is core-only, `tui-v*` is tui-only. A commit message like `release: tui-v0.1.0` paired with a `v0.1.0` tag (or vice versa) is wrong; double-check before pushing.
- If anything is unclear or smells wrong (e.g., no commits since last tag, csproj/tag version mismatch, weird state), stop and ask Chloe instead of guessing.
