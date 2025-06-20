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
    public class KeyboardButton : Button
    {
        private bool _isDragging = false;
        private bool _hasMoved = false;
        private Point _dragStartPoint;

        public KeyboardButton()
        {
            // AppStyles'dan stil uygula
            ApplyDefaultStyle();
        }

        private void ApplyDefaultStyle()
        {
            // Default değerler (bunlar AppStyles.ApplyKeyboardButton ile ezilecek)
            this.Text = "⌨️";
            this.FlatStyle = FlatStyle.Standard;
            this.Font = new Font("Segoe UI Ico", 16, FontStyle.Regular);
            this.BackColor = Color.LightGray;
            this.ForeColor = Color.White;
            this.FlatAppearance.BorderSize = 1;
            this.TextAlign = ContentAlignment.TopCenter;

            this.UseCompatibleTextRendering = false;
            this.UseVisualStyleBackColor = false;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _hasMoved = false;
                _dragStartPoint = e.Location;
                System.Diagnostics.Debug.WriteLine("KeyboardButton MouseDown");
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _isDragging)
            {
                if (!_hasMoved)
                {
                    System.Diagnostics.Debug.WriteLine("KeyboardButton Click algılandı - event tetikleniyor");
                    this.PerformClick();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("KeyboardButton sürüklendi - click iptal");
                }
                _isDragging = false;
                _hasMoved = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                int dx = Math.Abs(e.Location.X - _dragStartPoint.X);
                int dy = Math.Abs(e.Location.Y - _dragStartPoint.Y);

                if (dx > 4 || dy > 4)
                {
                    _hasMoved = true;
                    System.Diagnostics.Debug.WriteLine("KeyboardButton hareket ediyor");
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnPaint(PaintEventArgs pevent)
        {
            // Base çizmesini engelle
            // base.OnPaint(pevent);

            // Arka planı çiz
            using (var brush = new SolidBrush(this.BackColor))
            {
                pevent.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            // Text'i tam ortaya çiz
            var rect = this.ClientRectangle;
            rect.Y -= 0; // 3px yukarı kaydır (bu değeri ayarlayın)
            rect.Inflate(-1, -1); // 2px kenarlık bırak

            using (var brush = new SolidBrush(this.ForeColor))
            {
                var sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                pevent.Graphics.DrawString(this.Text, this.Font, brush, rect, sf);
            }
        }
    }
}