param(
    [switch]$SkipTests
)

& "$PSScriptRoot\build.ps1" -Configuration Debug -SkipTests:$SkipTests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
