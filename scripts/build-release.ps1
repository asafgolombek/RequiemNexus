param(
    [switch]$SkipTests
)

& "$PSScriptRoot\build.ps1" -Configuration Release -SkipTests:$SkipTests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
