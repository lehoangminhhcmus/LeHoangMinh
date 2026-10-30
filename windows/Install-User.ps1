param([switch]$Uninstall)
$ErrorActionPreference = 'Stop'
if (!$env:LOCALAPPDATA) { throw 'LOCALAPPDATA is unavailable.' }
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\GammaCNFReader-CSharp'
$programs = [Environment]::GetFolderPath('Programs')
$shortcut = Join-Path $programs 'Gamma CNF Reader (CSharp).lnk'
$registry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\GammaCNFReaderCSharp'
if ($Uninstall) {
    foreach ($file in @('GammaCNFReader.exe', 'Install-User.ps1')) {
        $target = Join-Path $installDir $file
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
    }
    if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
    if (Test-Path -LiteralPath $registry) { Remove-Item -LiteralPath $registry }
    Write-Host 'Gamma CNF Reader uninstalled.'
    exit 0
}
$source = Join-Path $PSScriptRoot 'GammaCNFReader.exe'
if (!(Test-Path -LiteralPath $source)) { $source = Join-Path $PSScriptRoot 'dist\app\GammaCNFReader.exe' }
if (!(Test-Path -LiteralPath $source)) { throw 'GammaCNFReader.exe missing. Extract the full Windows package or run build-windows.bat first.' }
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
$exe = Join-Path $installDir 'GammaCNFReader.exe'
Copy-Item -LiteralPath $source -Destination $exe -Force
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $installDir 'Install-User.ps1') -Force
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
$link.TargetPath = $exe
$link.WorkingDirectory = $installDir
$link.Description = 'Canberra gamma spectrum viewer'
$link.Save()
New-Item -Path $registry -Force | Out-Null
$uninstallCommand = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installDir 'Install-User.ps1') + '" -Uninstall'
$properties = @{ DisplayName='Gamma CNF Reader (CSharp)'; DisplayVersion='0.9.0'; DisplayIcon=$exe; InstallLocation=$installDir; UninstallString=$uninstallCommand }
$properties.GetEnumerator() | ForEach-Object {
    New-ItemProperty -Path $registry -Name $_.Key -Value $_.Value -PropertyType String -Force | Out-Null
}
Write-Host "Installed: $exe"
Write-Host 'Open Gamma CNF Reader (CSharp) from the Start menu. Uninstall from Windows Settings > Apps.'
