; WebView Keyboard Launcher - Simple URL Handling
!define APPNAME "WebView Keyboard Launcher"
!define COMPANYNAME "SezginBilge"
!define DESCRIPTION "Virtual keyboard launcher with WebView2"
!define VERSIONMAJOR 1
!define VERSIONMINOR 0
!define VERSIONBUILD 0

RequestExecutionLevel admin
Name "${APPNAME}"
outFile "WebViewKeyboardLauncher_Setup.exe"

; Modern UI includes
!include MUI2.nsh
!include LogicLib.nsh
!include nsDialogs.nsh
!include FileFunc.nsh
!include x64.nsh

; Modern UI settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Custom install types
InstType "Full Installation"
InstType "Minimal Installation"

; Pages
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
Page custom nsDialogsPageCreate nsDialogsPageLeave
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
Page custom FinishPageCreate FinishPageLeave

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Variables - simple approach
Var Dialog
Var UrlTextBox
Var UrlValue
Var StartWithWindowsCheckbox
Var StartWithWindowsValue
Var StartNowCheckbox
Var KioskModeCheckbox
Var KioskModeValue
Var FullscreenCheckbox
Var FullscreenValue

; Default values - URL removed, will be required from command line

Function .onInit
    ; Set install directory based on architecture
    ${If} ${RunningX64}
        StrCpy $INSTDIR "$PROGRAMFILES64\${APPNAME}"
    ${Else}
        StrCpy $INSTDIR "$PROGRAMFILES32\${APPNAME}"
    ${EndIf}
    
    ; Initialize with default values
    StrCpy $UrlValue ""
    StrCpy $StartWithWindowsValue "1"
    StrCpy $KioskModeValue "0"
    StrCpy $FullscreenValue "0"
    
    ; Get command line parameters
    ${GetParameters} $R0
    
    ; URL parameter - optional, write whatever is there
    ClearErrors
    ${GetOptions} $R0 "/URL=" $UrlValue
    
    ; Other parameters
    ClearErrors
    ${GetOptions} $R0 "/AUTOSTART=" $0
    ${IfNot} ${Errors}
        StrCpy $StartWithWindowsValue $0
    ${EndIf}
    
    ClearErrors
    ${GetOptions} $R0 "/KIOSK=" $0
    ${IfNot} ${Errors}
        StrCpy $KioskModeValue $0
    ${EndIf}
    
    ClearErrors
    ${GetOptions} $R0 "/FULLSCREEN=" $0
    ${IfNot} ${Errors}
        StrCpy $FullscreenValue $0
    ${EndIf}
FunctionEnd

; Custom URL configuration page
Function nsDialogsPageCreate
    nsDialogs::Create 1018
    Pop $Dialog

    ${If} $Dialog == error
        Abort
    ${EndIf}

    ; Title
    ${NSD_CreateLabel} 10 10 280 20 "Configuration Options"
    Pop $0
    
    ; URL input - optional
    ${NSD_CreateLabel} 10 40 100 15 "Homepage URL:"
    Pop $0
    
    ${NSD_CreateText} 10 60 350 20 $UrlValue
    Pop $UrlTextBox
    
    ; Start with Windows checkbox
    ${NSD_CreateCheckbox} 10 90 350 15 "Start with Windows (recommended)"
    Pop $StartWithWindowsCheckbox
    ${If} $StartWithWindowsValue == "1"
        ${NSD_Check} $StartWithWindowsCheckbox
    ${EndIf}
    
    ; Kiosk Mode section
    ${NSD_CreateGroupBox} 10 120 350 60 "Kiosk Mode (Advanced)"
    Pop $0
    
    ${NSD_CreateCheckbox} 20 140 330 15 "Enable Kiosk Mode (secure lockdown for terminals)"
    Pop $KioskModeCheckbox
    ${If} $KioskModeValue == "1"
        ${NSD_Check} $KioskModeCheckbox
    ${EndIf}
    
    ${NSD_CreateCheckbox} 20 160 330 15 "Fullscreen mode (hides taskbar)"
    Pop $FullscreenCheckbox
    ${If} $FullscreenValue == "1"
        ${NSD_Check} $FullscreenCheckbox
    ${EndIf}
    
    ; Warning label
    ${NSD_CreateLabel} 10 190 350 30 "Emergency exit: Ctrl+Shift+Alt+E"
    Pop $0

    nsDialogs::Show
FunctionEnd

Function nsDialogsPageLeave
    ; Get values from form
    ${NSD_GetText} $UrlTextBox $UrlValue
    ${NSD_GetState} $StartWithWindowsCheckbox $StartWithWindowsValue
    ${NSD_GetState} $KioskModeCheckbox $KioskModeValue
    ${NSD_GetState} $FullscreenCheckbox $FullscreenValue
    
    ; Warn about kiosk mode
    ${If} $KioskModeValue == 1
        MessageBox MB_YESNO|MB_ICONQUESTION "Kiosk mode will create a restricted environment. Continue?" IDYES +2
        Abort
    ${EndIf}
FunctionEnd

; Finish page
Function FinishPageCreate
    nsDialogs::Create 1018
    Pop $Dialog

    ${NSD_CreateLabel} 10 10 350 20 "Installation completed successfully!"
    Pop $0
    
    ${NSD_CreateLabel} 10 40 350 60 "WebView Keyboard Launcher has been installed."
    Pop $0
    
    ; Start now checkbox
    ${NSD_CreateCheckbox} 10 110 350 15 "Start WebView Keyboard Launcher now"
    Pop $StartNowCheckbox
    ${NSD_Check} $StartNowCheckbox

    nsDialogs::Show
FunctionEnd

Function FinishPageLeave
    ${NSD_GetState} $StartNowCheckbox $0
    ${If} $0 == 1
        Exec "$INSTDIR\WebViewKeyboardLauncher.exe"
    ${EndIf}
FunctionEnd

; Main installation section
Section "Core Application" SecCore
    SectionIn 1 2 RO
    
    SetOutPath $INSTDIR
    
    ; Application files
    File "${SOURCE_DIR}\WebViewKeyboardLauncher.exe"
    File /nonfatal "${SOURCE_DIR}\*.dll"
    File /nonfatal "${SOURCE_DIR}\*.json"
    File /nonfatal "${SOURCE_DIR}\*.config"
    File /nonfatal "${SOURCE_DIR}\*.pdb"
    
    ; Registry setup
    ${If} ${RunningX64}
        SetRegView 64
    ${Else}
        SetRegView 32
    ${EndIf}
    
    ; Write configuration to registry
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "Homepage" $UrlValue
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "InstallPath" "$INSTDIR"
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "Version" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    WriteRegDWORD HKLM "SOFTWARE\WebViewKeyboardLauncher" "KioskMode" $KioskModeValue
    WriteRegDWORD HKLM "SOFTWARE\WebViewKeyboardLauncher" "FullscreenMode" $FullscreenValue
    
    ; Debug - write what we parsed
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "DEBUG_ParsedURL" $UrlValue
    
    ; Uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
    
    ; Add/Remove Programs entry
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "QuietUninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayIcon" "$INSTDIR\WebViewKeyboardLauncher.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "Publisher" "${COMPANYNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
SectionEnd

Section "Start Menu Shortcuts" SecShortcuts
    SectionIn 1
    
    CreateDirectory "$SMPROGRAMS\${COMPANYNAME}"
    CreateShortCut "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk" "$INSTDIR\WebViewKeyboardLauncher.exe"
    CreateShortCut "$SMPROGRAMS\${COMPANYNAME}\Uninstall ${APPNAME}.lnk" "$INSTDIR\uninstall.exe"
    CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\WebViewKeyboardLauncher.exe"
SectionEnd

Section "Auto Start with Windows" SecAutoStart
    SectionIn 1
    
    ${If} $StartWithWindowsValue == 1
        ${If} $KioskModeValue == 1
            Call SetupKioskMode
        ${Else}
            WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher" '"$INSTDIR\WebViewKeyboardLauncher.exe"'
        ${EndIf}
    ${EndIf}
SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} "Core application files (required)"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecShortcuts} "Start menu and desktop shortcuts"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecAutoStart} "Automatically start with Windows"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; Variables for uninstaller
Var WasKioskMode

; Uninstaller
Section "Uninstall"
    ; Check for silent uninstall
    ${GetParameters} $R0
    ClearErrors
    ${GetOptions} $R0 "/S" $0
    
    ; Stop the application
    ExecWait 'taskkill /F /IM WebViewKeyboardLauncher.exe' $0
    
    ; Set correct registry view
    ${If} ${RunningX64}
        SetRegView 64
    ${Else}
        SetRegView 32
    ${EndIf}
    
    ; Check if kiosk mode was enabled
    ReadRegDWORD $0 HKLM "SOFTWARE\WebViewKeyboardLauncher" "KioskMode"
    StrCpy $WasKioskMode $0
    
    ${If} $WasKioskMode == 1
        ; Restore all kiosk settings without asking
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultUserName"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultPassword"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "AutoAdminLogon"
        
        ; Restore default shell (critical!)
        WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "Shell" "explorer.exe"
        
        ; Restore other settings
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" "DisableTaskMgr"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoRun"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoClose"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" "DisableCAD"
        
        ; Remove kiosk user automatically
        ExecWait 'net user KioskUser /delete' $0
    ${EndIf}
    
    ; Remove startup entries
    DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher"
    
    ; Remove application files
    Delete "$INSTDIR\WebViewKeyboardLauncher.exe"
    Delete "$INSTDIR\*.dll"
    Delete "$INSTDIR\*.json"
    Delete "$INSTDIR\uninstall.exe"
    RMDir /r "$INSTDIR"
    
    ; Remove shortcuts
    Delete "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk"
    Delete "$SMPROGRAMS\${COMPANYNAME}\Uninstall ${APPNAME}.lnk"
    RMDir "$SMPROGRAMS\${COMPANYNAME}"
    Delete "$DESKTOP\${APPNAME}.lnk"
    
    ; Remove registry entries
    DeleteRegKey HKLM "SOFTWARE\WebViewKeyboardLauncher"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
    
    ; If kiosk mode was active, restart system
    ${If} $WasKioskMode == 1
        MessageBox MB_OK "Kiosk mode has been removed. System will restart in 10 seconds."
        ExecWait 'shutdown /r /t 10 /c "WebView Keyboard Launcher uninstalled - Kiosk mode removed"' $0
    ${EndIf}
SectionEnd

; Kiosk mode setup function
Function SetupKioskMode
    ; Create kiosk user account
    ExecWait 'net user KioskUser /add /comment:"WebView Kiosk User" /passwordreq:no' $0
    
    ; Set auto-login for kiosk user
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultUserName" "KioskUser"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultPassword" ""
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "AutoAdminLogon" "1"
    
    ; Set custom shell for KioskUser - this is the key part!
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "Shell" "$INSTDIR\WebViewKeyboardLauncher.exe"
    
    ; Alternative: User-specific shell (even better - only affects kiosk user)
    ; Get KioskUser SID first
    ExecWait 'wmic useraccount where name="KioskUser" get sid /value' $0
    
    ; For now, use global shell override (simpler)
    ; When KioskUser logs in, our app will be the shell instead of explorer
    
    ; Basic kiosk lockdown (these become less critical since we're replacing shell)
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" "DisableTaskMgr" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoRun" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoClose" 1
    
    ; Disable Ctrl+Alt+Del for kiosk user
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" "DisableCAD" 1
FunctionEnd