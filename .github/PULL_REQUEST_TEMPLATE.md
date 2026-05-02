<!-- One-line summary above. Expand below. -->

## What

<!-- What does this change do? -->

## Why

<!-- Motivation. Link the issue if there is one. -->

## How

<!-- Notable implementation details, design tradeoffs, alternatives
considered. Skip if the diff speaks for itself. -->

## Tests

<!-- New tests added? Existing tests touched? Manual verification? -->

## Checklist

- [ ] CI green (build + test on Linux / macOS / Windows).
- [ ] `dotnet publish -p:PublishTrimmed=true -p:IsAotCompatible=true`
      still succeeds without warnings (when this PR touches `src/`).
- [ ] Public API additions have XML doc comments.
- [ ] If hot-path code changed, before/after benchmark numbers in the
      PR body.
- [ ] Commit message follows `<type>: <summary>`.
