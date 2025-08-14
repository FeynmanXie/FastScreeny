[Setup]
; Application Information
AppId={{8B5CF6A0-EC48-4899-A123-456789ABCDEF}
AppName=FastScreeny
AppVersion=1.0.0
AppPublisher=FeynmanXie
AppPublisherURL=https://github.com/FeynmanXie/FastScreeny
AppSupportURL=https://github.com/FeynmanXie/FastScreeny/issues
AppUpdatesURL=https://github.com/FeynmanXie/FastScreeny/releases
DefaultDirName={autopf}\FastScreeny
DefaultGroupName=FastScreeny
AllowNoIcons=yes
LicenseFile=setup\LICENSE.txt
InfoAfterFile=setup\README_INSTALL.txt
OutputDir=dist\installer
OutputBaseFilename=FastScreeny_Setup_v1.0.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; System Requirements
MinVersion=10.0.17763
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Installation Options
PrivilegesRequired=admin
DisableProgramGroupPage=yes
SetupIconFile=setup\app.ico

; Uninstall
UninstallDisplayIcon={app}\FastScreeny.exe
UninstallDisplayName=FastScreeny Screenshot Tool

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Create &Quick Launch shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "startmenu"; Description: "Add to &Start Menu"; GroupDescription: "System Integration:"; Flags: checkablealone
Name: "autostart"; Description: "&Auto-start on boot"; GroupDescription: "System Integration:"; Flags: checkablealone
Name: "contextmenu"; Description: "Add &context menu screenshot"; GroupDescription: "System Integration:"; Flags: unchecked

[Files]
; Main program files
Source: "bin\Release\net8.0-windows\FastScreeny.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Documentation files
Source: "docs\README.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\update_info.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "setup\README_INSTALL.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "setup\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\FastScreeny"; Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Comment: "Fast Screenshot Tool"; Tasks: startmenu
Name: "{group}\FastScreeny Settings"; Filename: "{app}\FastScreeny.exe"; Comment: "Screenshot Tool Settings"; Tasks: startmenu
Name: "{group}\Uninstall FastScreeny"; Filename: "{uninstallexe}"; Tasks: startmenu

; Desktop shortcut
Name: "{autodesktop}\FastScreeny"; Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Comment: "Fast Screenshot Tool"; Tasks: desktopicon

; Quick Launch
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\FastScreeny"; Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Comment: "Fast Screenshot Tool"; Tasks: quicklaunchicon

[Run]
; Post-installation run options
Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Description: "Launch FastScreeny now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop program before uninstall
Filename: "taskkill"; Parameters: "/f /im FastScreeny.exe"; Flags: runhidden; RunOnceId: "StopFastScreeny"

[Registry]
; Auto-start on boot
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FastScreeny"; ValueData: "{app}\FastScreeny.exe --background"; Flags: uninsdeletevalue; Tasks: autostart

; Context menu
Root: HKCR; Subkey: "Directory\Background\shell\FastScreeny"; ValueType: string; ValueData: "FastScreeny Screenshot"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\Background\shell\FastScreeny"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastScreeny.exe,0"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\Background\shell\FastScreeny\command"; ValueType: string; ValueData: "{app}\FastScreeny.exe"; Flags: uninsdeletekey; Tasks: contextmenu

[Code]
// Check .NET 8 Desktop Runtime
function IsDotNet8Installed(): Boolean;
var
  Success: Boolean;
  ResultCode: Integer;
begin
  Success := Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := Success and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet8Installed() then
  begin
    if MsgBox('System does not have .NET 8 Desktop Runtime installed.' + #13#10 + 
              'This is a required component to run FastScreeny.' + #13#10#13#10 + 
              'Continue installation? Please manually download and install .NET 8 Desktop Runtime after installation.', 
              mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

// Post-installation processing
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create image save directory
    ForceDirectories(ExpandConstant('{userdocs}\FastScreeny'));
    
    // If auto-start is selected, set registry
    if IsTaskSelected('autostart') then
    begin
      RegWriteStringValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 
                         'FastScreeny', ExpandConstant('{app}\FastScreeny.exe --background'));
    end;
  end;
end;

// Pre-uninstall processing
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Try to stop the program
  Exec('taskkill', '/f /im FastScreeny.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  // Clean up auto-start
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'FastScreeny');
  
  Result := True;
end;
