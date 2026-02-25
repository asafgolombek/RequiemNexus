$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent -Path $ScriptRoot
$SlnPath = Join-Path -Path $RootDir -ChildPath "RequiemNexus.slnx"

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Building Requiem Nexus - Release Configuration" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

dotnet build $SlnPath -c Release

Write-Host "`nBuild complete!" -ForegroundColor Green
