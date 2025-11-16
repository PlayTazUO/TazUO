@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo    清理并编译 OpenUO
echo ========================================
echo.

echo [0/3] 检查并关闭运行中的游戏...
tasklist /FI "IMAGENAME eq OpenUO.exe" 2>NUL | find /I /N "OpenUO.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo 发现运行中的 OpenUO.exe，正在关闭...
    taskkill /F /IM OpenUO.exe >NUL 2>&1
    timeout /t 2 /nobreak >NUL
    echo 已关闭游戏进程
) else (
    echo 没有运行中的游戏进程
)

echo.
echo [1/2] 快速增量编译...
dotnet build --configuration Debug --no-restore --verbosity minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo    ✓ 编译成功！
    echo ========================================
    echo.
    echo [2/2] 启动游戏...
    echo.
    
    cd bin\Debug\net9.0\win-x64
    start OpenUO.exe
    
    echo 游戏已启动！
    echo.
) else (
    echo.
    echo [错误] 编译失败！
    echo.
    pause
    exit /b 1
)

timeout /t 3 /nobreak >NUL
