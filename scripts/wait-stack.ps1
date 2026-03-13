param(
    [string]$StackName    = "RequiemNexus-Compute-Stack",
    [int]$IntervalSeconds = 15
)

$ErrorActionPreference = "Stop"

$terminalStates = @(
    "CREATE_COMPLETE",
    "CREATE_FAILED",
    "UPDATE_COMPLETE",
    "UPDATE_FAILED",
    "UPDATE_ROLLBACK_COMPLETE",
    "UPDATE_ROLLBACK_FAILED",
    "DELETE_COMPLETE",
    "DELETE_FAILED",
    "ROLLBACK_COMPLETE",
    "ROLLBACK_FAILED"
)

Write-Host "Watching '$StackName' -- polling every ${IntervalSeconds}s..." -ForegroundColor DarkGray

while ($true) {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $status = aws cloudformation describe-stacks `
        --stack-name $StackName `
        --query "Stacks[0].StackStatus" `
        --output text 2>$null
    $ErrorActionPreference = $prev

    $timestamp = Get-Date -Format "HH:mm:ss"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$timestamp] Stack not found or AWS error." -ForegroundColor Red
        exit 1
    }

    if ($status -like "*FAILED*" -or $status -like "*ROLLBACK*") {
        $color = "Red"
    } elseif ($status -like "*COMPLETE*") {
        $color = "Green"
    } else {
        $color = "Yellow"
    }

    Write-Host "[$timestamp] $status" -ForegroundColor $color

    if ($terminalStates -contains $status) {
        Write-Host ""
        if ($status -eq "CREATE_COMPLETE" -or $status -eq "UPDATE_COMPLETE") {
            Write-Host "[DONE] Stack is ready." -ForegroundColor Green
            exit 0
        } else {
            Write-Host "[DONE] Stack ended in: $status" -ForegroundColor Red
            exit 1
        }
    }

    Start-Sleep -Seconds $IntervalSeconds
}
