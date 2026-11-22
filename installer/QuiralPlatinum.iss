#define MyAppName "Quiral Platinum"
#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#define MyAppExeName "Platinum.Presentation.Wpf.exe"

[Setup]
AppId={{B7E4A1C9-42A6-4E10-9D3A-7F1B4C8E6A20}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Quiral-and-Co
DefaultDirName={autopf}\Quiral Platinum
DefaultGroupName=Quiral Platinum
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=dist
OutputBaseFilename=Quiral-Platinum-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
RestartApplications=yes
SetupLogging=yes

[Files]
Source: "dist\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Quiral Platinum"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Quiral Platinum"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crear una icona a l'escriptori"; GroupDescription: "Accessos directes:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar Quiral Platinum"; Flags: postinstall nowait skipifsilent
