param([switch]$CreateAdmin)
$ErrorActionPreference = 'Stop'
$launcher = Join-Path (Split-Path -Parent $PSScriptRoot) 'tools\Lodgo.ps1'
if ($CreateAdmin) { & $launcher -Mode Admin }
else { & $launcher -Mode Run }