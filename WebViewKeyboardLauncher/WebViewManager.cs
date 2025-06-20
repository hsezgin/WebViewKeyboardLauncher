/*
 * WebViewManager.cs - Script Injection Sorun Çözümü
 */

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WebViewKeyboardLauncher;

public class WebViewManager
{
    private readonly WebView2 _webView;
    private const string REGISTRY_KEY_64 = @"SOFTWARE\WebViewKeyboardLauncher";
    private const string REGISTRY_KEY_32 = @"SOFTWARE\WOW6432Node\WebViewKeyboardLauncher";
    private const string DEFAULT_URL = "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome.html";
    public event Action? OnFocusReceived;

    // Cache
    private bool _cacheInitialized = false;
    private bool _cachedKioskMode = false;
    private bool _cachedFullscreenMode = false;
    private string _cachedHomepageUrl = DEFAULT_URL;
    private bool _scriptInjected = false; // ✅ YENİ: Script injection takibi

    // TabTip pencere takibi için
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public WebViewManager(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
    }

    private void InitializeCache()
    {
        if (_cacheInitialized) return;

        try
        {
            using var key = OpenRegistryKey(false);
            if (key != null)
            {
                _cachedKioskMode = (key.GetValue("KioskMode") as int?) == 1;
                _cachedFullscreenMode = (key.GetValue("FullscreenMode") as int?) == 1;

                if (key.GetValue("Homepage") is string url && !string.IsNullOrWhiteSpace(url))
                {
                    _cachedHomepageUrl = url;
                }

                Debug.WriteLine($"[WebViewManager] Cache initialized - Kiosk:{_cachedKioskMode}, Fullscreen:{_cachedFullscreenMode}, URL:{_cachedHomepageUrl}");
            }
            else
            {
                Debug.WriteLine("[WebViewManager] Registry not found, using defaults");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Cache initialization error: {ex.Message}");
        }

        _cacheInitialized = true;
    }

    private string GetRegistryKey()
    {
        using var key64 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_64);
        if (key64 != null)
        {
            return REGISTRY_KEY_64;
        }

        using var key32 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_32);
        if (key32 != null)
        {
            return REGISTRY_KEY_32;
        }

        return REGISTRY_KEY_64;
    }

    private RegistryKey? OpenRegistryKey(bool writable = false)
    {
        string keyPath = GetRegistryKey();

        try
        {
            if (writable)
            {
                return Registry.LocalMachine.CreateSubKey(keyPath);
            }
            else
            {
                return Registry.LocalMachine.OpenSubKey(keyPath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Registry açma hatası ({keyPath}): {ex.Message}");
            return null;
        }
    }

    public void Initialize()
    {
        InitializeCache();

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebViewKeyboardLauncher");

        var env = CoreWebView2Environment.CreateAsync(null, userDataFolder).Result;

        _webView.EnsureCoreWebView2Async(env).ContinueWith(task =>
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                // ✅ UI thread'de çalıştır
                if (_webView.InvokeRequired)
                {
                    _webView.Invoke(new Action(() => {
                        RegisterFocusAndMessageListeners();
                        NavigateHomepage();
                    }));
                }
                else
                {
                    RegisterFocusAndMessageListeners();
                    NavigateHomepage();
                }
            }
            else
            {
                Debug.WriteLine($"[WebViewManager] WebView2 başlatılamadı: {task.Exception?.Message}");
            }
        });
    }

    private void RegisterFocusAndMessageListeners()
    {
        if (_webView.CoreWebView2 == null)
        {
            Debug.WriteLine("[WebViewManager] CoreWebView2 null - RegisterFocusAndMessageListeners failed");
            return;
        }

        // ✅ ÇOKLU SAYFA KONTROLÜ: NavigationCompleted'da script inject et
        _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

        // ✅ WebView2 ayarları
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;

        Debug.WriteLine("[WebViewManager] Context menu and dev tools disabled");

        // ✅ THROTTLE - Focus event'lerini de throttle et
        DateTime lastFocusEvent = DateTime.MinValue;
        const int FOCUS_COOLDOWN_MS = 300;

        _webView.CoreWebView2.WebMessageReceived += (sender, e) =>
        {
            string message = e.TryGetWebMessageAsString();
            Debug.WriteLine($"[WebViewManager] WebMessage received: {message}");

            switch (message)
            {
                case "focus":
                    // ✅ THROTTLE CHECK
                    var now = DateTime.Now;
                    if ((now - lastFocusEvent).TotalMilliseconds < FOCUS_COOLDOWN_MS)
                    {
                        Debug.WriteLine("[THROTTLE] Focus ignored - cooldown active (" + (now - lastFocusEvent).TotalMilliseconds + "ms)");
                        return;
                    }

                    lastFocusEvent = now;
                    Debug.WriteLine("[DEBUG] WebView Focus event received");

                    // TabTip'i aç
                    OnFocusReceived?.Invoke();

                    Debug.WriteLine("[DEBUG] Focus events completed");
                    break;

                case "script_loaded":
                    Debug.WriteLine("✅ [WebViewManager] Script başarıyla yüklendi!");
                    _scriptInjected = true;
                    break;

                case "refresh":
                    Debug.WriteLine("[WebViewManager] Refresh mesajı alındı");
                    Reload();
                    break;

                default:
                    Debug.WriteLine("[WebViewManager] Bilinmeyen mesaj: " + message);
                    break;
            }
        };

        // ✅ İlk script injection
        InjectFocusScript();

        Debug.WriteLine("[WebViewManager] Focus and message listeners registered");
    }

    // ✅ YENİ: NavigationCompleted event handler
    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        Debug.WriteLine($"[WebViewManager] Navigation completed: {e.IsSuccess}, URL: {_webView.CoreWebView2?.Source}");

        if (e.IsSuccess)
        {
            // Her sayfa yüklendiğinde script'i tekrar inject et
            InjectFocusScript();
        }
    }

    // ✅ YENİ: Script injection metodu
    private async void InjectFocusScript()
    {
        if (_webView.CoreWebView2 == null)
        {
            Debug.WriteLine("[WebViewManager] CoreWebView2 null - Script injection failed");
            return;
        }

        try
        {
            // JavaScript'i basitleştir ve debug bilgisi ekle
            string script = @"
(function() {
    console.log('🚀 WebView Keyboard Launcher Script Loading...');
    
    // Önceki listener'ları temizle
    if (window.webviewKeyboardHandler) {
        console.log('♻️ Cleaning previous listeners...');
        document.removeEventListener('focusin', window.webviewKeyboardHandler, true);
        document.removeEventListener('contextmenu', window.webviewContextHandler, true);
        document.removeEventListener('keydown', window.webviewKeyHandler, true);
    }
    
    let lastFocusTime = 0;
    const FOCUS_THROTTLE = 300; // 300ms throttle
    
    // Focus handler
    window.webviewKeyboardHandler = function(e) {
        const now = Date.now();
        if (now - lastFocusTime < FOCUS_THROTTLE) {
            console.log('⏰ Focus ignored - too frequent');
            return;
        }
        
        console.log('🎯 Focus event on:', e.target.tagName, e.target.type || 'no-type');
        
        const target = e.target;
        const needsKeyboard = (
            target.tagName === 'INPUT' && 
            ['text', 'email', 'password', 'number', 'tel', 'url', 'search'].includes(target.type)
        ) || 
        target.tagName === 'TEXTAREA' || 
        target.contentEditable === 'true' || 
        target.getAttribute('role') === 'textbox';
        
        if (needsKeyboard) {
            lastFocusTime = now;
            console.log('⌨️ Triggering keyboard for:', target.tagName, target.type);
            
            try {
                window.chrome.webview.postMessage('focus');
                console.log('✅ Focus message sent successfully');
            } catch (error) {
                console.error('❌ Error sending focus message:', error);
            }
        } else {
            console.log('🚫 Focus ignored for:', target.tagName);
        }
    };

    // Context menu blocker
    window.webviewContextHandler = function(e) {
        e.preventDefault();
        console.log('🚫 Right-click blocked');
        return false;
    };

    // Key blocker for dev tools
    window.webviewKeyHandler = function(e) {
        // F12 - Developer Tools
        if (e.key === 'F12') {
            e.preventDefault();
            console.log('🚫 F12 blocked');
            return false;
        }
        
        // Ctrl+Shift+I - Developer Tools
        if (e.ctrlKey && e.shiftKey && e.key === 'I') {
            e.preventDefault();
            console.log('🚫 Ctrl+Shift+I blocked');
            return false;
        }
        
        // Ctrl+Shift+C - Inspect Element
        if (e.ctrlKey && e.shiftKey && e.key === 'C') {
            e.preventDefault();
            console.log('🚫 Ctrl+Shift+C blocked');
            return false;
        }
        
        // Ctrl+U - View Source
        if (e.ctrlKey && e.key === 'u') {
            e.preventDefault();
            console.log('🚫 Ctrl+U blocked');
            return false;
        }
    };
    
    // Event listener'ları ekle
    document.addEventListener('focusin', window.webviewKeyboardHandler, true);
    document.addEventListener('contextmenu', window.webviewContextHandler, true);
    document.addEventListener('keydown', window.webviewKeyHandler, true);
    
    console.log('✅ All event listeners added successfully');
    
    // C# tarafına script yüklendiğini bildir
    try {
        window.chrome.webview.postMessage('script_loaded');
        console.log('📡 Script loaded notification sent');
    } catch (error) {
        console.error('❌ Error sending script loaded notification:', error);
    }
    
    console.log('🎉 WebView Keyboard Launcher Script Ready!');
})();
";

            // Script'i ekle
            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
            Debug.WriteLine("✅ [WebViewManager] Focus script injected");

            // Sayfa zaten yüklüyse script'i hemen çalıştır
            if (_webView.CoreWebView2.Source != "about:blank" && !string.IsNullOrEmpty(_webView.CoreWebView2.Source))
            {
                try
                {
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    Debug.WriteLine("✅ [WebViewManager] Script immediately executed for current page");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ [WebViewManager] Immediate script execution failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [WebViewManager] Script injection error: {ex.Message}");
        }
    }

    public string GetHomepageUrl()
    {
        InitializeCache();
        return _cachedHomepageUrl;
    }

    public void SetHomepageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        try
        {
            using var key = OpenRegistryKey(true);
            key?.SetValue("Homepage", url, RegistryValueKind.String);

            _cachedHomepageUrl = url;

            Debug.WriteLine($"[WebViewManager] URL registry'ye kaydedildi: {url}");
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine("[WebViewManager] Registry yazma hatası: Admin yetkisi gerekli");
            throw new InvalidOperationException("Administrator privileges required to change homepage URL");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Registry yazma hatası: {ex.Message}");
            throw;
        }
    }

    public bool IsKioskModeEnabled()
    {
        InitializeCache();
        return _cachedKioskMode;
    }

    public bool IsFullscreenModeEnabled()
    {
        InitializeCache();
        return _cachedFullscreenMode;
    }

    public bool IsKioskMode
    {
        get
        {
            InitializeCache();
            return _cachedKioskMode;
        }
    }

    public bool IsFullscreen
    {
        get
        {
            InitializeCache();
            return _cachedFullscreenMode;
        }
    }

    public void ExitKioskMode()
    {
        try
        {
            using var key = OpenRegistryKey(true);
            key?.SetValue("KioskMode", 0, RegistryValueKind.DWord);
            key?.SetValue("FullscreenMode", 0, RegistryValueKind.DWord);

            _cachedKioskMode = false;
            _cachedFullscreenMode = false;

            Debug.WriteLine("[WebViewManager] Kiosk mode disabled in registry and cache");
            ShowTaskbar();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Kiosk mode exit hatası: {ex.Message}");
        }
    }

    [DllImport("user32.dll")]
    private static extern int ShowWindow(int hwnd, int command);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 1;

    public void ShowTaskbar()
    {
        try
        {
            int hwnd = FindWindow("Shell_TrayWnd", "").ToInt32();
            ShowWindow(hwnd, SW_SHOW);

            hwnd = FindWindow("Button", null).ToInt32();
            ShowWindow(hwnd, SW_SHOW);

            Debug.WriteLine("[WebViewManager] Taskbar shown");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Taskbar show hatası: {ex.Message}");
        }
    }

    public void HideTaskbar()
    {
        try
        {
            int hwnd = FindWindow("Shell_TrayWnd", "").ToInt32();
            ShowWindow(hwnd, SW_HIDE);

            hwnd = FindWindow("Button", "").ToInt32();
            ShowWindow(hwnd, SW_HIDE);

            Debug.WriteLine("[WebViewManager] Taskbar hidden");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Taskbar hide hatası: {ex.Message}");
        }
    }

    public void NavigateHomepage()
    {
        if (_webView.CoreWebView2 is not null)
        {
            string url = GetHomepageUrl();
            _webView.CoreWebView2.Navigate(url);
            Debug.WriteLine($"[WebViewManager] Anasayfaya yönlendirildi: {url}");
        }
        else
        {
            Debug.WriteLine("[WebViewManager] WebView2 hazır değil, Navigate başarısız.");
        }
    }

    public void Reload()
    {
        _webView.CoreWebView2?.Reload();
        Debug.WriteLine("[WebViewManager] Sayfa yenilendi");
    }

    public WebView2 Raw => _webView;

    public void LogRegistryStatus()
    {
        Debug.WriteLine("=== Registry Status ===");

        using var key64 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_64);
        Debug.WriteLine($"64-bit Registry ({REGISTRY_KEY_64}): {(key64 != null ? "MEVCUT" : "YOK")}");

        using var key32 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_32);
        Debug.WriteLine($"32-bit Registry ({REGISTRY_KEY_32}): {(key32 != null ? "MEVCUT" : "YOK")}");

        Debug.WriteLine($"Aktif konum: {GetRegistryKey()}");
        Debug.WriteLine($"Cache: Kiosk={_cachedKioskMode}, Fullscreen={_cachedFullscreenMode}");
        Debug.WriteLine($"Script Injected: {_scriptInjected}");
        Debug.WriteLine("========================");
    }
}