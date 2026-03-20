#Requires -Version 5.1
<#
.SYNOPSIS
    Run NBomber performance scenarios against a running Requiem Nexus instance.

.NOTES
    Set TARGET_URL to match your launch profile (default matches launchSettings http port).
    Example: $env:TARGET_URL = 'https://localhost:7256'; .\scripts\run-performance.ps1
#>
param(
    [string]$TargetUrl = "http://localhost:5251"
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\_common.ps1"

$env:TARGET_URL = $TargetUrl
Write-Host "TARGET_URL=$($env:TARGET_URL)" -ForegroundColor DarkGray
dotnet run --project $PerformanceTests -c Release
