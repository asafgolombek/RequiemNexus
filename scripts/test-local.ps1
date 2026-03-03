#Requires -Version 5.1
<#
.SYNOPSIS
    Run all unit and integration tests locally with optional code coverage.

.PARAMETER Coverage
    When specified, collects code coverage and opens the HTML report after tests complete.

.PARAMETER Configuration
    Build configuration to test. Defaults to Debug.

.EXAMPLE
    .\test-local.ps1
    .\test-local.ps1 -Coverage
    .\test-local.ps1 -Configuration Release
#>
param(
    [switch]$Coverage,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$ScriptRoot  = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$RootDir     = Split-Path -Parent -Path $ScriptRoot
$SlnPath     = Join-Path $RootDir "src\RequiemNexus.slnx"
$DomainTests = Join-Path $RootDir "tests\RequiemNexus.Domain.Tests\RequiemNexus.Domain.Tests.csproj"
$DataTests   = Join-Path $RootDir "tests\RequiemNexus.Data.Tests\RequiemNexus.Data.Tests.csproj"
$E2ETests    = Join-Path $RootDir "tests\RequiemNexus.E2ETests\RequiemNexus.E2ETests.csproj"
$CoverageDir = Join-Path $RootDir "coverage"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "══════════════════════════════════════" -ForegroundColor DarkCyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════" -ForegroundColor DarkCyan
}

function Write-Success([string]$Message) {
    Write-Host "  ✓ $Message" -ForegroundColor Green
}

function Write-Fail([string]$Message) {
    Write-Host "  ✗ $Message" -ForegroundColor Red
}

# ── 1. Restore ───────────────────────────────────────────────────────────────
Write-Step "Restoring dependencies"
dotnet restore $SlnPath
Write-Success "Restore complete"

# ── 2. Build ─────────────────────────────────────────────────────────────────
Write-Step "Building solution ($Configuration)"
dotnet build $SlnPath --no-restore -c $Configuration
Write-Success "Build complete"

# ── 3. Prepare coverage output dir ───────────────────────────────────────────
if ($Coverage) {
    if (Test-Path $CoverageDir) { Remove-Item -Recurse -Force $CoverageDir }
    New-Item -ItemType Directory -Path $CoverageDir | Out-Null
}

$TestArgs = @("--no-build", "-c", $Configuration)
if ($Coverage) {
    $TestArgs += "--collect:XPlat Code Coverage"
    $TestArgs += "--results-directory"
    $TestArgs += $CoverageDir
}

# ── 4. Domain unit tests ──────────────────────────────────────────────────────
Write-Step "Running Domain unit tests"
$domainResult = 0
dotnet test $DomainTests @TestArgs
$domainResult = $LASTEXITCODE

# ── 5. Data integration tests ────────────────────────────────────────────────
Write-Step "Running Data integration tests"
$dataResult = 0
dotnet test $DataTests @TestArgs
$dataResult = $LASTEXITCODE

# ── 6. E2E tests ──────────────────────────────────────────────────────────────
Write-Step "Running E2E tests"
$e2eResult = 0
try {
    pwsh "$RootDir\tests\RequiemNexus.E2ETests\bin\$Configuration\net10.0\playwright.ps1" install
} catch {
    Write-Host "Playwright installation skipped or failed." -ForegroundColor Yellow
}
dotnet test $E2ETests @TestArgs
$e2eResult = $LASTEXITCODE

# ── 7. Summary ───────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════" -ForegroundColor DarkMagenta
Write-Host "  TEST SUMMARY" -ForegroundColor Magenta
Write-Host "══════════════════════════════════════" -ForegroundColor DarkMagenta

if ($domainResult -eq 0) { Write-Success "Domain tests:      PASSED" }
else                      { Write-Fail   "Domain tests:      FAILED" }

if ($dataResult -eq 0)   { Write-Success "Integration tests: PASSED" }
else                      { Write-Fail   "Integration tests: FAILED" }

if ($e2eResult -eq 0)    { Write-Success "E2E tests:         PASSED" }
else                      { Write-Fail   "E2E tests:         FAILED" }

Write-Host ""

# ── 7. Open coverage report ──────────────────────────────────────────────────
if ($Coverage) {
    $htmlReport = Get-ChildItem -Path $CoverageDir -Filter "index.html" -Recurse | Select-Object -First 1
    if ($htmlReport) {
        Write-Host "  Opening coverage report..." -ForegroundColor DarkGray
        Start-Process $htmlReport.FullName
    } else {
        Write-Host "  Coverage files saved to: $CoverageDir" -ForegroundColor DarkGray
        Write-Host "  (Install reportgenerator to generate HTML: dotnet tool install -g dotnet-reportgenerator-globaltool)" -ForegroundColor DarkGray
    }
}

# ── 9. Exit with failure if any test suite failed ────────────────────────────
if ($domainResult -ne 0 -or $dataResult -ne 0 -or $e2eResult -ne 0) {
    exit 1
}

Write-Host "  All tests passed!" -ForegroundColor Green
