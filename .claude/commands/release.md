---
description: Bump Retro.Crt version, promote PublicAPI, tag, push — release.yml then publishes to nuget.org.
argument-hint: [test|patch|minor|major|<x.y.z>]
---

# /release — cut a Retro.Crt release

You are cutting a Retro.Crt release. Follow these steps **exactly**, in order. **Never skip the confirmation step.** Speak to Chloe in German; commit messages and tag names stay English.

## Argument

`$ARGUMENTS` is one of:
- (empty) — auto-detect bump from commits since the last tag
- `test` — dry-run: print the plan, do nothing
- `patch` / `minor` / `major` — force that bump
- `1.0.0` (or any `X.Y.Z`) — set this exact version

## Step 1 — Sanity checks

Run all four. If any fails, stop and report to Chloe — do not proceed.

```bash
git rev-parse --abbrev-ref HEAD                                    # must be 'main'
test -z "$(git status --porcelain)" && echo clean || echo dirty    # must be 'clean'
git fetch origin main --quiet
test "$(git rev-parse HEAD)" = "$(git rev-parse origin/main)" && echo synced || echo behind  # must be 'synced'
test -f src/Retro.Crt/Retro.Crt.csproj && echo found || echo missing  # must be 'found'
```

## Step 2 — Determine current state

```bash
git describe --tags --abbrev=0          # last tag, e.g. v0.2.1
grep -oP '(?<=<Version>)[^<]+' src/Retro.Crt/Retro.Crt.csproj   # csproj version
git log $(git describe --tags --abbrev=0)..HEAD --format='%s'    # commits since last tag
```

If `git describe` finds no tag, treat last tag as `v0.0.0` and analyze all commits.

## Step 3 — Decide the bump

Parse `$ARGUMENTS`:

| Arg | Action |
|---|---|
| empty | classify commits: any `feat:` → minor; only `fix:` → patch; only `refactor:`/`test:`/`docs:`/`chore:`/`build:` → **nothing to release**, stop here |
| `test` | same classification as empty, but stop after the plan in Step 4 |
| `patch` | force Z+1 |
| `minor` | force Y+1, Z=0 |
| `major` | force X+1, Y=0, Z=0 (unusual pre-1.0 — confirm twice) |
| `X.Y.Z` | use literally — must be greater than current |

Compute `new_version` from the current csproj version (NOT from the tag, in case they differ).

## Step 4 — Show the plan, ask for confirmation

Print to Chloe in German, exactly like:

```
Letzter Tag:       v0.2.1
csproj <Version>:  0.2.1
Commits seit Tag:  19 (8 feat, 5 fix, 6 chore/docs/etc.)
Vorgeschlagener Bump: minor (weil 8 feats drin)
Neue Version:      0.3.0
Neuer Tag:         v0.3.0
PublicAPI promote: 47 Einträge aus Unshipped.txt → Shipped.txt
```

Then list the commits since the last tag, grouped by prefix (feat / fix / chore / docs / refactor / test / build / other), as a sanity check.

**If `$ARGUMENTS` is `test`: stop here. Do not change any files. Tell Chloe „Trockenlauf — nichts geändert."**

Otherwise: ask **„OK so? [j/n]"** and wait for her answer.
- `j` / `ja` / `y` / `yes` → continue to Step 5
- anything else → abort, change nothing

## Step 5 — Promote PublicAPI

Move all entries from `src/Retro.Crt/PublicAPI.Unshipped.txt` into `src/Retro.Crt/PublicAPI.Shipped.txt`:

1. Read both files.
2. Strip the `#nullable enable` header from each (track that it should be the first line of the result).
3. Concat the bodies, dedupe, sort alphabetically.
4. Write `Shipped.txt` as: `#nullable enable\n` + sorted unique body + trailing newline.
5. Write `Unshipped.txt` as: `#nullable enable\n` only (an empty unshipped list).

Do this with the Edit / Write tools — not with sed/awk pipelines that risk losing trailing newlines.

## Step 6 — Bump csproj version

Edit `src/Retro.Crt/Retro.Crt.csproj` line ~9: change `<Version>OLD</Version>` to `<Version>NEW</Version>`. Use the Edit tool with the full surrounding `<Version>…</Version>` string for uniqueness.

## Step 7 — Sanity build

```bash
dotnet build src/Retro.Crt/Retro.Crt.csproj --configuration Release --nologo
```

This catches PublicAPI drift errors locally before the tag goes out. If it fails, **stop**, show the error to Chloe, leave the working tree as-is so she can inspect — do not commit, do not tag, do not push.

## Step 8 — Commit, tag, push

```bash
git add src/Retro.Crt/Retro.Crt.csproj src/Retro.Crt/PublicAPI.Shipped.txt src/Retro.Crt/PublicAPI.Unshipped.txt
git commit -m "release: vNEW"
git tag vNEW
git push origin main
git push origin vNEW
```

Use the standard commit-message HEREDOC pattern; respect the repo's commit attribution settings (do not add a Co-Authored-By trailer if the repo's .claude config disables it).

## Step 9 — Hand off

Tell Chloe in German:

```
Release v0.3.0 ist raus.
- Tag gepusht → release.yml läuft
- nuget.org bekommt das Paket in ~3-5 min
- GitHub Release wird automatisch erstellt mit generated release notes
- CI-Status: https://github.com/chloe-dream/retro-crt/actions
```

Do **not** poll or wait for the CI run — just hand off.

## Hard rules

- Never push tags without Step 4 confirmation.
- Never skip Step 5 (PublicAPI promote) — releases without it are guaranteed to break the next build.
- Never use `--no-verify` or `--force` on any git command.
- Never touch `.csproj` fields other than `<Version>`.
- If anything is unclear or smells wrong (e.g., no commits since last tag, csproj/tag version mismatch, weird state), stop and ask Chloe instead of guessing.
