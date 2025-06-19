; WebView Keyboard Launcher with Kiosk Mode Support
!define APPNAME "WebView Keyboard Launcher"
!define COMPANYNAME "SezginBilge"
!define DESCRIPTION "Virtual keyboard launcher with WebView2 and Kiosk Mode"
!define VERSIONMAJOR 1
!define VERSIONMINOR 0
!define VERSIONBUILD 0

RequestExecutionLevel admin
InstallDir "$PROGRAMFILES64\${APPNAME}"
Name "${APPNAME}"
outFile "WebViewKeyboardLauncher_Setup.exe"

; Modern UI includes
!include MUI2.nsh
!include LogicLib.nsh
!include nsDialogs.nsh
!include FileFunc.nsh

; Modern UI settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Custom install types
InstType "Standard Installation"
InstType "Kiosk Mode (Auto-Start, Auto-Login)"
InstType "Portable Installation"

; Pages
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
Page custom nsDialogsPageCreate nsDialogsPageLeave ; Custom configuration page
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
Page custom FinishPageCreate FinishPageLeave ; Custom finish page

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Variables for custom configuration
Var Dialog
Var UrlTextBox
Var UrlValue
Var StartWithWindowsCheckbox
Var StartWithWindowsValue
Var KioskModeCheckbox
Var KioskModeValue
Var AutoLoginCheckbox
Var AutoLoginValue
Var DisableTaskbarCheckbox
Var DisableTaskbarValue
Var FullscreenCheckbox
Var FullscreenValue

; Custom configuration page
Function nsDialogsPageCreate
    nsDialogs::Create 1018
    Pop $Dialog

    ${If} $Dialog == error
        Abort
    ${EndIf}

    ; Title
    ${NSD_CreateLabel} 10 10 350 20 "WebView Keyboard Launcher Configuration"
    Pop $0
    CreateFont $1 "Segoe UI" 12 700
    SendMessage $0 ${WM_SETFONT} $1 0
    
    ; URL Configuration Section
    ${NSD_CreateGroupBox} 10 40 350 80 "Homepage Configuration"
    Pop $0
    
    ${NSD_CreateLabel} 20 60 100 15 "Homepage URL:"
    Pop $0
    
    ${NSD_CreateText} 20 75 320 20 "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome"
    Pop $UrlTextBox
    
    ; Startup Options Section
    ${NSD_CreateGroupBox} 10 130 350 120 "Startup Options"
    Pop $0
    
    ${NSD_CreateCheckbox} 20 150 320 15 "Start with Windows (recommended)"
    Pop $StartWithWindowsCheckbox
    ${NSD_Check} $StartWithWindowsCheckbox ; Default checked
    
    ${NSD_CreateCheckbox} 20 170 320 15 "Enable Kiosk Mode (locks down system)"
    Pop $KioskModeCheckbox
    
    ${NSD_CreateCheckbox} 20 190 320 15 "Auto-login current user (requires Kiosk Mode)"
    Pop $AutoLoginCheckbox
    
    ${NSD_CreateCheckbox} 20 210 320 15 "Hide taskbar and disable system shortcuts"
    Pop $DisableTaskbarCheckbox
    
    ${NSD_CreateCheckbox} 20 230 320 15 "Force fullscreen mode"
    Pop $FullscreenCheckbox
    
    ; Description
    ${NSD_CreateLabel} 10 260 350 60 "Kiosk Mode: Automatically starts the application on system boot, disables user switching, and provides a locked-down environment. Recommended for dedicated kiosk machines."
    Pop $0

    ; Set up checkbox dependencies
    ${NSD_OnClick} $KioskModeCheckbox KioskModeChanged

    nsDialogs::Show
FunctionEnd

Function KioskModeChanged
    Pop $0 ; checkbox handle
    ${NSD_GetState} $KioskModeCheckbox $1
    
    ${If} $1 == 1
        ; Enable dependent options
        EnableWindow $AutoLoginCheckbox 1
        EnableWindow $DisableTaskbarCheckbox 1
        EnableWindow $FullscreenCheckbox 1
        
        ; Check them by default
        ${NSD_Check} $DisableTaskbarCheckbox
        ${NSD_Check} $FullscreenCheckbox
    ${Else}
        ; Disable and uncheck dependent options
        EnableWindow $AutoLoginCheckbox 0
        EnableWindow $DisableTaskbarCheckbox 0
        EnableWindow $FullscreenCheckbox 0
        
        ${NSD_Uncheck} $AutoLoginCheckbox
        ${NSD_Uncheck} $DisableTaskbarCheckbox
        ${NSD_Uncheck} $FullscreenCheckbox
    ${EndIf}
FunctionEnd

Function nsDialogsPageLeave
    ${NSD_GetText} $UrlTextBox $UrlValue
    ${NSD_GetState} $StartWithWindowsCheckbox $StartWithWindowsValue
    ${NSD_GetState} $KioskModeCheckbox $KioskModeValue
    ${NSD_GetState} $AutoLoginCheckbox $AutoLoginValue
    ${NSD_GetState} $DisableTaskbarCheckbox $DisableTaskbarValue
    ${NSD_GetState} $FullscreenCheckbox $FullscreenValue
    
    ; Validate URL
    ${If} $UrlValue == ""
        MessageBox MB_OK "Please enter a valid URL."
        Abort
    ${EndIf}
    
    ; Warning for Kiosk Mode
    ${If} $KioskModeValue == 1
        MessageBox MB_YESNO|MB_ICONQUESTION "Kiosk Mode will lock down this system and disable normal user access. Are you sure you want to continue?$\n$\nThis is recommended only for dedicated kiosk machines." IDYES continue IDNO abort
        abort:
            Abort
        continue:
    ${EndIf}
FunctionEnd

; Finish page
Function FinishPageCreate
    nsDialogs::Create 1018
    Pop $Dialog

    ${NSD_CreateLabel} 10 10 350 20 "Installation completed successfully!"
    Pop $0
    CreateFont $1 "Segoe UI" 12 700
    SendMessage $0 ${WM_SETFONT} $1 0
    
    ${If} $KioskModeValue == 1
        ${NSD_CreateLabel} 10 40 350 80 "Kiosk Mode has been configured. The system will automatically start the application on boot and provide a locked-down environment.$\n$\nPlease restart the system to activate Kiosk Mode."
        Pop $0
        
        ; Auto restart checkbox for kiosk
        ${NSD_CreateCheckbox} 10 130 350 15 "Restart system now to activate Kiosk Mode"
        Pop $0
        ${NSD_Check} $0 ; Default checked for kiosk
    ${Else}
        ${NSD_CreateLabel} 10 40 350 60 "WebView Keyboard Launcher has been installed. The application will start automatically when Windows starts (if selected)."
        Pop $0
        
        ; Start now checkbox
        ${NSD_CreateCheckbox} 10 110 350 15 "Start WebView Keyboard Launcher now"
        Pop $0
        ${NSD_Check} $0 ; Default checked
    ${EndIf}

    nsDialogs::Show
FunctionEnd

Function FinishPageLeave
    ; Check if user wants to start now or restart
    ${NSD_GetState} $0 $1
    
    ${If} $KioskModeValue == 1
        ${If} $1 == 1
            ; Restart for kiosk mode
            ExecWait 'shutdown /r /t 5 /c "Restarting to activate Kiosk Mode..."'
        ${EndIf}
    ${Else}
        ${If} $1 == 1
            ; Start application now
            Exec "$INSTDIR\WebViewKeyboardLauncher.exe"
        ${EndIf}
    ${EndIf}
FunctionEnd

; Main installation section
Section "Core Application" SecCore
    SectionIn 1 2 3 RO ; Required in all install types
    
    SetOutPath $INSTDIR
    
    ; Application files
    File "${SOURCE_DIR}\WebViewKeyboardLauncher.exe"
    File /nonfatal "${SOURCE_DIR}\*.dll"
    File /nonfatal "${SOURCE_DIR}\*.json"
    
    ; Registry - Application settings
    WriteRegStr HKCU "Software\WebViewKeyboardLauncher" "Homepage" $UrlValue
    WriteRegStr HKCU "Software\WebViewKeyboardLauncher" "InstallPath" $INSTDIR
    WriteRegStr HKCU "Software\WebViewKeyboardLauncher" "Version" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    WriteRegDWORD HKCU "Software\WebViewKeyboardLauncher" "KioskMode" $KioskModeValue
    WriteRegDWORD HKCU "Software\WebViewKeyboardLauncher" "DisableTaskbar" $DisableTaskbarValue
    WriteRegDWORD HKCU "Software\WebViewKeyboardLauncher" "Fullscreen" $FullscreenValue
    
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
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "VersionMajor" ${VERSIONMAJOR}
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "VersionMinor" ${VERSIONMINOR}
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
SectionEnd

Section "Start Menu Shortcuts" SecShortcuts
    SectionIn 1 ; Only in standard installation
    
    CreateDirectory "$SMPROGRAMS\${COMPANYNAME}"
    CreateShortCut "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk" "$INSTDIR\WebViewKeyboardLauncher.exe"
    CreateShortCut "$SMPROGRAMS\${COMPANYNAME}\Uninstall ${APPNAME}.lnk" "$INSTDIR\uninstall.exe"
    CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\WebViewKeyboardLauncher.exe"
SectionEnd

Section "Auto Start with Windows" SecAutoStart
    SectionIn 1 2 ; Standard and Kiosk installations
    
    ; Only if user selected this option or kiosk mode
    ${If} $StartWithWindowsValue == 1
    ${OrIf} $KioskModeValue == 1
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher" '"$INSTDIR\WebViewKeyboardLauncher.exe"'
    ${EndIf}
SectionEnd

Section "Kiosk Mode Configuration" SecKiosk
    SectionIn 2 ; Only in Kiosk installation
    
    ; Only configure if Kiosk Mode is enabled
    ${If} $KioskModeValue == 1
        ; Create kiosk user account (optional)
        ; ExecWait 'net user kioskuser /add /passwordreq:no /comment:"Kiosk Mode User"'
        ; ExecWait 'net localgroup users kioskuser /delete'
        
        ; Disable Ctrl+Alt+Del
        WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Policies\System" "DisableCAD" 1
        
        ; Disable Task Manager
        WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Policies\System" "DisableTaskMgr" 1
        
        ; Auto-login configuration
        ${If} $AutoLoginValue == 1
            ; Get current username
            ReadEnvStr $1 "USERNAME"
            WriteRegStr HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "AutoAdminLogon" "1"
            WriteRegStr HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultUserName" "$1"
            WriteRegStr HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultPassword" ""
        ${EndIf}
        
        ; Disable Windows key combinations
        WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoWinKeys" 1
        
        ; Hide taskbar
        ${If} $DisableTaskbarValue == 1
            WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3" "Settings" 0x02000003
            WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" "TaskbarAutoHideInTabletMode" 1
        ${EndIf}
        
        ; Set shell to our application (full kiosk mode)
        ; WriteRegStr HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "Shell" "$INSTDIR\WebViewKeyboardLauncher.exe"
        
        ; Create startup script for kiosk mode
        FileOpen $0 "$INSTDIR\KioskStartup.bat" w
        FileWrite $0 "@echo off$\r$\n"
        FileWrite $0 "REM Kiosk Mode Startup Script$\r$\n"
        FileWrite $0 "REM Auto-generated by WebView Keyboard Launcher installer$\r$\n"
        FileWrite $0 "$\r$\n"
        FileWrite $0 "REM Kill explorer to prevent desktop access$\r$\n"
        FileWrite $0 "taskkill /f /im explorer.exe 2>nul$\r$\n"
        FileWrite $0 "$\r$\n"
        FileWrite $0 "REM Start our application$\r$\n"
        FileWrite $0 'start "" "$INSTDIR\WebViewKeyboardLauncher.exe"$\r$\n'
        FileWrite $0 "$\r$\n"
        FileWrite $0 "REM Keep running and restart if closed$\r$\n"
        FileWrite $0 ":loop$\r$\n"
        FileWrite $0 "timeout /t 5 >nul$\r$\n"
        FileWrite $0 "tasklist | find /i WebViewKeyboardLauncher.exe >nul$\r$\n"
        FileWrite $0 "if errorlevel 1 ($\r$\n"
        FileWrite $0 '    start "" "$INSTDIR\WebViewKeyboardLauncher.exe"$\r$\n'
        FileWrite $0 ")$\r$\n"
        FileWrite $0 "goto loop$\r$\n"
        FileClose $0
        
        ; Add kiosk startup script to run
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "KioskMode" '"$INSTDIR\KioskStartup.bat"'
    ${EndIf}
SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} "Core application files (required)"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecShortcuts} "Start menu and desktop shortcuts"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecAutoStart} "Automatically start with Windows"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecKiosk} "Configure system for kiosk mode operation"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; Uninstaller
Section "Uninstall"
    ; Remove kiosk mode configurations
    DeleteRegValue HKLM "Software\Microsoft\Windows\CurrentVersion\Policies\System" "DisableCAD"
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Policies\System" "DisableTaskMgr"
    DeleteRegValue HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "AutoAdminLogon"
    DeleteRegValue HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultUserName"
    DeleteRegValue HKLM "Software\Microsoft\Windows NT\CurrentVersion\Winlogon" "DefaultPassword"
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoWinKeys"
    
    ; Remove startup entries
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher"
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "KioskMode"
    
    ; Remove application files
    Delete "$INSTDIR\WebViewKeyboardLauncher.exe"
    Delete "$INSTDIR\*.dll"
    Delete "$INSTDIR\*.json"
    Delete "$INSTDIR\KioskStartup.bat"
    Delete "$INSTDIR\uninstall.exe"
    RMDir /r "$INSTDIR"
    
    ; Remove shortcuts
    Delete "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk"
    Delete "$SMPROGRAMS\${COMPANYNAME}\Uninstall ${APPNAME}.lnk"
    RMDir "$SMPROGRAMS\${COMPANYNAME}"
    Delete "$DESKTOP\${APPNAME}.lnk"
    
    ; Remove registry entries
    DeleteRegKey HKCU "Software\WebViewKeyboardLauncher"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
    
    ; Restart explorer (in case it was killed for kiosk mode)
    Exec "explorer.exe"
SectionEnd

; Command line support for automated kiosk deployment
Function .onInit
    ; Check command line parameters
    ${GetParameters} $R0
    
    ; Silent install with URL parameter
    ; Usage: setup.exe /S /URL=https://example.com /KIOSK=1 /AUTOLOGIN=1
    ClearErrors
    ${GetOptions} $R0 "/URL=" $UrlValue
    ${If} ${Errors}
        StrCpy $UrlValue "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome"
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
    
    ; Auto login parameter
    ClearErrors
    ${GetOptions} $R0 "/AUTOLOGIN=" $AutoLoginValue
    ${If} ${Errors}
        StrCpy $AutoLoginValue "0"
    ${EndIf}
    
    ; Disable taskbar parameter
    ClearErrors
    ${GetOptions} $R0 "/HIDETASKBAR=" $DisableTaskbarValue
    ${If} ${Errors}
        StrCpy $DisableTaskbarValue "0"
    ${EndIf}
    
    ; Fullscreen parameter
    ClearErrors
    ${GetOptions} $R0 "/FULLSCREEN=" $FullscreenValue
    ${If} ${Errors}
        StrCpy $FullscreenValue "0"
    ${EndIf}
FunctionEnd