$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent -Path $ScriptRoot
$SlnPath = Join-Path -Path $RootDir -ChildPath "tests\RequiemNexus.Tests.slnx"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Cleaning Requiem Nexus Build Products" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

Write-Host "Running dotnet clean..." -ForegroundColor Cyan
dotnet clean $SlnPath

Write-Host "`nRemoving build and coverage artifacts..." -ForegroundColor Cyan

# Directories to remove
$dirsToClean = @("bin", "obj", "coverage", "cdk.out")
$directoriesToRemove = Get-ChildItem -Path $RootDir -Directory -Recurse -Include $dirsToClean

foreach ($dir in $directoriesToRemove) {
    Write-Host "Removing: $($dir.FullName)" -ForegroundColor DarkGray
    Remove-Item -Path $dir.FullName -Force -Recurse
}

Write-Host "`nClean complete!" -ForegroundColor Green
