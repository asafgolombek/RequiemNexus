# Strips SQLite-specific column type annotations from EF migration C# files
# after generating on SQLite — use when aligning artifacts for PostgreSQL.
# Prefer generating migrations against the target provider when possible.
$ErrorActionPreference = "Stop"

$migrationsDir = Join-Path (Split-Path -Parent $PSScriptRoot) "src\RequiemNexus.Data\Migrations"

$files = Get-ChildItem "$migrationsDir/*.cs"

foreach ($file in $files) {
    $original = Get-Content $file.FullName -Raw
    $fixed = $original

    # ── Migration .cs files: remove type: "SQLITE_TYPE" from migrationBuilder calls ──
    # Remove 'type: "SQLITE_TYPE", ' (type is first/middle param — comma trails)
    $fixed = $fixed -replace '(?<![A-Za-z])type: "(TEXT|BLOB|INTEGER|REAL)",\s*', ''
    # Remove ', type: "SQLITE_TYPE"' (type is last param — comma leads)
    $fixed = $fixed -replace ',\s*(?<![A-Za-z])type: "(TEXT|BLOB|INTEGER|REAL)"', ''

    # ── Designer.cs / ModelSnapshot: remove .HasColumnType("SQLITE_TYPE") ──
    $fixed = $fixed -replace '\.HasColumnType\("(TEXT|BLOB|INTEGER|REAL)"\)', ''

    if ($fixed -ne $original) {
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($file.FullName, $fixed, $utf8NoBom)
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`nDone." -ForegroundColor Green
