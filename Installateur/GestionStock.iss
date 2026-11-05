; Installateur optionnel via Inno Setup (https://jrsoftware.org/isinfo.php)
; 1) Executer INSTALLER_ET_PUBLIER_WIN64.bat dans le dossier racine.
; 2) Ouvrir ce fichier .iss dans Inno Setup puis Build > Compile.
; SQL Server Express n'est PAS inclus dans Setup_GestionStock.exe.

#define MyAppName "Gestion de Stock PRO"
#define MyAppVersion "2.0"
#define MyAppExeName "GestionStock.exe"

[Setup]
AppId={{5FD0A0A1-45FA-437F-BC53-6947797DA20D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\GestionStockPro
DefaultGroupName={#MyAppName}
OutputDir=Sortie
OutputBaseFilename=Setup_GestionStock_PRO_V2
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Files]
Source: "..\Publication\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer Gestion de Stock PRO"; Flags: nowait postinstall skipifsilent
