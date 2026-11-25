param([int]$Port = 33492, [string]$MySqlBin = '')
$ErrorActionPreference = 'Stop'
$marker = Join-Path $PSScriptRoot 'ExistingDatabase.json'
if (!(Test-Path -LiteralPath $marker)) {
    throw 'ExistingDatabase.json is missing. Refusing to initialize a new database.'
}
$link = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
$databaseProject = if ($link.UseLocalData) { $PSScriptRoot } else { $link.ProjectRoot }
$state = Get-Content -LiteralPath (Join-Path $databaseProject 'App_Data\MySQL\instance.json') -Raw | ConvertFrom-Json
if ($PSBoundParameters.ContainsKey('Port') -and $Port -ne $state.Port) {
    throw 'Use the existing database saved port. No settings were changed.'
}
& (Join-Path $PSScriptRoot 'Existing-Database.ps1') -MySqlBin $MySqlBin