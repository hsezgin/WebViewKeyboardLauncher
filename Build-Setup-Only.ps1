param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\\dist",
    [switch]$SkipBuild,
    [switch]$SkipSetup,
    [switch]$OpenOutput
)

# Build and Package Script for WebView Keyboard Launcher - Setup Only
Write-Host "===============================================" -ForegroundColor Green
Write-Host "  WebView Keyboard Launcher - Setup Builder" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Stop"
$ProjectName = "WebViewKeyboardLauncher"
$ProjectPath = ".\\$ProjectName\\$ProjectName.csproj"
$NSISScript = ".\\WebViewKeyboardLauncher_Setup.nsi"

$NSISPath = "${env:ProgramFiles(x86)}\\NSIS\\makensis.exe"
$PublishDir = ".\$ProjectName\bin\$Configuration\net8.0-windows"

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
        dotnet publish $ProjectPath -c $Configuration -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true -o $PublishDir
        Write-Host "✅ Build completed" -ForegroundColor Green
    }
    catch {
        Write-Error "Build failed: $($_.Exception.Message)"
        exit 1
    }

    # Verify build output
    $BuildOutput = ".\\$ProjectName\\bin\\$Configuration\\net8.0-windows\\$ProjectName.exe"
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

# Step 2: Create NSIS Installer
if (!$SkipSetup) {
    Write-Host "🔧 Creating NSIS installer..." -ForegroundColor Yellow
    
    try {
        # Update version in NSIS script (basic replacement)
        $ResolvedSourceDir = Resolve-Path ".\$ProjectName\bin\$Configuration\net8.0-windows"
        $NSISContent = Get-Content $NSISScript -Raw
        $NSISContent = "!define SOURCE_DIR `"$ResolvedSourceDir`"`n" + $NSISContent
        $NSISContent = $NSISContent -replace '!define VERSIONMAJOR \d+', "!define VERSIONMAJOR $($Version.Split('.')[0])"
        $NSISContent = $NSISContent -replace '!define VERSIONMINOR \d+', "!define VERSIONMINOR $($Version.Split('.')[1])"
        $NSISContent = $NSISContent -replace '!define VERSIONBUILD \d+', "!define VERSIONBUILD $($Version.Split('.')[2])"
        
        $TempNSIS = "$env:TEMP\WebViewKeyboardLauncher_Setup_Temp.nsi"
        $NSISContent | Out-File -FilePath $TempNSIS -Encoding UTF8

        if (Test-Path "LICENSE.txt") {
            Copy-Item "LICENSE.txt" -Destination (Split-Path $TempNSIS)
        }
        elseif (Test-Path "LICENSE") {
            Copy-Item "LICENSE" -Destination (Split-Path $TempNSIS)
            $NSISContent = $NSISContent -replace '"LICENSE\.txt"', '"LICENSE"'
        }
        else {
            $NSISContent = $NSISContent -replace '!insertmacro MUI_PAGE_LICENSE.*', ''
            $NSISContent = $NSISContent -replace 'LicenseData.*', ''
            Write-Warning "License file not found, skipping license page"
        }
        
        # Compile NSIS
        & $NSISPath $TempNSIS
        
        # Move to output directory
        $TempExePath = "$env:TEMP\WebViewKeyboardLauncher_Setup.exe"
        if (Test-Path $TempExePath) {
            $SetupFile = "$OutputDir\\WebViewKeyboardLauncher-$Version-Setup.exe"
            Move-Item $TempExePath $SetupFile -Force
            
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

# Step 3: Create Release Notes
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
- 🔒 **Kiosk Mode** - Secure lockdown for dedicated terminals

## Installation

### 📦 Installer
- **File**: WebViewKeyboardLauncher-$Version-Setup.exe
- **Size**: ~$(if (Test-Path "$OutputDir\\WebViewKeyboardLauncher-$Version-Setup.exe") { [math]::Round((Get-Item "$OutputDir\\WebViewKeyboardLauncher-$Version-Setup.exe").Length / 1KB, 1) } else { "XXX" }) KB
- **Features**: Auto-start, registry integration, uninstaller, **Kiosk Mode option**
- **Usage**: Run as Administrator

## Kiosk Mode
- 🔒 **Secure system lockdown**
- 👤 **Dedicated kiosk user account**
- 🚫 **Disabled system shortcuts**
- 🖥️ **Fullscreen mode**
- 🛡️ **No remote access (maximum security)**
- 🆘 **Emergency exit: Ctrl+Shift+Alt+E**

## Command Line Usage

### Standard Installation
``````bash
# Silent install with custom URL
WebViewKeyboardLauncher-$Version-Setup.exe /S /URL=https://example.com

# Install without auto-start
WebViewKeyboardLauncher-$Version-Setup.exe /S /AUTOSTART=0
``````

### Kiosk Mode Installation
``````bash
# Full kiosk setup
WebViewKeyboardLauncher-$Version-Setup.exe /S /KIOSK=1 /FULLSCREEN=1 /URL=https://your-kiosk-url.com

# Kiosk with custom URL
WebViewKeyboardLauncher-$Version-Setup.exe /S /KIOSK=1 /URL=https://example.com
``````

## System Requirements
- Windows 10/11 (x64)
- .NET 8.0 Runtime
- Microsoft WebView2 Runtime
- Administrator rights (for kiosk mode)

## License
Apache License 2.0

## Developer
SezginBilge

---
Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@

$ReleaseNotes | Out-File -FilePath "$OutputDir\\RELEASE-NOTES.md" -Encoding UTF8

# Step 4: Build Summary
Write-Host ""
Write-Host "🎉 Setup Build Completed!" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

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
Write-Host "✅ Setup ready for distribution!" -ForegroundColor Green
Write-Host "   🔒 Kiosk mode available in installer! 🔒" -ForegroundColor Cyan