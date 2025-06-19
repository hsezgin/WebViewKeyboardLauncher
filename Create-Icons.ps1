# Simple icon creator for MSIX package
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

function Create-SimpleIcon {
    param(
        [int]$Width,
        [int]$Height,
        [string]$Text,
        [string]$OutputPath
    )
    
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Background
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(41, 128, 185))
    $graphics.FillRectangle($brush, 0, 0, $Width, $Height)
    
    # Text
    $font = New-Object System.Drawing.Font("Segoe UI", [math]::Max(8, $Width / 8), [System.Drawing.FontStyle]::Bold)
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    
    $textSize = $graphics.MeasureString($Text, $font)
    $x = ($Width - $textSize.Width) / 2
    $y = ($Height - $textSize.Height) / 2
    
    $graphics.DrawString($Text, $font, $textBrush, $x, $y)
    
    # Save
    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Cleanup
    $graphics.Dispose()
    $bitmap.Dispose()
    $brush.Dispose()
    $textBrush.Dispose()
    $font.Dispose()
    
    Write-Host "Created: $OutputPath ($Width x $Height)" -ForegroundColor Green
}

# Create Images directory
$ImagesDir = ".\WebViewKeyboardLauncher.Package\Images"
if (!(Test-Path $ImagesDir)) {
    New-Item -ItemType Directory -Path $ImagesDir -Force
}

Write-Host "Creating MSIX package icons..." -ForegroundColor Yellow

# Create all required icons
Create-SimpleIcon -Width 44 -Height 44 -Text "⌨️" -OutputPath "$ImagesDir\Square44x44Logo.png"
Create-SimpleIcon -Width 150 -Height 150 -Text "⌨️" -OutputPath "$ImagesDir\Square150x150Logo.png"
Create-SimpleIcon -Width 310 -Height 150 -Text "WebView KB" -OutputPath "$ImagesDir\Wide310x150Logo.png"
Create-SimpleIcon -Width 50 -Height 50 -Text "⌨️" -OutputPath "$ImagesDir\StoreLogo.png"
Create-SimpleIcon -Width 620 -Height 300 -Text "WebView Keyboard Launcher" -OutputPath "$ImagesDir\SplashScreen.png"

Write-Host "✅ All icons created successfully!" -ForegroundColor Green