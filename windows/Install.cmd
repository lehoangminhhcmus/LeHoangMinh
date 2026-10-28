@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-User.ps1"
if errorlevel 1 (
  echo Installation failed. Close the running app and check the error above.
  pause
  exit /b 1
)
pause
