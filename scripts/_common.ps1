# Shared paths and helpers — dot-source this from build scripts.
$ErrorActionPreference = "Stop"

$RootDir     = Split-Path -Parent -Path $PSScriptRoot
$SlnPath     = Join-Path $RootDir "src\RequiemNexus.slnx"
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'WebProjPath',
    Justification = 'Defined here for dot-sourcing scripts (e.g. build-debug.ps1) — cross-file usage is invisible to PSScriptAnalyzer.')]
$WebProjPath = Join-Path $RootDir "src\RequiemNexus.Web\RequiemNexus.Web.csproj"
$DomainTests = Join-Path $RootDir "tests\RequiemNexus.Domain.Tests\RequiemNexus.Domain.Tests.csproj"
$DataTests   = Join-Path $RootDir "tests\RequiemNexus.Data.Tests\RequiemNexus.Data.Tests.csproj"

function Invoke-Restore {
    Write-Host "Restoring dependencies..." -ForegroundColor DarkGray
    dotnet restore $SlnPath
}

function Invoke-Tests {
    param([string]$Configuration = "Debug")

    Write-Host "`nRunning tests..." -ForegroundColor DarkGray

    dotnet test $DomainTests --no-build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[ABORTED] Domain unit tests failed. Fix tests before proceeding. Use -SkipTests to bypass." -ForegroundColor Red
        exit 1
    }

    dotnet test $DataTests --no-build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[ABORTED] Integration tests failed. Fix tests before proceeding. Use -SkipTests to bypass." -ForegroundColor Red
        exit 1
    }

    Write-Host "`n  All tests passed!" -ForegroundColor Green
}
