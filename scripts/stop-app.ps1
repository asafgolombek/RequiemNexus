# stop-app.ps1
# Stops the RequiemNexus application process to free up file locks.

Write-Host "Searching for RequiemNexus.Web processes..." -ForegroundColor Cyan

$processName = "RequiemNexus.Web"
$processes = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($processes) {
    Write-Host "Stopping $($processes.Count) process(es)..." -ForegroundColor Yellow
    foreach ($p in $processes) {
        try {
            Stop-Process -Id $p.Id -Force
            Write-Host "Stopped process ID: $($p.Id)" -ForegroundColor Green
        } catch {
            Write-Host "Failed to stop process ID: $($p.Id). You might need to run as Administrator." -ForegroundColor Red
        }
    }
} else {
    Write-Host "No $processName processes found." -ForegroundColor Gray
}

# Also cleanup any orphaned dotnet processes running our project
$dotnetProcesses = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe' AND CommandLine LIKE '%RequiemNexus.Web%'"
if ($dotnetProcesses) {
    Write-Host "Stopping orphaned dotnet runner..." -ForegroundColor Yellow
    foreach ($dp in $dotnetProcesses) {
        Stop-Process -Id $dp.ProcessId -Force
        Write-Host "Stopped dotnet process ID: $($dp.ProcessId)" -ForegroundColor Green
    }
}

Write-Host "Done!" -ForegroundColor Cyan
