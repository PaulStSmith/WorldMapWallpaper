@echo off
setlocal EnableDelayedExpansion

echo ===============================================
echo     WorldMapWallpaper Build and Package
echo ===============================================
echo.

REM Check for Release parameter
set "RELEASE_MODE=%~1"
if /i "%RELEASE_MODE%"=="Release" (
    echo RELEASE MODE: Will create GitHub release after build
    echo.
    
    REM Check if git is available
    where git >nul 2>nul
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Git not found in PATH. Git is required for release mode.
        pause
        exit /b 1
    )
    
    REM Check for clean working tree
    echo Checking git working tree status...
    git status --porcelain > nul 2>&1
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Not in a git repository or git command failed.
        pause
        exit /b 1
    )
    
    for /f %%i in ('git status --porcelain') do (
        echo ERROR: Working tree is not clean. Please commit or stash changes before release.
        echo.
        echo Uncommitted changes detected:
        git status --short
        echo.
        echo Please run 'git status' to see all changes and commit them before creating a release.
        pause
        exit /b 1
    )
    
    echo Git working tree is clean. Proceeding with release build...
    echo.
)

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
    if /i "%RELEASE_MODE%"=="Release" (
        echo [RELEASE] Triggering GitHub release...
        echo.
        
        REM Check if gh CLI is available
        where gh >nul 2>nul
        if !ERRORLEVEL! neq 0 (
            echo WARNING: GitHub CLI not found. Please install GitHub CLI to create releases.
            echo You can download it from: https://cli.github.com/
            echo.
            echo Alternatively, trigger the release manually with:
            echo gh api repos/:owner/:repo/dispatches -f event_type=create-release
        ) else (
            echo Creating GitHub release...
            gh api repos/:owner/:repo/dispatches -f event_type=create-release
            if !ERRORLEVEL! equ 0 (
                echo GitHub release workflow triggered successfully!
                echo Check https://github.com/%USERNAME%/DesktopImageChanger/actions for progress.
            ) else (
                echo ERROR: Failed to trigger GitHub release workflow.
                echo Make sure you're authenticated with: gh auth login
            )
        )
        echo.
    )
    echo The installer is ready for distribution!
) else (
    echo WARNING: Install.exe not found.
    if /i "%RELEASE_MODE%"=="Release" (
        echo Cannot create release without installer!
        exit /b 1
    )
)
echo.
