#Requires -Version 5.1
<#
.SYNOPSIS
    Run Playwright E2E tests (and optionally visual regression) against the in-process host.

.DESCRIPTION
    Requires PostgreSQL reachable with the same database the app uses in CI (default connection
    string targets localhost). Sets ASPNETCORE_ENVIRONMENT=Testing. Installs Chromium via the
    Playwright CLI after building the test project.

    Unit and integration tests remain in .\scripts\test-local.ps1 — run that first.

.PARAMETER Configuration
    Build configuration for test projects (CI uses Release).

.PARAMETER VisualRegression
    Also run RequiemNexus.VisualRegression.Tests after E2E.

.PARAMETER InstallBrowsersOnly
    Only restore, build, and run playwright.ps1 install (no dotnet test).

.NOTES
    Override the database with environment variable ConnectionStrings__DefaultConnection before
    invoking this script. Optional: PLAYWRIGHT_RETRIES (default 0 locally; CI uses 2).
#>
param(
    [string]$Configuration = "Release",
    [switch]$VisualRegression,
    [switch]$InstallBrowsersOnly
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\_common.ps1"

$Pwsh = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }

$E2EProj = $E2ETests
$VrProj = $VisualRegressionTests

if (-not $env:ASPNETCORE_ENVIRONMENT) {
    $env:ASPNETCORE_ENVIRONMENT = "Testing"
}

if (-not $env:ConnectionStrings__DefaultConnection) {
    $env:ConnectionStrings__DefaultConnection = (
        "Host=localhost;Port=5432;Database=requiem_nexus_e2e;Username=postgres;Password=postgres"
    )
}

if ($null -eq $env:PLAYWRIGHT_RETRIES) {
    $env:PLAYWRIGHT_RETRIES = "0"
}

Write-Host "`n=== E2E local run (PostgreSQL + Playwright) ===" -ForegroundColor Cyan
Write-Host "  ConnectionStrings__DefaultConnection: $($env:ConnectionStrings__DefaultConnection.Substring(0, [Math]::Min(60, $env:ConnectionStrings__DefaultConnection.Length)))..." -ForegroundColor DarkGray

dotnet restore $SlnPath
dotnet build $E2EProj --no-restore -c $Configuration

$e2eProjDir = Split-Path -Parent $E2EProj
$playwrightE2e = Join-Path $e2eProjDir "bin\$Configuration\net10.0\playwright.ps1"
if (-not (Test-Path $playwrightE2e)) {
    Write-Error "Playwright bootstrap script not found at $playwrightE2e — build E2E project first."
}

& $Pwsh -NoProfile -ExecutionPolicy Bypass -File $playwrightE2e install --with-deps chromium

if ($InstallBrowsersOnly) {
    Write-Host "InstallBrowsersOnly: skipping tests." -ForegroundColor Yellow
    exit 0
}

Write-Host "`n--- RequiemNexus.E2E.Tests ---" -ForegroundColor Cyan
dotnet test $E2EProj --no-build -c $Configuration --filter "Category!=VisualRegression"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($VisualRegression) {
    dotnet build $VrProj --no-restore -c $Configuration
    $vrProjDir = Split-Path -Parent $VrProj
    $playwrightVr = Join-Path $vrProjDir "bin\$Configuration\net10.0\playwright.ps1"
    if (-not (Test-Path $playwrightVr)) {
        Write-Error "Playwright script not found at $playwrightVr."
    }

    & $Pwsh -NoProfile -ExecutionPolicy Bypass -File $playwrightVr install --with-deps chromium
    Write-Host "`n--- RequiemNexus.VisualRegression.Tests ---" -ForegroundColor Cyan
    dotnet test $VrProj --no-build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host "`nE2E local run finished." -ForegroundColor Green
