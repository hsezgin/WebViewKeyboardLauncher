# Enable Developer Mode for MSIX sideloading
Write-Host "Enabling Developer Mode for MSIX sideloading..." -ForegroundColor Yellow

try {
    # Enable Developer Mode via Registry
    $DevModeKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"
    
    if (!(Test-Path $DevModeKey)) {
        New-Item -Path $DevModeKey -Force | Out-Null
    }
    
    Set-ItemProperty -Path $DevModeKey -Name "AllowDevelopmentWithoutDevLicense" -Value 1 -Type DWord
    Set-ItemProperty -Path $DevModeKey -Name "AllowAllTrustedApps" -Value 1 -Type DWord
    
    Write-Host "✅ Developer Mode enabled successfully!" -ForegroundColor Green
    Write-Host "You can now install unsigned MSIX packages." -ForegroundColor Green
}
catch {
    Write-Warning "Failed to enable Developer Mode: $($_.Exception.Message)"
    Write-Host "Please enable manually:" -ForegroundColor Yellow
    Write-Host "Settings → Privacy & Security → For developers → Developer mode" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Now you can install the MSIX package:" -ForegroundColor Yellow
Write-Host "Add-AppxPackage -Path 'WebViewKeyboardLauncher-1.0.1.msix'" -ForegroundColor Cyan