@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Lodgo.ps1" -Mode Run
if errorlevel 1 echo Lodgo could not complete this action. See the message above.
pause
