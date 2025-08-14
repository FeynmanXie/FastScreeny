@echo off
setlocal
chcp 65001 >nul

echo FastScreeny 快速构建
echo ==================

rem 构建应用程序
echo [1/3] 构建应用程序...
call build_release.bat
if %errorlevel% neq 0 (
    echo 构建失败！
    pause
    exit /b %errorlevel%
)

rem 创建基本图标（如果需要）
if not exist "setup\app.ico" (
    echo [2/3] 创建图标...
    if not exist setup mkdir setup
    powershell -ExecutionPolicy Bypass -File "setup\create_icon.ps1" -OutputPath "setup\app.ico" 2>nul
)

rem 检查是否有 Inno Setup，如果有则构建安装程序
where ISCC.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo [3/3] 构建安装程序...
    ISCC.exe "setup\FastScreeny_Setup.iss"
    if %errorlevel% equ 0 (
        echo.
        echo ✅ 安装程序构建完成！
        echo 位置: dist\installer\FastScreeny_Setup_v1.0.0.exe
        echo.
    ) else (
        echo ❌ 安装程序构建失败
    )
) else (
    echo [3/3] 跳过安装程序构建（未找到 Inno Setup）
    echo 💡 如需构建安装程序，请运行: build_installer.bat
)

echo.
echo 应用程序已构建完成: bin\Release\net8.0-windows\FastScreeny.exe
echo 可以直接运行测试，或使用 build_installer.bat 构建完整安装包。
pause
