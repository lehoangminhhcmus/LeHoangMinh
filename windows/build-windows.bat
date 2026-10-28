@echo off
setlocal
cd /d "%~dp0"
dotnet run --project GammaCnf.Checks -c Release
if errorlevel 1 goto error
dotnet publish GammaCnf.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o dist\app
if errorlevel 1 goto error
where ISCC.exe >nul 2>nul
if not errorlevel 1 (
  ISCC.exe installer.iss
) else if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
  "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" installer.iss
) else (
  echo Portable app ready: dist\app\GammaCNFReader.exe
  echo Install Inno Setup 6 to create Setup.exe.
  exit /b 0
)
if errorlevel 1 goto error
echo Installer ready: dist\installer\GammaCNFReader-CSharp-Setup.exe
exit /b 0
:error
echo Build failed. Read the errors above.
exit /b 1
