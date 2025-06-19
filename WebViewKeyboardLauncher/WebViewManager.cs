/*
 * WebViewManager.cs - Registry Cache ile Performance Düzeltmesi
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

    // 🔧 CACHE EKLEME - Registry sürekli okunmasın
    private bool _cacheInitialized = false;
    private bool _cachedKioskMode = false;
    private bool _cachedFullscreenMode = false;
    private string _cachedHomepageUrl = DEFAULT_URL;

    public WebViewManager(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
    }

    /// <summary>
    /// Cache'i bir kez initialize et
    /// </summary>
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

    /// <summary>
    /// Registry anahtarının mevcut olduğu konumu bulur (32-bit veya 64-bit)
    /// </summary>
    private string GetRegistryKey()
    {
        // Önce 64-bit konumunu kontrol et
        using var key64 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_64);
        if (key64 != null)
        {
            return REGISTRY_KEY_64;
        }

        // Bulamazsa 32-bit konumunu kontrol et
        using var key32 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_32);
        if (key32 != null)
        {
            return REGISTRY_KEY_32;
        }

        // Hiçbiri yoksa 64-bit konumunu varsayılan olarak döndür
        return REGISTRY_KEY_64;
    }

    /// <summary>
    /// Hem 32-bit hem 64-bit konumlardan registry değeri okur
    /// </summary>
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
        // İlk başta cache'i yükle
        InitializeCache();

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebViewKeyboardLauncher");

        var env = CoreWebView2Environment.CreateAsync(null, userDataFolder).Result;

        _webView.EnsureCoreWebView2Async(env).ContinueWith(task =>
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                RegisterFocusAndMessageListeners();
                NavigateHomepage();
            }
            else
            {
                Debug.WriteLine($"[WebViewManager] WebView2 başlatılamadı: {task.Exception?.Message}");
            }
        });
    }

    private void RegisterFocusAndMessageListeners()
    {
        _webView.CoreWebView2.WebMessageReceived += (sender, e) =>
        {
            string message = e.TryGetWebMessageAsString();
            switch (message)
            {
                case "focus":
                    System.Diagnostics.Debug.WriteLine("🔍 [DEBUG] WebView Focus event received - BEFORE keyboard trigger");
                    OnFocusReceived?.Invoke();

                    // Focus event sonrası 100ms bekleyip toolbar'ı koru
                    System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                    {
                        System.Diagnostics.Debug.WriteLine("🔍 [DEBUG] Post-focus toolbar protection triggered");
                        OnToolbarProtectionNeeded?.Invoke();
                    });


                    System.Diagnostics.Debug.WriteLine("🔍 [DEBUG] WebView Focus event processed - AFTER keyboard trigger");
                    break;
                case "refresh":
                    Debug.WriteLine("[WebViewManager] Refresh mesajı alındı");
                    Reload();
                    break;
                default:
                    Debug.WriteLine($"[WebViewManager] Bilinmeyen mesaj: {message}");
                    break;
            }
        };

        // Script aynı kalacak...
        string script = @"
        window.addEventListener('load', () => {
            console.log('🔍 [WEB DEBUG] Page loaded, setting up focus listeners');
            
            document.body.addEventListener('focusin', function(e) {
                console.log('🔍 [WEB DEBUG] Focus event on:', e.target.tagName, e.target.type);
                
                if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || 
                    e.target.contentEditable === 'true' || e.target.getAttribute('role') === 'textbox') {
                    console.log('🔍 [WEB DEBUG] Triggering keyboard for focus on:', e.target.tagName);
                    window.chrome.webview.postMessage('focus');
                } else {
                    console.log('🔍 [WEB DEBUG] Focus ignored for:', e.target.tagName);
                }
            }, true);
            
            window.addEventListener('focus', () => {
                console.log('🔍 [WEB DEBUG] Window gained focus');
            });
            
            window.addEventListener('blur', () => {
                console.log('🔍 [WEB DEBUG] Window lost focus');
            });
        });
    ";

        _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
    }

    public event Action? OnToolbarProtectionNeeded;

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

            // Cache'i güncelle
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

    // 🔧 CACHE'DEN DÖNECEK - Registry'ye sürekli erişim yok
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

    // Property versions for MainForm compatibility - CACHE'den gelecek
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

            // Cache'i güncelle
            _cachedKioskMode = false;
            _cachedFullscreenMode = false;

            Debug.WriteLine("[WebViewManager] Kiosk mode disabled in registry and cache");

            // Show taskbar
            ShowTaskbar();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Kiosk mode exit hatası: {ex.Message}");
        }
    }

    [DllImport("user32.dll")]
    private static extern int FindWindow(string? className, string? windowText);

    [DllImport("user32.dll")]
    private static extern int ShowWindow(int hwnd, int command);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 1;

    public void ShowTaskbar()
    {
        try
        {
            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, SW_SHOW);

            // Also show start button
            hwnd = FindWindow("Button", null);
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
            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, SW_HIDE);

            // Also hide start button
            hwnd = FindWindow("Button", "");
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

    /// <summary>
    /// Debug amaçlı: Registry durumunu kontrol eder
    /// </summary>
    public void LogRegistryStatus()
    {
        Debug.WriteLine("=== Registry Status ===");

        using var key64 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_64);
        Debug.WriteLine($"64-bit Registry ({REGISTRY_KEY_64}): {(key64 != null ? "MEVCUT" : "YOK")}");

        using var key32 = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_32);
        Debug.WriteLine($"32-bit Registry ({REGISTRY_KEY_32}): {(key32 != null ? "MEVCUT" : "YOK")}");

        Debug.WriteLine($"Aktif konum: {GetRegistryKey()}");
        Debug.WriteLine($"Cache: Kiosk={_cachedKioskMode}, Fullscreen={_cachedFullscreenMode}");
        Debug.WriteLine("========================");
    }
}