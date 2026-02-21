$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Requiem Nexus - Debug Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

dotnet build -c Debug

Write-Host "`nBuild complete!" -ForegroundColor Green
