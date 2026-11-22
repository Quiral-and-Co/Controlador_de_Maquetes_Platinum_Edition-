#define MyAppName "Quiral Platinum"
#ifndef MyAppVersion
  #define MyAppVersion "0.2.0"
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
UsePreviousAppDir=yes

[Files]
Source: "dist\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Quiral Platinum"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Quiral Platinum"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Crear una icona a l'escriptori"; GroupDescription: "Accessos directes:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar Quiral Platinum"; Flags: postinstall nowait runasoriginaluser

[Code]
function FindPreviousUninstaller(var UninstallerPath: String): Boolean;
var
  UninstallCommand: String;
  UninstallKey: String;
begin
  Result := False;
  UninstallerPath := '';

  UninstallKey := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  if RegQueryStringValue(HKLM64, UninstallKey, 'UninstallString', UninstallCommand) or
     RegQueryStringValue(HKLM32, UninstallKey, 'UninstallString', UninstallCommand) or
     RegQueryStringValue(HKCU64, UninstallKey, 'UninstallString', UninstallCommand) or
     RegQueryStringValue(HKCU32, UninstallKey, 'UninstallString', UninstallCommand) then
  begin
    UninstallerPath := RemoveQuotes(UninstallCommand);
    Result := UninstallerPath <> '';
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  UninstallerPath: String;
  ResultCode: Integer;
begin
  Result := '';
  if not FindPreviousUninstaller(UninstallerPath) then
    Exit;

  Log('Desinstal·lant la versió anterior de Quiral Platinum: ' + UninstallerPath);
  if not Exec(UninstallerPath,
       '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := 'No s''ha pogut iniciar la desinstal·lació de la versió anterior. ' +
      'Tanca Quiral Platinum i torna-ho a provar.';
    Exit;
  end;

  if ResultCode <> 0 then
    Result := 'La desinstal·lació de la versió anterior ha fallat (codi ' +
      IntToStr(ResultCode) + '). No s''instal·larà la nova versió.';
end;
