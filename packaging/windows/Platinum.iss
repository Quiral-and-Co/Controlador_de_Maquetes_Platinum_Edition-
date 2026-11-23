#define AppName "Platinum"
#define AppVersion "0.1.0"
#ifndef AppDir
  #define AppDir "artifacts/Platinum-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "artifacts"
#endif
[Setup]
AppId={{8D56B5A7-85AF-4E4B-9B5A-8D56B5A785AF}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\Platinum
DefaultGroupName=Platinum
OutputDir={#OutputDir}
OutputBaseFilename=Platinum-Setup-win-x64
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
