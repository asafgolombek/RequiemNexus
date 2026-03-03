$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

& "$ScriptRoot\build-debug.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

& "$ScriptRoot\build-release.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host ""

Write-Host "All builds completed successfully!" -ForegroundColor Yellow
