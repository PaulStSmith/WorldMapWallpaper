!define ENABLE_LOGGING
!define MUI_ABORTWARNING
!define TEMP1 $R0 ; Temporary variable 1
!include "MUI.nsh"
!include "LogicLib.nsh"

;
; Define the application name and event log source
;
!define EVENT_LOG "Application"
!define EVENT_SOURCE "World Map Wallpaper Source"

;
; Define the Registry path for the event log source
;
!define REG_PATH "SYSTEM\CurrentControlSet\Services\EventLog\${EVENT_LOG}\${EVENT_SOURCE}"

;
; ========================================
; Build Configuration - SIMPLIFIED FOR PUBLISHED BUILDS
; ========================================
; Now uses the published build directory instead of individual bin folders
; Can be overridden via command line:
; makensis /DBUILD_CONFIG=Release InstallMaker.nsi
; ========================================
;
; Set default values if not provided via command line
!ifndef BUILD_CONFIG
  !define BUILD_CONFIG          "Debug"
!endif

!define APP_NAME                "WorldMapWallpaper"
!define FRIEND_NAME             "World Map Wallpaper"

; Use the published build directory - this contains all necessary files
!define PUBLISH_BUILD_PATH      ".\bin\publish-64"

;
; Define application file names
;
!define MAIN_APP_EXE     "${APP_NAME}.exe"
!define SETTINGS_APP_EXE "${APP_NAME}.Settings.exe"
!define MONITOR_APP_EXE  "WorldMapWallPaper.Monitor.exe"
!define SERVICE_NAME     "WorldMapWallpaperMonitor"
!define SERVICE_DISPLAY  "World Map Wallpaper Monitor"

Name "${FRIEND_NAME}"
OutFile "Install.exe"

;
; Installer page order
;
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

ReserveFile "License.txt"

;
; Uninstaller page order
;
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;  ___         _        _ _         
; |_ _|_ _  __| |_ __ _| | |___ _ _ 
;  | || ' \(_-<  _/ _` | | / -_) '_|
; |___|_||_/__/\__\__,_|_|_\___|_|  
;                                   

Function .onInit
    LogSet on

    LogText "Initializing installation..."
    InitPluginsDir

    ;
    ; Set the installation directory
    ;
    StrCpy $INSTDIR "C:\Program Files\${APP_NAME}"
    LogText "Installation path: '$INSTDIR'"
    SetOutPath "$INSTDIR"
FunctionEnd

Section "Installer Section" SecInstaller
    LogSet on

    ;
    ; Install all files from the published build directory
    ; This includes the main app, settings app, shared library, and all dependencies
    ;
    LogText "Installing application files from published build..."
    File /a /r "${PUBLISH_BUILD_PATH}\*.*"
    File "License.txt"

    ;
    ; Create the Event Source
    ;
    Call CreateEventSource

    ;
    ; Create the scheduled task
    ;
    Call CreateSchedulerTask
    
    ;
    ; Install and start the monitor service
    ;
    Call InstallMonitorService
    
    ;
    ; Register wallpaper provider for Windows Personalization
    ;
    Call RegisterWallpaperProvider

    ;
    ; Create a log directory
    ;
    LogText "Creating log directory..."
    CreateDirectory "$INSTDIR\log"
    
    ;
    ; Set permissions on the log directory
    ;
    LogText "Setting permissions on the log directory..."
    nsExec::ExecToLog 'icacls "$INSTDIR\log" /grant "Users":(OI)(CI)F'

    ;
    ; Create uninstaller
    ;
    LogText "Creating uninstaller..."
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ;
    ; Running the program and then launch settings
    ;
    LogText "Running the program to generate initial wallpaper..."
    ExecWait '"$INSTDIR\${MAIN_APP_EXE}"'
    LogText "Program executed."
    
    ;
    ; Launch settings interface for user configuration
    ;
    LogText "Launching settings interface..."
    Exec '"$INSTDIR\${SETTINGS_APP_EXE}"'
    LogText "Settings interface launched."

    LogText "Installation complete."
SectionEnd

Function CreateEventSource
    LogText "Creating event source..."

    ; Create the registry key
    WriteRegStr HKLM "${REG_PATH}" "EventMessageFile" "$INSTDIR\${MAIN_APP_EXE}"
    WriteRegDWORD HKLM "${REG_PATH}" "TypesSupported" 7

    LogText "Event source created."
FunctionEnd

Function CreateSchedulerTask
    
    LogText "Creating XML file for the scheduler task..."

    ;
    ; Create XML file for the scheduler task
    ;
    StrCpy $0 "$INSTDIR\${APP_NAME}Task.xml"
    FileOpen $1 $0 w

	ClearErrors
	UserInfo::GetName
	Pop $0

    FileWrite $1 '<?xml version="1.0" encoding="UTF-16"?>$\r$\n'
    FileWrite $1 '<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">$\r$\n'
    FileWrite $1 '  <RegistrationInfo>$\r$\n'
    FileWrite $1 '    <Date>2020-01-01T00:00:00</Date>$\r$\n'
    FileWrite $1 '    <Author>World Map Wallpaper</Author>$\r$\n'
    FileWrite $1 '  </RegistrationInfo>$\r$\n'
    FileWrite $1 '  <Triggers>$\r$\n'
    FileWrite $1 '    <BootTrigger>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    </BootTrigger>$\r$\n'
    FileWrite $1 '    <LogonTrigger>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    </LogonTrigger>$\r$\n'
    FileWrite $1 '    <EventTrigger>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '      <Subscription>&lt;QueryList&gt;&lt;Query Id="0" Path="System"&gt;&lt;Select Path="System"&gt;*[System[Provider[@Name=$\'Microsoft-Windows-Power-Troubleshooter$\'] and EventID=1]]&lt;/Select&gt;&lt;/Query&gt;&lt;/QueryList&gt;</Subscription>$\r$\n'
    FileWrite $1 '    </EventTrigger>$\r$\n'
    FileWrite $1 '    <TimeTrigger>$\r$\n'
    FileWrite $1 '      <Repetition>$\r$\n'
    FileWrite $1 '        <Interval>PT1H</Interval>$\r$\n'
    FileWrite $1 '        <StopAtDurationEnd>false</StopAtDurationEnd>$\r$\n'
    FileWrite $1 '      </Repetition>$\r$\n'
    FileWrite $1 '      <StartBoundary>2020-01-01T00:00:00</StartBoundary>$\r$\n'
    FileWrite $1 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $1 '    </TimeTrigger>$\r$\n'
    FileWrite $1 '  </Triggers>$\r$\n'
    FileWrite $1 '  <Principals>$\r$\n'
    FileWrite $1 '    <Principal id="Author">$\r$\n'
    FileWrite $1 '      <UserId>$0</UserId>$\r$\n'
    FileWrite $1 '      <LogonType>InteractiveToken</LogonType>$\r$\n'
    FileWrite $1 '      <RunLevel>HighestAvailable</RunLevel>$\r$\n'
    FileWrite $1 '    </Principal>$\r$\n'
    FileWrite $1 '  </Principals>$\r$\n'
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
    FileWrite $1 '  <Actions Context="Author">$\r$\n'
    FileWrite $1 '    <Exec>$\r$\n'
    FileWrite $1 '      <Command>"$INSTDIR\${MAIN_APP_EXE}"</Command>$\r$\n'
    FileWrite $1 '    </Exec>$\r$\n'
    FileWrite $1 '  </Actions>$\r$\n'
    FileWrite $1 '</Task>$\r$\n'
    FileClose $1
    LogText "XML file created: $0"

    LogText "Creating scheduler task to run the program..."
    StrCpy $0 '"schtasks" /create /tn "${FRIEND_NAME}" /xml "$INSTDIR\${APP_NAME}Task.xml" /f'
    LogText "Command: $0"
    nsExec::Exec "$0"
    pop $0
    LogText "Exit Code: $0"

FunctionEnd

Function RegisterWallpaperProvider
    LogText "Registering comprehensive wallpaper provider for Windows Personalization..."
    
    ; Register as proper Windows Background Provider
    LogText "Registering background provider..."
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "" "World Map Wallpaper"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "DisplayName" "World Map Wallpaper"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "Description" "Dynamic wallpaper with real-time day/night cycle, ISS tracking, and timezone clocks"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "ApplicationPath" "$INSTDIR\${MAIN_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "SettingsPath" "$INSTDIR\${SETTINGS_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "Category" "Dynamic"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "SupportedFormats" "jpg,jpeg,png,bmp"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "Version" "1.0"
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "SupportsSlideshow" 0
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "SupportsRealTime" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "SupportsScheduling" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}" "SupportsMultiMonitor" 1
    
    ; Register for personalization themes integration
    LogText "Registering personalization theme integration..."
    WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\${APP_NAME}" "DisplayName" "World Map Wallpaper"
    WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\${APP_NAME}" "Description" "Dynamic wallpaper with real-time day/night cycle, ISS tracking, and timezone clocks"
    WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\${APP_NAME}" "ApplicationPath" "$INSTDIR\${MAIN_APP_EXE}"
    WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\${APP_NAME}" "ThemeId" "${APP_NAME}"
    WriteRegDWORD HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\${APP_NAME}" "IsBackgroundProvider" 1
    
    ; Register application paths for Windows
    LogText "Registering application paths..."
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "" "$INSTDIR\${MAIN_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "Path" "$INSTDIR"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "" "$INSTDIR\${SETTINGS_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "Path" "$INSTDIR"
    
    ; Enable background access for the application
    LogText "Enabling background access permissions..."
    WriteRegDWORD HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications\${APP_NAME}" "Disabled" 0
    WriteRegDWORD HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications\${APP_NAME}" "DisabledByUser" 0
    
    ; Legacy compatibility registration
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background" "" "World Map Dynamic Wallpaper"
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background" "FriendlyName" "World Map with Day/Night Cycle"
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background\shell\configure" "" "Configure World Map Settings"
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background\shell\configure\command" "" '"$INSTDIR\${SETTINGS_APP_EXE}"'
    
    ; Add desktop right-click context menu for easy access to settings
    LogText "Adding desktop context menu integration..."
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapSettings" "" "World Map Wallpaper Settings"
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapSettings" "Icon" "$INSTDIR\${SETTINGS_APP_EXE},0"
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapSettings" "Position" "Middle"
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapSettings\command" "" '"$INSTDIR\${SETTINGS_APP_EXE}"'
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapSettings" "SeparatorBefore" ""
    
    ; Also add "Update Wallpaper Now" option for quick updates
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapUpdate" "" "Update World Map Wallpaper Now"
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapUpdate" "Icon" "$INSTDIR\${MAIN_APP_EXE},0"
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapUpdate" "Position" "Middle"
    WriteRegStr HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapUpdate\command" "" '"$INSTDIR\${MAIN_APP_EXE}"'
    
    ; Add integration with Windows Personalization settings
    LogText "Adding Windows Personalization settings integration..."
    WriteRegStr HKLM "SOFTWARE\Classes\ms-settings" "world-map-wallpaper" "ms-settings:personalization-background"
    WriteRegStr HKLM "SOFTWARE\Classes\Applications\${SETTINGS_APP_EXE}\shell\open" "FriendlyAppName" "World Map Wallpaper Settings"
    
    LogText "Comprehensive wallpaper provider registration complete."
FunctionEnd

Function InstallMonitorService
    LogText "Installing and starting monitor service..."
    
    ; Install the service using the Monitor exe
    LogText "Installing service ${SERVICE_NAME}..."
    nsExec::ExecToLog '"$INSTDIR\${MONITOR_APP_EXE}" install'
    Pop $0
    LogText "Service install exit code: $0"
    
    ; Start the service
    LogText "Starting service ${SERVICE_NAME}..."
    nsExec::ExecToLog '"$INSTDIR\${MONITOR_APP_EXE}" start'
    Pop $0
    LogText "Service start exit code: $0"
    
    ; Verify service is running
    Sleep 2000  ; Wait 2 seconds for service to start
    LogText "Verifying service status..."
    nsExec::ExecToLog 'sc query "${SERVICE_NAME}"'
    Pop $0
    LogText "Service query exit code: $0"
    
    LogText "Monitor service installation completed."
FunctionEnd

;  _   _      _         _        _ _         
; | | | |_ _ (_)_ _  __| |_ __ _| | |___ _ _ 
; | |_| | ' \| | ' \(_-<  _/ _` | | / -_) '_|
;  \___/|_||_|_|_||_/__/\__\__,_|_|_\___|_|  
;
Section "Uninstall" SecUninstaller
    LogSet on

    LogText "Uninstalling ${APP_NAME}..."
    LogText "Installation directory: $INSTDIR"

    ;
    ; Stop and uninstall the monitor service
    ;
    Call UninstallMonitorService

    ;
    ; Remove the installation directory
    ;
    LogText "Removing installation directory..."
    RMDir /r "$INSTDIR"

    ;
    ; Remove the scheduler tasks
    ;
    LogText "Removing scheduler task to run the program every hour on the hour..."
    StrCpy $0 '"schtasks" /delete /tn "${FRIEND_NAME}" /f'
    LogText "Command: $0"
    nsExec::Exec "$0"
    pop $0
    LogText "Exit Code: $0"

    ;
    ; Remove the event source
    ;
    LogText "Removing event source..."
    DeleteRegKey HKLM "${REG_PATH}"
    
    ;
    ; Remove comprehensive wallpaper provider registry entries
    ;
    LogText "Removing wallpaper provider registry entries..."
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders\${APP_NAME}"
    DeleteRegKey HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\${APP_NAME}"
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}"
    DeleteRegKey HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications\${APP_NAME}"
    DeleteRegKey HKLM "SOFTWARE\Classes\${APP_NAME}.Background"
    DeleteRegValue HKLM "SOFTWARE\Classes\ms-settings" "world-map-wallpaper"
    DeleteRegKey HKLM "SOFTWARE\Classes\Applications\${SETTINGS_APP_EXE}"
    
    ;
    ; Remove desktop context menu entries
    ;
    LogText "Removing desktop context menu entries..."
    DeleteRegKey HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapSettings"
    DeleteRegKey HKLM "SOFTWARE\Classes\DesktopBackground\Shell\WorldMapUpdate"

SectionEnd

Function UninstallMonitorService
    LogText "Stopping and uninstalling monitor service..."
    
    ; Stop the service first
    LogText "Stopping service ${SERVICE_NAME}..."
    nsExec::ExecToLog '"$INSTDIR\${MONITOR_APP_EXE}" stop'
    Pop $0
    LogText "Service stop exit code: $0"
    
    ; Wait a moment for service to stop
    Sleep 2000
    
    ; Uninstall the service
    LogText "Uninstalling service ${SERVICE_NAME}..."
    nsExec::ExecToLog '"$INSTDIR\${MONITOR_APP_EXE}" uninstall'
    Pop $0
    LogText "Service uninstall exit code: $0"
    
    ; Verify service is removed
    LogText "Verifying service removal..."
    nsExec::ExecToLog 'sc query "${SERVICE_NAME}"'
    Pop $0
    LogText "Service query exit code (should be error): $0"
    
    LogText "Monitor service uninstallation completed."
FunctionEnd

;  _                ___             _   _             
; | |   ___  __ _  | __|  _ _ _  __| |_(_)___ _ _  ___
; | |__/ _ \/ _` | | _| || | ' \/ _|  _| / _ \ ' \(_-<
; |____\___/\__, | |_| \_,_|_||_\__|\__|_\___/_||_/__/
;           |___/                                     

Function .onInstSuccess
    LogSet on
    LogText "Installation successful."
FunctionEnd

Function .onInstFailed
    LogSet on
    LogText "Installation failed."
FunctionEnd
