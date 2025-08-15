@echo off
setlocal
chcp 65001 >nul

rem FastScreeny installer build script
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo ================================
echo   FastScreeny Installer Builder
echo ================================
echo.

rem Check if Inno Setup is installed
set "INNO_PATH="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
) else if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=%ProgramFiles%\Inno Setup 6\ISCC.exe"
) else (
    echo [ERROR] Inno Setup 6 not found
    echo Please download and install from:
    echo https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo [CHECK] Found Inno Setup: %INNO_PATH%

rem Check .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK not found
    echo Please install .NET 8 SDK first
    pause
    exit /b 1
)

echo [CHECK] Found .NET SDK

rem Stop running application
tasklist /FI "IMAGENAME eq FastScreeny.exe" | find /I "FastScreeny.exe" >nul
if %errorlevel% equ 0 (
    echo [CLEAN] Stopping running FastScreeny...
    taskkill /IM FastScreeny.exe /F >nul 2>&1
    timeout /t 2 /nobreak >nul
)

rem Clean old build files
echo [CLEAN] Removing old build files...
if exist bin rd /s /q bin
if exist obj rd /s /q obj
if exist dist rd /s /q dist

rem Build application
echo [BUILD] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] NuGet package restore failed
    pause
    exit /b %errorlevel%
)

echo [BUILD] Compiling Release version...
dotnet build -c Release --no-restore
if %errorlevel% neq 0 (
    echo [ERROR] Compilation failed
    pause
    exit /b %errorlevel%
)

rem Verify build output
set "EXE_PATH=bin\Release\net8.0-windows\FastScreeny.exe"
if not exist "%EXE_PATH%" (
    echo [ERROR] Build output not found: %EXE_PATH%
    pause
    exit /b 1
)

echo [BUILD] Build successful: %EXE_PATH%

rem Create required directories
if not exist setup mkdir setup
if not exist dist mkdir dist
if not exist dist\installer mkdir dist\installer

rem Create app icon if missing
if not exist "setup\app.ico" (
    echo [RESOURCES] Creating app icon...
    powershell -ExecutionPolicy Bypass -File "setup\create_icon.ps1" -OutputPath "setup\app.ico"
    if not exist "setup\app.ico" (
        echo [WARNING] Icon creation failed, using default icon
        copy /y "%SystemRoot%\System32\imageres.dll" "setup\app.ico" >nul 2>&1
    )
)

rem Build installer
echo [INSTALLER] Building installer package...
"%INNO_PATH%" "setup\FastScreeny_Setup.iss"
if %errorlevel% neq 0 (
    echo [ERROR] Installer build failed
    pause
    exit /b %errorlevel%
)

rem Verify installer output
set "INSTALLER_PATH=dist\installer\FastScreeny_Setup_v1.0.0.exe"
if exist "%INSTALLER_PATH%" (
    echo.
    echo ================================
    echo   BUILD COMPLETE!
    echo ================================
    echo Installer location: %INSTALLER_PATH%
    echo File size: 
    for %%I in ("%INSTALLER_PATH%") do echo   %%~zI bytes (%%~nxI)
    echo.
    echo You can test the installer with:
    echo   "%INSTALLER_PATH%"
    echo.
) else (
    echo [ERROR] Installer output not found
    exit /b 1
)

rem Prompt to run installer
set /p "choice=Run installer for testing now? (y/N): "
if /i "%choice%"=="y" (
    echo [TEST] Launching installer...
    start "" "%INSTALLER_PATH%"
)

echo Build complete!
pause
exit /b 0
