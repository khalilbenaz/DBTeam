; DB TEAM — Inno Setup installer script
; Produces a single-file Windows installer (.exe) that behaves like an MSI
;   - Per-user install (no admin required)
;   - Start menu entry
;   - Registered in Apps & Features (clean uninstall)
;   - Silent install: DBTeam-Setup.exe /SILENT or /VERYSILENT

#define AppName "DB TEAM"
#ifndef AppVersion
  #define AppVersion "1.2.0"
#endif
#define AppPublisher "Khalil Benazzouz"
#define AppURL "https://github.com/khalilbenaz/DBTeam"
#define AppExeName "DBTeam.App.exe"
#ifndef SourceDir
  #define SourceDir "..\dist\DBTeam-" + AppVersion + "-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
AppId={{B3D7CA10-5B7B-4D72-9A3E-DBTEAM00001}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\DBTeam
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile={#SourceDir}\LICENSE.txt
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}

Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
DisableReadyPage=no
DisableDirPage=no

OutputDir={#OutputDir}
OutputBaseFilename=DBTeam-Setup-{#AppVersion}
SetupIconFile=..\src\DBTeam.App\Resources\app-icon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french";  MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Create a &Quick Launch shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}";       Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
