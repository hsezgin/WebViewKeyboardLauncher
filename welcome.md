# WebView Keyboard Launcher

🎉 **Hoş Geldiniz!**

Bu uygulama başarıyla kuruldu ve çalışıyor.

## 🚀 Özellikler

- **Virtual Keyboard**: TabTip/OSK açma
- **Floating Toolbar**: Sürüklenebilir araç çubuğu
- **WebView Integration**: Modern web tabanlı arayüz
- **System Integration**: Otomatik başlatma ve sistem kontrolü
- **Kiosk Mode**: Güvenli terminal kilit sistemi

## ⌨️ Kullanım

1. **Klavye Butonu** (⌨️): Virtual keyboard açar
2. **Settings Butonu** (⚙️): Ayarlar menüsünü açar
   - 🔄 **Refresh**: Sayfayı yeniler (2sn basılı tutma = Homepage)
   - ⏻ **Restart**: Sistemi yeniden başlatır (3sn basılı tutma)

### 🔒 Kiosk Mode
- **Emergency Exit**: Ctrl+Shift+Alt+E
- **Güvenli kilitleme** terminal uygulamaları için
- **Fullscreen desteği** ile tam ekran deneyim

## 🛠️ Command Line Kurulum

### **Silent Installation (Sessiz Kurulum):**

#### Basic kurulum:
```bash
WebViewKeyboardLauncher-Setup.exe /S
```

#### Custom URL ile:
```bash
WebViewKeyboardLauncher-Setup.exe /S /URL=https://example.com
```

#### Kiosk Mode kurulumu:
```bash
WebViewKeyboardLauncher-Setup.exe /S /KIOSK=1 /FULLSCREEN=1
```

#### Tam kiosk kurulumu:
```bash
WebViewKeyboardLauncher-Setup.exe /S /KIOSK=1 /FULLSCREEN=1 /URL=https://your-app.com /AUTOSTART=1
```

### **Kurulum Parametreleri:**

| Parameter | Değerler | Açıklama |
|-----------|----------|----------|
| `/S` | - | Silent installation (GUI yok) |
| `/URL=` | URL string | Homepage URL |
| `/AUTOSTART=` | 0 veya 1 | Windows ile başlatma |
| `/KIOSK=` | 0 veya 1 | Kiosk mode |
| `/FULLSCREEN=` | 0 veya 1 | Fullscreen mode |

### **Batch Script Örneği:**
```batch
@echo off
echo Installing WebView Keyboard Launcher...

REM Kiosk mode for terminals
WebViewKeyboardLauncher-Setup.exe /S /KIOSK=1 /FULLSCREEN=1 /URL=https://terminal-app.com

echo Installation complete!
pause
```

### **PowerShell Script Örneği:**
```powershell
# Install for different environments
param([string]$Environment = "production")

switch ($Environment) {
    "kiosk" {
        .\WebViewKeyboardLauncher-Setup.exe /S /KIOSK=1 /FULLSCREEN=1 /URL=https://kiosk-app.com
    }
    "development" {
        .\WebViewKeyboardLauncher-Setup.exe /S /URL=http://localhost:3000 /AUTOSTART=0
    }
    "production" {
        .\WebViewKeyboardLauncher-Setup.exe /S /URL=https://production-app.com /AUTOSTART=1
    }
}
```

## 🔧 Konfigürasyon

URL ayarları Windows Registry'de saklanır:
```
HKEY_LOCAL_MACHINE\SOFTWARE\WebViewKeyboardLauncher\
├── Homepage
├── KioskMode
├── FullscreenMode
└── Version
```

**Not:** Registry ayarları admin yetkisi gerektirir (güvenlik için).

## 📝 Sonraki Adımlar

Bu sayfayı kendi ihtiyaçlarınıza göre özelleştirebilirsiniz:

1. **URL'i değiştirin**: Registry'de Homepage değerini güncelleyin
2. **Otomatik başlatma**: Uygulama Windows başlangıcında otomatik açılır
3. **Custom content**: Bu sayfayı kendi içeriğinizle değiştirin
4. **Kiosk deployment**: Command line ile otomatik kurulum

## 🏢 Kullanım Senaryoları

### **Digital Signage**
```bash
setup.exe /S /KIOSK=1 /FULLSCREEN=1 /URL=https://digital-signage.com
```

### **Self-Service Terminals**
```bash
setup.exe /S /KIOSK=1 /URL=https://self-service-app.com
```

### **Information Kiosks**
```bash
setup.exe /S /KIOSK=1 /FULLSCREEN=1 /URL=https://info-portal.com
```

### **Development Environment**
```bash
setup.exe /S /URL=http://localhost:3000 /AUTOSTART=0
```

## 🛠️ Geliştirici Bilgileri

- **Repository**: [hsezgin/WebViewKeyboardLauncher](https://github.com/hsezgin/WebViewKeyboardLauncher)
- **Technology**: .NET 8, WebView2, Windows Forms
- **License**: Apache 2.0
- **Platform**: Windows 10/11 (x64)

---

**Version**: 1.0.0  
**Last Updated**: 2025

<style>
body {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    max-width: 800px;
    margin: 0 auto;
    padding: 20px;
    background: #f5f5f5;
}

h1 {
    color: #2c3e50;
    border-bottom: 2px solid #3498db;
    padding-bottom: 10px;
}

h2 {
    color: #34495e;
    margin-top: 30px;
}

h3 {
    color: #2980b9;
    margin-top: 25px;
}

h4 {
    color: #16a085;
    margin-top: 20px;
}

code {
    background: #ecf0f1;
    padding: 2px 5px;
    border-radius: 3px;
    font-family: 'Consolas', monospace;
}

pre {
    background: #2c3e50;
    color: #ecf0f1;
    padding: 15px;
    border-radius: 5px;
    overflow-x: auto;
    margin: 10px 0;
}

table {
    border-collapse: collapse;
    width: 100%;
    margin: 15px 0;
}

table, th, td {
    border: 1px solid #bdc3c7;
}

th, td {
    padding: 8px 12px;
    text-align: left;
}

th {
    background-color: #34495e;
    color: white;
}

tr:nth-child(even) {
    background-color: #f8f9fa;
}

.emoji {
    font-size: 1.2em;
}

blockquote {
    border-left: 4px solid #3498db;
    padding-left: 15px;
    margin: 15px 0;
    background: #ecf0f1;
    border-radius: 0 5px 5px 0;
}
</style>