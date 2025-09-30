@echo off
setlocal EnableDelayedExpansion

echo ===============================================
echo     WorldMapWallpaper Build and Package
echo ===============================================
echo.

REM Get the current directory
set "PROJECT_DIR=%~dp0"
cd /d "%PROJECT_DIR%"

echo Building and publishing WorldMapWallpaper...
echo.

REM Clean previous build
echo [1/4] Cleaning previous build...
if exist "bin\publish-64\" (
    rmdir /s /q "bin\publish-64\"
    echo Previous build cleaned.
) else (
    echo No previous build found.
)
echo.

REM Publish the application
echo [2/4] Publishing application...
dotnet publish WorldMapWallpaper.csproj -p:PublishProfile=Publish_x64 --verbosity minimal
if !ERRORLEVEL! neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo Publish completed successfully.
echo.

REM Check if NSIS is available
echo [3/4] Checking for NSIS installer...
where makensis >nul 2>nul
if !ERRORLEVEL! neq 0 (
    echo WARNING: NSIS makensis not found in PATH.
    echo Please ensure NSIS is installed and makensis.exe is in your PATH.
    echo Common NSIS installation path: "C:\Program Files (x86)\NSIS\makensis.exe"
    echo.
    echo You can manually run: makensis InstallMaker.nsi
    pause
    exit /b 1
)
echo NSIS found.
echo.

REM Create installer
echo [4/4] Creating installer...
makensis InstallMaker.nsi
if !ERRORLEVEL! neq 0 (
    echo ERROR: Installer creation failed!
    pause
    exit /b 1
)
echo.

echo ===============================================
echo              BUILD COMPLETED
echo ===============================================
echo.
echo Published files: %PROJECT_DIR%bin\publish-64\
if exist "Install.exe" (
    echo Installer created: %PROJECT_DIR%Install.exe
    echo.
    echo The installer is ready for distribution!
) else (
    echo WARNING: Install.exe not found.
)
echo.
