# Records the Retro.Crt demo as an asciinema cast plus an animated GIF.
#
# Prereqs (one-time):
#   winget install asciinema           # or via WSL: sudo apt install asciinema
#   cargo install --git https://github.com/asciinema/agg
#
# Output:
#   docs/images/demo.cast   (canonical, version-controlled, ~10 KB)
#   docs/images/demo.gif    (rendered for the README hero, ~500 KB-2 MB)
#
# Run from the repo root:
#   pwsh ./scripts/record-demo.ps1

param(
    [int]$Cols     = 80,
    [int]$Rows     = 24,
    [string]$Theme = 'monokai',
    [double]$Speed = 1.0,
    [int]$FontSize = 14
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repoRoot

$cast = 'docs/images/demo.cast'
$gif  = 'docs/images/demo.gif'

Write-Host "==> Building demo (Release, no rebuild during cast)"
dotnet build samples/Retro.Crt.Demo -c Release --nologo | Out-Null

Write-Host "==> Recording $cast (${Cols}x${Rows})"
Write-Host "    asciinema closes when the demo exits."
asciinema rec $cast `
    -c "dotnet run --project samples/Retro.Crt.Demo -c Release --no-build" `
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
Write-Host "Re-record:  pwsh ./scripts/record-demo.ps1"
