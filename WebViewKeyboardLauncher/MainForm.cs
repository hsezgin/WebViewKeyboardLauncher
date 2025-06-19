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

#if DEBUG
        private System.Windows.Forms.Timer? debugSafetyTimer;
        private int debugCountdown = 30;
#endif

        // Windows API for window management
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int GWL_HWNDPARENT = -8;

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
            debugSafetyTimer.Interval = 1000; // Her saniye
            debugSafetyTimer.Tick += DebugSafetyTimer_Tick;
            debugSafetyTimer.Start();

            System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety timer started - Auto exit in 30 seconds");
        }

        private void DebugSafetyTimer_Tick(object? sender, EventArgs e)
        {
            debugCountdown--;

            // Her 5 saniyede log
            if (debugCountdown % 5 == 0)
            {
                System.Diagnostics.Debug.WriteLine($"🛡️ DEBUG: Auto exit in {debugCountdown} seconds...");
            }

            // Title'da countdown göster (sadece kiosk mode'da)
            if (webViewManager?.IsKioskMode == true)
            {
                this.Text = $"DEBUG Kiosk Mode - Auto Exit: {debugCountdown}s";
            }

            if (debugCountdown <= 0)
            {
                debugSafetyTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety timer triggered - Force exit!");

                // Zorla kiosk mode'dan çık
                ForceExitKioskMode();
            }
        }

        private void ForceExitKioskMode()
        {
            try
            {
                // Registry'yi temizle
                using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WebViewKeyboardLauncher");
                key?.SetValue("KioskMode", 0, Microsoft.Win32.RegistryValueKind.DWord);
                key?.SetValue("FullscreenMode", 0, Microsoft.Win32.RegistryValueKind.DWord);

                System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Registry cleaned");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🛡️ DEBUG: Registry cleanup error: {ex.Message}");
            }

            // Form'u normal moda döndür
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
                toolbar.TopMost = false;
            }

            // WebViewManager'ı bilgilendir
            webViewManager?.ExitKioskMode();

            // Uyarı mesajı
            MessageBox.Show("DEBUG SAFETY: Kiosk mode automatically disabled after 30 seconds!\n\nThis prevents being stuck in debug mode.",
                "Debug Safety Exit", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            System.Diagnostics.Debug.WriteLine("🛡️ DEBUG: Safety exit completed");
        }

        // Timer'ı durdurma metodu (manuel exit'te kullanmak için)
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
#endif

        private void InitializeForm()
        {
            // Basic form settings
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

            // ✅ TEMIZ ÇÖZÜM: Sadece throttle, Z-order manipülasyonu yok
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

            if (webViewManager.IsKioskMode)
            {
                toolbar.SetKioskMode(true);

                // ✅ WINDOW GROUPING: Toolbar'ı MainForm'un "owned window"ı yap
                SetupWindowGrouping();

                System.Diagnostics.Debug.WriteLine("[MainForm] Toolbar in kiosk mode with window grouping");
            }

            toolbar.Show();
        }

        // ✅ TEST ÇÖZÜMÜ: Group + Periodic Z-Order Maintenance
        private void SetupWindowGrouping()
        {
            try
            {
                // 1. Owner relationship kur
                SetWindowLong(toolbar.Handle, GWL_HWNDPARENT, this.Handle);

                // 2. Her ikisini de TopMost yap
                this.TopMost = true;
                toolbar.TopMost = true;

                // 3. Toolbar'ı öne getir
                toolbar.BringToFront();

                System.Diagnostics.Debug.WriteLine("🔗 [GROUPING] Basic grouping established");

                // 4. ✅ YENİ: Periyodik Z-order maintenance (sadece kiosk mode'da)
                if (webViewManager.IsKioskMode)
                {
                    var zOrderTimer = new System.Windows.Forms.Timer();
                    zOrderTimer.Interval = 2000; // Her 2 saniyede
                    zOrderTimer.Tick += (s, e) => {
                        try
                        {
                            if (toolbar != null && !toolbar.IsDisposed && toolbar.Visible)
                            {
                                toolbar.BringToFront();
                                System.Diagnostics.Debug.WriteLine("🔗 [MAINTENANCE] Toolbar Z-order maintained");
                            }
                        }
                        catch { }
                    };
                    zOrderTimer.Start();

                    System.Diagnostics.Debug.WriteLine("🔗 [GROUPING] Z-order maintenance timer started");
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [GROUPING] Grouping failed: {ex.Message}");

                // Fallback
                toolbar.TopMost = true;
                this.TopMost = true;
                toolbar.BringToFront();
            }
        }

        private void ApplyKioskModeToForm()
        {
            if (webViewManager?.IsKioskMode != true)
            {
                // Normal mode - not topmost
                this.TopMost = false;
                return;
            }

            System.Diagnostics.Debug.WriteLine("[MainForm] Applying kiosk mode settings");

            // Prevent form from being moved or resized
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.ShowInTaskbar = false;

            // Disable Alt+F4, Alt+Tab etc.
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // Hide taskbar in fullscreen mode
            if (webViewManager.IsFullscreen)
            {
                webViewManager.HideTaskbar();
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Maximized;
            }

            System.Diagnostics.Debug.WriteLine("[MainForm] Kiosk mode applied - Using window grouping for Z-Order");
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (webViewManager?.IsKioskMode != true) return;

            // Block system key combinations in kiosk mode
            switch (e.KeyCode)
            {
                case Keys.F4 when e.Alt: // Alt+F4
                case Keys.Tab when e.Alt: // Alt+Tab
                case Keys.Escape when e.Control: // Ctrl+Esc
                case Keys.LWin: // Windows key
                case Keys.RWin: // Windows key
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    System.Diagnostics.Debug.WriteLine($"[MainForm] Blocked key combination: {e.KeyCode}");
                    break;

                // Emergency exit: Ctrl+Shift+Alt+E
                case Keys.E when e.Control && e.Shift && e.Alt:
                    if (MessageBox.Show("Exit kiosk mode?", "Kiosk Mode",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        ExitKioskMode();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void ExitKioskMode()
        {
            System.Diagnostics.Debug.WriteLine("[MainForm] Exiting kiosk mode...");

#if DEBUG
            StopDebugSafetyTimer();
#endif

            // Exit kiosk mode in WebViewManager
            webViewManager.ExitKioskMode();

            // Restore normal form behavior
            this.TopMost = false;
            this.ShowInTaskbar = true;
            this.ControlBox = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.WindowState = FormWindowState.Normal;

            // Remove topmost flag
            SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            // Restore normal toolbar mode
            if (toolbar != null)
            {
                toolbar.SetKioskMode(false); // Normal mode'a döndür
                toolbar.TopMost = false; // Remove TopMost from toolbar
                toolbar.Show();
            }

            // Remove key blocking
            this.KeyDown -= MainForm_KeyDown;
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

                    // Focus the form in kiosk mode
                    if (webViewManager.IsKioskMode)
                    {
                        this.Activate();
                        this.Focus();
                        SetForegroundWindow(this.Handle);
                    }

                    System.Diagnostics.Debug.WriteLine($"🌐 Uygulama başlatıldı - Kiosk Mode: {webViewManager.IsKioskMode}");
                }));
            });
        }

        // Override WndProc to block certain system messages in kiosk mode
        protected override void WndProc(ref Message m)
        {
            if (webViewManager?.IsKioskMode == true)
            {
                const int WM_SYSCOMMAND = 0x0112;
                const int SC_MOVE = 0xF010;
                const int SC_SIZE = 0xF000;
                const int SC_MINIMIZE = 0xF020;
                const int SC_CLOSE = 0xF060;

                switch (m.Msg)
                {
                    case WM_SYSCOMMAND:
                        int command = m.WParam.ToInt32() & 0xFFF0;
                        if (command == SC_MOVE || command == SC_SIZE ||
                            command == SC_MINIMIZE || command == SC_CLOSE)
                        {
                            // Block these system commands in kiosk mode
                            return;
                        }
                        break;
                }
            }

            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (webViewManager?.IsKioskMode == true)
            {
                // Block additional key combinations
                switch (keyData)
                {
                    case Keys.Control | Keys.Shift | Keys.Escape: // Task Manager
                    case Keys.Control | Keys.Alt | Keys.Delete: // Security screen
                    case Keys.Alt | Keys.F4: // Close window
                        return true; // Block the key
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
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

#if DEBUG
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
    }
}