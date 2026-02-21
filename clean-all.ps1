$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Cleaning Requiem Nexus Build Products" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

$slnPath = "c:\gitrepo\RequiemNexus\RequiemNexus.slnx"

Write-Host "Running dotnet clean..." -ForegroundColor Cyan
dotnet clean $slnPath

Write-Host "`nRemoving all bin and obj folders..." -ForegroundColor Cyan

# Find and remove all bin and obj directories recursively
$directoriesToRemove = Get-ChildItem -Path "c:\gitrepo\RequiemNexus" -Directory -Recurse -Include "bin", "obj"

foreach ($dir in $directoriesToRemove) {
    Write-Host "Removing: $($dir.FullName)" -ForegroundColor DarkGray
    Remove-Item -Path $dir.FullName -Force -Recurse
}

Write-Host "`nClean complete!" -ForegroundColor Green
