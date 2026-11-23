#define AppName "Quiral Platinum"
#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif
#ifndef AppDir
  #define AppDir "artifacts/Platinum-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "artifacts"
#endif
[Setup]
AppId={{B7E4A1C9-42A6-4E10-9D3A-7F1B4C8E6A20}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\Platinum
DefaultGroupName=Quiral Platinum
OutputDir={#OutputDir}
OutputBaseFilename=Quiral-Platinum-Setup-win-x64-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
[Files]
Source: "{#AppDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
[Icons]
Name: "{group}\Platinum"; Filename: "{app}\Platinum.Presentation.Avalonia.exe"
Name: "{autodesktop}\Platinum"; Filename: "{app}\Platinum.Presentation.Avalonia.exe"
[Run]
Filename: "{app}\Platinum.Presentation.Avalonia.exe"; Description: "Inicia Platinum"; Flags: postinstall nowait skipifsilent
