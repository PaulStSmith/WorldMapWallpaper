@echo off
setlocal EnableDelayedExpansion

echo ===============================================
echo     WorldMapWallpaper Build and Package
echo ===============================================
echo.

REM Check for Release parameter
set "cfg=%~1"
if "%cfg%"=="" set cfg=Debug
if /i "%cfg%"=="Release" (
    echo RELEASE MODE: Will create GitHub release after build
    echo.
    
    REM Check if git is available
    where git >nul 2>nul
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Git not found in PATH. Git is required for release mode.
        exit /b 1
    )
    
    REM Check for clean working tree
    echo Checking git working tree status...
    git status --porcelain > nul 2>&1
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Not in a git repository or git command failed.
        exit /b 1
    )
    
    for /f %%i in ('git status --porcelain') do (
        echo ERROR: Working tree is not clean. Please commit or stash changes before release.
        echo.
        echo Uncommitted changes detected:
        git status --short
        echo.
        echo Please run 'git status' to see all changes and commit them before creating a release.
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
echo [1/5] Cleaning previous build...
if exist "bin\publish-64\" (
    rmdir /s /q "bin\publish-64\"
    echo Previous build cleaned.
) else (
    echo No previous build found.
)
echo.

echo [2/5] Building solutions...
call :BuildSolutions
if !ERRORLEVEL! neq 0 (
    echo ERROR: Build failed!
    exit /b 1
)

REM Publish the application
echo [3/5] Publishing application...
call :PublishApplication
if !ERRORLEVEL! neq 0 (
    echo ERROR: Publish failed!
    exit /b 1
)
echo Publish completed successfully.
echo.

REM Check if NSIS is available
echo [4/5] Checking for NSIS installer...
where makensis >nul 2>nul
if !ERRORLEVEL! neq 0 (
    echo WARNING: NSIS makensis not found in PATH.
    echo Please ensure NSIS is installed and makensis.exe is in your PATH.
    echo Common NSIS installation path: "C:\Program Files (x86)\NSIS\makensis.exe"
    echo.
    echo You can manually run: makensis InstallMaker.nsi
    exit /b 1
)
echo NSIS found.
echo.

REM Create installer
echo [5/5] Creating installer...
makensis InstallMaker.nsi
if !ERRORLEVEL! neq 0 (
    echo ERROR: Installer creation failed!
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
    echo. Configuration: %cfg%
    if /i "%cfg%"=="Release" (
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
            
            REM Get version from AssemblyInfo.cs
            set "VERSION="
            for /f "tokens=2 delims=(" %%a in ('findstr "AssemblyVersion" Properties\AssemblyInfo.cs') do (
                for /f "tokens=1 delims=)" %%b in ("%%a") do (
                    set VERSION=%%b
                    set VERSION=!VERSION:"=!
                )
            )
            
            if "!VERSION!"=="" (
                echo ERROR: Could not extract version from AssemblyInfo.cs
                exit /b 1
            )
            
            echo Creating release v!VERSION! with installer...
            
            REM Check if tag already exists and find available suffix
            set "FINAL_VERSION=!VERSION!"
            set "SUFFIX_LETTERS=abcdefghijklmnopqrstuvwxyz"
            
            REM Check if base version exists
            gh release view "v!VERSION!" >nul 2>&1
            if !ERRORLEVEL! equ 0 (
                echo Tag v!VERSION! already exists, finding available suffix...
                
                REM Try sequential letters
                for /l %%i in (0,1,25) do (
                    set /a "CHAR_INDEX=%%i"
                    call set "LETTER=%%SUFFIX_LETTERS:~!CHAR_INDEX!,1%%"
                    set "TEST_VERSION=!VERSION!!LETTER!"
                    
                    gh release view "v!TEST_VERSION!" >nul 2>&1
                    if !ERRORLEVEL! neq 0 (
                        set "FINAL_VERSION=!TEST_VERSION!"
                        echo Using tag: v!FINAL_VERSION!
                        goto :create_release
                    )
                )
                
                echo ERROR: All suffixes a-z are taken for version !VERSION!
                exit /b 1
            )
            
            :create_release
            gh release create "v!FINAL_VERSION!" Install.exe --title "WorldMapWallpaper v!FINAL_VERSION!" --notes "WorldMapWallpaper Release v!FINAL_VERSION! with real-time day/night cycle visualization and ISS tracking. Download and run Install.exe to install. Requires Windows 10 version 1809 or later."
            
            if !ERRORLEVEL! equ 0 (
                echo GitHub release created successfully!
                echo View at: https://github.com/PaulStSmith/DesktopImageChanger/releases/tag/v!FINAL_VERSION!
            ) else (
                echo ERROR: Failed to create GitHub release.
                echo Make sure you're authenticated with: gh auth login
            )
        )
        echo.
    ) else (
        echo Debug build completed. No release created.
    )
    echo The installer is ready for distribution!
) else (
    echo WARNING: Install.exe not found.
    if /i "%cfg%"=="Release" (
        echo Cannot create release without installer!
        exit /b 1
    )
)
echo.
exit /b 0

:BuildSolutions
echo "  ___      _ _    _ _                  "
echo " | _ )_  _(_) |__| (_)_ _  __ _        "
echo " | _ \ || | | / _` | | ' \/ _` |_ _ _  "
echo " |___/\_,_|_|_\__,_|_|_||_\__, (_)_)_) "
echo "                          |___/        "
echo.
echo Configuration: !cfg!
echo.

:: Find all solution files in the current directory
set SOLUTION_COUNT=0
set SOLUTIONS=
for %%f in ("%~dp0*.sln") do (
    set /a SOLUTION_COUNT+=1
    set SOLUTIONS=!SOLUTIONS! "%%f"
    echo Found solution: %%~nxf
)

if !SOLUTION_COUNT! equ 0 (
    echo No solution files found in the current directory.
    echo Please ensure there are .sln files in the same folder as this script.
    exit /b 1
)

echo.
echo Found !SOLUTION_COUNT! solution(s) to build.
echo.

echo Cleaning `obj` and `bin` folders...
for /r /d %%i in (obj) do if exist "%%i"       rmdir /s /q "%%i"       2> nul
for /r /d %%i in (bin) do if exist "%%i\!cfg!" rmdir /s /q "%%i\!cfg!" 2> nul

:: Create Logs directory if it doesn't exist
if not exist "%~dp0Logs\Build" mkdir "%~dp0Logs\Build"

echo Building solutions...
set BUILD_FAILED=0
for %%s in (!SOLUTIONS!) do (
    echo.
    echo ========================================
    echo Building: %%~nxs
    echo ========================================
    call :CompileSolution %%s > "%~dp0Logs\Build\%%~ns.log" 2>&1
    if !ERRORLEVEL! neq 0 (
        echo Build failed for: %%~nxs
        set BUILD_FAILED=1
    ) else (
        echo Build succeeded for: %%~nxs
    )
)

if !BUILD_FAILED! equ 1 (
    echo.
    echo "  ___      _ _    _     ___     _ _        _ _  "
    echo " | _ )_  _(_) |__| |   | __|_ _(_) |___ __| | | "
    echo " | _ \ || | | / _` |   | _/ _` | | / -_) _` |_| "
    echo " |___/\_,_|_|_\__,_|   |_|\__,_|_|_\___\__,_(_) "
    echo.
    echo One or more builds failed. Check the log files in "%~dp0Logs\" for details.
    exit /b 1
)

echo.
echo All solutions built successfully!
exit /b 0

:CompileSolution
set PROJECT_FILE=%1
if "%PROJECT_FILE%"=="" (
    echo No project file specified.
    echo Usage: %0 ProjectFile.sln
    exit /b 1
)

echo Searching for MSBuild...

:: Try to find Visual Studio 2022 installation
set VS2022_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set VS2022_PATH=C:\Program Files\Microsoft Visual Studio\2022\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find Visual Studio 2019 installation
set VS2019_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set VS2019_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find Visual Studio 2017 installation
set VS2017_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\%%e\MSBuild\15.0\Bin\MSBuild.exe" (
        set VS2017_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\%%e\MSBuild\15.0\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find older MSBuild from .NET Framework
set NETFX_MSBUILD=
for %%v in (4.0.30319 14.0 12.0) do (
    if exist "C:\Windows\Microsoft.NET\Framework\v%%v\MSBuild.exe" (
        set NETFX_MSBUILD=C:\Windows\Microsoft.NET\Framework\v%%v\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find MSBuild in the path
where msbuild >nul 2>&1
if %ERRORLEVEL% equ 0 (
    set MSBUILD_PATH=msbuild
    goto :found_msbuild
)

echo ERROR: MSBuild not found. Please install Visual Studio or .NET Framework SDK.
exit /b 1

:found_msbuild
if defined VS2022_PATH (
    echo Found MSBuild from Visual Studio 2022
    set MSBUILD_PATH=!VS2022_PATH!
) else if defined VS2019_PATH (
    echo Found MSBuild from Visual Studio 2019
    set MSBUILD_PATH=!VS2019_PATH!
) else if defined VS2017_PATH (
    echo Found MSBuild from Visual Studio 2017
    set MSBUILD_PATH=!VS2017_PATH!
) else if defined NETFX_MSBUILD (
    echo Found MSBuild from .NET Framework
    set MSBUILD_PATH=!NETFX_MSBUILD!
) else (
    echo Found MSBuild in PATH
)

echo Using MSBuild: !MSBUILD_PATH!

echo Running NuGet restore...
"!MSBUILD_PATH!" "%PROJECT_FILE%" /t:Restore /p:Configuration=!cfg! /p:Platform="Any CPU" /p:RuntimeIdentifiers=win-x64
if %ERRORLEVEL% neq 0 (
    echo Build failed for AnyCPU.
    exit /b %ERRORLEVEL%
)

echo.
echo Building AnyCPU version...
"!MSBUILD_PATH!" "%PROJECT_FILE%" /p:Configuration=!cfg! /p:Platform="Any CPU" /p:RuntimeIdentifiers=win-x64 /v:m
if %ERRORLEVEL% neq 0 (
    echo Build failed for AnyCPU.
    exit /b %ERRORLEVEL%
)

echo.
echo Build complete successfully.
exit /b 0

:PublishApplication
echo Publishing with MSBuild...

REM Find MSBuild (reuse logic from CompileSolution)
set MSBUILD_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :publish_found_msbuild
    )
)

for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :publish_found_msbuild
    )
)

where msbuild >nul 2>&1
if %ERRORLEVEL% equ 0 (
    set MSBUILD_PATH=msbuild
    goto :publish_found_msbuild
)

echo ERROR: MSBuild not found for publishing.
exit /b 1

:publish_found_msbuild
echo Using MSBuild for publish: !MSBUILD_PATH!

REM Use MSBuild to publish with the profile
"!MSBUILD_PATH!" WorldMapWallpaper.csproj /p:PublishProfile=Publish_x64 /p:Configuration=!cfg! /t:Publish /v:m
exit /b %ERRORLEVEL%