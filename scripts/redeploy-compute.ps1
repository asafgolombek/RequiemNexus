param(
    [string]$StackName = "RequiemNexus-Compute-Stack"
)

$ErrorActionPreference = "Stop"
$ScriptsDir = $PSScriptRoot

# ── Step 1: Delete the existing stack ────────────────────────────────────────
Write-Host "Deleting stack '$StackName'..." -ForegroundColor DarkGray
$prev = $ErrorActionPreference
$ErrorActionPreference = "Continue"
aws cloudformation delete-stack --stack-name $StackName 2>$null
$ErrorActionPreference = $prev

# ── Step 2: Wait for deletion to complete ────────────────────────────────────
# wait-stack.ps1 treats "stack not found" as an error; use AWS CLI wait for delete completion.
Write-Host "Waiting for stack deletion (aws cloudformation wait)..." -ForegroundColor DarkGray
$prev = $ErrorActionPreference
$ErrorActionPreference = "Continue"
aws cloudformation wait stack-delete-complete --stack-name $StackName 2>$null
$waitExit = $LASTEXITCODE
$ErrorActionPreference = $prev

if ($waitExit -ne 0) {
    $ErrorActionPreference = "Continue"
    $null = aws cloudformation describe-stacks --stack-name $StackName --query "Stacks[0].StackStatus" --output text 2>$null
    $describeExit = $LASTEXITCODE
    $ErrorActionPreference = $prev

    if ($describeExit -eq 0) {
        Write-Host "[ERROR] wait stack-delete-complete failed (exit $waitExit) but the stack still exists. Resolve manually, then retry." -ForegroundColor Red
        exit $waitExit
    }

    Write-Host "Stack absent or fully deleted; proceeding with deploy." -ForegroundColor DarkGray
}

# ── Step 3: Deploy fresh ──────────────────────────────────────────────────────
Write-Host "`nDeploying fresh stack..." -ForegroundColor DarkGray
& "$ScriptsDir\deploy-compute.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[FAILED] Deploy failed with code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}
