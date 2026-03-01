param(
    [switch]$SkipTests
)

. "$PSScriptRoot\_common.ps1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building and Booting Requiem Nexus - Debug Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Invoke-Restore

Write-Host "`nBuilding solution..." -ForegroundColor DarkGray
dotnet build $SlnPath -c Debug

if (-not $SkipTests) {
    Invoke-Tests -Configuration Debug
} else {
    Write-Host "`n  [Skipping tests as requested]" -ForegroundColor DarkYellow
}

Write-Host "`nApplying database migrations..." -ForegroundColor DarkGray
dotnet ef database update --project (Join-Path $RootDir "src\RequiemNexus.Data") --startup-project $WebProjPath

Write-Host "`nBooting application..." -ForegroundColor Green
dotnet run --project $WebProjPath -c Debug

Write-Host "`nApplication exited!" -ForegroundColor Green
