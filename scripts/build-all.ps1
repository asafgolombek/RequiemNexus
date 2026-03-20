$ErrorActionPreference = "Stop"

& "$PSScriptRoot\build-debug.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

& "$PSScriptRoot\build-release.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "All builds completed successfully!" -ForegroundColor Yellow
