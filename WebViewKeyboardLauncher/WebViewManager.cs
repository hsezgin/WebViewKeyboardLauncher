using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using System.Diagnostics;

namespace WebViewKeyboardLauncher;

public class WebViewManager
{
    private readonly WebView2 _webView;
    private const string REGISTRY_KEY = @"SOFTWARE\WebViewKeyboardLauncher";
    private const string DEFAULT_URL = "https://promanage.sanovel.com.tr/sanovel/ui";

    public event Action? OnFocusReceived;

    public WebViewManager(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
    }

    public void Initialize()
    {
        _webView.EnsureCoreWebView2Async(null).ContinueWith(_ =>
        {
            RegisterFocusAndMessageListeners();
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
                    Debug.WriteLine("[WebViewManager] Focus mesajı alındı");
                    OnFocusReceived?.Invoke();
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

        string script = "window.addEventListener('load', () => { document.body.addEventListener('focusin', function(e) { if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') { window.chrome.webview.postMessage('focus'); } }, true); });";

        _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
    }

    public string GetHomepageUrl()
    {
        try
        {
            // HKEY_CURRENT_USER'dan URL'i oku
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
            if (key?.GetValue("Homepage") is string url && !string.IsNullOrWhiteSpace(url))
            {
                Debug.WriteLine($"[WebViewManager] Registry'den URL alındı: {url}");
                return url;
            }

            // Fallback: HKEY_LOCAL_MACHINE'den dene
            using var keyLM = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY);
            if (keyLM?.GetValue("Homepage") is string urlLM && !string.IsNullOrWhiteSpace(urlLM))
            {
                Debug.WriteLine($"[WebViewManager] HKLM Registry'den URL alındı: {urlLM}");
                return urlLM;
            }

            Debug.WriteLine("[WebViewManager] Registry'de URL bulunamadı, default URL kullanılıyor");
            return DEFAULT_URL;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Registry okuma hatası: {ex.Message}");
            return DEFAULT_URL;
        }
    }

    public void SetHomepageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);
            key?.SetValue("Homepage", url, RegistryValueKind.String);
            Debug.WriteLine($"[WebViewManager] URL registry'ye kaydedildi: {url}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Registry yazma hatası: {ex.Message}");
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
}