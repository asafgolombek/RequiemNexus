# Shared paths and helpers — dot-source this from build scripts.
$ErrorActionPreference = "Stop"

$RootDir     = Split-Path -Parent -Path $PSScriptRoot
$SlnPath     = Join-Path $RootDir "tests\RequiemNexus.Tests.slnx"
$WebProjPath = Join-Path $RootDir "src\RequiemNexus.Web\RequiemNexus.Web.csproj"
$DataProj    = Join-Path $RootDir "src\RequiemNexus.Data\RequiemNexus.Data.csproj"

# Test Projects
$DomainTests = Join-Path $RootDir "tests\RequiemNexus.Domain.Tests\RequiemNexus.Domain.Tests.csproj"
$DataTests   = Join-Path $RootDir "tests\RequiemNexus.Data.Tests\RequiemNexus.Data.Tests.csproj"
$WebTests    = Join-Path $RootDir "tests\RequiemNexus.Web.Tests\RequiemNexus.Web.Tests.csproj"

function Invoke-Restore {
    Write-Host "Restoring dependencies..." -ForegroundColor DarkGray
    dotnet restore $SlnPath
}

