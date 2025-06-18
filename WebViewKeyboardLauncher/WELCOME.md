# WebView Keyboard Launcher

🎉 **Hoş Geldiniz!**

Bu uygulama başarıyla kuruldu ve çalışıyor.

## 🚀 Özellikler

- **Virtual Keyboard**: TabTip/OSK açma
- **Floating Toolbar**: Sürüklenebilir araç çubuğu
- **WebView Integration**: Modern web tabanlı arayüz
- **System Integration**: Otomatik başlatma ve sistem kontrolü

## ⌨️ Kullanım

1. **Klavye Butonu** (⌨️): Virtual keyboard açar
2. **Settings Butonu** (⚙️): Ayarlar menüsünü açar
   - 🔄 **Refresh**: Sayfayı yeniler (2sn basılı tutma = Homepage)
   - ⏻ **Restart**: Sistemi yeniden başlatır (3sn basılı tutma)

## 🔧 Konfigürasyon

URL ayarları Windows Registry'de saklanır:
```
HKEY_CURRENT_USER\Software\WebViewKeyboardLauncher\Homepage
```

## 📝 Sonraki Adımlar

Bu sayfayı kendi ihtiyaçlarınıza göre özelleştirebilirsiniz:

1. **URL'i değiştirin**: Registry'de Homepage değerini güncelleyin
2. **Otomatik başlatma**: Uygulama Windows başlangıcında otomatik açılır
3. **Custom content**: Bu sayfayı kendi içeriğinizle değiştirin

## 🛠️ Geliştirici Bilgileri

- **Repository**: [hsezgin/WebViewKeyboardLauncher](https://github.com/hsezgin/WebViewKeyboardLauncher)
- **Technology**: .NET 8, WebView2, Windows Forms
- **License**: MIT

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
}

.emoji {
    font-size: 1.2em;
}
</style>