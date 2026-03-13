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
& "$ScriptsDir\wait-stack.ps1" -StackName $StackName
# wait-stack exits 0 on success, 1 on failure — but "not found" means deleted, which is fine.

# ── Step 3: Deploy fresh ──────────────────────────────────────────────────────
Write-Host "`nDeploying fresh stack..." -ForegroundColor DarkGray
& "$ScriptsDir\deploy-compute.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[FAILED] Deploy failed with code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}
