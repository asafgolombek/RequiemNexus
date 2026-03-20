# Shared paths and helpers — dot-source this from build scripts.
$ErrorActionPreference = "Stop"

$RootDir      = Split-Path -Parent -Path $PSScriptRoot
# Full app + all test projects (matches tests/RequiemNexus.Tests.slnx).
$SlnPath      = Join-Path $RootDir "tests\RequiemNexus.Tests.slnx"
# App-only solution (src/RequiemNexus.slnx) — use when tests are not needed.
$SrcSlnPath   = Join-Path $RootDir "src\RequiemNexus.slnx"
$WebProjPath  = Join-Path $RootDir "src\RequiemNexus.Web\RequiemNexus.Web.csproj"
$DataProjPath = Join-Path $RootDir "src\RequiemNexus.Data\RequiemNexus.Data.csproj"

# Test projects (each listed in RequiemNexus.Tests.slnx)
$DomainTests      = Join-Path $RootDir "tests\RequiemNexus.Domain.Tests\RequiemNexus.Domain.Tests.csproj"
$DataTests        = Join-Path $RootDir "tests\RequiemNexus.Data.Tests\RequiemNexus.Data.Tests.csproj"
$ApplicationTests = Join-Path $RootDir "tests\RequiemNexus.Application.Tests\RequiemNexus.Application.Tests.csproj"
$WebTests         = Join-Path $RootDir "tests\RequiemNexus.Web.Tests\RequiemNexus.Web.Tests.csproj"
# NBomber console app (not xUnit) — use .\scripts\run-performance.ps1 or dotnet run --project $PerformanceTests.
$PerformanceTests = Join-Path $RootDir "tests\RequiemNexus.PerformanceTests\RequiemNexus.PerformanceTests.csproj"

function Invoke-Restore {
    Write-Host "Restoring dependencies..." -ForegroundColor DarkGray
    dotnet restore $SlnPath
}

