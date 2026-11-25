param([string]$MySqlBin = '', [switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
$link = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ExistingDatabase.json') -Raw | ConvertFrom-Json
$legacy = if ($link.UseLocalData) { $PSScriptRoot } else { [IO.Path]::GetFullPath($link.ProjectRoot) }
$private = Join-Path $legacy 'App_Data\MySQL'
foreach ($required in @('instance.json','client.ini','root-client.ini','data\ibdata1','data\mysql.ibd','data\lodgo_a9\users.ibd')) {
    if (!(Test-Path -LiteralPath (Join-Path $private $required))) { throw "Existing database file missing: $required. Refusing to initialize a replacement database." }
}
$state = Get-Content -LiteralPath (Join-Path $private 'instance.json') -Raw | ConvertFrom-Json
$expected = (Get-Content -LiteralPath (Join-Path $legacy 'appsettings.Local.json') -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
$actual = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'appsettings.Local.json') -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
if ($actual -ne $expected) { throw 'Latest and existing project connection settings differ. Refusing to start.' }
if (!$MySqlBin) {
    $candidate = Get-ChildItem -LiteralPath (Join-Path $env:ProgramFiles 'MySQL') -Directory |
        Sort-Object Name -Descending | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'bin\mysqld.exe') } | Select-Object -First 1
    if (!$candidate) { throw 'MySQL Server not found.' }
    $MySqlBin = Join-Path $candidate.FullName 'bin'
}
$client = Join-Path $MySqlBin 'mysql.exe'
function Test-ExistingDatabase {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $client
    $info.WorkingDirectory = $private
    $info.Arguments = '--defaults-extra-file=client.ini --connect-timeout=2 --batch --raw --skip-column-names'
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($info)
    try {
        $process.StandardInput.WriteLine('SELECT @@datadir; SELECT COUNT(*) FROM lodgo_a9.users;')
        $process.StandardInput.Close()
        $stdout = $process.StandardOutput.ReadToEnd()
        $null = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { return $false }
        $result = @($stdout.TrimEnd().Split("`n") | ForEach-Object { $_.TrimEnd("`r") })
    } finally { $process.Dispose() }
    $wanted = [IO.Path]::GetFullPath((Join-Path $private 'data')).TrimEnd('\','/')
    if ($result.Count -ne 2 -or [IO.Path]::GetFullPath([string]$result[0]).TrimEnd('\','/') -ne $wanted) {
        throw 'MySQL is using a different data directory. No database was changed.'
    }
    return $true
}
if (!(Test-ExistingDatabase)) {
    if ($CheckOnly) { throw 'Existing database is not running or authentication failed.' }
    $probe = New-Object Net.Sockets.TcpClient
    try { $probe.Connect('127.0.0.1', [int]$state.Port); throw 'Database port is occupied by another instance. No server was stopped.' }
    catch [Net.Sockets.SocketException] {} finally { $probe.Dispose() }
    $base = Split-Path -Parent $MySqlBin
    $argsList = @('--no-defaults','--no-monitor',"--basedir=`"$base`"", "--datadir=`"$private\data`"", "--tmpdir=`"$private\tmp`"", "--log-error=`"$private\server.log`"", "--pid-file=`"$private\mysql.pid`"", "--port=$($state.Port)", '--bind-address=127.0.0.1','--mysqlx=OFF','--innodb-buffer-pool-size=64M')
    # Existing data only: no initialization, CREATE DATABASE, password reset or seed SQL.
    $server = Start-Process -FilePath (Join-Path $MySqlBin 'mysqld.exe') -ArgumentList $argsList -WindowStyle Hidden -PassThru
    $ready = $false
    for ($n=0; $n -lt 30; $n++) {
        if (Test-ExistingDatabase) { $ready = $true; break }
        if ($server.HasExited) { break }
        Start-Sleep -Milliseconds 400
    }
    if (!$ready) { throw "Existing MySQL did not become ready. Inspect $private\server.log" }
}
Write-Host "Verified existing database: $legacy (port $($state.Port))."
