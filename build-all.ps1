$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Requiem Nexus - Debug Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
dotnet build -c Debug
Write-Host ""

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Building Requiem Nexus - Release Configuration" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
dotnet build -c Release
Write-Host ""

Write-Host "All builds completed successfully!" -ForegroundColor Yellow
