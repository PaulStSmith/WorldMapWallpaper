; ================================================================================================
; World Map Wallpaper - NSIS Installer Script
; ================================================================================================
; This script creates an installer for the World Map Wallpaper application using NSIS 
; (Nullsoft Scriptable Install System). The installer handles:
; - Application file deployment from published builds
; - Windows Event Log source registration
; - Scheduled task creation for automatic wallpaper updates
; - Windows shell integration and startup registration
; - Proper uninstallation with cleanup
; ================================================================================================

!define ENABLE_LOGGING
!define MUI_ABORTWARNING
!define TEMP1 $R0 ; Temporary variable 1
!include "MUI.nsh"
!include "LogicLib.nsh"

; ================================================================================================
; Event Log Configuration
; ================================================================================================
; Define the application name and event log source for Windows Event Log integration
!define EVENT_LOG "Application"
!define EVENT_SOURCE "World Map Wallpaper Source"

; Define the Registry path for the event log source registration
!define REG_PATH "SYSTEM\CurrentControlSet\Services\EventLog\${EVENT_LOG}\${EVENT_SOURCE}"

; ================================================================================================
; Build Configuration - SIMPLIFIED FOR PUBLISHED BUILDS
; ================================================================================================
; Now uses the published build directory instead of individual bin folders
; Can be overridden via command line:
; makensis /DBUILD_CONFIG=Release InstallMaker.nsi
; ================================================================================================

; Set default values if not provided via command line
!ifndef BUILD_CONFIG
  !define BUILD_CONFIG          "Debug"
!endif

; Application identifiers and friendly names
!define APP_NAME                "WorldMapWallpaper"
!define FRIEND_NAME             "World Map Wallpaper"

; Use the published build directory - this contains all necessary files including dependencies
!define PUBLISH_BUILD_PATH      ".\bin\publish-64"

; Define application executable file names
!define MAIN_APP_EXE     "${APP_NAME}.exe"
!define SETTINGS_APP_EXE "${APP_NAME}.Settings.exe"

; Installer configuration
Name "${FRIEND_NAME}"
OutFile "Install.exe"

; ================================================================================================
; Modern UI Page Configuration
; ================================================================================================

; Installer page order - defines the sequence of pages shown during installation
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Reserve the license file for faster installer startup
ReserveFile "License.txt"

; Uninstaller page order - defines the sequence of pages shown during uninstallation
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Set the installer language
!insertmacro MUI_LANGUAGE "English"

; ================================================================================================
; Installer Functions
; ================================================================================================

; ------------------------------------------------------------------------------------------------
; Function: .onInit
; Description: Initializes the installer, sets up logging, and configures the default installation directory
; Called: Automatically when the installer starts
; ------------------------------------------------------------------------------------------------
Function .onInit
    LogSet on

    LogText "Initializing installation..."
    InitPluginsDir

    ; Set the default installation directory to Program Files
    StrCpy $INSTDIR "C:\Program Files\${APP_NAME}"
    LogText "Installation path: '$INSTDIR'"
    SetOutPath "$INSTDIR"
FunctionEnd

; ------------------------------------------------------------------------------------------------
; Section: Installer Section
; Description: Main installation section that handles file deployment, registry setup, 
;              task creation, and initial application launch
; ------------------------------------------------------------------------------------------------
Section "Installer Section" SecInstaller
    LogSet on

    ; Install all files from the published build directory
    ; This includes the main app, settings app, shared library, and all dependencies
    LogText "Installing application files from published build..."
    File /a /r "${PUBLISH_BUILD_PATH}\*.*"
    File "License.txt"

    ; Create the Windows Event Log source for application logging
    Call CreateEventSource

    ; Create the Windows scheduled task for automatic wallpaper updates
    Call CreateSchedulerTask
    
    ; Register wallpaper provider for Windows Personalization integration
    Call RegisterWallpaperProvider

    ; Create a log directory with proper permissions for application logging
    LogText "Creating log directory..."
    CreateDirectory "$INSTDIR\log"
    
    ; Set permissions on the log directory to allow user access
    LogText "Setting permissions on the log directory..."
    nsExec::ExecToLog 'icacls "$INSTDIR\log" /grant "Users":(OI)(CI)F'

    ; Create the uninstaller executable
    LogText "Creating uninstaller..."
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Run the main program to generate the initial wallpaper
    LogText "Running the program to generate initial wallpaper..."
    ExecWait '"$INSTDIR\${MAIN_APP_EXE}"'
    LogText "Program executed."
    
    ; Launch the settings interface for user configuration
    LogText "Launching settings interface..."
    Exec '"$INSTDIR\${SETTINGS_APP_EXE}"'
    LogText "Settings interface launched."

    LogText "Installation complete."
SectionEnd

; ------------------------------------------------------------------------------------------------
; Function: CreateEventSource
; Description: Creates a Windows Event Log source for the application to enable proper logging
;              to the Windows Event Log system
; Registry Keys: Creates entries under HKLM\SYSTEM\CurrentControlSet\Services\EventLog\Application
; ------------------------------------------------------------------------------------------------
Function CreateEventSource
    LogText "Creating event source..."

    ; Create the registry key for the event source
    ; EventMessageFile points to the main executable for event message resolution
    WriteRegStr HKLM "${REG_PATH}" "EventMessageFile" "$INSTDIR\${MAIN_APP_EXE}"
    ; TypesSupported = 7 allows Information (1), Warning (2), and Error (4) events
    WriteRegDWORD HKLM "${REG_PATH}" "TypesSupported" 7

    LogText "Event source created."
FunctionEnd

; ------------------------------------------------------------------------------------------------
; Function: CreateSchedulerTask
; Description: Creates a Windows scheduled task that automatically runs the wallpaper application
;              on multiple triggers: boot, logon, system wake, and hourly intervals
; Task Features: 
;   - Runs with highest available privileges
;   - Multiple instance policy set to ignore new instances
;   - Designed to work on battery power
;   - 72-hour execution time limit
; ------------------------------------------------------------------------------------------------
Function CreateSchedulerTask
    
    LogText "Creating XML file for the scheduler task..."

    ; Create XML file for the scheduler task configuration
    StrCpy $0 "$INSTDIR\${APP_NAME}Task.xml"
    FileOpen $1 $0 w

    ; Get the current user name for the task principal
	ClearErrors
	UserInfo::GetName
	Pop $0

    ; Write the complete task XML definition
    ; This XML defines a comprehensive scheduled task with multiple triggers
    FileWrite $1 '<?xml version="1.0" encoding="UTF-16"?>$\r$\n'
    FileWrite $1 '<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">$\r$\n'
    FileWrite $1 '  <RegistrationInfo>$\r$\n'
    FileWrite $1 '    <Date>2020-01-01T00:00:00</Date>$\r$\n'
    FileWrite $1 '    <Author>World Map Wallpaper</Author>$\r$\n'
    FileWrite $1 '  </RegistrationInfo>$\r$\n'
    FileWrite $1 '  <Triggers>$\r$\n'
    ; Boot trigger - runs when system starts
    FileWrite $1 '    <BootTrigger>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    </BootTrigger>$\r$\n'
    ; Logon trigger - runs when user logs in
    FileWrite $1 '    <LogonTrigger>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    </LogonTrigger>$\r$\n'
    ; Event trigger - runs when system wakes from sleep (Power Troubleshooter event)
    FileWrite $1 '    <EventTrigger>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '      <Subscription>&lt;QueryList&gt;&lt;Query Id="0" Path="System"&gt;&lt;Select Path="System"&gt;*[System[Provider[@Name=$\'Microsoft-Windows-Power-Troubleshooter$\'] and EventID=1]]&lt;/Select&gt;&lt;/Query&gt;&lt;/QueryList&gt;</Subscription>$\r$\n'
    FileWrite $1 '    </EventTrigger>$\r$\n'
    ; Time trigger - runs every hour to keep wallpaper updated
    FileWrite $1 '    <TimeTrigger>$\r$\n'
    FileWrite $1 '      <Repetition>$\r$\n'
    FileWrite $1 '        <Interval>PT1H</Interval>$\r$\n'
    FileWrite $1 '        <StopAtDurationEnd>false</StopAtDurationEnd>$\r$\n'
    FileWrite $1 '      </Repetition>$\r$\n'
    FileWrite $1 '      <StartBoundary>2020-01-01T00:00:00</StartBoundary>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    </TimeTrigger>$\r$\n'
    FileWrite $1 '  </Triggers>$\r$\n'
    ; Principal configuration - runs under current user with highest available privileges
    FileWrite $1 '  <Principals>$\r$\n'
    FileWrite $1 '    <Principal id="Author">$\r$\n'
    FileWrite $1 '      <UserId>$0</UserId>$\r$\n'
    FileWrite $1 '      <LogonType>InteractiveToken</LogonType>$\r$\n'
    FileWrite $1 '      <RunLevel>HighestAvailable</RunLevel>$\r$\n'
    FileWrite $1 '    </Principal>$\r$\n'
    FileWrite $1 '  </Principals>$\r$\n'
    ; Task settings - optimized for background operation
    FileWrite $1 '  <Settings>$\r$\n'
    FileWrite $1 '    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>$\r$\n'
    FileWrite $1 '    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>$\r$\n'
    FileWrite $1 '    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>$\r$\n'
    FileWrite $1 '    <AllowHardTerminate>true</AllowHardTerminate>$\r$\n'
    FileWrite $1 '    <StartWhenAvailable>true</StartWhenAvailable>$\r$\n'
    FileWrite $1 '    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>$\r$\n'
    FileWrite $1 '    <IdleSettings>$\r$\n'
    FileWrite $1 '      <StopOnIdleEnd>false</StopOnIdleEnd>$\r$\n'
    FileWrite $1 '      <RestartOnIdle>false</RestartOnIdle>$\r$\n'
    FileWrite $1 '    </IdleSettings>$\r$\n'
    FileWrite $1 '    <AllowStartOnDemand>true</AllowStartOnDemand>$\r$\n'
    FileWrite $1 '    <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    <Hidden>false</Hidden>$\r$\n'
    FileWrite $1 '    <RunOnlyIfIdle>false</RunOnlyIfIdle>$\r$\n'
    FileWrite $1 '    <WakeToRun>false</WakeToRun>$\r$\n'
    FileWrite $1 '    <ExecutionTimeLimit>PT72H</ExecutionTimeLimit>$\r$\n'
    FileWrite $1 '    <Priority>7</Priority>$\r$\n'
    FileWrite $1 '  </Settings>$\r$\n'
    ; Action configuration - specifies the executable to run
    FileWrite $1 '  <Actions Context="Author">$\r$\n'
    FileWrite $1 '    <Exec>$\r$\n'
    FileWrite $1 '      <Command>"$INSTDIR\${MAIN_APP_EXE}"</Command>$\r$\n'
    FileWrite $1 '    </Exec>$\r$\n'
    FileWrite $1 '  </Actions>$\r$\n'
    FileWrite $1 '</Task>$\r$\n'
    FileClose $1
    LogText "XML file created: $0"

    ; Create the scheduled task using the XML file
    LogText "Creating scheduler task to run the program..."
    StrCpy $0 '"schtasks" /create /tn "${FRIEND_NAME}" /xml "$INSTDIR\${APP_NAME}Task.xml" /f'
    LogText "Command: $0"
    nsExec::Exec "$0"
    pop $0
    LogText "Exit Code: $0"

FunctionEnd

; ------------------------------------------------------------------------------------------------
; Function: RegisterWallpaperProvider
; Description: Registers the application with Windows shell integration and startup services
; Registry Operations:
;   - Registers application paths for shell integration
;   - Sets up automatic startup of settings app (minimized to tray)
; Purpose: Enables seamless Windows integration and automatic startup
; ------------------------------------------------------------------------------------------------
Function RegisterWallpaperProvider
    LogText "Registering World Map Wallpaper integration..."
    
    ; Register application paths for Windows shell integration
    ; This allows Windows to find the executables when referenced by name
    LogText "Registering application paths..."
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "" "$INSTDIR\${MAIN_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "Path" "$INSTDIR"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "" "$INSTDIR\${SETTINGS_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "Path" "$INSTDIR"
    
    ; Register Settings app to start with Windows (minimized to tray)
    ; This ensures the tray icon and settings are always available to the user
    LogText "Registering Settings app for startup..."
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WorldMapWallpaperSettings" '"$INSTDIR\${SETTINGS_APP_EXE}" --minimized'
    
    LogText "World Map Wallpaper integration complete."
FunctionEnd

; ================================================================================================
; Uninstaller Functions
; ================================================================================================

; ------------------------------------------------------------------------------------------------
; Section: Uninstall
; Description: Complete uninstallation section that removes all application components
; Cleanup Operations:
;   - Removes all installed files and directories
;   - Deletes scheduled tasks
;   - Removes Windows Event Log source
;   - Cleans up all registry entries
; ------------------------------------------------------------------------------------------------
Section "Uninstall" SecUninstaller
    LogSet on

    LogText "Uninstalling ${APP_NAME}..."
    LogText "Installation directory: $INSTDIR"

    ; Remove the entire installation directory and all its contents
    LogText "Removing installation directory..."
    RMDir /r "$INSTDIR"

    ; Remove the scheduled task that was created during installation
    LogText "Removing scheduler task to run the program every hour on the hour..."
    StrCpy $0 '"schtasks" /delete /tn "${FRIEND_NAME}" /f'
    LogText "Command: $0"
    nsExec::Exec "$0"
    pop $0
    LogText "Exit Code: $0"

    ; Remove the Windows Event Log source registration
    LogText "Removing event source..."
    DeleteRegKey HKLM "${REG_PATH}"
    
    ; Remove all registry entries created during installation
    LogText "Removing registry entries..."
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}"
    DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WorldMapWallpaperSettings"

SectionEnd

; ================================================================================================
; Installation Event Handlers
; ================================================================================================

; ------------------------------------------------------------------------------------------------
; Function: .onInstSuccess
; Description: Called automatically when installation completes successfully
; Purpose: Logs successful installation for troubleshooting and audit purposes
; ------------------------------------------------------------------------------------------------
Function .onInstSuccess
    LogSet on
    LogText "Installation successful."
FunctionEnd

; ------------------------------------------------------------------------------------------------
; Function: .onInstFailed  
; Description: Called automatically when installation fails
; Purpose: Logs installation failure for troubleshooting purposes
; ------------------------------------------------------------------------------------------------
Function .onInstFailed
    LogSet on
    LogText "Installation failed."
FunctionEnd
