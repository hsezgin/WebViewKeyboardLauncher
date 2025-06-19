param(
    [string]$Version = "1.0.1",
    [string]$OutputDir = ".\dist\msix"
)

Write-Host "===============================================" -ForegroundColor Green
Write-Host "  Simple MSIX Package Builder" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

$ErrorActionPreference = "Stop"

# Create output directory
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Step 1: Build the application
Write-Host "🔨 Building application..." -ForegroundColor Yellow

# Create Package directory first
$PackageDir = "$OutputDir\Package"
New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null

try {
    # Use win-x64 instead of win10-x64 (correct RID for .NET 8/9)
    dotnet publish .\WebViewKeyboardLauncher\WebViewKeyboardLauncher.csproj -c Release -r win-x64 --self-contained -o $PackageDir

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    Write-Host "✅ Application built successfully" -ForegroundColor Green
}
catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    exit 1
}

# Step 2: Create AppxManifest.xml
Write-Host "📝 Creating MSIX manifest..." -ForegroundColor Yellow

# Ensure Package directory exists
if (!(Test-Path $PackageDir)) {
    New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null
}

$Manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">

  <Identity Name="WebViewKeyboardLauncher"
            Publisher="CN=SezginBilge"
            Version="$Version.0" />

  <Properties>
    <DisplayName>WebView Keyboard Launcher</DisplayName>
    <PublisherDisplayName>SezginBilge</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Virtual keyboard launcher with WebView2</Description>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="tr-tr"/>
  </Resources>

  <Applications>
    <Application Id="App" Executable="WebViewKeyboardLauncher.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="WebView Keyboard Launcher"
                          Description="Virtual keyboard launcher"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png"
                          BackgroundColor="transparent">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@


$Manifest | Out-File -FilePath "$PackageDir\AppxManifest.xml" -Encoding UTF8

# Verify the main executable exists
if (!(Test-Path "$PackageDir\WebViewKeyboardLauncher.exe")) {
    Write-Error "Main executable not found after build. Check build output."
    exit 1
}

# Step 3: Create dummy assets
Write-Host "🎨 Creating assets..." -ForegroundColor Yellow
$AssetsDir = "$PackageDir\Assets"
New-Item -ItemType Directory -Path $AssetsDir -Force | Out-Null

@("Square44x44Logo.png", "Square150x150Logo.png", "Wide310x150Logo.png", "StoreLogo.png", "SplashScreen.png") | ForEach-Object {
    $Base64PNG = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="
    $Bytes = [System.Convert]::FromBase64String($Base64PNG)
    [System.IO.File]::WriteAllBytes("$AssetsDir\$_", $Bytes)
}

# Step 4: Create MSIX package using Windows SDK tools
Write-Host "📆 Creating MSIX package..." -ForegroundColor Yellow

$WindowsSDKs = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Directory | Sort-Object Name -Descending
$MakeAppx = $null

foreach ($sdk in $WindowsSDKs) {
    $MakeAppxPath = Join-Path $sdk.FullName "x64\makeappx.exe"
    if (Test-Path $MakeAppxPath) {
        $MakeAppx = $MakeAppxPath
        break
    }
}

if (!$MakeAppx) {
    Write-Error "Windows SDK not found. Please install Windows 10/11 SDK."
    exit 1
}

$PackagePath = "$OutputDir\WebViewKeyboardLauncher-$Version.msix"
& $MakeAppx pack /d $PackageDir /p $PackagePath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ MSIX package created successfully!" -ForegroundColor Green

    $PackageInfo = Get-Item $PackagePath
    Write-Host "   📦 Package: $($PackageInfo.Name) ($([math]::Round($PackageInfo.Length / 1KB, 1)) KB)" -ForegroundColor Cyan
} else {
    Write-Error "Failed to create MSIX package"
    exit 1
}

# Step 5: Create installation instructions
$Instructions = @"
# WebView Keyboard Launcher - MSIX Installation

## Quick Install
Double-click: WebViewKeyboardLauncher-$Version.msix

## PowerShell Install
Add-AppxPackage -Path "WebViewKeyboardLauncher-$Version.msix"

## Note
This is an unsigned package. You may need to enable Developer Mode:
Settings → Update & Security → For developers → Developer mode

## Uninstall
Settings → Apps → WebView Keyboard Launcher → Uninstall

Package created: $(Get-Date)
"@

$Instructions | Out-File -FilePath "$OutputDir\INSTALL.md" -Encoding UTF8

Write-Host ""
Write-Host "🎉 MSIX Package Ready!" -ForegroundColor Green
Write-Host "📁 Location: $((Get-Item $PackagePath).FullName)" -ForegroundColor Yellow
Write-Host ""
Write-Host "ℹ️  This is an unsigned package. Enable Developer Mode to install." -ForegroundColor Cyan