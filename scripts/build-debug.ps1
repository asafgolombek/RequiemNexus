param(
    [switch]$SkipTests
)

. "$PSScriptRoot\_common.ps1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Requiem Nexus - Debug Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Invoke-Restore

Write-Host "`nBuilding solution..." -ForegroundColor DarkGray
dotnet build $SlnPath -c Debug

if (-not $SkipTests) {
    & "$PSScriptRoot\test-local.ps1" -Configuration Debug
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "`n  [Skipping tests as requested]" -ForegroundColor DarkYellow
}
Write-Host "`nBuild complete!" -ForegroundColor Green
