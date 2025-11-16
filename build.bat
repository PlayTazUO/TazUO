@echo off
setlocal enabledelayedexpansion

echo ========================================
echo    Build and Run OpenUO
echo ========================================
echo.

echo [1/3] Checking for running game...
tasklist /FI "IMAGENAME eq OpenUO.exe" 2>NUL | find /I /N "OpenUO.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo Found running OpenUO.exe, closing...
    taskkill /F /IM OpenUO.exe >NUL 2>&1
    timeout /t 2 /nobreak >NUL
    echo Game process closed
) else (
    echo No running game process
)

echo.
echo [2/3] Building...
dotnet build --configuration Debug --no-restore --verbosity minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo    Build Success!
    echo ========================================
    echo.
    echo [3/3] Starting game...
    echo.
    
    cd bin\Debug\net9.0\win-x64

    if exist "Data\Profiles\" (
        rmdir /s /q "Data\Profiles\"
        echo Deleted Profiles directory
    )
    
    if exist "OpenUO.exe" (
        start "" "OpenUO.exe"
        echo Game started!
    ) else (
        echo [ERROR] OpenUO.exe not found
        pause
        exit /b 1
    )
    
    echo.
) else (
    echo.
    echo [ERROR] Build failed!
    echo.
    pause
    exit /b 1
)

timeout /t 3 /nobreak >NUL
