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

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebViewKeyboardLauncher
{
    public partial class SettingsButtonForm : Form
    {
        private readonly SettingsController _controller;

        // Timer'lar uzun basma kontrolü için
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private System.Windows.Forms.Timer _restartTimer = null!;
        private System.Windows.Forms.Timer _autoCloseTimer = null!;

        // Basılı tutma durumu kontrolü
        private bool _refreshHoldCompleted = false;
        private bool _restartHoldCompleted = false;

        // Windows API for Z-Order management
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public SettingsButtonForm()
        {
            InitializeComponent();
            _controller = new SettingsController();

            // AppStyles'dan form stilini uygula
            AppStyles.ApplySettingsForm(this);

            // Butonlara stil uygula
            ApplyButtonStyles();

            BindEvents();
            InitializeTimers();
        }

        public void SetKioskModeTopMost(bool kioskMode)
        {
            if (kioskMode)
            {
                this.TopMost = true;
                // Z-Order'ı explicit olarak en üstte ayarla
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                System.Diagnostics.Debug.WriteLine("[SettingsButtonForm] TopMost enabled for kiosk mode");
            }
            else
            {
                this.TopMost = false;
                System.Diagnostics.Debug.WriteLine("[SettingsButtonForm] TopMost disabled for normal mode");
            }
        }

        private void ApplyButtonStyles()
        {
            // AppStyles'dan buton stillerini uygula
            AppStyles.ApplyRefreshButton(this.btnRefresh);
            AppStyles.ApplyRestartButton(this.btnRestart);

            // Icon'ları tekrar set et (style uygulandıktan sonra)
            this.btnRefresh.Text = "🔄";
            this.btnRestart.Text = "⏻";
        }

        private void InitializeTimers()
        {
            // Refresh timer - 2 saniye
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 2000;
            _refreshTimer.Tick += RefreshTimer_Tick;

            // Restart timer - 3 saniye
            _restartTimer = new System.Windows.Forms.Timer();
            _restartTimer.Interval = 3000;
            _restartTimer.Tick += RestartTimer_Tick;

            // Auto close timer - 10 saniye
            _autoCloseTimer = new System.Windows.Forms.Timer();
            _autoCloseTimer.Interval = 10000;
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        }

        private void BindEvents()
        {
            // Mouse events for long press
            this.btnRefresh.MouseDown += BtnRefresh_MouseDown;
            this.btnRefresh.MouseUp += BtnRefresh_MouseUp;
            this.btnRefresh.MouseLeave += BtnRefresh_MouseLeave;

            this.btnRestart.MouseDown += BtnRestart_MouseDown;
            this.btnRestart.MouseUp += BtnRestart_MouseUp;
            this.btnRestart.MouseLeave += BtnRestart_MouseLeave;

            // Form events for auto close
            this.Shown += SettingsButtonForm_Shown;
            this.MouseMove += SettingsButtonForm_UserActivity;
            this.btnRefresh.MouseMove += SettingsButtonForm_UserActivity;
            this.btnRestart.MouseMove += SettingsButtonForm_UserActivity;
        }

        #region Refresh Button Events

        private void BtnRefresh_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _refreshHoldCompleted = false;
                _refreshTimer.Start();
                ResetAutoCloseTimer();

                // Visual feedback - AppStyles'dan pressed rengini kullan
                btnRefresh.BackColor = AppStyles.RefreshPressed;
                System.Diagnostics.Debug.WriteLine("[Refresh] MouseDown - Timer başlatıldı (2 saniye)");
            }
        }

        private void BtnRefresh_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _refreshTimer.Stop();

                // Normal renge geri döndür
                btnRefresh.BackColor = AppStyles.RefreshNormal;

                if (!_refreshHoldCompleted)
                {
                    // Normal click - just refresh
                    _controller.ReloadWebView();
                    System.Diagnostics.Debug.WriteLine("[Refresh] Normal tıklama - Sayfa yenilendi");
                }

                _refreshHoldCompleted = false;
            }
        }

        private void BtnRefresh_MouseLeave(object? sender, EventArgs e)
        {
            _refreshTimer.Stop();
            btnRefresh.BackColor = AppStyles.RefreshNormal;
            _refreshHoldCompleted = false;
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            _refreshTimer.Stop();
            _refreshHoldCompleted = true;

            // Normal renge geri döndür
            btnRefresh.BackColor = AppStyles.RefreshNormal;

            // 2 seconds held - go to homepage
            _controller.NavigateHomepage();
            System.Diagnostics.Debug.WriteLine("[Refresh] 2 saniye basılı tutuldu - Homepage'e yönlendiriliyor");
        }

        #endregion

        #region Restart Button Events

        private void BtnRestart_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _restartHoldCompleted = false;
                _restartTimer.Start();
                ResetAutoCloseTimer();

                // Visual feedback - AppStyles'dan pressed rengini kullan
                btnRestart.BackColor = AppStyles.RestartPressed;
                System.Diagnostics.Debug.WriteLine("[Restart] MouseDown - Timer başlatıldı (3 saniye)");
            }
        }

        private void BtnRestart_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _restartTimer.Stop();

                // Normal renge geri döndür
                btnRestart.BackColor = AppStyles.RestartNormal;

                if (!_restartHoldCompleted)
                {
                    // Normal click - do nothing for security
                    System.Diagnostics.Debug.WriteLine("[Restart] Normal tıklama - İşlem yapılmadı (güvenlik)");
                }

                _restartHoldCompleted = false;
            }
        }

        private void BtnRestart_MouseLeave(object? sender, EventArgs e)
        {
            _restartTimer.Stop();
            btnRestart.BackColor = AppStyles.RestartNormal;
            _restartHoldCompleted = false;
        }

        private void RestartTimer_Tick(object? sender, EventArgs e)
        {
            _restartTimer.Stop();
            _restartHoldCompleted = true;

            // Normal renge geri döndür
            btnRestart.BackColor = AppStyles.RestartNormal;

            // 3 seconds held - restart
            _controller.RestartSystem();
            System.Diagnostics.Debug.WriteLine("[Restart] 3 saniye basılı tutuldu - Sistem restart ediliyor");
        }

        #endregion

        #region Auto Close Events

        private void SettingsButtonForm_Shown(object? sender, EventArgs e)
        {
            _autoCloseTimer.Start();
            System.Diagnostics.Debug.WriteLine("[SettingsForm] Form gösterildi - 10 saniye auto close timer başladı");
        }

        private void SettingsButtonForm_UserActivity(object? sender, EventArgs e)
        {
            ResetAutoCloseTimer();
        }

        private void ResetAutoCloseTimer()
        {
            _autoCloseTimer.Stop();
            _autoCloseTimer.Start();
            System.Diagnostics.Debug.WriteLine("[SettingsForm] Auto close timer resetlendi (10 saniye)");
        }

        private void AutoCloseTimer_Tick(object? sender, EventArgs e)
        {
            _autoCloseTimer.Stop();
            this.Hide();
            System.Diagnostics.Debug.WriteLine("[SettingsForm] 10 saniye geçti - Form otomatik kapatıldı");
        }

        #endregion

        #region Public Methods

        public void SetWebViewManager(WebViewManager webViewManager)
        {
            _controller.SetWebViewManager(webViewManager);
        }

        public void PauseAutoCloseTimer()
        {
            _autoCloseTimer?.Stop();
            System.Diagnostics.Debug.WriteLine("[SettingsForm] Auto-close timer durduruldu (sürükleme)");
        }

        public void ResumeAutoCloseTimer()
        {
            _autoCloseTimer?.Stop();
            _autoCloseTimer?.Start();
            System.Diagnostics.Debug.WriteLine("[SettingsForm] Auto-close timer yeniden başlatıldı (10 saniye)");
        }

        #endregion
    }
}