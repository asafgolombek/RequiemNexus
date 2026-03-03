param(
    [switch]$SkipTests
)

. "$PSScriptRoot\_common.ps1"

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Building Requiem Nexus - Release Configuration" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Invoke-Restore

Write-Host "`nBuilding solution..." -ForegroundColor DarkGray
dotnet build $SlnPath -c Release

if (-not $SkipTests) {
    & "$PSScriptRoot\test-local.ps1" -Configuration Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "`n  [Skipping tests as requested]" -ForegroundColor DarkYellow
}

Write-Host "`nBuild complete!" -ForegroundColor Green
