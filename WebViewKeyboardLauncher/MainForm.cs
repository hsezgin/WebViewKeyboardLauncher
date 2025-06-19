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

using Microsoft.Web.WebView2.WinForms;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebViewKeyboardLauncher
{
    public partial class MainForm : Form
    {
        private WebView2 webView = null!;
        private FloatingToolbar toolbar = null!;
        private KeyboardManager keyboardManager = null!;
        private WebViewManager webViewManager = null!;
        private KeyboardBlocker? keyboardBlocker; // ✅ YENİ: Tuş engelleme sistemi

#if DEBUG
        private System.Windows.Forms.Timer? debugSafetyTimer;
        private int debugCountdown = 30;
#endif

        // Windows API for window management (sadece gerekli olanlar)
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public MainForm()
        {
            InitializeComponent();
            InitializeForm();
            InitializeManagers();
            InitializeAsync();

#if DEBUG
            SetupDebugSafetyTimer();
#endif
        }

#if DEBUG
        private void SetupDebugSafetyTimer()
        {
            debugSafetyTimer = new System.Windows.Forms.Timer();
            debugSafetyTimer.Interval = 1000;
            debugSafetyTimer.Tick += DebugSafetyTimer_Tick;
            debugSafetyTimer.Start();

            System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety timer started - Auto exit in 30 seconds");
        }

        private void DebugSafetyTimer_Tick(object? sender, EventArgs e)
        {
            debugCountdown--;

            if (debugCountdown % 5 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"🛡️ DEBUG: Auto exit in {debugCountdown} seconds...");
            }

            if (webViewManager?.IsKioskMode == true)
            {
                this.Text = $"DEBUG Kiosk Mode - Auto Exit: {debugCountdown}s";
            }

            if (debugCountdown <= 0)
            {
                debugSafetyTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety timer triggered - Force exit!");
                ForceExitKioskMode();
            }
        }

        private void ForceExitKioskMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WebViewKeyboardLauncher");
                key?.SetValue("KioskMode", 0, Microsoft.Win32.RegistryValueKind.DWord);
                key?.SetValue("FullscreenMode", 0, Microsoft.Win32.RegistryValueKind.DWord);

                System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Registry cleaned");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🛡️ DEBUG: Registry cleanup error: {ex.Message}");
            }

            // Keyboard blocker'ı durdur
            keyboardBlocker?.StopBlocking();

            // Normal form davranışı
            this.TopMost = false;
            this.ShowInTaskbar = true;
            this.ControlBox = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Text = "WebView Keyboard Launcher - DEBUG MODE";

            // Toolbar'ı normal moda döndür
            if (toolbar != null)
            {
                toolbar.SetKioskMode(false);
            }

            webViewManager?.ExitKioskMode();

            MessageBox.Show("DEBUG: Kiosk mode automatically disabled after 30 seconds!\n\nThis prevents being stuck in debug mode.",
                "Debug Safety Exit", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety exit completed");
        }

        private void StopDebugSafetyTimer()
        {
            if (debugSafetyTimer != null)
            {
                debugSafetyTimer.Stop();
                debugSafetyTimer.Dispose();
                debugSafetyTimer = null;
                System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety timer stopped");
            }
        }

        private void LogKioskStatus()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WebViewKeyboardLauncher");
                if (key != null)
                {
                    var kioskMode = key.GetValue("KioskMode");
                    var fullscreenMode = key.GetValue("FullscreenMode");
                    var homepage = key.GetValue("Homepage");

                    System.Diagnostics.Debug.WriteLine("========= KIOSK STATUS =========");
                    System.Diagnostics.Debug.WriteLine($"Registry KioskMode: {kioskMode}");
                    System.Diagnostics.Debug.WriteLine($"Registry FullscreenMode: {fullscreenMode}");
                    System.Diagnostics.Debug.WriteLine($"Registry Homepage: {homepage}");
                    System.Diagnostics.Debug.WriteLine($"WebViewManager.IsKioskMode: {webViewManager?.IsKioskMode}");
                    System.Diagnostics.Debug.WriteLine($"KeyPreview: {this.KeyPreview}");
                    System.Diagnostics.Debug.WriteLine($"TopMost: {this.TopMost}");
                    System.Diagnostics.Debug.WriteLine($"WindowState: {this.WindowState}");
                    System.Diagnostics.Debug.WriteLine("===============================");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ REGISTRY KEY NOT FOUND!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Registry read error: {ex.Message}");
            }
        }

        private void SetDebugKioskMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WebViewKeyboardLauncher");
                key?.SetValue("KioskMode", 1, Microsoft.Win32.RegistryValueKind.DWord);
                key?.SetValue("FullscreenMode", 1, Microsoft.Win32.RegistryValueKind.DWord);
                key?.SetValue("Homepage", "https://promanage.sanovel.com.tr/sanovel/ui", Microsoft.Win32.RegistryValueKind.String);

                System.Diagnostics.Debug.WriteLine("🔧 DEBUG: Kiosk mode zorla aktif edildi!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Registry yazma hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("⚠️  Visual Studio'yu Administrator olarak çalıştırın!");
            }
        }
#endif

        private void InitializeForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.Size = Screen.PrimaryScreen?.Bounds.Size ?? new Size(1920, 1080);
            this.WindowState = FormWindowState.Normal;

            System.Diagnostics.Debug.WriteLine($"[MainForm] Form initialized: {this.Size}");
        }

        private void InitializeManagers()
        {
            keyboardManager = new KeyboardManager();

            // WebView2 setup
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            // WebViewManager setup
            webViewManager = new WebViewManager(webView);
            webViewManager.OnFocusReceived += () => keyboardManager.On();

#if DEBUG
            SetDebugKioskMode();
#endif
            webViewManager.Initialize();

            // Toolbar setup
            toolbar = new FloatingToolbar();
            toolbar.KeyboardButtonClicked += Toolbar_KeyboardButtonClicked;
            toolbar.SetWebViewManager(webViewManager);

            // Apply kiosk mode settings
            ApplyKioskModeToForm();

            // Toolbar mode ayarı
            if (webViewManager.IsKioskMode)
            {
                toolbar.SetKioskMode(true);
                System.Diagnostics.Debug.WriteLine("[MainForm] Kiosk mode - Using normal z-index behavior");
            }

#if DEBUG
            LogKioskStatus();
#endif

            toolbar.Show();
        }

        private void ApplyKioskModeToForm()
        {
            if (webViewManager?.IsKioskMode != true)
            {
                return; // Normal mode
            }

            System.Diagnostics.Debug.WriteLine("[MainForm] Applying kiosk mode - KEYS ONLY, NO Z-INDEX FORCING");

            // ✅ Form kontrolleri (kapanma engelleme)
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.ShowInTaskbar = false;

            // ✅ YENİ: Keyboard blocker'ı başlat
            keyboardBlocker = new KeyboardBlocker(webViewManager, this);
            keyboardBlocker.EmergencyExitRequested += OnEmergencyExit;
            keyboardBlocker.StartBlocking();

            // ✅ Fullscreen mode
            if (webViewManager.IsFullscreen)
            {
                webViewManager.HideTaskbar();
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Maximized;
            }

            System.Diagnostics.Debug.WriteLine("[MainForm] Kiosk mode applied - KeyboardBlocker started");
        }

        // ✅ YENİ: Emergency exit handler
        private void OnEmergencyExit()
        {
            if (MessageBox.Show("Exit kiosk mode?", "Kiosk Mode",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine("✅ [EMERGENCY] User confirmed - Exiting kiosk mode");
                ExitKioskMode();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ [EMERGENCY] User cancelled - Staying in kiosk mode");
            }
        }

        private void ExitKioskMode()
        {
            System.Diagnostics.Debug.WriteLine("[MainForm] Exiting kiosk mode...");

#if DEBUG
            StopDebugSafetyTimer();
#endif

            // ✅ Keyboard blocker'ı durdur
            keyboardBlocker?.StopBlocking();
            keyboardBlocker = null;

            // Exit kiosk mode in WebViewManager
            webViewManager.ExitKioskMode();

            // Restore normal form behavior
            this.TopMost = false;
            this.ShowInTaskbar = true;
            this.ControlBox = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.WindowState = FormWindowState.Normal;

            // Restore normal toolbar mode
            if (toolbar != null)
            {
                toolbar.SetKioskMode(false);
                toolbar.Show();
            }
        }

        private void Toolbar_KeyboardButtonClicked(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔍 [DEBUG] Keyboard button clicked");
            keyboardManager.On();
        }

        private void InitializeAsync()
        {
            webView.EnsureCoreWebView2Async(null).ContinueWith(_ =>
            {
                Invoke(new Action(() =>
                {
                    webViewManager.NavigateHomepage();
                    System.Diagnostics.Debug.WriteLine($"🌐 Uygulama başlatıldı - Kiosk Mode: {webViewManager.IsKioskMode}");
                }));
            });
        }

        // ✅ KeyboardBlocker kullanarak ProcessCmdKey
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyboardBlocker?.ProcessCmdKey(ref msg, keyData) == true)
            {
                return true; // KeyboardBlocker engelledi
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ✅ KeyboardBlocker kullanarak WndProc
        protected override void WndProc(ref Message m)
        {
            if (keyboardBlocker?.ProcessWndProc(ref m) == true)
            {
                return; // KeyboardBlocker engelledi
            }
            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            // In kiosk mode, always stay visible
            if (webViewManager?.IsKioskMode == true)
            {
                base.SetVisibleCore(true);
            }
            else
            {
                base.SetVisibleCore(value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
#if DEBUG
                StopDebugSafetyTimer();
#endif

                // ✅ KeyboardBlocker'ı temizle
                keyboardBlocker?.Dispose();

                keyboardManager?.Cleanup();
                toolbar?.Dispose();

                // Restore taskbar when exiting (safety measure)
                if (webViewManager?.IsKioskMode == true)
                {
                    webViewManager.ShowTaskbar();
                }

                System.Diagnostics.Debug.WriteLine("🧹 MainForm disposed");
            }
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prevent closing in kiosk mode unless specifically allowed
            if (webViewManager?.IsKioskMode == true && e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                System.Diagnostics.Debug.WriteLine("[MainForm] Form close blocked in kiosk mode");
                return;
            }

            keyboardManager?.Cleanup();
            base.OnFormClosing(e);
        }
    }
}