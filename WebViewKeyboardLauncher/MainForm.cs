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

        // Windows API for kiosk mode window management
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public MainForm()
        {
            InitializeComponent();
            InitializeForm();
            InitializeManagers();
            InitializeAsync();
        }

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
            webViewManager.OnFocusReceived += () => keyboardManager.On();
            webViewManager.Initialize();

            // Toolbar setup - klavye erişimi her zaman garantili
            toolbar = new FloatingToolbar();
            toolbar.KeyboardButtonClicked += Toolbar_KeyboardButtonClicked;
            toolbar.SetWebViewManager(webViewManager);

            // Apply kiosk mode settings to form AFTER toolbar creation
            ApplyKioskModeToForm();

            // Kiosk mode'da toolbar'ı özel modda göster (sadece klavye butonu)
            if (webViewManager.IsKioskMode)
            {
                toolbar.SetKioskMode(true); // Settings gizle, sadece keyboard kalsın
                System.Diagnostics.Debug.WriteLine("[MainForm] Toolbar in kiosk mode - keyboard only");
            }

            // Her durumda toolbar'ı göster (klavye erişimi için)
            toolbar.Show();

            // Z-Order düzeltmesi - hem toolbar hem settings formları için
            EnsureToolbarAndSettingsVisibility();
        }

        private void EnsureToolbarAndSettingsVisibility()
        {
            if (webViewManager?.IsKioskMode == true && toolbar != null)
            {
                // Toolbar'ı en üstte tut
                toolbar.TopMost = true;
                toolbar.Show();
                toolbar.BringToFront();

                // Toolbar'ın Z-Order pozisyonu
                SetWindowPos(toolbar.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                System.Diagnostics.Debug.WriteLine("[MainForm] Toolbar Z-Order: TopMost, above MainForm");
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

            // Kiosk mode - set as topmost but lowest priority among topmost windows
            this.TopMost = true;

            // Prevent form from being moved or resized
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.ShowInTaskbar = false;

            // Disable Alt+F4, Alt+Tab etc.
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // Set MainForm as TopMost but with lower priority than toolbar
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            // Hide taskbar in fullscreen mode
            if (webViewManager.IsFullscreen)
            {
                webViewManager.HideTaskbar();
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Maximized;
            }

            System.Diagnostics.Debug.WriteLine("[MainForm] Kiosk mode applied - Z-Order: MainForm < Toolbar/Settings < TabTip");
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
            keyboardManager.On();
            System.Diagnostics.Debug.WriteLine("Toolbar keyboard button tıklandı - TabTip açılıyor");
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

                        // Re-ensure toolbar visibility after form activation
                        EnsureToolbarAndSettingsVisibility();
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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Ensure toolbar stays visible when form gets focus in kiosk mode
            if (webViewManager?.IsKioskMode == true)
            {
                EnsureToolbarAndSettingsVisibility();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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