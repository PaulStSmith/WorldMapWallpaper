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
; Build Configuration - CHANGE HERE FOR DIFFERENT BUILDS
; ========================================
; Can be overridden via command line:
; makensis /DBUILD_CONFIG=Release InstallMaker.nsi
; makensis /DTARGET_FRAMEWORK=net9.0-windows10.0.17763.0 InstallMaker.nsi
; ========================================
;
; Set default values if not provided via command line
!ifndef BUILD_CONFIG
  !define BUILD_CONFIG          "Debug"
!endif

!ifndef TARGET_FRAMEWORK
  !define TARGET_FRAMEWORK      "net9.0-windows10.0.17763.0"
!endif

!define APP_NAME                "WorldMapWallpaper"
!define FRIEND_NAME             "World Map Wallpaper"
!define MAIN_APP_BUILD_PATH     ".\bin\${BUILD_CONFIG}\${TARGET_FRAMEWORK}"
!define SHARED_LIB_BUILD_PATH   "..\Shared\bin\${BUILD_CONFIG}\${TARGET_FRAMEWORK}"
!define SETTINGS_APP_BUILD_PATH "..\Settings\bin\${BUILD_CONFIG}\${TARGET_FRAMEWORK}"

;
; Define application file names
;
!define MAIN_APP_EXE     "${APP_NAME}.exe"
!define MAIN_APP_DLL     "${APP_NAME}.dll"
!define SETTINGS_APP_EXE "${APP_NAME}.Settings.exe"
!define SETTINGS_APP_DLL "${APP_NAME}.Settings.dll"
!define SHARED_LIB_DLL   "${APP_NAME}.Shared.dll"

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
    ; Files to be installed - Main Application from build output
    ;
    File /a /r "${MAIN_APP_BUILD_PATH}\*.*"
    File "License.txt"
    
    ;
    ; Install Settings Application
    ;
    LogText "Installing Settings application..."
    File /oname=${SETTINGS_APP_EXE} "${SETTINGS_APP_BUILD_PATH}\${SETTINGS_APP_EXE}"
    File /oname=${SETTINGS_APP_DLL} "${SETTINGS_APP_BUILD_PATH}\${SETTINGS_APP_DLL}"
    File /oname=${SETTINGS_APP_EXE}.runtimeconfig.json "${SETTINGS_APP_BUILD_PATH}\${SETTINGS_APP_EXE}.runtimeconfig.json"
    File /oname=${SETTINGS_APP_EXE}.deps.json "${SETTINGS_APP_BUILD_PATH}\${SETTINGS_APP_EXE}.deps.json"
    File /oname=${SHARED_LIB_DLL} "${SETTINGS_APP_BUILD_PATH}\${SHARED_LIB_DLL}"

    ;
    ; Create the Event Source
    ;
    Call CreateEventSource

    ;
    ; Create the scheduled task
    ;
    Call CreateSchedulerTask
    
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
    ; Running the program
    ;
    LogText "Running the program..."
    ExecWait '"$INSTDIR\${MAIN_APP_EXE}"'
    LogText "Program executed."

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
    LogText "Registering wallpaper provider for Windows Personalization..."
    
    ; Register the application for wallpaper settings integration
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "" "$INSTDIR\${MAIN_APP_EXE}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "Path" "$INSTDIR"
    
    ; Add to Windows Settings integration (optional - may require additional permissions)
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background" "" "World Map Dynamic Wallpaper"
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background" "FriendlyName" "World Map with Day/Night Cycle"
    
    ; Register the settings command
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background\shell\configure" "" "Configure World Map Settings"
    WriteRegStr HKLM "SOFTWARE\Classes\${APP_NAME}.Background\shell\configure\command" "" '"$INSTDIR\${MAIN_APP_EXE}" --settings'
    
    LogText "Wallpaper provider registration complete."
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
    ; Remove wallpaper provider registry entries
    ;
    LogText "Removing wallpaper provider registry entries..."
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
    DeleteRegKey HKLM "SOFTWARE\Classes\${APP_NAME}.Background"

SectionEnd

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
