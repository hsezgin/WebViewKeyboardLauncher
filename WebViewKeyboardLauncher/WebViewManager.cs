/*
 * Copyright 2025 SezginBilge
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
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
    private const string REGISTRY_KEY = @"SOFTWARE\WebViewKeyboardLauncher";
    private const string DEFAULT_URL = "https://hsezgin.github.io/WebViewKeyboardLauncher/welcome.html";

    public event Action? OnFocusReceived;

    public WebViewManager(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
    }

    public void Initialize()
    {
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
            // HKEY_LOCAL_MACHINE'den URL'i oku (admin controlled)
            using var key = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY);
            if (key?.GetValue("Homepage") is string url && !string.IsNullOrWhiteSpace(url))
            {
                Debug.WriteLine($"[WebViewManager] Registry'den URL alındı: {url}");
                return url;
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
            // HKEY_LOCAL_MACHINE'e yaz (admin rights gerekli)
            using var key = Registry.LocalMachine.CreateSubKey(REGISTRY_KEY);
            key?.SetValue("Homepage", url, RegistryValueKind.String);
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
        return GetKioskModeFromRegistry();
    }

    public bool IsFullscreenModeEnabled()
    {
        return GetFullscreenModeFromRegistry();
    }

    // Property versions for MainForm compatibility
    public bool IsKioskMode => GetKioskModeFromRegistry();
    public bool IsFullscreen => GetFullscreenModeFromRegistry();

    private bool GetKioskModeFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY);
            if (key?.GetValue("KioskMode") is int kioskMode)
            {
                return kioskMode == 1;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Kiosk mode kontrolü hatası: {ex.Message}");
            return false;
        }
    }

    private bool GetFullscreenModeFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY);
            if (key?.GetValue("FullscreenMode") is int fullscreenMode)
            {
                return fullscreenMode == 1;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Fullscreen mode kontrolü hatası: {ex.Message}");
            return false;
        }
    }

    public void ExitKioskMode()
    {
        try
        {
            // Set kiosk mode to disabled in registry
            using var key = Registry.LocalMachine.CreateSubKey(REGISTRY_KEY);
            key?.SetValue("KioskMode", 0, RegistryValueKind.DWord);
            key?.SetValue("FullscreenMode", 0, RegistryValueKind.DWord);

            Debug.WriteLine("[WebViewManager] Kiosk mode disabled in registry");

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
}