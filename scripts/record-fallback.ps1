# Records the Retro.Crt capability-fallback demo four times — once per
# color-depth tier — so the README can show the same scene under
# truecolor, 256-color, BIOS-16, and no-color side by side.
#
# Each run sets the corresponding env var, captures an asciinema cast,
# and (if `agg` is on PATH) renders an animated GIF.
#
# Prereqs (one-time):
#   winget install asciinema           # or via WSL: sudo apt install asciinema
#   cargo install --git https://github.com/asciinema/agg
#
# Run from the repo root:
#   pwsh ./scripts/record-fallback.ps1
#
# Output (per tier):
#   docs/images/fallback-truecolor.cast / .gif
#   docs/images/fallback-256.cast       / .gif
#   docs/images/fallback-16.cast        / .gif
#   docs/images/fallback-nocolor.cast   / .gif

param(
    [int]$Cols     = 80,
    [int]$Rows     = 28,
    [string]$Theme = 'monokai',
    [double]$Speed = 1.0,
    [int]$FontSize = 14
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repoRoot

$project = 'samples/Retro.Crt.Capabilities.Demo'

Write-Host "==> Building $project (Release)"
dotnet build $project -c Release --nologo | Out-Null

# Each entry: a friendly label, the cast/gif basename, and the env-var
# state for that recording. The demo itself reads no flags — the
# library's auto-detection picks up FORCE_COLOR / NO_COLOR and quantizes
# accordingly.
$tiers = @(
    @{ Label = 'truecolor'; Slug = 'fallback-truecolor'; Env = @{ FORCE_COLOR = '3'; NO_COLOR = $null } },
    @{ Label = '256';       Slug = 'fallback-256';       Env = @{ FORCE_COLOR = '2'; NO_COLOR = $null } },
    @{ Label = '16';        Slug = 'fallback-16';        Env = @{ FORCE_COLOR = '1'; NO_COLOR = $null } },
    @{ Label = 'no-color';  Slug = 'fallback-nocolor';   Env = @{ FORCE_COLOR = $null; NO_COLOR = '1' } }
)

foreach ($tier in $tiers) {
    $cast = "docs/images/$($tier.Slug).cast"
    $gif  = "docs/images/$($tier.Slug).gif"

    Write-Host ""
    Write-Host "==> [$($tier.Label)] recording $cast (${Cols}x${Rows})"

    # Set the tier-specific env vars only for the asciinema invocation.
    # We restore the previous values after — running the next tier with
    # leftovers in $env: would silently break detection.
    $previous = @{}
    foreach ($key in $tier.Env.Keys) {
        $previous[$key] = [Environment]::GetEnvironmentVariable($key)
        if ($null -eq $tier.Env[$key]) {
            [Environment]::SetEnvironmentVariable($key, $null)
        }
        else {
            [Environment]::SetEnvironmentVariable($key, $tier.Env[$key])
        }
    }

    try {
        asciinema rec $cast `
            -c "dotnet run --project $project -c Release --no-build" `
            --overwrite `
            --idle-time-limit 1 `
            --cols $Cols --rows $Rows
    }
    finally {
        foreach ($key in $previous.Keys) {
            [Environment]::SetEnvironmentVariable($key, $previous[$key])
        }
    }

    if (Get-Command agg -ErrorAction SilentlyContinue) {
        Write-Host "==> [$($tier.Label)] rendering $gif"
        agg $cast $gif --theme $Theme --speed $Speed --font-size $FontSize
    }
    else {
        Write-Warning "agg not found on PATH — skipped GIF render for $($tier.Slug)."
    }
}

Write-Host ""
Write-Host "==> Done. Four casts captured under docs/images/."
Write-Host "    Preview any of them with: asciinema play docs/images/<slug>.cast"
if (-not (Get-Command agg -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Warning "Install agg to render GIFs in one go:"
    Write-Warning "  cargo install --git https://github.com/asciinema/agg"
}
