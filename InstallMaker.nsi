!define ENABLE_LOGGING
!define MUI_ABORTWARNING
!define TEMP1 $R0 ; Temporary variable 1
!define TEMP2 $R1 ; Temporary variable 2
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

    ; Create scheduler task to run the program every hour on the hour
    LogText "Creating scheduler task to run the program every hour on the hour..."
    StrCpy $0 '"schtasks" /create /tn "World Map Wallpaper 01" /tr "\"$INSTDIR\WorldMapWallpaper.exe\"" /sc HOURLY /mo 1 /st 00:00:00 /f'
    LogText "Command: $0"
    nsExec::Exec "$0"

    ; Create scheduler task to run the program when the user logs in
    LogText "Creating scheduler task to run the program when the user logs in..."
    nsExec::Exec '"schtasks" /create /tn "World Map Wallpaper 02" /tr "\"$INSTDIR\WorldMapWallpaper.exe\"" /sc ONLOGON /f'

    ; Create scheduler task to run the program when the computer starts
    LogText "Creating scheduler task to run the program when the computer starts..."
    nsExec::Exec '"schtasks" /create /tn "World Map Wallpaper 03" /tr "\"$INSTDIR\WorldMapWallpaper.exe\"" /sc ONSTART /f'

    LogText "Installation complete."
SectionEnd

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
    nsExec::Exec '"schtasks" /delete /tn "World Map Wallpaper 01" /f'

    LogText "Removing scheduler task to run the program when the user logs in..."
    nsExec::Exec '"schtasks" /delete /tn "World Map Wallpaper 02" /f'

    LogText "Removing scheduler task to run the program when the computer starts..."
    nsExec::Exec '"schtasks" /delete /tn "World Map Wallpaper 03" /f'

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
