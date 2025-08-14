@echo off
setlocal
chcp 65001 >nul

rem Fast build script for FastScreeny
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
  echo [.NET SDK] 未安装或未加入 PATH，请先安装 .NET 8 SDK。
  exit /b 1
)

rem 如果程序正在运行，先结束以避免文件被占用
tasklist /FI "IMAGENAME eq FastScreeny.exe" | find /I "FastScreeny.exe" >nul
if %errorlevel% equ 0 (
  echo 检测到 FastScreeny 正在运行，尝试结束进程...
  taskkill /IM FastScreeny.exe /F >nul 2>&1
  rem 等待进程完全退出
  :wait_exit
  tasklist /FI "IMAGENAME eq FastScreeny.exe" | find /I "FastScreeny.exe" >nul
  if %errorlevel% equ 0 (
    timeout /t 1 /nobreak >nul
    goto :wait_exit
  )
)

echo [1/2] 正在还原依赖...
dotnet restore
if %errorlevel% neq 0 goto :error

echo [2/2] 正在编译 Release...
dotnet build -c Release --no-restore
if %errorlevel% neq 0 goto :error

set "OUT=bin\Release\net8.0-windows\FastScreeny.exe"
echo 构建成功: "%CD%\%OUT%"

if "%1"=="run" (
  echo 正在运行...
  "%OUT%" --background
)

exit /b 0

:error
echo 构建失败，错误码 %errorlevel% 。
exit /b %errorlevel%


