; WebView Keyboard Launcher - 64-bit Registry Fixed Version
!define APPNAME "WebView Keyboard Launcher"
!define COMPANYNAME "SezginBilge"
!define DESCRIPTION "Virtual keyboard launcher with WebView2"
!define VERSIONMAJOR 1
!define VERSIONMINOR 0
!define VERSIONBUILD 0

; Cross-platform installer (32-bit NSIS compatible)
RequestExecutionLevel admin
Name "${APPNAME}"
outFile "WebViewKeyboardLauncher_Setup.exe"

; Auto-detect system architecture for install path
!ifdef NSIS_WIN32_MAKENSIS
  ; 32-bit NSIS detected - use conditional install dir
  Var InstallDir64
!else
  ; 64-bit NSIS detected
!endif

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
Page custom nsDialogsPageCreate nsDialogsPageLeave ; Custom URL page
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
Page custom FinishPageCreate FinishPageLeave ; Custom finish page

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Variables for custom URL input
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

; Check system architecture and set install directory
Function .onInit
    ; Detect system architecture and set appropriate install directory
    ${If} ${RunningX64}
        StrCpy $INSTDIR "$PROGRAMFILES64\${APPNAME}"
        DetailPrint "64-bit Windows detected - installing to Program Files"
    ${Else}
        StrCpy $INSTDIR "$PROGRAMFILES32\${APPNAME}"
        DetailPrint "32-bit Windows detected - installing to Program Files (x86)"
    ${EndIf}
    
    ; Check command line parameters
    ${GetParameters} $R0
    
    ; Silent install with URL parameter
    ClearErrors
    ${GetOptions} $R0 "/URL=" $UrlValue
    ${If} ${Errors}
        StrCpy $UrlValue "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome.html"
    ${EndIf}
    
    ; Auto start parameter
    ClearErrors
    ${GetOptions} $R0 "/AUTOSTART=" $StartWithWindowsValue
    ${If} ${Errors}
        StrCpy $StartWithWindowsValue "1"
    ${EndIf}
    
    ; Kiosk mode parameter
    ClearErrors
    ${GetOptions} $R0 "/KIOSK=" $KioskModeValue
    ${If} ${Errors}
        StrCpy $KioskModeValue "0"
    ${EndIf}
    
    ; Fullscreen parameter
    ClearErrors
    ${GetOptions} $R0 "/FULLSCREEN=" $FullscreenValue
    ${If} ${Errors}
        StrCpy $FullscreenValue "0"
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
    
    ; URL input
    ${NSD_CreateLabel} 10 40 100 15 "Homepage URL:"
    Pop $0
    
    ${NSD_CreateText} 10 60 350 20 "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome"
    Pop $UrlTextBox
    
    ; Start with Windows checkbox
    ${NSD_CreateCheckbox} 10 90 350 15 "Start with Windows (recommended)"
    Pop $StartWithWindowsCheckbox
    ${NSD_Check} $StartWithWindowsCheckbox ; Default checked
    
    ; Kiosk Mode section
    ${NSD_CreateGroupBox} 10 120 350 60 "Kiosk Mode (Advanced)"
    Pop $0
    
    ${NSD_CreateCheckbox} 20 140 330 15 "Enable Kiosk Mode (secure lockdown for terminals)"
    Pop $KioskModeCheckbox
    
    ${NSD_CreateCheckbox} 20 160 330 15 "Fullscreen mode (hides taskbar)"
    Pop $FullscreenCheckbox
    
    ; Warning label
    ${NSD_CreateLabel} 10 190 350 30 "Emergency exit: Ctrl+Shift+Alt+E"
    Pop $0

    nsDialogs::Show
FunctionEnd

Function nsDialogsPageLeave
    ${NSD_GetText} $UrlTextBox $UrlValue
    ${NSD_GetState} $StartWithWindowsCheckbox $StartWithWindowsValue
    ${NSD_GetState} $KioskModeCheckbox $KioskModeValue
    ${NSD_GetState} $FullscreenCheckbox $FullscreenValue
    
    ; Validate URL
    ${If} $UrlValue == ""
        MessageBox MB_OK "Please enter a valid URL."
        Abort
    ${EndIf}
    
    ; Warn about kiosk mode
    ${If} $KioskModeValue == 1
        MessageBox MB_YESNO|MB_ICONQUESTION "Kiosk mode will create a restricted user account and limit system access. This is intended for dedicated kiosk terminals only. Continue?" IDYES +2
        Abort
    ${EndIf}
FunctionEnd

; Finish page
Function FinishPageCreate
    nsDialogs::Create 1018
    Pop $Dialog

    ${NSD_CreateLabel} 10 10 350 20 "Installation completed successfully!"
    Pop $0
    
    ${NSD_CreateLabel} 10 40 350 60 "WebView Keyboard Launcher has been installed. The application will start automatically when Windows starts (if selected)."
    Pop $0
    
    ; Start now checkbox
    ${NSD_CreateCheckbox} 10 110 350 15 "Start WebView Keyboard Launcher now"
    Pop $StartNowCheckbox
    ${NSD_Check} $StartNowCheckbox ; Default checked

    nsDialogs::Show
FunctionEnd

Function FinishPageLeave
    ; Check if user wants to start now
    ${NSD_GetState} $StartNowCheckbox $1
    ${If} $1 == 1
        Exec "$INSTDIR\WebViewKeyboardLauncher.exe"
    ${EndIf}
FunctionEnd

; Main installation section
Section "Core Application" SecCore
    SectionIn 1 2 RO ; Required in all install types
    
    DetailPrint "Installing to 64-bit Program Files..."
    SetOutPath $INSTDIR
    
    ; Application files
    File "${SOURCE_DIR}\WebViewKeyboardLauncher.exe"
    
    ; Copy additional files with /nonfatal flag
    File /nonfatal "${SOURCE_DIR}\*.dll"
    File /nonfatal "${SOURCE_DIR}\*.json"
    File /nonfatal "${SOURCE_DIR}\*.config"
    File /nonfatal "${SOURCE_DIR}\*.pdb"
    
    ; Smart registry handling - prefer 64-bit when available
    DetailPrint "Configuring registry entries..."
    
    ; Clean old entries from both locations
    ${If} ${RunningX64}
        DetailPrint "Cleaning old registry entries..."
        SetRegView 64
        DeleteRegKey /ifempty HKLM "SOFTWARE\WebViewKeyboardLauncher"
        SetRegView 32
        DeleteRegKey /ifempty HKLM "SOFTWARE\WebViewKeyboardLauncher"
        
        ; Write to 64-bit registry (preferred on x64 systems)
        DetailPrint "Writing to 64-bit registry..."
        SetRegView 64
    ${Else}
        ; On 32-bit systems, use native registry
        DetailPrint "Writing to native registry..."
        SetRegView 32
    ${EndIf}
    
    ; Write configuration
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "Homepage" $UrlValue
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "InstallPath" "$INSTDIR"
    WriteRegStr HKLM "SOFTWARE\WebViewKeyboardLauncher" "Version" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    WriteRegDWORD HKLM "SOFTWARE\WebViewKeyboardLauncher" "KioskMode" $KioskModeValue
    WriteRegDWORD HKLM "SOFTWARE\WebViewKeyboardLauncher" "FullscreenMode" $FullscreenValue
    
    ; Verify registry write
    ReadRegStr $0 HKLM "SOFTWARE\WebViewKeyboardLauncher" "Homepage"
    ${If} $0 == ""
        DetailPrint "WARNING: Registry write may have failed"
    ${Else}
        DetailPrint "Registry write successful: $0"
    ${EndIf}
    
    ; Uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
    
    ; Add/Remove Programs entry (also in 64-bit registry)
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "QuietUninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayIcon" "$INSTDIR\WebViewKeyboardLauncher.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "Publisher" "${COMPANYNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "VersionMajor" ${VERSIONMAJOR}
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "VersionMinor" ${VERSIONMINOR}
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
SectionEnd

Section "Start Menu Shortcuts" SecShortcuts
    SectionIn 1 ; Only in full installation
    
    CreateDirectory "$SMPROGRAMS\${COMPANYNAME}"
    CreateShortCut "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk" "$INSTDIR\WebViewKeyboardLauncher.exe"
    CreateShortCut "$SMPROGRAMS\${COMPANYNAME}\Uninstall ${APPNAME}.lnk" "$INSTDIR\uninstall.exe"
    CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\WebViewKeyboardLauncher.exe"
SectionEnd

Section "Auto Start with Windows" SecAutoStart
    SectionIn 1 ; Only in full installation
    
    ; Only if user selected this option
    ${If} $StartWithWindowsValue == 1
        ${If} ${RunningX64}
            SetRegView 64
        ${Else}
            SetRegView 32
        ${EndIf}
        
        ${If} $KioskModeValue == 1
            ; Kiosk mode - create kiosk user and auto-login
            Call SetupKioskMode
        ${Else}
            ; Normal auto-start
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

; Uninstaller
Section "Uninstall"
    ; Stop the application if running
    ExecWait 'taskkill /F /IM WebViewKeyboardLauncher.exe' $0
    
    ; Check for kiosk mode settings
    ${If} ${RunningX64}
        SetRegView 64
    ${Else}
        SetRegView 32
    ${EndIf}
    
    ReadRegDWORD $0 HKLM "SOFTWARE\WebViewKeyboardLauncher" "KioskMode"
    ${If} $0 == 1
        ; Restore system settings
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultUserName"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultPassword"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "AutoAdminLogon"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" "DisableTaskMgr"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoRun"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoClose"
        
        ; Remove kiosk user (optional)
        MessageBox MB_YESNO "Remove Kiosk user account? (This will delete all user data)" IDNO +2
        ExecWait 'net user KioskUser /delete' $0
    ${EndIf}
    
    ; Remove startup entries from appropriate registry location
    ${If} ${RunningX64}
        SetRegView 64
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher"
        SetRegView 32
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher"
    ${Else}
        SetRegView 32
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher"
    ${EndIf}
    
    ; Remove application registry entries
    ${If} ${RunningX64}
        SetRegView 64
        DeleteRegKey HKLM "SOFTWARE\WebViewKeyboardLauncher"
        SetRegView 32
        DeleteRegKey HKLM "SOFTWARE\WebViewKeyboardLauncher"
    ${Else}
        SetRegView 32
        DeleteRegKey HKLM "SOFTWARE\WebViewKeyboardLauncher"
    ${EndIf}
    
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
    
    ; Remove uninstall registry entry
    ${If} ${RunningX64}
        SetRegView 64
    ${Else}
        SetRegView 32
    ${EndIf}
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
SectionEnd

; Kiosk mode setup function
Function SetupKioskMode
    DetailPrint "Setting up Kiosk Mode..."
    
    ; Create kiosk user account
    ExecWait 'net user KioskUser /add /comment:"WebView Kiosk User" /passwordreq:no' $0
    ${If} $0 == 0
        DetailPrint "Kiosk user created successfully"
    ${Else}
        DetailPrint "Kiosk user may already exist or creation failed"
    ${EndIf}
    
    ; Set auto-login for kiosk user
    ${If} ${RunningX64}
        SetRegView 64
    ${Else}
        SetRegView 32
    ${EndIf}
    
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultUserName" "KioskUser"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultPassword" ""
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" "AutoAdminLogon" "1"
    
    ; Add kiosk user to auto-start
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher" '"$INSTDIR\WebViewKeyboardLauncher.exe"'
    
    ; Disable Windows key and other shortcuts (basic kiosk lockdown)
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" "DisableTaskMgr" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoRun" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoClose" 1
    
    DetailPrint "Kiosk mode configured"
FunctionEnd