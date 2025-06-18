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
using System.Windows.Forms;

namespace WebViewKeyboardLauncher
{
    public partial class MainForm : Form
    {
        private WebView2 webView;
        private FloatingToolbar toolbar;
        private KeyboardManager keyboardManager;
        private WebViewManager webViewManager;

        public MainForm()
        {
            InitializeComponent();

            // Tam ekran ayarları - taskbar'ı da kapsayacak şekilde
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal; // Önce Normal, sonra manual ayar
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.Size = Screen.PrimaryScreen.Bounds.Size; // WorkingArea değil, Bounds kullan
            this.TopMost = false; // WebView için TopMost olmasın

            System.Diagnostics.Debug.WriteLine($"[MainForm] Tam ekran ayarlandı: {this.Size} - Taskbar dahil");

            keyboardManager = new KeyboardManager();

            // ÖNCE WebView2 ve WebViewManager'ı oluştur
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            webViewManager = new WebViewManager(webView);
            webViewManager.OnFocusReceived += () => keyboardManager.On();
            webViewManager.Initialize();

            // SONRA toolbar'ı oluştur ve webViewManager'ı geçir
            toolbar = new FloatingToolbar();
            toolbar.KeyboardButtonClicked += Toolbar_KeyboardButtonClicked;
            toolbar.SetWebViewManager(webViewManager); // ✅ Artık webViewManager hazır!
            toolbar.Show();

            InitializeAsync();
        }

        private void Toolbar_KeyboardButtonClicked(object sender, EventArgs e)
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
                    System.Diagnostics.Debug.WriteLine("🌐 Uygulama başlatıldı - TabTip On event'larını bekliyor");
                }));
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                keyboardManager?.Cleanup();
                toolbar?.Dispose();
                System.Diagnostics.Debug.WriteLine("🧹 MainForm disposed");
            }
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            keyboardManager?.Cleanup();
            base.OnFormClosing(e);
        }
    }
}