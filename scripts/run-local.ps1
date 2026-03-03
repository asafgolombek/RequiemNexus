param(
    [string]$Configuration = "Debug"
)

. "$PSScriptRoot\_common.ps1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running Requiem Nexus - $Configuration Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nApplying database migrations..." -ForegroundColor DarkGray
dotnet ef database update --project (Join-Path $RootDir "src\RequiemNexus.Data") --startup-project $WebProjPath

Write-Host "`nBooting application..." -ForegroundColor Green
dotnet run --project $WebProjPath -c $Configuration

Write-Host "`nApplication exited!" -ForegroundColor Green
