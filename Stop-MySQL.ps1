$ErrorActionPreference = 'Stop'
$privateRoot = Join-Path $PSScriptRoot 'App_Data\MySQL'
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'ExistingDatabase.json')) {
    & (Join-Path $PSScriptRoot 'Existing-Database.ps1') -CheckOnly
    $link = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ExistingDatabase.json') -Raw | ConvertFrom-Json
    $databaseProject = if ($link.UseLocalData) { $PSScriptRoot } else { $link.ProjectRoot }
    $privateRoot = Join-Path $databaseProject 'App_Data\MySQL'
}
$clientPath = Join-Path $privateRoot 'root-client.ini'
if (!(Test-Path -LiteralPath $clientPath)) { Write-Host 'No private MySQL instance has been configured.'; return }
$candidate = Get-ChildItem -LiteralPath (Join-Path $env:ProgramFiles 'MySQL') -Directory |
    Sort-Object Name -Descending | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'bin\mysql.exe') } | Select-Object -First 1
if (!$candidate) { throw 'mysql.exe was not found.' }
Push-Location -LiteralPath $privateRoot
try {
    & (Join-Path $candidate.FullName 'bin\mysql.exe') '--defaults-extra-file=root-client.ini' --execute=SHUTDOWN
    if ($LASTEXITCODE -ne 0) { throw 'The project database could not be stopped. No other process was changed.' }
} finally {
    Pop-Location
}
Write-Host 'Project MySQL stopped. Its saved data is retained.'
