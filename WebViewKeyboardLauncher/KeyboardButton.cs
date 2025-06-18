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
            this.Font = new Font("Segoe UI", 16, FontStyle.Regular);
            this.BackColor = Color.LightGray;
            this.ForeColor = Color.Black;
            this.FlatAppearance.BorderSize = 1;
            this.TextAlign = ContentAlignment.MiddleCenter;
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
    }
}