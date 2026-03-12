#Requires -Version 5.1
<#
.SYNOPSIS
    Run all unit and integration tests locally with optional code coverage.
#>
param(
    [switch]$Coverage,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# --- Paths ---
$ScriptRoot  = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$RootDir     = Split-Path -Parent -Path $ScriptRoot
$SlnPath     = Join-Path $RootDir "tests\RequiemNexus.Tests.slnx"
$CoverageDir = Join-Path $RootDir "coverage"

# --- Project Configuration ---
$TestProjects = @(
    @{ Name = "Domain Unit"; Path = "tests\RequiemNexus.Domain.Tests\RequiemNexus.Domain.Tests.csproj" },
    @{ Name = "Data Integration"; Path = "tests\RequiemNexus.Data.Tests\RequiemNexus.Data.Tests.csproj" }
)

# --- UI Helpers ---
function Write-Step([string]$Message) {
    $line = "=" * 40
    Write-Host "`n$line" -ForegroundColor DarkCyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "$line" -ForegroundColor DarkCyan
}

function Write-Success([string]$Message) {
    Write-Host "  [PASS] $Message" -ForegroundColor Green
}

function Write-Fail([string]$Message) {
    Write-Host "  [FAIL] $Message" -ForegroundColor Red
}

# --- 1. Restore & Build ---
Write-Step "Preparing Solution ($Configuration)"
dotnet restore $SlnPath
dotnet build $SlnPath --no-restore -c $Configuration
Write-Success "Build complete"

# --- 2. Prepare Coverage ---
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

# --- 3. Formatting Check ---
$Results = @{}
Write-Step "Checking Code Formatting"
dotnet format $SlnPath --verify-no-changes
$Results["Formatting"] = $LASTEXITCODE
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Formatting check failed. Run 'dotnet format' to fix."
} else {
    Write-Success "Formatting is correct."
}

# --- 4. Execution Loop ---
foreach ($Project in $TestProjects) {
    Write-Step "Running $($Project.Name) tests"
    


    $ProjectPath = Join-Path $RootDir $Project.Path
    dotnet test $ProjectPath @TestArgs
    $Results[$Project.Name] = $LASTEXITCODE
    

}

# --- 4. Generate Coverage Report ---
if ($Coverage) {
    Write-Step "Generating Coverage Report"
    $Generator = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if ($Generator) {
        & $Generator.Source "-reports:$CoverageDir\**\coverage.cobertura.xml" "-targetdir:$CoverageDir\report" "-reporttypes:Html"
        $HtmlReport = Join-Path $CoverageDir "report\index.html"
        if (Test-Path $HtmlReport) {
            Write-Host "  Opening report..." -ForegroundColor DarkGray
            Start-Process $HtmlReport
        }
    } else {
        Write-Host "  ! reportgenerator tool not found. Only raw XML generated." -ForegroundColor Yellow
        Write-Host "  Run: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor DarkGray
    }
}

# --- 5. Summary ---
$summaryLine = "=" * 40
Write-Host "`n$summaryLine" -ForegroundColor DarkMagenta
Write-Host "  TEST SUMMARY" -ForegroundColor Magenta
Write-Host "$summaryLine" -ForegroundColor DarkMagenta

$GlobalFail = $false
foreach ($Key in $Results.Keys) {
    if ($Results[$Key] -eq 0) { 
        Write-Success "$($Key.PadRight(18)): PASSED" 
    } else { 
        Write-Fail "$($Key.PadRight(18)): FAILED" 
        $GlobalFail = $true
    }
}

if ($GlobalFail) { 
    Write-Host "`n  One or more test suites failed." -ForegroundColor Red
    exit 1 
}

Write-Host "`n  All tests passed! 🚀" -ForegroundColor Green