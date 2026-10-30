[Setup]
AppId={{0913EDC8-A1FD-47CD-BCC1-401887F22915}
AppName=Gamma CNF Reader
AppVersion=0.9.0
DefaultDirName={localappdata}\Programs\GammaCNFReader-CSharp
DefaultGroupName=Gamma CNF Reader (C#)
PrivilegesRequired=lowest
OutputDir=dist\installer
OutputBaseFilename=GammaCNFReader-CSharp-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
UninstallDisplayIcon={app}\GammaCNFReader.exe
[Files]
Source: "dist\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "associate"; Description: "Add Open with Gamma CNF Reader for .cnf files"; Flags: unchecked
[Icons]
Name: "{group}\Gamma CNF Reader"; Filename: "{app}\GammaCNFReader.exe"
Name: "{autodesktop}\Gamma CNF Reader"; Filename: "{app}\GammaCNFReader.exe"; Tasks: desktopicon
[Registry]
Root: HKCU; Subkey: "Software\Classes\GammaCNFReader.CSharp.cnf"; ValueType: string; ValueData: "Canberra gamma spectrum"; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\GammaCNFReader.CSharp.cnf\shell\open\command"; ValueType: string; ValueData: """{app}\GammaCNFReader.exe"" ""%1"""; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\.cnf\OpenWithProgids"; ValueType: string; ValueName: "GammaCNFReader.CSharp.cnf"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate
[Run]
Filename: "{app}\GammaCNFReader.exe"; Description: "Launch Gamma CNF Reader"; Flags: nowait postinstall skipifsilent
