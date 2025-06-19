param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\dist",
    [switch]$SkipBuild,
    [switch]$SkipSetup,
    [switch]$OpenOutput,
    [switch]$KioskOnly,
    [string]$KioskUrl = "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome"
)

# Build and Package Script for WebView Keyboard Launcher with Kiosk Mode
Write-Host "===============================================" -ForegroundColor Green
Write-Host "  WebView Keyboard Launcher - Build & Package" -ForegroundColor Green
Write-Host "           🖥️  KIOSK MODE SUPPORT  🖥️" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Stop"
$ProjectName = "WebViewKeyboardLauncher"
$ProjectPath = ".\$ProjectName\$ProjectName.csproj"
$NSISScript = ".\WebViewKeyboardLauncher_Setup.nsi"
$NSISKioskScript = ".\WebViewKeyboardLauncher_Setup_Kiosk.nsi"
$NSISPath = "${env:ProgramFiles(x86)}\NSIS\makensis.exe"
$PublishDir = ".\$ProjectName\bin\$Configuration\net8.0-windows"

# Validate paths
if (!(Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 1
}

if (!(Test-Path $NSISKioskScript)) {
    Write-Error "NSIS Kiosk script not found: $NSISKioskScript"
    exit 1
}

if (!(Test-Path $NSISPath)) {
    Write-Error "NSIS not found. Please install NSIS from https://nsis.sourceforge.io/"
    exit 1
}

# Create output directory
Write-Host "📁 Creating output directory..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Step 1: Clean and Build
if (!$SkipBuild) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    try {
        dotnet clean $ProjectPath -c $Configuration --verbosity minimal
        Write-Host "✅ Clean completed" -ForegroundColor Green
    }
    catch {
        Write-Error "Clean failed: $($_.Exception.Message)"
        exit 1
    }

    Write-Host "🔨 Building project..." -ForegroundColor Yellow
    try {
        dotnet publish $ProjectPath -c $Configuration -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true -o $PublishDir
        Write-Host "✅ Build completed" -ForegroundColor Green
    }
    catch {
        Write-Error "Build failed: $($_.Exception.Message)"
        exit 1
    }

    # Verify build output
    $BuildOutput = ".\$ProjectName\bin\$Configuration\net8.0-windows\$ProjectName.exe"
    if (!(Test-Path $BuildOutput)) {
        Write-Error "Build output not found: $BuildOutput"
        exit 1
    }
    
    $FileInfo = Get-Item $BuildOutput
    Write-Host "   📦 Output: $($FileInfo.Name) ($([math]::Round($FileInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
}

# Step 2: Create Kiosk-specific Packages
if (!$KioskOnly) {
    # Create standard portable package
    Write-Host "📦 Creating standard portable package..." -ForegroundColor Yellow
    $PortableDir = "$OutputDir\Portable-Standard"
    New-Item -ItemType Directory -Path $PortableDir -Force | Out-Null

    $SourceDir = ".\$ProjectName\bin\$Configuration\net8.0-windows"
    Copy-Item "$SourceDir\$ProjectName.exe" -Destination $PortableDir
    Copy-Item "$SourceDir\*.dll" -Destination $PortableDir -ErrorAction SilentlyContinue
    Copy-Item "$SourceDir\*.json" -Destination $PortableDir -ErrorAction SilentlyContinue

    # Standard launcher
    $StandardLauncher = @"
@echo off
echo WebView Keyboard Launcher - Standard Edition
echo ==========================================
start "" "$ProjectName.exe"
"@
    $StandardLauncher | Out-File -FilePath "$PortableDir\Start.bat" -Encoding ASCII
}

# Create Kiosk portable package
Write-Host "🖥️  Creating kiosk portable package..." -ForegroundColor Cyan
$KioskPortableDir = "$OutputDir\Portable-Kiosk"
New-Item -ItemType Directory -Path $KioskPortableDir -Force | Out-Null

$SourceDir = ".\$ProjectName\bin\$Configuration\net8.0-windows"
Copy-Item "$SourceDir\$ProjectName.exe" -Destination $KioskPortableDir
Copy-Item "$SourceDir\*.dll" -Destination $KioskPortableDir -ErrorAction SilentlyContinue
Copy-Item "$SourceDir\*.json" -Destination $KioskPortableDir -ErrorAction SilentlyContinue

# Kiosk launcher script
$KioskLauncher = @"
@echo off
echo WebView Keyboard Launcher - KIOSK MODE
echo ====================================
echo.
echo WARNING: This will start the system in kiosk mode!
echo Press Ctrl+C to cancel, or any other key to continue...
pause >nul

echo.
echo Configuring kiosk mode...

REM Create kiosk registry entries
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "Homepage" /t REG_SZ /d "$KioskUrl" /f >nul 2>&1
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "KioskMode" /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "Fullscreen" /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "DisableTaskbar" /t REG_DWORD /d 1 /f >nul 2>&1

REM Disable system shortcuts
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System" /v "DisableTaskMgr" /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" /v "NoWinKeys" /t REG_DWORD /d 1 /f >nul 2>&1

REM Add to startup
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "WebViewKeyboardLauncher" /t REG_SZ /d "%~dp0$ProjectName.exe" /f >nul 2>&1

echo Kiosk mode configured!
echo.
echo Starting application...
echo.
echo EMERGENCY EXIT: Press Ctrl+Shift+Alt+E in the application
echo.

REM Kill explorer to prevent desktop access
taskkill /f /im explorer.exe 2>nul

REM Start our application
start "" "$ProjectName.exe"

REM Monitor and restart if closed
:loop
timeout /t 5 >nul
tasklist | find /i "$ProjectName.exe" >nul
if errorlevel 1 (
    start "" "$ProjectName.exe"
)
goto loop
"@

$KioskLauncher | Out-File -FilePath "$KioskPortableDir\Start-Kiosk.bat" -Encoding ASCII

# Kiosk exit script
$KioskExit = @"
@echo off
echo Exiting Kiosk Mode...
echo ===================

REM Kill the application
taskkill /f /im "$ProjectName.exe" 2>nul

REM Remove kiosk registry entries
reg delete "HKCU\Software\WebViewKeyboardLauncher" /v "KioskMode" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System" /v "DisableTaskMgr" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" /v "NoWinKeys" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "WebViewKeyboardLauncher" /f >nul 2>&1

REM Restart explorer
start explorer.exe

echo Kiosk mode disabled. System restored to normal operation.
pause
"@

$KioskExit | Out-File -FilePath "$KioskPortableDir\Exit-Kiosk.bat" -Encoding ASCII

# Kiosk README
$KioskReadme = @"
# WebView Keyboard Launcher - KIOSK MODE

⚠️  **WARNING: KIOSK MODE WILL LOCK DOWN YOUR SYSTEM**

## Quick Start (KIOSK)
1. **BACKUP YOUR SYSTEM FIRST!**
2. Run **Start-Kiosk.bat** as Administrator
3. System will enter kiosk mode automatically
4. To exit: Press **Ctrl+Shift+Alt+E** in application, then run **Exit-Kiosk.bat**

## Emergency Recovery
If system gets locked:
1. Boot from Windows installation media
2. Access Command Prompt
3. Navigate to this folder
4. Run: **Exit-Kiosk.bat**

## Kiosk Features
- 🔒 **Full system lockdown**
- 🚫 **Disabled taskbar, start menu, Alt+Tab**
- 🛡️ **Task Manager blocked**
- 🔄 **Auto-restart if application closes**
- 🖥️ **Fullscreen forced mode**

## Configuration
- **Homepage URL**: $KioskUrl
- **Emergency Exit**: Ctrl+Shift+Alt+E
- **Auto-restart**: Enabled

## System Requirements
- Windows 10/11 (x64) with Administrator rights
- .NET 8.0 Runtime
- Microsoft WebView2 Runtime

## ⚠️ IMPORTANT SAFETY NOTES
- **Test in a virtual machine first**
- **Have a recovery plan ready**
- **Only use on dedicated kiosk machines**
- **Do not use on production workstations**

## Version: $Version
## License: Apache 2.0

---
**USE AT YOUR OWN RISK - ALWAYS TEST FIRST!**
"@

$KioskReadme | Out-File -FilePath "$KioskPortableDir\README-KIOSK.md" -Encoding UTF8

# Compress kiosk package
Write-Host "🗜️  Compressing kiosk package..." -ForegroundColor Cyan
$KioskZip = "$OutputDir\WebViewKeyboardLauncher-$Version-Kiosk-Portable.zip"
Compress-Archive -Path "$KioskPortableDir\*" -DestinationPath $KioskZip -Force
$KioskZipInfo = Get-Item $KioskZip
Write-Host "   📦 Created: $($KioskZipInfo.Name) ($([math]::Round($KioskZipInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan

# Compress standard package if not kiosk-only
if (!$KioskOnly) {
    Write-Host "🗜️  Compressing standard package..." -ForegroundColor Yellow
    $StandardZip = "$OutputDir\WebViewKeyboardLauncher-$Version-Standard-Portable.zip"
    Compress-Archive -Path "$PortableDir\*" -DestinationPath $StandardZip -Force
    $StandardZipInfo = Get-Item $StandardZip
    Write-Host "   📦 Created: $($StandardZipInfo.Name) ($([math]::Round($StandardZipInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
}

# Step 3: Create NSIS Installers
if (!$SkipSetup) {
    # Create Kiosk Installer
    Write-Host "🔧 Creating KIOSK NSIS installer..." -ForegroundColor Cyan
    try {
        $ResolvedSourceDir = Resolve-Path ".\$ProjectName\bin\$Configuration\net8.0-windows"
        $NSISContent = Get-Content $NSISKioskScript -Raw
        $NSISContent = "!define SOURCE_DIR `"$ResolvedSourceDir`"`n" + $NSISContent
        $NSISContent = $NSISContent -replace '!define VERSIONMAJOR \d+', "!define VERSIONMAJOR $($Version.Split('.')[0])"
        $NSISContent = $NSISContent -replace '!define VERSIONMINOR \d+', "!define VERSIONMINOR $($Version.Split('.')[1])"
        $NSISContent = $NSISContent -replace '!define VERSIONBUILD \d+', "!define VERSIONBUILD $($Version.Split('.')[2])"
        
        $TempNSISKiosk = "$env:TEMP\WebViewKeyboardLauncher_Setup_Kiosk_Temp.nsi"
        $NSISContent | Out-File -FilePath $TempNSISKiosk -Encoding UTF8

        # Copy license file
        if (Test-Path "LICENSE.txt") {
            Copy-Item "LICENSE.txt" -Destination (Split-Path $TempNSISKiosk)
        }
        
        # Compile NSIS Kiosk
        & $NSISPath $TempNSISKiosk
        
        # Move to output directory
        $TempKioskExePath = "$env:TEMP\WebViewKeyboardLauncher_Setup.exe"
        if (Test-Path $TempKioskExePath) {
            $KioskSetupFile = "$OutputDir\WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe"
            Move-Item $TempKioskExePath $KioskSetupFile -Force
            
            $KioskSetupInfo = Get-Item $KioskSetupFile
            Write-Host "✅ KIOSK installer created" -ForegroundColor Green
            Write-Host "   📦 Setup: $($KioskSetupInfo.Name) ($([math]::Round($KioskSetupInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
        }
        
        Remove-Item $TempNSISKiosk -ErrorAction SilentlyContinue
    }
    catch {
        Write-Error "NSIS Kiosk compilation failed: $($_.Exception.Message)"
        exit 1
    }

    # Create Standard Installer (if not kiosk-only)
    if (!$KioskOnly) {
        Write-Host "🔧 Creating STANDARD NSIS installer..." -ForegroundColor Yellow
        try {
            $NSISStandardContent = Get-Content $NSISScript -Raw
            $NSISStandardContent = "!define SOURCE_DIR `"$ResolvedSourceDir`"`n" + $NSISStandardContent
            $NSISStandardContent = $NSISStandardContent -replace '!define VERSIONMAJOR \d+', "!define VERSIONMAJOR $($Version.Split('.')[0])"
            $NSISStandardContent = $NSISStandardContent -replace '!define VERSIONMINOR \d+', "!define VERSIONMINOR $($Version.Split('.')[1])"
            $NSISStandardContent = $NSISStandardContent -replace '!define VERSIONBUILD \d+', "!define VERSIONBUILD $($Version.Split('.')[2])"
            
            $TempNSISStandard = "$env:TEMP\WebViewKeyboardLauncher_Setup_Standard_Temp.nsi"
            $NSISStandardContent | Out-File -FilePath $TempNSISStandard -Encoding UTF8
            
            # Compile NSIS Standard
            & $NSISPath $TempNSISStandard
            
            # Move to output directory
            $TempStandardExePath = "$env:TEMP\WebViewKeyboardLauncher_Setup.exe"
            if (Test-Path $TempStandardExePath) {
                $StandardSetupFile = "$OutputDir\WebViewKeyboardLauncher-$Version-Standard-Setup.exe"
                Move-Item $TempStandardExePath $StandardSetupFile -Force
                
                $StandardSetupInfo = Get-Item $StandardSetupFile
                Write-Host "✅ STANDARD installer created" -ForegroundColor Green
                Write-Host "   📦 Setup: $($StandardSetupInfo.Name) ($([math]::Round($StandardSetupInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
            }
            
            Remove-Item $TempNSISStandard -ErrorAction SilentlyContinue
        }
        catch {
            Write-Warning "Standard installer creation failed: $($_.Exception.Message)"
        }
    }
}

# Step 4: Create Deployment Scripts
Write-Host "📝 Creating deployment scripts..." -ForegroundColor Yellow

# Silent Kiosk Deployment Script
$SilentKioskDeploy = @"
@echo off
REM Silent Kiosk Deployment Script
REM Usage: Deploy-Kiosk.bat [URL] [AutoRestart]
REM Example: Deploy-Kiosk.bat https://example.com 1

set URL=%1
set AUTORESTART=%2

if "%URL%"=="" set URL=$KioskUrl
if "%AUTORESTART%"=="" set AUTORESTART=0

echo ========================================
echo  WebView Keyboard Launcher - Kiosk Deploy
echo ========================================
echo.
echo URL: %URL%
echo Auto-restart: %AUTORESTART%
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script requires Administrator privileges!
    echo Please run as Administrator.
    pause
    exit /b 1
)

echo Installing kiosk mode...

REM Run silent installation
if exist "WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe" (
    WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe /S /KIOSK=1 /AUTOLOGIN=1 /HIDETASKBAR=1 /FULLSCREEN=1 /URL=%URL%
    echo Installation completed.
    
    if "%AUTORESTART%"=="1" (
        echo System will restart in 10 seconds...
        shutdown /r /t 10 /c "Restarting to activate Kiosk Mode"
    ) else (
        echo Please restart the system to activate Kiosk Mode.
        pause
    )
) else (
    echo ERROR: Kiosk setup file not found!
    echo Please ensure WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe is in the same directory.
    pause
    exit /b 1
)
"@

$SilentKioskDeploy | Out-File -FilePath "$OutputDir\Deploy-Kiosk.bat" -Encoding ASCII

# Quick Kiosk Test Script
$QuickKioskTest = @"
@echo off
echo ========================================
echo  KIOSK MODE - QUICK TEST (VM RECOMMENDED)
echo ========================================
echo.
echo ⚠️  WARNING: This will configure kiosk mode!
echo    Only use on test systems or VMs!
echo.
echo Press Ctrl+C to cancel, or
pause

REM Temporary kiosk mode (no installation)
echo Setting up temporary kiosk mode...

REM Create temp config
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "Homepage" /t REG_SZ /d "$KioskUrl" /f >nul 2>&1
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "KioskMode" /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\WebViewKeyboardLauncher" /v "Fullscreen" /t REG_DWORD /d 1 /f >nul 2>&1

echo Starting kiosk test...
echo Emergency exit: Ctrl+Shift+Alt+E
echo.

REM Start application
if exist "Portable-Kiosk\WebViewKeyboardLauncher.exe" (
    cd Portable-Kiosk
    WebViewKeyboardLauncher.exe
    cd ..
) else (
    echo ERROR: Portable kiosk files not found!
    pause
    exit /b 1
)

REM Cleanup after test
echo Cleaning up test configuration...
reg delete "HKCU\Software\WebViewKeyboardLauncher" /v "KioskMode" /f >nul 2>&1
echo Test completed.
pause
"@

$QuickKioskTest | Out-File -FilePath "$OutputDir\Test-Kiosk.bat" -Encoding ASCII

# Step 5: Create Enhanced Release Notes
Write-Host "📝 Creating release notes..." -ForegroundColor Yellow
$ReleaseNotes = @"
# WebView Keyboard Launcher v$Version
## 🖥️ **KIOSK MODE EDITION** 🖥️

### Release Date: $(Get-Date -Format 'yyyy-MM-dd')

## 🆕 NEW: Kiosk Mode Features
- 🔒 **Full System Lockdown** - Disables system shortcuts, taskbar, and user switching
- 🖥️ **Fullscreen Force Mode** - Application takes over entire screen
- 🛡️ **Security Controls** - Blocks Task Manager, Windows keys, Alt+Tab
- 🔄 **Auto-restart Protection** - Application restarts if closed
- 🚨 **Emergency Exit** - Ctrl+Shift+Alt+E for system recovery
- ⚡ **Auto-login Support** - Automatic user login on system boot

## 📦 Installation Options

### 🖥️ **KIOSK MODE Installer (NEW)**
- **File**: WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe
- **Size**: ~$(if (Test-Path "$OutputDir\WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe") { [math]::Round((Get-Item "$OutputDir\WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe").Length / 1KB, 1) } else { "XXX" }) KB
- **Features**: Full kiosk lockdown, auto-start, system integration
- **Usage**: Run as Administrator
- **⚠️ WARNING**: Only for dedicated kiosk machines!

### 📦 Standard Installer
$(if (!$KioskOnly) { 
"- **File**: WebViewKeyboardLauncher-$Version-Standard-Setup.exe
- **Features**: Normal mode, optional auto-start
- **Usage**: Standard installation"
} else {
"- Not included in kiosk-only build"
})

### 🎒 Portable Editions
#### Kiosk Portable
- **File**: WebViewKeyboardLauncher-$Version-Kiosk-Portable.zip
- **Size**: ~$([math]::Round((Get-Item "$OutputDir\WebViewKeyboardLauncher-$Version-Kiosk-Portable.zip").Length / 1KB, 1)) KB
- **Features**: Portable kiosk mode with batch scripts
- **Usage**: Extract and run Start-Kiosk.bat as Administrator

$(if (!$KioskOnly) {
"#### Standard Portable
- **File**: WebViewKeyboardLauncher-$Version-Standard-Portable.zip
- **Features**: Normal portable mode
- **Usage**: Extract and run Start.bat"
})

## 🚀 Quick Deployment

### Automated Kiosk Deployment
``````bash
# Deploy with custom URL and auto-restart
Deploy-Kiosk.bat https://your-kiosk-url.com 1

# Deploy with default settings
Deploy-Kiosk.bat
``````

### Silent Installation
``````bash
# Full kiosk setup
WebViewKeyboardLauncher-$Version-Kiosk-Setup.exe /S /KIOSK=1 /AUTOLOGIN=1 /HIDETASKBAR=1 /FULLSCREEN=1 /URL=https://example.com

# Standard installation  
WebViewKeyboardLauncher-$Version-Standard-Setup.exe /S /URL=https://example.com
``````

### Testing (VM Recommended)
``````bash
# Quick kiosk test (temporary mode)
Test-Kiosk.bat
``````

## ⚠️ **KIOSK MODE SAFETY**

### Before Deployment:
1. **Test in a Virtual Machine first**
2. **Backup your system**
3. **Have recovery media ready**
4. **Ensure you have admin access**

### Emergency Recovery:
1. **In Application**: Ctrl+Shift+Alt+E
2. **Portable Mode**: Run Exit-Kiosk.bat
3. **Boot Recovery**: Use Windows installation media

### Recovery Registry Commands:
``````cmd
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System" /v "DisableTaskMgr" /f
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" /v "NoWinKeys" /f
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "WebViewKeyboardLauncher" /f
``````

## 🔧 Application Features
- 🎛️ **Floating Toolbar** - Draggable virtual keyboard launcher
- ⌨️ **TabTip Integration** - Windows virtual keyboard support
- 🌐 **WebView2** - Modern web rendering with customizable homepage
- 🔄 **Settings Panel** - Refresh (2s hold = homepage) and restart (3s hold)
- 📱 **Touch-friendly UI** - Optimized for touch screen kiosks

## 🖥️ System Requirements
- **Windows 10/11** (x64)
- **.NET 8.0 Runtime**
- **Microsoft WebView2 Runtime**
- **Administrator Rights** (for kiosk mode)

## 🎯 Use Cases
- **Information Kiosks** - Public information displays
- **Digital Signage** - Advertising and promotional displays  
- **Interactive Terminals** - Customer service points
- **Education Kiosks** - School and library terminals
- **Retail Displays** - Product demonstration stations

## 📄 License
Apache License 2.0

## 👨‍💻 Developer
SezginBilge

---
**⚠️ KIOSK MODE DISCLAIMER**
Kiosk mode will significantly restrict system functionality. Only use on dedicated kiosk machines. Always test thoroughly before production deployment. The developer is not responsible for system lockouts or configuration issues.

Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@

$ReleaseNotes | Out-File -FilePath "$OutputDir\RELEASE-NOTES-KIOSK.md" -Encoding UTF8

# Step 6: Build Summary
Write-Host ""
Write-Host "🎉 Kiosk Mode Build Completed!" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Green

Write-Host ""
Write-Host "📦 PACKAGES CREATED:" -ForegroundColor Cyan
Get-ChildItem $OutputDir -File | ForEach-Object {
    $Size = "$([math]::Round($_.Length / 1KB, 1)) KB"
    $Icon = switch -Wildcard ($_.Name) {
        "*Kiosk*Setup*" { "🖥️ " }
        "*Kiosk*Portable*" { "📱 " }
        "*Standard*" { "📦 " }
        "*.bat" { "⚡ " }
        "*.md" { "📝 " }
        default { "📁 " }
    }
    Write-Host "   $Icon$($_.Name) ($Size)" -ForegroundColor White
}

Write-Host ""
Write-Host "🚀 DEPLOYMENT SCRIPTS:" -ForegroundColor Yellow
Write-Host "   ⚡ Deploy-Kiosk.bat - Automated kiosk deployment" -ForegroundColor White
Write-Host "   🧪 Test-Kiosk.bat - Safe kiosk testing (VM recommended)" -ForegroundColor White

Write-Host ""
Write-Host "⚠️  KIOSK MODE WARNINGS:" -ForegroundColor Red
Write-Host "   🔒 Test in VM before production use" -ForegroundColor Yellow
Write-Host "   💾 Always backup system before deployment" -ForegroundColor Yellow
Write-Host "   🆘 Emergency exit: Ctrl+Shift+Alt+E" -ForegroundColor Yellow

Write-Host ""
Write-Host "📁 Output Directory: $((Get-Item $OutputDir).FullName)" -ForegroundColor Cyan

# Optional: Open output directory
if ($OpenOutput) {
    Write-Host "🔍 Opening output directory..." -ForegroundColor Yellow
    Invoke-Item $OutputDir
}

Write-Host ""
Write-Host "✅ Ready for Kiosk Deployment!" -ForegroundColor Green
Write-Host "   Remember: Test first, deploy safely! 🛡️" -ForegroundColor Cyan