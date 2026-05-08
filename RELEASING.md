# Releasing Retro.Crt

This is the canonical release playbook for the two NuGet packages
shipped from this repo:

- **`Retro.Crt`** (the core) — tagged `v<MAJOR>.<MINOR>.<PATCH>`,
  csproj `src/Retro.Crt/Retro.Crt.csproj`.
- **`Retro.Crt.Tui`** — tagged `tui-v<MAJOR>.<MINOR>.<PATCH>`,
  csproj `src/Retro.Crt.Tui/Retro.Crt.Tui.csproj`.

Each package has its own `<Version>`, its own PublicAPI tracking, its
own tag scheme, and its own job in `.github/workflows/release.yml`.
**Tag schemes never mix:** a `v0.7.0` tag must point at a commit that
bumps `Retro.Crt.csproj`; a `tui-v0.1.0` tag at one that bumps
`Retro.Crt.Tui.csproj`. Mismatch = the workflow refuses to publish.

## TL;DR — `/release`

The day-to-day release loop is the **`/release` slash command**
(definition: `.claude/commands/release.md`). It runs the eight-step
checklist below, asks for confirmation, then commits + tags + pushes.

```
/release            # core, auto-detect bump from commits since v*
/release tui        # tui, auto-detect bump from commits since tui-v*
/release tui minor  # force a minor bump
/release all        # core first, then tui — each package classifies
                    #   its own commits + bumps independently
/release core test  # dry-run: print the plan, change nothing
/release core 1.0.0 # explicit version
```

The skill enforces the hard rules of this document — sanity checks,
PublicAPI promotion, single-tag-per-package, no `--no-verify`, no
force-pushes — so the manual playbook below is the fallback for cases
where the skill can't run (e.g. you are reviewing a PR by hand).

## What the loop does

For each package being released:

### 1. Sanity checks

- Branch is `main`.
- Working tree is clean.
- `main` is in sync with `origin/main`.
- The package's csproj exists.

If any fails: stop, fix the cause, do not proceed.

### 2. Determine the current state

```bash
# Last tag for this package (none on a first-ever tui release):
git describe --tags --match 'v*'      --abbrev=0   # core
git describe --tags --match 'tui-v*'  --abbrev=0   # tui

# Current csproj version:
grep -oE '<Version>[^<]+</Version>' src/Retro.Crt/Retro.Crt.csproj \
  | sed 's|<Version>||;s|</Version>||'

# Commits since the last tag, restricted to the package's tree:
git log <last-tag>..HEAD --format='%s' -- src/Retro.Crt/ tests/Retro.Crt.Tests/        # core
git log <last-tag>..HEAD --format='%s' -- src/Retro.Crt.Tui/ tests/Retro.Crt.Tui.Tests/ # tui
```

The path filter is what lets a Tui-only commit not trigger a core bump
when running `/release all`.

### 3. Decide the bump

Auto-detect from the conventional-commit prefixes of the
package-filtered commit list:

- Any `feat:` → **minor** (`0.7.0` → `0.8.0`).
- Only `fix:` / `perf:` → **patch** (`0.7.0` → `0.7.1`).
- Only `refactor:` / `test:` / `docs:` / `chore:` / `build:` → **nothing
  to release** for this package; skip it.

Override with an explicit `patch` / `minor` / `major` token, or pin to
an exact `X.Y.Z`. Pre-1.0 majors are unusual — confirm twice.

> ⚠️ A `fix:` commit that adds a new public API entry (`Unshipped.txt`
> grew) is technically a minor bump, even though the prefix says
> `fix`. Auto-detect goes by prefix; the human confirming the plan is
> expected to spot the public-API addition and override to `minor` if
> it really is new surface. The `0.7.1` plan ran into exactly this case
> (`Crt.UseHiddenCursor`) and stayed patch by deliberate call.

### 4. Show the plan, ask for confirmation

```
Paket: Retro.Crt (core)
  Letzter Tag:       v0.7.0
  csproj <Version>:  0.7.0
  Commits seit Tag:  3 (0 feat, 2 fix, 1 perf)
  Vorgeschlagener Bump: patch
  Neue Version:      0.7.1
  Neuer Tag:         v0.7.1
  PublicAPI promote: 1 Eintrag aus Unshipped.txt → Shipped.txt
```

Plus the commit list since the package's last tag, grouped by prefix.
Operator answers `j` / `n`; nothing changes on `n`.

### 5. Promote the PublicAPI

`Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks every public type
and member in two sibling files next to each csproj:

- `<package>/PublicAPI.Shipped.txt` — frozen at the last release.
- `<package>/PublicAPI.Unshipped.txt` — additions / changes since.

Move every entry from `Unshipped.txt` into `Shipped.txt` (alphabetical
within `Shipped.txt`, header `#nullable enable` stays at line 1).
After the move, `Unshipped.txt` is just the header. The build fails if
a public symbol exists in neither file, so any drift between source
and tracking files is caught at compile time.

For removals: prefix the line in `Unshipped.txt` with `*REMOVED*`,
e.g. `*REMOVED*static Retro.Crt.Crt.LegacyMethod() -> void`. After the
release, drop the matching line from `Shipped.txt` and the marker
line from `Unshipped.txt` together.

### 6. Bump the csproj `<Version>`

Edit one element. Touch nothing else.

```xml
<Version>0.7.1</Version>
```

### 7. Sanity build

```bash
dotnet build <CSPROJ> --configuration Release --nologo
```

Catches PublicAPI drift errors locally before the tag goes out. If it
fails, **stop**, surface the error, leave the working tree as-is so
the operator can inspect — no commit, no tag, no push.

### 8. Commit, tag, push

```bash
git add <CSPROJ> <PACKAGE_DIR>/PublicAPI.Shipped.txt <PACKAGE_DIR>/PublicAPI.Unshipped.txt
git commit -m "release: <TAG_PREFIX><NEW>"
git tag <TAG_PREFIX><NEW>
git push origin main
git push origin <TAG_PREFIX><NEW>
```

The push to `refs/tags/v*` (core) or `refs/tags/tui-v*` (tui) triggers
`.github/workflows/release.yml`, which:

1. Verifies the tag name matches the csproj `<Version>` of the
   relevant package.
2. Restores, builds, tests on Linux.
3. `dotnet pack`s the right csproj.
4. Uploads `.nupkg` + `.snupkg` as a workflow artifact.
5. Pushes them to nuget.org with `--skip-duplicate`.
6. Creates a GitHub Release with auto-generated notes.

For `/release all`: core's commit + tag + push first, then tui's. Two
separate commits, two separate tags. Order matters — see the lesson
below.

## Mono-repo lesson: bump core first

(Documented because we paid the tuition.)

ProjectReference → PackageReference rewrite at `dotnet pack` time uses
the *referenced project's current `<Version>`* as the lower-bound
dependency in the produced nupkg. If `Retro.Crt.Tui`'s compiled DLL
references a core internal added on `main` since the last core
release, the Tui nupkg ships with a dependency on the OLD core nupkg
that doesn't have that internal — runtime `TypeLoadException` for
downstream consumers.

That happened with `Retro.Crt.Tui 0.1.0` (referenced
`Retro.Crt.Internals.WindowResize` at compile time, but the published
core was still `0.6.0`, which didn't have it). Fixed by
shipping `Retro.Crt 0.7.0` first and re-shipping the Tui package as
`0.1.1` with the bumped lower-bound.

**Rule:** if a Tui change touches a core internal that's only on
`main`, ship core first, then Tui. Never the reverse. `/release all`
runs in this order automatically.

## Versioning policy

[Semantic Versioning](https://semver.org/spec/v2.0.0.html) **from
`1.0` onward**. Pre-`1.0`, the public API may move between minor
versions; breaking changes are called out in `CHANGELOG.md`.

- Bug fix only → patch
- New API, backwards-compatible → minor
- Breaking change pre-`1.0` → minor + clearly flagged in changelog
- Breaking change post-`1.0` → major

Each package's version lives in **one** place — its own `.csproj`
under `<Version>`. The release workflow refuses to publish if the
pushed tag doesn't match.

## Manual / dry-run trigger

The workflow also accepts a manual dispatch from the *Actions* tab
with a `dry_run` toggle (default `true`). Dry runs pack and upload the
artifact without pushing to nuget.org — useful for sanity-checking the
pipeline without burning a version number.

`/release <package> test` is the local equivalent: prints the plan,
changes nothing.

## One-time setup (only needed once per repo)

The release workflow needs a NuGet API key to push.

1. Sign in at [nuget.org](https://www.nuget.org/account/apikeys).
2. **Create** a new key:
   - Key Name: `Retro.Crt`
   - Expires In: `365 days` (max)
   - Package Owner: your account
   - Scopes: **Push** → *Push new packages and package versions*
   - Glob Pattern: `Retro.Crt*` (covers `Retro.Crt.Tui` and any
     future sub-packages)
3. Copy the key — shown **only once**.
4. In GitHub: *Settings* → *Secrets and variables* → *Actions* →
   *New repository secret*. Name: `NUGET_API_KEY`.

The key expires after one year. Set a calendar reminder for ~11
months out so the next release doesn't fail at the push step.

## Failure modes and fixes

| Symptom | Cause | Fix |
|---|---|---|
| `Tag v0.7.1 does not match csproj <Version>0.7.0` | Tag and csproj drifted. | Delete the tag, bump the csproj or rename the tag, retry. |
| `Tag tui-v0.1.0 paired with core csproj` | Tag-scheme cross-up. | Delete the tag. The right scheme is `v*` for core, `tui-v*` for tui. |
| `NUGET_API_KEY secret is not set` | Repo secret missing or expired. | Re-create at nuget.org, re-add to GitHub secrets. |
| `409 Conflict` from nuget.org | Version already published. | Bump to the next patch — published versions are immutable. `--skip-duplicate` already handles re-runs. |
| `RS0016 / RS0017` build error on tag push | PublicAPI drift — Step 5 was skipped or the new symbol isn't tracked. | Promote properly, push a fix-up commit, re-tag. |
| Downstream `TypeLoadException` after a Tui release | Tui DLL referenced a core internal not yet shipped. | Ship core *first*, then re-pack Tui (the next Tui release picks up the new core lower-bound automatically). |
| GitHub Release step fails with permission error | `permissions: contents: write` missing. | Already set in `release.yml`. If forking, keep it. |
| Tag pointing to a commit *before* `release.yml` existed | Workflow file is read from the tagged commit. | Delete the orphan tag locally and on origin; bump again from a current commit. |

## Yanking a bad release

Published versions on nuget.org are **immutable**. You cannot replace
them. Two options:

1. **Unlist** via the nuget.org web UI (*Manage Package* → *Listing*).
   The version stays installable for anyone who pinned to it but is
   hidden from search and from default `dotnet add package`. Use this
   for non-critical regressions.
2. **Ship a fix as the next patch** as fast as possible. Always
   preferable for actively broken builds — the Tui 0.1.0 → 0.1.1
   fix-up is the canonical example.

There is no "delete a published version". Plan accordingly.

## Pre-release versions

For preview / RC builds, use SemVer pre-release suffixes in the csproj:

```xml
<Version>0.8.0-rc.1</Version>
```

Tag accordingly: `v0.8.0-rc.1` (core) or `tui-v0.2.0-rc.1` (tui). The
release workflow marks any tag containing a `-` as `prerelease: true`
on the GitHub Release, and nuget.org displays it under the *Prerelease*
filter rather than the default *Stable* listing. Consumers must opt in
with `--prerelease` or by pinning the exact version.
