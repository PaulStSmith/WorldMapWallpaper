!define ENABLE_LOGGING
!define MUI_ABORTWARNING
!define TEMP1 $R0 ; Temporary variable 1
!include "MUI.nsh"

Name "World Map Wallpaper"
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
    StrCpy $INSTDIR "C:\Program Files\WorldMapWallpaper"
    LogText "Installation path: '$INSTDIR'"
    SetOutPath "$INSTDIR"
FunctionEnd

Section "Installer Section" SecInstaller
    LogSet on
    ;
    ; Files to be installed
    ;
    File /a /r ".\bin\publish-64\*.*"
    File "License.txt"

    ;
    ; Create uninstaller
    ;
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ;
    ; Create XML file for the scheduler task
    ;
    Call CreateSchedulerTask

    ;
    ; Running the program
    ;
    LogText "Running the program..."
    ExecWait '"$INSTDIR\WorldMapWallpaper.exe"'
    LogText "Program executed."

    LogText "Installation complete."
SectionEnd

Function CreateSchedulerTask
    
    LogText "Creating XML file for the scheduler task..."
    ; Create the XML file
    StrCpy $0 "$INSTDIR\WorldMapWallpaperTask.xml"
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
    FileWrite $1 '      <Command>"$INSTDIR\WorldMapWallpaper.exe"</Command>$\r$\n'
    FileWrite $1 '    </Exec>$\r$\n'
    FileWrite $1 '  </Actions>$\r$\n'
    FileWrite $1 '</Task>$\r$\n'
    FileClose $1
    LogText "XML file created: $0"

    LogText "Creating scheduler task to run the program..."
    StrCpy $0 '"schtasks" /create /tn "World Map Wallpaper" /xml "$INSTDIR\WorldMapWallpaperTask.xml" /f'
    LogText "Command: $0"
    nsExec::Exec "$0"
    pop $0
    LogText "Exit Code: $0"

FunctionEnd

;  _   _      _         _        _ _         
; | | | |_ _ (_)_ _  __| |_ __ _| | |___ _ _ 
; | |_| | ' \| | ' \(_-<  _/ _` | | / -_) '_|
;  \___/|_||_|_|_||_/__/\__\__,_|_|_\___|_|  
;
Section "Uninstall" SecUninstaller
    LogSet on

    LogText "Uninstalling WorldMapWallpaper..."

    LogText "Installation directory: $INSTDIR"

    ; Remove the installation directory
    LogText "Removing installation directory..."
    RMDir /r "$INSTDIR"

    ; Remove the scheduler tasks
    LogText "Removing scheduler task to run the program every hour on the hour..."
    nsExec::Exec '"schtasks" /delete /tn "World Map Wallpaper" /f'

SectionEnd

;  _                ___             _   _             
; | |   ___  __ _  | __|  _ _ _  __| |_(_)___ _ _  ___
; | |__/ _ \/ _` | | _| || | ' \/ _|  _| / _ \ ' \(_-<
; |____\___/\__, | |_| \_,_|_||_\__|\__|_\___/_||_/__/
;           |___/                                     

Function .onInstSuccess
    LogText "Installation successful."
FunctionEnd

Function .onInstFailed
    LogText "Installation failed."
FunctionEnd
