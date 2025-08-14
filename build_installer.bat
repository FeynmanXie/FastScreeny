@echo off
setlocal
chcp 65001 >nul

rem FastScreeny 安装程序构建脚本
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo ================================
echo   FastScreeny 安装程序构建工具
echo ================================
echo.

rem 检查 Inno Setup 是否安装
set "INNO_PATH="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
) else if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=%ProgramFiles%\Inno Setup 6\ISCC.exe"
) else (
    echo [错误] 未找到 Inno Setup 6
    echo 请从以下地址下载并安装 Inno Setup：
    echo https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo [检查] 找到 Inno Setup: %INNO_PATH%

rem 检查 .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 .NET SDK
    echo 请先安装 .NET 8 SDK
    pause
    exit /b 1
)

echo [检查] 找到 .NET SDK

rem 停止正在运行的程序
tasklist /FI "IMAGENAME eq FastScreeny.exe" | find /I "FastScreeny.exe" >nul
if %errorlevel% equ 0 (
    echo [清理] 停止正在运行的 FastScreeny...
    taskkill /IM FastScreeny.exe /F >nul 2>&1
    timeout /t 2 /nobreak >nul
)

rem 清理旧的构建文件
echo [清理] 删除旧的构建文件...
if exist bin rd /s /q bin
if exist obj rd /s /q obj
if exist dist rd /s /q dist

rem 构建应用程序
echo [构建] 还原 NuGet 包...
dotnet restore
if %errorlevel% neq 0 (
    echo [错误] NuGet 包还原失败
    pause
    exit /b %errorlevel%
)

echo [构建] 编译 Release 版本...
dotnet build -c Release --no-restore
if %errorlevel% neq 0 (
    echo [错误] 编译失败
    pause
    exit /b %errorlevel%
)

rem 检查构建结果
set "EXE_PATH=bin\Release\net8.0-windows\FastScreeny.exe"
if not exist "%EXE_PATH%" (
    echo [错误] 未找到编译输出文件: %EXE_PATH%
    pause
    exit /b 1
)

echo [构建] 编译成功: %EXE_PATH%

rem 创建必要的目录
if not exist setup mkdir setup
if not exist dist mkdir dist
if not exist dist\installer mkdir dist\installer

rem 创建应用图标（如果不存在）
if not exist "setup\app.ico" (
    echo [资源] 创建应用图标...
    powershell -ExecutionPolicy Bypass -File "setup\create_icon.ps1" -OutputPath "setup\app.ico"
    if not exist "setup\app.ico" (
        echo [警告] 图标创建失败，使用默认图标
        copy /y "%SystemRoot%\System32\imageres.dll" "setup\app.ico" >nul 2>&1
    )
)

rem 构建安装程序
echo [安装程序] 开始构建安装包...
"%INNO_PATH%" "setup\FastScreeny_Setup.iss"
if %errorlevel% neq 0 (
    echo [错误] 安装程序构建失败
    pause
    exit /b %errorlevel%
)

rem 检查安装程序输出
set "INSTALLER_PATH=dist\installer\FastScreeny_Setup_v1.0.0.exe"
if exist "%INSTALLER_PATH%" (
    echo.
    echo ================================
    echo   构建完成！
    echo ================================
    echo 安装程序位置: %INSTALLER_PATH%
    echo 文件大小: 
    for %%I in ("%INSTALLER_PATH%") do echo   %%~zI 字节 (%%~nxI)
    echo.
    echo 可以运行以下命令测试安装程序:
    echo   "%INSTALLER_PATH%"
    echo.
) else (
    echo [错误] 未找到安装程序输出文件
    exit /b 1
)

rem 询问是否立即运行安装程序
set /p "choice=是否立即运行安装程序进行测试？(y/N): "
if /i "%choice%"=="y" (
    echo [测试] 启动安装程序...
    start "" "%INSTALLER_PATH%"
)

echo 构建完成！
pause
exit /b 0
