# Releasing Retro.Crt

This is the canonical release playbook. Following it end-to-end takes
~5 minutes and ships a new version to **nuget.org** plus a matching
**GitHub Release** with the `.nupkg` and `.snupkg` attached.

## TL;DR

```bash
# 1. Bump the version in src/Retro.Crt/Retro.Crt.csproj
# 2. Move CHANGELOG.md "Unreleased" content into a new dated section
# 3. Commit:
git add src/Retro.Crt/Retro.Crt.csproj CHANGELOG.md
git commit -m "release: <version> — <one-line summary>"
git push origin main

# 4. Tag and push the tag (this triggers the release workflow):
git tag v<version>
git push origin v<version>
```

That's the whole loop. The rest of this document is the *why* and the
*what to do when something goes wrong*.

## Versioning policy

We follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
**from `1.0` onward**. Pre-`1.0`, the public API may change between
minor versions; breaking changes are called out in `CHANGELOG.md`.

- Bug fix only → patch bump (`0.2.0` → `0.2.1`)
- New API, backwards-compatible → minor bump (`0.2.1` → `0.3.0`)
- Breaking change pre-`1.0` → minor bump + clearly flagged in changelog
- Breaking change post-`1.0` → major bump (`1.x` → `2.0.0`)

The version lives in **one** place: `src/Retro.Crt/Retro.Crt.csproj`
under `<Version>`. The release workflow verifies that the pushed tag
matches this value and refuses to publish if they drift.

## Step-by-step

### 1. Bump `<Version>` in the csproj

Edit `src/Retro.Crt/Retro.Crt.csproj`:

```xml
<Version>0.2.1</Version>
```

### 2. Update `CHANGELOG.md`

Move everything currently under `## [Unreleased]` into a new dated
section. Add the comparison link at the bottom.

```markdown
## [Unreleased]

## [0.2.1] — 2026-05-03

### Added
- ...

[Unreleased]: https://github.com/chloe-dream/retro-crt/compare/v0.2.1...HEAD
[0.2.1]: https://github.com/chloe-dream/retro-crt/releases/tag/v0.2.1
```

Keep the format from
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/):
`Added`, `Changed`, `Fixed`, `Deprecated`, `Removed`, `Security`.

### 3. Local sanity build

```bash
dotnet build Retro.Crt.slnx -c Release
dotnet test  Retro.Crt.slnx -c Release --no-build
dotnet pack  src/Retro.Crt/Retro.Crt.csproj -c Release --no-build -o nupkg
```

The pack step is just a smoke check — the real pack runs in CI on the
tag. Delete the local `nupkg/` afterwards (it is gitignored anyway).

### 4. Commit and push to `main`

```bash
git add src/Retro.Crt/Retro.Crt.csproj CHANGELOG.md
git commit -m "release: 0.2.1 — <summary>"
git push origin main
```

Wait for CI on `main` to go green before tagging — otherwise you risk
publishing a broken package.

### 5. Tag and push the tag

```bash
git tag v0.2.1
git push origin v0.2.1
```

Tag format is **strict**: `v<MAJOR>.<MINOR>.<PATCH>` matching the
csproj `<Version>` exactly. The `v` prefix is required.

The push to `refs/tags/v*.*.*` triggers
`.github/workflows/release.yml`, which:

1. Verifies the tag name matches the csproj `<Version>`.
2. Restores, builds, and tests the solution on Linux.
3. `dotnet pack`s `Retro.Crt.csproj` to `nupkg/`.
4. Uploads the `.nupkg` + `.snupkg` as a workflow artifact.
5. Pushes them to `https://api.nuget.org/v3/index.json` using the
   `NUGET_API_KEY` repo secret (with `--skip-duplicate` so re-runs are
   safe).
6. Creates a GitHub Release at `v<version>` with auto-generated notes,
   attaching the `.nupkg` and `.snupkg`.

Watch the run:

```bash
gh run watch --exit-status
```

### 6. Verify on nuget.org

```bash
curl -s https://api.nuget.org/v3-flatcontainer/retro.crt/index.json
```

The new version should appear in the `versions` array within seconds
of the workflow finishing. The web UI at
[nuget.org/packages/Retro.Crt](https://www.nuget.org/packages/Retro.Crt)
takes 5–15 minutes longer to update its search index — that is normal
and not a sign of a failed publish.

## One-time setup (only needed once per repo)

The release workflow needs a NuGet API key to push.

1. Sign in at [nuget.org](https://www.nuget.org/account/apikeys).
2. **Create** a new key:
   - Key Name: `Retro.Crt`
   - Expires In: `365 days` (the maximum)
   - Package Owner: your account
   - Scopes: **Push** → *Push new packages and package versions*
   - Glob Pattern: `Retro.Crt*` (covers future sub-packages)
3. Copy the key — it is shown **only once**.
4. In GitHub: *Settings* → *Secrets and variables* → *Actions* →
   *New repository secret*. Name: `NUGET_API_KEY`. Value: the key.

The key expires after one year. Set a calendar reminder for ~11 months
out so the next release does not fail at the push step.

## Manual / dry-run trigger

The workflow also accepts a manual dispatch from the *Actions* tab,
with a `dry_run` toggle (default `true`). Dry runs pack and upload the
artifact without pushing to nuget.org — useful for sanity-checking the
pipeline without burning a version number.

## Failure modes and fixes

| Symptom | Cause | Fix |
|---|---|---|
| `Tag v0.2.1 does not match csproj <Version>0.2.0` | Tag and csproj drifted. | Delete the tag, bump the csproj or rename the tag, retry. |
| `NUGET_API_KEY secret is not set` | Repo secret missing or expired. | Re-create at nuget.org, re-add to GitHub secrets. |
| `409 Conflict` from nuget.org | Version already published. | Bump to the next patch — published versions are immutable on nuget.org. `--skip-duplicate` already handles this gracefully on re-runs. |
| GitHub Release step fails with permission error | `permissions: contents: write` missing. | Already set in `release.yml`. If forking, make sure to keep it. |
| Tag pointing to a commit *before* `release.yml` existed | The workflow file is read from the tagged commit. No workflow exists there → nothing fires. | Delete the orphan tag locally and on origin (or skip the version entirely and bump again). |

## Yanking a bad release

Published versions on nuget.org are **immutable**. You cannot replace
them. The two options:

1. **Unlist** via the nuget.org web UI (*Manage Package* → *Listing*).
   The version stays installable for anyone who pinned to it but is
   hidden from search and from `dotnet add package` without an explicit
   version. Use this for non-critical regressions.
2. **Ship a fix as the next patch** as fast as possible. Always
   preferable for actively broken builds.

There is no "delete a published version". Plan accordingly.

## Pre-release versions

For preview / RC builds, use SemVer pre-release suffixes in the csproj:

```xml
<Version>0.3.0-rc.1</Version>
```

Tag accordingly: `v0.3.0-rc.1`. The release workflow marks any tag
containing a `-` as `prerelease: true` on the GitHub Release, and
nuget.org displays it under the *Prerelease* filter rather than the
default *Stable* listing. Consumers must opt in with
`--prerelease` or by pinning the exact version.
