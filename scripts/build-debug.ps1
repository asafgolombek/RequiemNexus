$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent -Path $ScriptRoot
$SlnPath = Join-Path -Path $RootDir -ChildPath "RequiemNexus.slnx"
$WebProjPath = Join-Path -Path $RootDir -ChildPath "src\RequiemNexus.Web\RequiemNexus.Web.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building and Booting Requiem Nexus - Debug Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "Restoring dependencies..." -ForegroundColor DarkGray
dotnet restore $SlnPath

Write-Host "`nBuilding solution..." -ForegroundColor DarkGray
dotnet build $SlnPath -c Debug

Write-Host "`nApplying database migrations..." -ForegroundColor DarkGray
dotnet ef database update --project (Join-Path -Path $RootDir -ChildPath "src\RequiemNexus.Data") --startup-project $WebProjPath

Write-Host "`nBooting application..." -ForegroundColor Green
dotnet run --project $WebProjPath -c Debug

Write-Host "`nApplication exited!" -ForegroundColor Green
