@echo off
setlocal
chcp 65001 >nul

rem Fast build script for FastScreeny
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
  echo [.NET SDK] Not installed or not in PATH, please install .NET 8 SDK first.
  exit /b 1
)

rem If app is running, kill it first to avoid file locks
tasklist /FI "IMAGENAME eq FastScreeny.exe" | find /I "FastScreeny.exe" >nul
if %errorlevel% equ 0 (
  echo FastScreeny is running, attempting to kill process...
  taskkill /IM FastScreeny.exe /F >nul 2>&1
  rem Wait for process to fully exit
  :wait_exit
  tasklist /FI "IMAGENAME eq FastScreeny.exe" | find /I "FastScreeny.exe" >nul
  if %errorlevel% equ 0 (
    timeout /t 1 /nobreak >nul
    goto :wait_exit
  )
)

echo [1/2] Restoring dependencies...
dotnet restore
if %errorlevel% neq 0 goto :error

echo [2/2] Building Release...
dotnet build -c Release --no-restore
if %errorlevel% neq 0 goto :error

set "OUT=bin\Release\net8.0-windows\FastScreeny.exe"
echo Build successful: "%CD%\%OUT%"

if "%1"=="run" (
  echo Running...
  "%OUT%" --background
)

exit /b 0

:error
echo Build failed with error code %errorlevel%.
exit /b %errorlevel%
