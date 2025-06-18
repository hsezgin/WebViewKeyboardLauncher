; WebView Keyboard Launcher
!define APPNAME "WebView Keyboard Launcher"
!define COMPANYNAME "SezginBilge"
!define DESCRIPTION "Virtual keyboard launcher with WebView2"
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

; Custom URL configuration page
Function nsDialogsPageCreate
    nsDialogs::Create 1018
    Pop $Dialog

    ${If} $Dialog == error
        Abort
    ${EndIf}

    ; Title
    ${NSD_CreateLabel} 10 10 280 20 "Homepage URL Configuration"
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
    
    ; Description
    ${NSD_CreateLabel} 10 120 350 40 "The application will open this URL when started. You can change this later by reinstalling or modifying the Windows Registry."
    Pop $0

    nsDialogs::Show
FunctionEnd

Function nsDialogsPageLeave
    ${NSD_GetText} $UrlTextBox $UrlValue
    ${NSD_GetState} $StartWithWindowsCheckbox $StartWithWindowsValue
    
    ; Validate URL
    ${If} $UrlValue == ""
        MessageBox MB_OK "Please enter a valid URL."
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
    Pop $0
    ${NSD_Check} $0 ; Default checked

    nsDialogs::Show
FunctionEnd

Function FinishPageLeave
    ; Check if user wants to start now
    ${NSD_GetState} $0 $1
    ${If} $1 == 1
        Exec "$INSTDIR\WebViewKeyboardLauncher.exe"
    ${EndIf}
FunctionEnd

; Main installation section
Section "Core Application" SecCore
    SectionIn 1 2 RO ; Required in all install types
    
    SetOutPath $INSTDIR
    
    ; Application files
    File "${SOURCE_DIR}\WebViewKeyboardLauncher.exe"
    File /nonfatal "${SOURCE_DIR}\*.dll"
    File /nonfatal "${SOURCE_DIR}\*.json"
    
    ; Registry - Homepage URL
    WriteRegStr HKCU "Software\WebViewKeyboardLauncher" "Homepage" $UrlValue
    WriteRegStr HKCU "Software\WebViewKeyboardLauncher" "InstallPath" $INSTDIR
    WriteRegStr HKCU "Software\WebViewKeyboardLauncher" "Version" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    
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
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher" '"$INSTDIR\WebViewKeyboardLauncher.exe"'
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
    ; Remove startup entry
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "WebViewKeyboardLauncher"
    
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
    DeleteRegKey HKCU "Software\WebViewKeyboardLauncher"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
SectionEnd

; Command line support
Function .onInit
    ; Check command line parameters
    ${GetParameters} $R0
    
    ; Silent install with URL parameter
    ; Usage: setup.exe /S /URL=https://example.com
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
FunctionEnd