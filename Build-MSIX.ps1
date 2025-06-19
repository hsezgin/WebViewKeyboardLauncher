param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\\dist\\msix",
    [switch]$Sign,
    [string]$CertPath = "",
    [string]$CertPassword = ""
)

# MSIX Build Script for WebView Keyboard Launcher
Write-Host "===============================================" -ForegroundColor Green
Write-Host "  WebView Keyboard Launcher - MSIX Package" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Stop"
$ProjectName = "WebViewKeyboardLauncher"
$PackagingProject = "WebViewKeyboardLauncher.Package"
$SolutionPath = ".\WebViewKeyboardLauncher.sln"

# Validate paths
if (!(Test-Path $SolutionPath)) {
    Write-Error "Solution file not found: $SolutionPath"
    exit 1
}

# Create output directory
Write-Host "📁 Creating output directory..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Step 1: Update version in Package.appxmanifest
Write-Host "🔧 Updating package version..." -ForegroundColor Yellow
$ManifestPath = ".\$PackagingProject\Package.appxmanifest"
if (Test-Path $ManifestPath) {
    $manifest = Get-Content $ManifestPath -Raw
    $manifest = $manifest -replace 'Version="[\d\.]+"', "Version=`"$Version.0`""
    $manifest | Out-File -FilePath $ManifestPath -Encoding UTF8
    Write-Host "   ✅ Version updated to $Version.0" -ForegroundColor Green
}

# Step 2: Build the packaging project
Write-Host "🔨 Building MSIX package..." -ForegroundColor Yellow
try {
    # Build for x64 platform
    dotnet build $SolutionPath -c $Configuration -p:Platform=x64 -p:AppxBundle=Never -p:UapAppxPackageBuildMode=SideloadOnly
    
    Write-Host "✅ MSIX package built successfully" -ForegroundColor Green
}
catch {
    Write-Error "MSIX build failed: $($_.Exception.Message)"
    exit 1
}

# Step 3: Find and copy the MSIX package
Write-Host "📦 Locating MSIX package..." -ForegroundColor Yellow
$MSIXPath = Get-ChildItem -Path ".\$PackagingProject\bin\$Configuration" -Filter "*.msix" -Recurse | Select-Object -First 1

if ($MSIXPath) {
    $DestinationPath = "$OutputDir\WebViewKeyboardLauncher-$Version.msix"
    Copy-Item $MSIXPath.FullName -Destination $DestinationPath -Force
    
    $PackageInfo = Get-Item $DestinationPath
    Write-Host "   📦 Package: $($PackageInfo.Name) ($([math]::Round($PackageInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
} else {
    Write-Error "MSIX package not found after build"
    exit 1
}

# Step 4: Create certificate (for development)
if (!$Sign -and !(Test-Path $CertPath)) {
    Write-Host "🔐 Creating development certificate..." -ForegroundColor Yellow
    $CertName = "CN=SezginBilge"
    $DevCertPath = "$OutputDir\WebViewKeyboardLauncher-Dev.pfx"
    $DevCertPassword = "DevPassword123"
    
    try {
        # Create self-signed certificate
        $cert = New-SelfSignedCertificate -Type Custom -Subject $CertName -KeyUsage DigitalSignature -FriendlyName "WebView Keyboard Launcher Dev Cert" -CertStoreLocation "Cert:\LocalMachine\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
        
        # Export certificate
        $certPassword = ConvertTo-SecureString -String $DevCertPassword -Force -AsPlainText
        Export-PfxCertificate -cert "Cert:\LocalMachine\My\$($cert.Thumbprint)" -FilePath $DevCertPath -Password $certPassword
        
        Write-Host "   ✅ Development certificate created: $DevCertPath" -ForegroundColor Green
        Write-Host "   🔑 Password: $DevCertPassword" -ForegroundColor Yellow
        
        $CertPath = $DevCertPath
        $CertPassword = $DevCertPassword
    }
    catch {
        Write-Warning "Could not create development certificate: $($_.Exception.Message)"
    }
}

# Step 5: Sign the package (optional)
if ($Sign -and (Test-Path $CertPath)) {
    Write-Host "✍️ Signing MSIX package..." -ForegroundColor Yellow
    try {
        $SignToolPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe"
        $SignTool = Get-ChildItem $SignToolPath | Sort-Object Name -Descending | Select-Object -First 1
        
        if ($SignTool) {
            $Arguments = @(
                "sign"
                "/f", $CertPath
                "/p", $CertPassword
                "/fd", "SHA256"
                "/tr", "http://timestamp.sectigo.com"
                "/td", "SHA256"
                $DestinationPath
            )
            
            & $SignTool.FullName @Arguments
            Write-Host "   ✅ Package signed successfully" -ForegroundColor Green
        } else {
            Write-Warning "SignTool not found. Install Windows SDK."
        }
    }
    catch {
        Write-Warning "Signing failed: $($_.Exception.Message)"
    }
}

# Step 6: Create installation script
Write-Host "📝 Creating installation script..." -ForegroundColor Yellow
$InstallScript = @"
@echo off
echo Installing WebView Keyboard Launcher MSIX Package...
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as administrator...
) else (
    echo This script requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

REM Install the MSIX package
echo Installing package...
powershell -Command "Add-AppxPackage -Path 'WebViewKeyboardLauncher-$Version.msix'"

if %errorLevel% == 0 (
    echo.
    echo ✅ Installation completed successfully!
    echo The application is now available in the Start Menu.
) else (
    echo.
    echo ❌ Installation failed!
    echo Please check if the certificate is trusted.
)

pause
"@

$InstallScript | Out-File -FilePath "$OutputDir\Install.bat" -Encoding ASCII

# Step 7: Create uninstall script
$UninstallScript = @"
@echo off
echo Uninstalling WebView Keyboard Launcher...
echo.

powershell -Command "Get-AppxPackage | Where-Object {`$_.Name -eq 'WebViewKeyboardLauncher'} | Remove-AppxPackage"

if %errorLevel% == 0 (
    echo ✅ Uninstallation completed successfully!
) else (
    echo ❌ Uninstallation failed!
)

pause
"@

$UninstallScript | Out-File -FilePath "$OutputDir\Uninstall.bat" -Encoding ASCII

# Step 8: Create README
$ReadmeContent = @"
# WebView Keyboard Launcher - MSIX Package

## Installation

### Method 1: Batch Script (Recommended)
1. Run **Install.bat** as Administrator
2. Package will be installed automatically

### Method 2: Manual Installation
1. Double-click **WebViewKeyboardLauncher-$Version.msix**
2. Click "Install" in the dialog

### Method 3: PowerShell
``````powershell
Add-AppxPackage -Path "WebViewKeyboardLauncher-$Version.msix"
``````

## Trust Certificate (Development Build)

If this is a development build, you need to trust the certificate:

1. Right-click **WebViewKeyboardLauncher-Dev.pfx**
2. Select "Install PFX..."
3. Choose "Local Machine" → Next
4. Enter password: **$DevCertPassword**
5. Select "Place all certificates in the following store"
6. Browse → "Trusted Root Certification Authorities"
7. Finish

## Uninstallation

### Method 1: Batch Script
Run **Uninstall.bat**

### Method 2: Settings
Settings → Apps → WebView Keyboard Launcher → Uninstall

### Method 3: PowerShell
``````powershell
Get-AppxPackage | Where-Object {`$_.Name -eq 'WebViewKeyboardLauncher'} | Remove-AppxPackage
``````

## Features
- Modern Windows App Store style installation
- Automatic updates support
- Clean uninstallation
- Sandboxed security model

## System Requirements
- Windows 10 1809 (Build 17763) or later
- x64 architecture
- Administrator rights for installation

Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@

$ReadmeContent | Out-File -FilePath "$OutputDir\README.md" -Encoding UTF8

# Summary
Write-Host ""
Write-Host "🎉 MSIX Package Created Successfully!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

Get-ChildItem $OutputDir | ForEach-Object {
    $Size = if ($_.PSIsContainer) { "DIR" } else { "$([math]::Round($_.Length / 1KB, 1)) KB" }
    Write-Host "   📁 $($_.Name) ($Size)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "📁 Output Directory: $((Get-Item $OutputDir).FullName)" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ Ready for distribution via Microsoft Store or sideloading!" -ForegroundColor Green