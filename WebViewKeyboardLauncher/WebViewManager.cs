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

    // Windows API for kiosk mode
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    public event Action? OnFocusReceived;

    // Kiosk mode properties
    public bool IsKioskMode { get; private set; }
    public bool IsFullscreen { get; private set; }
    public bool IsTaskbarHidden { get; private set; }

    public WebViewManager(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        LoadKioskConfiguration();
    }

    private void LoadKioskConfiguration()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
            if (key != null)
            {
                IsKioskMode = Convert.ToBoolean(key.GetValue("KioskMode", false));
                IsFullscreen = Convert.ToBoolean(key.GetValue("Fullscreen", false));
                IsTaskbarHidden = Convert.ToBoolean(key.GetValue("DisableTaskbar", false));

                Debug.WriteLine($"[WebViewManager] Kiosk Mode: {IsKioskMode}, Fullscreen: {IsFullscreen}, Hide Taskbar: {IsTaskbarHidden}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Kiosk configuration load error: {ex.Message}");
        }
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
                ApplyKioskMode();
                NavigateHomepage();
            }
            else
            {
                Debug.WriteLine($"[WebViewManager] WebView2 başlatılamadı: {task.Exception?.Message}");
            }
        });
    }

    private void ApplyKioskMode()
    {
        if (!IsKioskMode) return;

        try
        {
            // Hide taskbar if requested
            if (IsTaskbarHidden)
            {
                HideTaskbar();
            }

            // Disable context menu and developer tools
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

                Debug.WriteLine("[WebViewManager] Kiosk mode restrictions applied to WebView2");
            }

            // Disable right-click and F12
            var disableScript = @"
                document.addEventListener('contextmenu', e => e.preventDefault());
                document.addEventListener('keydown', e => {
                    // Disable F12, Ctrl+Shift+I, Ctrl+U, etc.
                    if (e.key === 'F12' || 
                        (e.ctrlKey && e.shiftKey && e.key === 'I') ||
                        (e.ctrlKey && e.key === 'u') ||
                        (e.ctrlKey && e.shiftKey && e.key === 'J')) {
                        e.preventDefault();
                    }
                });
                
                // Send kiosk mode flag to page
                window.KIOSK_MODE = true;
                window.FULLSCREEN_MODE = " + (IsFullscreen ? "true" : "false") + @";
            ";

            _webView.CoreWebView2?.AddScriptToExecuteOnDocumentCreatedAsync(disableScript);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Kiosk mode application error: {ex.Message}");
        }
    }

    private void HideTaskbar()
    {
        try
        {
            // Find and hide taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            if (taskbarHandle != IntPtr.Zero)
            {
                ShowWindow(taskbarHandle, SW_HIDE);
                Debug.WriteLine("[WebViewManager] Taskbar hidden");
            }

            // Hide start button
            IntPtr startButtonHandle = FindWindow("Button", "Start");
            if (startButtonHandle != IntPtr.Zero)
            {
                ShowWindow(startButtonHandle, SW_HIDE);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Hide taskbar error: {ex.Message}");
        }
    }

    public void ShowTaskbar()
    {
        try
        {
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            if (taskbarHandle != IntPtr.Zero)
            {
                ShowWindow(taskbarHandle, SW_SHOW);
                Debug.WriteLine("[WebViewManager] Taskbar shown");
            }

            // Show start button
            IntPtr startButtonHandle = FindWindow("Button", "Start");
            if (startButtonHandle != IntPtr.Zero)
            {
                ShowWindow(startButtonHandle, SW_SHOW);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Show taskbar error: {ex.Message}");
        }
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
                case "exit_kiosk":
                    if (IsKioskMode)
                    {
                        Debug.WriteLine("[WebViewManager] Exit kiosk mode requested");
                        ExitKioskMode();
                    }
                    break;
                case "toggle_taskbar":
                    if (IsKioskMode && IsTaskbarHidden)
                    {
                        Debug.WriteLine("[WebViewManager] Toggle taskbar requested");
                        ShowTaskbar();
                    }
                    break;
                default:
                    Debug.WriteLine($"[WebViewManager] Bilinmeyen mesaj: {message}");
                    break;
            }
        };

        // Enhanced focus detection script
        string focusScript = @"
            window.addEventListener('load', () => {
                // Focus detection for input elements
                document.body.addEventListener('focusin', function(e) {
                    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.contentEditable === 'true') {
                        window.chrome.webview.postMessage('focus');
                    }
                }, true);
                
                // Kiosk mode helper functions
                if (window.KIOSK_MODE) {
                    window.exitKiosk = function() {
                        window.chrome.webview.postMessage('exit_kiosk');
                    };
                    
                    window.toggleTaskbar = function() {
                        window.chrome.webview.postMessage('toggle_taskbar');
                    };
                    
                    // Add emergency exit sequence (Ctrl+Shift+Alt+X)
                    document.addEventListener('keydown', function(e) {
                        if (e.ctrlKey && e.shiftKey && e.altKey && e.key === 'X') {
                            if (confirm('Exit kiosk mode?')) {
                                window.exitKiosk();
                            }
                        }
                    });
                }
            });
        ";

        _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(focusScript);
    }

    public void ExitKioskMode()
    {
        if (!IsKioskMode) return;

        try
        {
            Debug.WriteLine("[WebViewManager] Exiting kiosk mode...");

            // Show taskbar
            ShowTaskbar();

            // Re-enable WebView2 features
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            }

            // Update registry
            using var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);
            key?.SetValue("KioskMode", false, RegistryValueKind.DWord);

            IsKioskMode = false;
            Debug.WriteLine("[WebViewManager] Kiosk mode exited");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewManager] Exit kiosk mode error: {ex.Message}");
        }
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