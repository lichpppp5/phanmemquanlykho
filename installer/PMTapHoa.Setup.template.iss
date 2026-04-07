[Setup]
AppName=PM Tap Hoa
AppVersion=__APP_VERSION__
AppPublisher=PM Tap Hoa
DefaultDirName={autopf}\PMTapHoa
DefaultGroupName=PM Tap Hoa
OutputDir=..\installer-output
OutputBaseFilename=PMTapHoa_Setup___APP_VERSION__
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\__APP_EXE__

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "__PUBLISH_DIR__\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\PM Tap Hoa"; Filename: "{app}\__APP_EXE__"
Name: "{autodesktop}\PM Tap Hoa"; Filename: "{app}\__APP_EXE__"

[Run]
Filename: "{app}\__APP_EXE__"; Description: "Launch PM Tap Hoa"; Flags: nowait postinstall skipifsilent
