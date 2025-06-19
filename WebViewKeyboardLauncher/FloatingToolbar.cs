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
using System.Windows.Forms;

namespace WebViewKeyboardLauncher
{
    public partial class FloatingToolbar : Form
    {
        private KeyboardButton _keyboardButton = null!;
        private Button _settingsButton = null!;
        private SettingsButtonForm? _settingsButtonForm;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // WebViewManager referansı
        private WebViewManager? _webViewManager;

        public event EventHandler? KeyboardButtonClicked;

        public FloatingToolbar()
        {
            InitializeComponent();
            SetupToolbar();
            SetupButtons();
            SetupDragAndDrop();
        }

        public void SetWebViewManager(WebViewManager webViewManager)
        {
            _webViewManager = webViewManager;
            System.Diagnostics.Debug.WriteLine("[FloatingToolbar] WebViewManager bağlandı");
        }

        private bool _kioskMode = false;

        public void SetKioskMode(bool kioskMode)
        {
            _kioskMode = kioskMode;

            // Settings button'ı HER ZAMAN göster
            _settingsButton.Visible = true;
            this.Size = new Size(120, 50);

            // Butonları normal konumlarında tut
            _keyboardButton.Location = new Point(5, 5);
            _keyboardButton.Size = new Size(50, 40);
            _settingsButton.Location = new Point(65, 5);
            _settingsButton.Size = new Size(50, 40);

            this.TopMost = true;

            System.Diagnostics.Debug.WriteLine($"[FloatingToolbar] Mode: {(kioskMode ? "Kiosk" : "Normal")} - Standard behavior");
        }

        private void SetupToolbar()
        {
            // AppStyles'dan form stilini uygula
            AppStyles.ApplyToolbarForm(this);

            // Başlangıç konumu - taskbar dahil ekran sınırları
            Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            this.Location = new Point(
                screenBounds.Width - this.Width - 100,
                screenBounds.Height - this.Height - 50
            );

            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.SetTopLevel(true);
            this.BringToFront();
        }

        private void SetupButtons()
        {
            // Designer'da oluşturulan butonlara field'ları ata
            _keyboardButton = this.keyboardButton;
            _settingsButton = this.settingsButton;

            // AppStyles'dan stil uygula
            AppStyles.ApplyKeyboardButton(_keyboardButton);
            AppStyles.ApplyStandardButton(_settingsButton);

            // Event'ları bağla
            _keyboardButton.Click += (s, e) => KeyboardButtonClicked?.Invoke(this, EventArgs.Empty);
            _settingsButton.Click += SettingsButton_Click;
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            if (_settingsButtonForm == null || _settingsButtonForm.IsDisposed)
            {
                _settingsButtonForm = new SettingsButtonForm();

                if (_webViewManager != null)
                {
                    _settingsButtonForm.SetWebViewManager(_webViewManager);
                }

                // ✅ BASİT: Normal TopMost davranışı
                _settingsButtonForm.TopMost = false;

                PositionSettingsForm();
                _settingsButtonForm.Show();
            }
            else if (_settingsButtonForm.Visible)
            {
                _settingsButtonForm.Hide();
            }
            else
            {
                PositionSettingsForm();
                _settingsButtonForm.Show();
                _settingsButtonForm.Focus();
            }
        }

        // SettingsForm'un konumunu toolbar'a göre optimal şekilde ayarlayan metod
        private void PositionSettingsForm()
        {
            if (_settingsButtonForm == null || _settingsButtonForm.IsDisposed)
                return;

            Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            Point toolbarLocation = this.Location;
            Size toolbarSize = this.Size;
            Size settingsSize = _settingsButtonForm.Size;

            Point optimalLocation;

            // Alt taraf kontrolü (ekranın alt 1/10'u)
            bool isInBottomArea = toolbarLocation.Y >= (screenBounds.Height - screenBounds.Height / 10);

            // Sağ tarafa yapışık mı kontrolü (ekranın sağ 50px'i)
            bool isAtRightEdge = toolbarLocation.X >= (screenBounds.Width - toolbarSize.Width - 50);

            if (isInBottomArea)
            {
                if (isAtRightEdge)
                {
                    // Sağ tarafa yapışık - SOL'a aç
                    Point leftPosition = new Point(
                        toolbarLocation.X - settingsSize.Width - 2,
                        toolbarLocation.Y
                    );

                    if (leftPosition.X >= 0)
                    {
                        optimalLocation = leftPosition;
                    }
                    else
                    {
                        optimalLocation = new Point(
                            toolbarLocation.X,
                            Math.Max(0, toolbarLocation.Y - settingsSize.Height - 2)
                        );
                    }
                }
                else
                {
                    // Alt tarafta ama sağ köşede değil - SAĞ'a aç
                    Point rightPosition = new Point(
                        toolbarLocation.X + toolbarSize.Width + 2,
                        toolbarLocation.Y
                    );

                    if (rightPosition.X + settingsSize.Width <= screenBounds.Width)
                    {
                        optimalLocation = rightPosition;
                    }
                    else
                    {
                        Point leftPosition = new Point(
                            toolbarLocation.X - settingsSize.Width - 2,
                            toolbarLocation.Y
                        );

                        if (leftPosition.X >= 0)
                        {
                            optimalLocation = leftPosition;
                        }
                        else
                        {
                            optimalLocation = new Point(
                                toolbarLocation.X,
                                Math.Max(0, toolbarLocation.Y - settingsSize.Height - 2)
                            );
                        }
                    }
                }
            }
            else
            {
                // Üst/orta tarafta - normale alta aç
                Point bottomPosition = new Point(
                    toolbarLocation.X,
                    toolbarLocation.Y + toolbarSize.Height + 2
                );

                if (bottomPosition.Y + settingsSize.Height <= screenBounds.Height)
                {
                    optimalLocation = bottomPosition;
                }
                else
                {
                    // Yer yoksa sağa aç
                    optimalLocation = new Point(
                        toolbarLocation.X + toolbarSize.Width + 2,
                        toolbarLocation.Y
                    );
                }
            }

            _settingsButtonForm.StartPosition = FormStartPosition.Manual;
            _settingsButtonForm.Location = optimalLocation;
        }

        private void SetupDragAndDrop()
        {
            this.MouseDown += ToolbarMouseDown;
            this.MouseMove += ToolbarMouseMove;
            this.MouseUp += ToolbarMouseUp;

            this.keyboardButton.MouseDown += ToolbarMouseDown;
            this.keyboardButton.MouseMove += ToolbarMouseMove;
            this.keyboardButton.MouseUp += ToolbarMouseUp;

            this.settingsButton.MouseDown += ToolbarMouseDown;
            this.settingsButton.MouseMove += ToolbarMouseMove;
            this.settingsButton.MouseUp += ToolbarMouseUp;
        }

        private void ToolbarMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;

                if (sender is Control control && control != this)
                {
                    _dragStartPoint = new Point(
                        _dragStartPoint.X + control.Location.X,
                        _dragStartPoint.Y + control.Location.Y
                    );
                }

                if (_settingsButtonForm != null && !_settingsButtonForm.IsDisposed && _settingsButtonForm.Visible)
                {
                    _settingsButtonForm.PauseAutoCloseTimer();
                }
            }
        }

        private void ToolbarMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging && e.Button == MouseButtons.Left)
            {
                Point currentPoint = e.Location;

                if (sender is Control control && control != this)
                {
                    currentPoint = new Point(
                        currentPoint.X + control.Location.X,
                        currentPoint.Y + control.Location.Y
                    );
                }

                Point newLocation = new Point(
                    this.Location.X + (currentPoint.X - _dragStartPoint.X),
                    this.Location.Y + (currentPoint.Y - _dragStartPoint.Y)
                );

                newLocation = KeepWithinScreenBounds(newLocation);
                this.Location = newLocation;

                if (_settingsButtonForm != null && !_settingsButtonForm.IsDisposed && _settingsButtonForm.Visible)
                {
                    PositionSettingsForm();
                }
            }
        }

        private Point KeepWithinScreenBounds(Point location)
        {
            Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

            if (location.X < 0)
                location.X = 0;

            if (location.X + this.Width > screenBounds.Width)
                location.X = screenBounds.Width - this.Width;

            if (location.Y < 0)
                location.Y = 0;

            if (location.Y + this.Height > screenBounds.Height)
                location.Y = screenBounds.Height - this.Height;

            return location;
        }

        private void ToolbarMouseUp(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                if (_settingsButtonForm != null && !_settingsButtonForm.IsDisposed && _settingsButtonForm.Visible)
                {
                    _settingsButtonForm.ResumeAutoCloseTimer();
                }
            }
        }
    }
}