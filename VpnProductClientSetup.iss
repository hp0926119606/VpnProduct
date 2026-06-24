[Setup]
AppId={{7A6E9D5C-8D55-4A9E-9A5D-000000000001}
AppName=VpnProduct Client
AppVersion=4.0.0
AppPublisher=VpnProduct
DefaultDirName={autopf}\VpnProduct Client
DefaultGroupName=VpnProduct Client
OutputDir=C:\Projects\VpnProductClean\src\installer-output
OutputBaseFilename=VpnProductSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
DisableDirPage=no
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=VpnProduct Client
UninstallDisplayIcon={app}\VpnProduct.Desktop.exe

[Files]
Source: "C:\Projects\VpnProductClean\src\installer-output\VpnProduct.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Projects\VpnProductClean\src\installer-assets\wireguard-amd64-1.1.msi"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Projects\VpnProductClean\src\installer-assets\wstunnel.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Projects\VpnProductClean\src\installer-assets\vc_redist.x64.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{commondesktop}\VpnProduct Client"; Filename: "{app}\VpnProduct.Desktop.exe"
Name: "{group}\VpnProduct Client"; Filename: "{app}\VpnProduct.Desktop.exe"

[Run]
Filename: "{app}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Microsoft Visual C++ Runtime..."; Flags: waituntilterminated runhidden

Filename: "taskkill.exe"; Parameters: "/IM VpnProduct.Desktop.exe /F"; Flags: runhidden waituntilterminated
Filename: "taskkill.exe"; Parameters: "/IM udp2raw.exe /F"; Flags: runhidden waituntilterminated
Filename: "taskkill.exe"; Parameters: "/IM wstunnel.exe /F"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "stop WireGuardTunnel$wg0"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete WireGuardTunnel$wg0"; Flags: runhidden waituntilterminated
Filename: "msiexec.exe"; Parameters: "/i ""{app}\wireguard-amd64-1.1.msi"" /quiet /norestart"; Flags: runhidden waituntilterminated
Filename: "{app}\VpnProduct.Desktop.exe"; Description: "Launch VpnProduct Client"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop WireGuardTunnel$wg0"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete WireGuardTunnel$wg0"; Flags: runhidden waituntilterminated
Filename: "taskkill.exe"; Parameters: "/IM udp2raw.exe /F"; Flags: runhidden waituntilterminated
Filename: "taskkill.exe"; Parameters: "/IM wstunnel.exe /F"; Flags: runhidden waituntilterminated
Filename: "taskkill.exe"; Parameters: "/IM VpnProduct.Desktop.exe /F"; Flags: runhidden waituntilterminated