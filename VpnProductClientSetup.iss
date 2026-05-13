[Setup]
AppName=VpnProduct Client
AppVersion=1.0.0
DefaultDirName={autopf}\VpnProduct Client
DefaultGroupName=VpnProduct Client
OutputDir=C:\Projects\VpnProductClean\src\installer-output
OutputBaseFilename=VpnProductSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "C:\Projects\VpnProductClean\src\publish\desktop-win-x64\VpnProduct.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{commondesktop}\VpnProduct Client"; Filename: "{app}\VpnProduct.Desktop.exe"
Name: "{group}\VpnProduct Client"; Filename: "{app}\VpnProduct.Desktop.exe"

[Run]
Filename: "{app}\VpnProduct.Desktop.exe"; Description: "Launch VpnProduct Client"; Flags: nowait postinstall skipifsilent