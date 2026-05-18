[Setup]
AppName=VpnProduct Client
AppVersion=1.0.2
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
Source: "C:\Projects\VpnProductClean\src\installer-assets\wireguard-amd64-1.1.msi"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{commondesktop}\VpnProduct Client"; Filename: "{app}\VpnProduct.Desktop.exe"
Name: "{group}\VpnProduct Client"; Filename: "{app}\VpnProduct.Desktop.exe"

[Run]
Filename: "msiexec.exe"; Parameters: "/i ""{tmp}\wireguard-amd64-1.1.msi"" /quiet /norestart"; StatusMsg: "Installing VPN components..."; Flags: waituntilterminated runhidden
Filename: "taskkill.exe"; Parameters: "/IM wireguard.exe /F"; Flags: runhidden waituntilterminated
Filename: "{app}\VpnProduct.Desktop.exe"; Description: "Launch VpnProduct Client"; Flags: nowait postinstall skipifsilent