param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\dist",
    [switch]$SkipBuild,
    [switch]$SkipSetup,
    [switch]$OpenOutput
)

# Build and Package Script for WebView Keyboard Launcher
Write-Host "===============================================" -ForegroundColor Green
Write-Host "  WebView Keyboard Launcher - Build & Package" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Stop"
$ProjectName = "WebViewKeyboardLauncher"
$ProjectPath = ".\$ProjectName\$ProjectName.csproj"
$NSISScript = ".\WebViewKeyboardLauncher_Setup.nsi"
$NSISPath = "${env:ProgramFiles(x86)}\NSIS\makensis.exe"

# Validate paths
if (!(Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 1
}

if (!(Test-Path $NSISScript)) {
    Write-Error "NSIS script not found: $NSISScript"
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
        dotnet build $ProjectPath -c $Configuration --no-restore --verbosity minimal
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
else {
    Write-Host "⏭️  Skipping build (as requested)" -ForegroundColor Yellow
}

# Step 2: Create Portable Package
Write-Host "📦 Creating portable package..." -ForegroundColor Yellow
$PortableDir = "$OutputDir\Portable"
New-Item -ItemType Directory -Path $PortableDir -Force | Out-Null

$SourceDir = ".\$ProjectName\bin\$Configuration\net8.0-windows"
Copy-Item "$SourceDir\$ProjectName.exe" -Destination $PortableDir
Copy-Item "$SourceDir\*.dll" -Destination $PortableDir -ErrorAction SilentlyContinue
Copy-Item "$SourceDir\*.json" -Destination $PortableDir -ErrorAction SilentlyContinue

# Create portable launcher script
$PortableLauncher = @"
@echo off
echo WebView Keyboard Launcher - Portable Edition
echo ============================================
echo.

REM Set portable mode
set WEBVIEW_PORTABLE=1

REM Create local config if not exists
if not exist "config.json" (
    echo {
    echo   "Homepage": "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome",
    echo   "AutoStart": false,
    echo   "Portable": true
    echo }
    echo ) > config.json
)

echo Starting application...
start "" "$ProjectName.exe"
"@

$PortableLauncher | Out-File -FilePath "$PortableDir\Start.bat" -Encoding ASCII

# Create portable README
$PortableReadme = @"
# WebView Keyboard Launcher - Portable Edition

## Quick Start
1. Run 'Start.bat' to launch the application
2. Edit 'config.json' to customize settings
3. No installation required!

## Features
- Virtual keyboard launcher (⌨️)
- Floating toolbar with settings (⚙️)
- Customizable homepage URL
- No registry modifications

## Configuration
Edit config.json to change:
- Homepage URL
- Application settings

## System Requirements
- Windows 10/11 (x64)
- .NET 8.0 Runtime
- Microsoft WebView2 Runtime

## Version: $Version
## License: Apache 2.0
"@

$PortableReadme | Out-File -FilePath "$PortableDir\README.md" -Encoding UTF8

# Compress portable package
Write-Host "🗜️  Compressing portable package..." -ForegroundColor Yellow
$PortableZip = "$OutputDir\WebViewKeyboardLauncher-$Version-Portable.zip"
Compress-Archive -Path "$PortableDir\*" -DestinationPath $PortableZip -Force
$ZipInfo = Get-Item $PortableZip
Write-Host "   📦 Created: $($ZipInfo.Name) ($([math]::Round($ZipInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan

# Step 3: Create NSIS Installer
if (!$SkipSetup) {
    Write-Host "🔧 Creating NSIS installer..." -ForegroundColor Yellow
    try {
        # Update version in NSIS script (basic replacement)
        $NSISContent = Get-Content $NSISScript -Raw
        $NSISContent = $NSISContent -replace '!define VERSIONMAJOR \d+', "!define VERSIONMAJOR $($Version.Split('.')[0])"
        $NSISContent = $NSISContent -replace '!define VERSIONMINOR \d+', "!define VERSIONMINOR $($Version.Split('.')[1])"
        $NSISContent = $NSISContent -replace '!define VERSIONBUILD \d+', "!define VERSIONBUILD $($Version.Split('.')[2])"
        
        # Fix license file path
        if (Test-Path "LICENSE.txt") {
            $NSISContent = $NSISContent -replace '"LICENSE\.txt"', '"LICENSE.txt"'
        } elseif (Test-Path "LICENSE") {
            $NSISContent = $NSISContent -replace '"LICENSE\.txt"', '"LICENSE"'
        } else {
            # Remove license page if no license file found
            $NSISContent = $NSISContent -replace '!insertmacro MUI_PAGE_LICENSE.*', ''
            Write-Warning "License file not found, skipping license page"
        }
        
        $TempNSIS = "$env:TEMP\WebViewKeyboardLauncher_Setup_Temp.nsi"
        $NSISContent | Out-File -FilePath $TempNSIS -Encoding UTF8
        
        # Compile NSIS
        & $NSISPath $TempNSIS
        
        # Move to output directory
        if (Test-Path ".\WebViewKeyboardLauncher_Setup.exe") {
            $SetupFile = "$OutputDir\WebViewKeyboardLauncher-$Version-Setup.exe"
            Move-Item ".\WebViewKeyboardLauncher_Setup.exe" $SetupFile -Force
            
            $SetupInfo = Get-Item $SetupFile
            Write-Host "✅ NSIS installer created" -ForegroundColor Green
            Write-Host "   📦 Setup: $($SetupInfo.Name) ($([math]::Round($SetupInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
        }
        else {
            Write-Warning "NSIS compilation succeeded but setup file not found"
        }
        
        # Cleanup
        Remove-Item $TempNSIS -ErrorAction SilentlyContinue
    }
    catch {
        Write-Error "NSIS compilation failed: $($_.Exception.Message)"
        exit 1
    }
}
else {
    Write-Host "⏭️  Skipping NSIS setup (as requested)" -ForegroundColor Yellow
}

# Step 4: Create Release Notes
Write-Host "📝 Creating release notes..." -ForegroundColor Yellow
$ReleaseNotes = @"
# WebView Keyboard Launcher v$Version

## Release Date: $(Get-Date -Format 'yyyy-MM-dd')

## Features
- 🎛️ Floating toolbar with virtual keyboard launcher
- ⌨️ TabTip/OSK integration
- 🌐 WebView2 integration with customizable homepage
- 🔧 Settings panel with refresh and restart options
- 🚀 Auto-start with Windows
- 📱 Modern UI with drag & drop support

## Installation Options

### 📦 Installer (Recommended)
- **File**: WebViewKeyboardLauncher-$Version-Setup.exe
- **Size**: ~$(if (Test-Path "$OutputDir\WebViewKeyboardLauncher-$Version-Setup.exe") { [math]::Round((Get-Item "$OutputDir\WebViewKeyboardLauncher-$Version-Setup.exe").Length / 1KB, 1) } else { "XXX" }) KB
- **Features**: Auto-start, registry integration, uninstaller
- **Usage**: Run as Administrator

### 🎒 Portable Edition
- **File**: WebViewKeyboardLauncher-$Version-Portable.zip
- **Size**: ~$([math]::Round((Get-Item $PortableZip).Length / 1KB, 1)) KB
- **Features**: No installation required, portable settings
- **Usage**: Extract and run Start.bat

## Command Line Usage

### Installer
``````bash
# Silent install with custom URL
WebViewKeyboardLauncher-$Version-Setup.exe /S /URL=https://example.com

# Install without auto-start
WebViewKeyboardLauncher-$Version-Setup.exe /S /AUTOSTART=0
``````

## System Requirements
- Windows 10/11 (x64)
- .NET 8.0 Runtime
- Microsoft WebView2 Runtime

## License
Apache License 2.0

## Developer
SezginBilge

---
Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@

$ReleaseNotes | Out-File -FilePath "$OutputDir\RELEASE-NOTES.md" -Encoding UTF8

# Step 5: Build Summary
Write-Host ""
Write-Host "🎉 Build and Package Completed!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

Get-ChildItem $OutputDir | ForEach-Object {
    $Size = if ($_.PSIsContainer) { "DIR" } else { "$([math]::Round($_.Length / 1KB, 1)) KB" }
    Write-Host "   📁 $($_.Name) ($Size)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "📁 Output Directory: $((Get-Item $OutputDir).FullName)" -ForegroundColor Yellow

# Optional: Open output directory
if ($OpenOutput) {
    Write-Host "🔍 Opening output directory..." -ForegroundColor Yellow
    Invoke-Item $OutputDir
}

Write-Host ""
Write-Host "✅ All done! Ready for distribution." -ForegroundColor Green