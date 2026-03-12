param(
    [string]$TargetUrl = "http://localhost:5154"
)

$endpoints = @("/health", "/ready")
$allPassed = $true

foreach ($endpoint in $endpoints) {
    $url = "$TargetUrl$endpoint"
    Write-Host "Checking health at $url..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ [PASS] $endpoint returned 200 OK" -ForegroundColor Green
        } else {
            Write-Host "❌ [FAIL] $endpoint returned $($response.StatusCode)" -ForegroundColor Red
            $allPassed = $false
        }
    } catch {
        Write-Host "❌ [FAIL] Could not reach $url. Error: $($_.Exception.Message)" -ForegroundColor Red
        $allPassed = $false
    }
}

if (-not $allPassed) {
    Write-Host "❌ Smoke tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ All smoke tests passed!" -ForegroundColor Green
exit 0
