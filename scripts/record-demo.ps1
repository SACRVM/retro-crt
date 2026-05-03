# Records one of the Retro.Crt sample showcases as an asciinema cast
# plus an animated GIF.
#
# Prereqs (one-time):
#   winget install asciinema           # or via WSL: sudo apt install asciinema
#   cargo install --git https://github.com/asciinema/agg
#
# Run from the repo root:
#   pwsh ./scripts/record-demo.ps1                  # default: tour
#   pwsh ./scripts/record-demo.ps1 -Demo themes
#   pwsh ./scripts/record-demo.ps1 -Demo matrix
#   pwsh ./scripts/record-demo.ps1 -Demo boot
#
# Output:
#   docs/images/<demo>.cast   (canonical, version-controlled)
#   docs/images/<demo>.gif    (rendered for the README hero)

param(
    [ValidateSet('tour', 'themes', 'matrix', 'boot')]
    [string]$Demo     = 'tour',
    [int]$Cols        = 80,
    [int]$Rows        = 24,
    [string]$Theme    = 'monokai',
    [double]$Speed    = 1.0,
    [int]$FontSize    = 14
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repoRoot

# Map the friendly demo name onto its sample project.
$projects = @{
    'tour'   = 'samples/Retro.Crt.Demo'
    'themes' = 'samples/Retro.Crt.Themes.Demo'
    'matrix' = 'samples/Retro.Crt.Matrix.Demo'
    'boot'   = 'samples/Retro.Crt.Boot.Demo'
}

$project = $projects[$Demo]
$cast    = "docs/images/$Demo.cast"
$gif     = "docs/images/$Demo.gif"

Write-Host "==> Building $project (Release)"
dotnet build $project -c Release --nologo | Out-Null

Write-Host "==> Recording $cast (${Cols}x${Rows})"
Write-Host "    asciinema closes when the demo exits."
asciinema rec $cast `
    -c "dotnet run --project $project -c Release --no-build" `
    --overwrite `
    --idle-time-limit 1 `
    --cols $Cols --rows $Rows

if (Get-Command agg -ErrorAction SilentlyContinue) {
    Write-Host "==> Rendering GIF $gif (theme=$Theme, speed=${Speed}x, font=$FontSize)"
    agg $cast $gif --theme $Theme --speed $Speed --font-size $FontSize
    Write-Host "==> Done."
    Write-Host "Cast:   $cast"
    Write-Host "GIF:    $gif"
} else {
    Write-Warning "agg not found on PATH — skipping GIF render."
    Write-Warning "Install with: cargo install --git https://github.com/asciinema/agg"
    Write-Host   "Cast: $cast (you can render the GIF later)."
}

Write-Host ""
Write-Host "Preview:    asciinema play $cast"
Write-Host "Re-record:  pwsh ./scripts/record-demo.ps1 -Demo $Demo"
