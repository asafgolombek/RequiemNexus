param(
    [Parameter(Mandatory=$true)]
    [string]$Configuration,
    [switch]$SkipTests
)

. "$PSScriptRoot\_common.ps1"

$Color = if ($Configuration -eq "Release") { "Magenta" } else { "Cyan" }

Write-Host "========================================" -ForegroundColor $Color
Write-Host "Building Requiem Nexus - $Configuration Configuration" -ForegroundColor $Color
Write-Host "========================================" -ForegroundColor $Color

Write-Host "`nDotnet version:" -ForegroundColor DarkGray
dotnet --version

Invoke-Restore

Write-Host "`nBuilding solution..." -ForegroundColor DarkGray
dotnet build $SlnPath -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Build failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not $SkipTests) {
    & "$PSScriptRoot\test-local.ps1" -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "`n  [Skipping tests as requested]" -ForegroundColor DarkYellow
}

Write-Host "`nBuild complete!" -ForegroundColor Green
