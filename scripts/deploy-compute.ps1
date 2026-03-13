param(
    [ValidateSet("deploy", "destroy", "diff", "synth")]
    [string]$Action = "deploy",

    [string]$AwsRegion    = $env:CDK_DEFAULT_REGION,
    [string]$Stack        = "RequiemNexus-Compute-Stack"
)

$AwsAccountId = "216938126042"

$ErrorActionPreference = "Stop"

# ── Apply defaults ───────────────────────────────────────────────────────────
if (-not $AwsRegion) { $AwsRegion = "us-east-1" }

# ── Resolve account ID ───────────────────────────────────────────────────────
if (-not $AwsAccountId) {
    Write-Host "Resolving AWS account ID via STS..." -ForegroundColor DarkGray
    # Temporarily allow native command errors so aws CLI stderr doesn't throw.
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $AwsAccountId = aws sts get-caller-identity --query Account --output text 2>$null
    $stsExit = $LASTEXITCODE
    $ErrorActionPreference = $prev

    if ($stsExit -ne 0 -or -not $AwsAccountId) {
        Write-Host "[ERROR] Could not resolve AWS account. Ensure credentials are configured:" -ForegroundColor Red
        Write-Host "  - Run 'aws configure'  (access key + secret)" -ForegroundColor Yellow
        Write-Host "  - Or set AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY env vars" -ForegroundColor Yellow
        Write-Host "  - Or run 'aws sso login' if using SSO" -ForegroundColor Yellow
        Write-Host "  - Or pass -AwsAccountId 216938126042 explicitly" -ForegroundColor Yellow
        exit 1
    }
}

# ── Set CDK environment variables ────────────────────────────────────────────
$env:CDK_DEFAULT_ACCOUNT = $AwsAccountId
$env:CDK_DEFAULT_REGION  = $AwsRegion

Write-Host "Account : $AwsAccountId" -ForegroundColor DarkGray
Write-Host "Region  : $AwsRegion"    -ForegroundColor DarkGray
Write-Host "Stack   : $Stack"        -ForegroundColor DarkGray
Write-Host "Action  : $Action`n"     -ForegroundColor DarkGray

# ── Run CDK from the infra directory ─────────────────────────────────────────
$InfraDir = Join-Path (Split-Path -Parent -Path $PSScriptRoot) "infra"

# Remove cached synthesis output so CDK recomputes asset hashes from current
# source files. Without this, CDK may reuse a stale Docker image from ECR.
$cdkOut = Join-Path $InfraDir "cdk.out"
if (Test-Path $cdkOut) {
    Write-Host "Clearing cdk.out cache..." -ForegroundColor DarkGray
    Remove-Item -Recurse -Force $cdkOut
}

Push-Location $InfraDir
try {
    switch ($Action) {
        "deploy"  { cdk deploy  $Stack --require-approval never }
        "destroy" { cdk destroy $Stack --force }
        "diff"    { cdk diff    $Stack }
        "synth"   { cdk synth   $Stack }
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[FAILED] cdk $Action exited with code $LASTEXITCODE." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "`n[OK] cdk $Action $Stack completed." -ForegroundColor Green
}
finally {
    Pop-Location
}
