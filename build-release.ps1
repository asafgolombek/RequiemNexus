$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Building Requiem Nexus - Release Configuration" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

dotnet build -c Release

Write-Host "`nBuild complete!" -ForegroundColor Green
