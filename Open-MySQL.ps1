param([string]$MySqlBin = "", [switch]$ListUsers)
$ErrorActionPreference = 'Stop'
if (!$MySqlBin) {
    $candidate = Get-ChildItem (Join-Path $env:ProgramFiles 'MySQL') -Directory |
        Sort-Object Name -Descending | Where-Object { Test-Path (Join-Path $_.FullName 'bin/mysql.exe') } | Select-Object -First 1
    if (!$candidate) { throw 'MySQL client not found. Specify -MySqlBin.' }
    $MySqlBin = Join-Path $candidate.FullName 'bin'
}
$client = Join-Path $MySqlBin 'mysql.exe'
$privateRoot = Join-Path $PSScriptRoot 'App_Data\MySQL'
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'ExistingDatabase.json')) {
    & (Join-Path $PSScriptRoot 'Existing-Database.ps1') -CheckOnly
    $link = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ExistingDatabase.json') -Raw | ConvertFrom-Json
    $databaseProject = if ($link.UseLocalData) { $PSScriptRoot } else { $link.ProjectRoot }
    $privateRoot = Join-Path $databaseProject 'App_Data\MySQL'
}
$config = Join-Path $privateRoot 'client.ini'
if (!(Test-Path -LiteralPath $config)) { throw 'Run Start-MySQL.ps1 first. For an external database, connect with mysql -h HOST -P PORT -u USER -p lodgo_a9.' }
$arguments = @('--defaults-extra-file=client.ini', '--default-character-set=utf8mb4', 'lodgo_a9')
if ($ListUsers) { $arguments += @('--table', '-e', 'SELECT Id, FirstName, LastName, Email, Role, CreatedAtUtc FROM users ORDER BY Id;') }
Push-Location -LiteralPath $privateRoot
try {
    & $client @arguments
    if ($LASTEXITCODE -ne 0) { throw 'MySQL command failed. Check that Start-MySQL.ps1 is running.' }
} finally {
    Pop-Location
}
