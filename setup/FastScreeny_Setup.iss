[Setup]
; 应用程序信息
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

; 系统要求
MinVersion=10.0.17763
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; 安装选项
PrivilegesRequired=admin
DisableProgramGroupPage=yes
SetupIconFile=setup\app.ico

; 卸载
UninstallDisplayIcon={app}\FastScreeny.exe
UninstallDisplayName=FastScreeny 截图工具

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式(&D)"; GroupDescription: "附加图标:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "创建快速启动栏图标(&Q)"; GroupDescription: "附加图标:"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "startmenu"; Description: "添加到开始菜单(&S)"; GroupDescription: "系统集成:"; Flags: checkablealone
Name: "autostart"; Description: "开机自动启动(&A)"; GroupDescription: "系统集成:"; Flags: checkablealone
Name: "contextmenu"; Description: "添加右键菜单快捷截图(&C)"; GroupDescription: "系统集成:"; Flags: unchecked

[Files]
; 主程序文件
Source: "bin\Release\net8.0-windows\FastScreeny.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\FastScreeny.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; 文档文件
Source: "docs\README.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "docs\update_info.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "setup\README_INSTALL.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "setup\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; 开始菜单
Name: "{group}\FastScreeny"; Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Comment: "快速截图工具"; Tasks: startmenu
Name: "{group}\FastScreeny 设置"; Filename: "{app}\FastScreeny.exe"; Comment: "截图工具设置"; Tasks: startmenu
Name: "{group}\卸载 FastScreeny"; Filename: "{uninstallexe}"; Tasks: startmenu

; 桌面快捷方式
Name: "{autodesktop}\FastScreeny"; Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Comment: "快速截图工具"; Tasks: desktopicon

; 快速启动
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\FastScreeny"; Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Comment: "快速截图工具"; Tasks: quicklaunchicon

[Run]
; 安装后运行选项
Filename: "{app}\FastScreeny.exe"; Parameters: "--background"; Description: "立即启动 FastScreeny"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; 卸载前停止程序
Filename: "taskkill"; Parameters: "/f /im FastScreeny.exe"; Flags: runhidden; RunOnceId: "StopFastScreeny"

[Registry]
; 开机自启动
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FastScreeny"; ValueData: "{app}\FastScreeny.exe --background"; Flags: uninsdeletevalue; Tasks: autostart

; 右键菜单
Root: HKCR; Subkey: "Directory\Background\shell\FastScreeny"; ValueType: string; ValueData: "FastScreeny 截图"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\Background\shell\FastScreeny"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FastScreeny.exe,0"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\Background\shell\FastScreeny\command"; ValueType: string; ValueData: "{app}\FastScreeny.exe"; Flags: uninsdeletekey; Tasks: contextmenu

[Code]
// 检查 .NET 8 Desktop Runtime
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
    if MsgBox('检测到系统未安装 .NET 8 Desktop Runtime。' + #13#10 + 
              '这是运行 FastScreeny 所必需的组件。' + #13#10#13#10 + 
              '是否继续安装？安装完成后请手动下载并安装 .NET 8 Desktop Runtime。', 
              mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

// 安装完成后的处理
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 创建图片保存目录
    ForceDirectories(ExpandConstant('{userdocs}\FastScreeny'));
    
    // 如果选择了开机自启，设置注册表
    if IsTaskSelected('autostart') then
    begin
      RegWriteStringValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 
                         'FastScreeny', ExpandConstant('{app}\FastScreeny.exe --background'));
    end;
  end;
end;

// 卸载前处理
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // 尝试停止程序
  Exec('taskkill', '/f /im FastScreeny.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  // 清理开机自启动
  RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'FastScreeny');
  
  Result := True;
end;
