using System.Drawing;
using System.Windows.Forms;

namespace WebViewKeyboardLauncher
{
    /// <summary>
    /// Uygulamanın tüm görsel stil ayarlarını merkezi olarak yöneten static class
    /// </summary>
    public static class AppStyles
    {
        #region Colors
        public static readonly Color BackgroundDark = Color.FromArgb(64, 64, 64);
        public static readonly Color BackgroundMedium = Color.FromArgb(80, 80, 80);
        public static readonly Color ButtonNormal = Color.FromArgb(96, 96, 96);
        public static readonly Color ButtonHover = Color.FromArgb(128, 128, 128);
        public static readonly Color ButtonPressed = Color.FromArgb(160, 160, 160);

        public static readonly Color RefreshNormal = Color.FromArgb(70, 130, 180);
        public static readonly Color RefreshHover = Color.FromArgb(100, 150, 200);
        public static readonly Color RefreshPressed = Color.FromArgb(50, 110, 160);

        public static readonly Color RestartNormal = Color.FromArgb(220, 20, 60);
        public static readonly Color RestartHover = Color.FromArgb(240, 60, 90);
        public static readonly Color RestartPressed = Color.FromArgb(180, 10, 40);

        public static readonly Color TextWhite = Color.White;
        public static readonly Color TextBlack = Color.Black;
        #endregion

        #region Fonts
        public static readonly Font ButtonFont = new Font("Segoe UI Emoji", 13F, FontStyle.Regular);
        public static readonly Font KeyboardFont = new Font("Segoe UI", 20F, FontStyle.Regular); // 24'den 20'ye düşürdük
        #endregion

        #region Button Styles
        public static void ApplyStandardButton(Button button)
        {
            button.BackColor = ButtonNormal;
            button.ForeColor = TextWhite;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = ButtonFont;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ButtonHover;
            button.FlatAppearance.MouseDownBackColor = ButtonPressed;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseCompatibleTextRendering = false;
            button.UseVisualStyleBackColor = false;
            button.Margin = new Padding(0);
            button.Padding = new Padding(0);

            // Focus border'ını kaldır
            button.TabStop = false; // Tab ile focus almasını engelle
        }

        public static void ApplyKeyboardButton(Button button)
        {
            button.BackColor = ButtonNormal;
            button.ForeColor = TextBlack;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = KeyboardFont;
            button.FlatAppearance.BorderSize = 0; // Border'ı tamamen kaldır
            button.FlatAppearance.MouseOverBackColor = ButtonHover;
            button.FlatAppearance.MouseDownBackColor = ButtonPressed;
            button.TextAlign = ContentAlignment.MiddleCenter; // Tekrar orta hizalama
            button.UseCompatibleTextRendering = false;
            button.UseVisualStyleBackColor = false;
            button.Margin = new Padding(0);
            button.Padding = new Padding(0);

            // Focus border'ını kaldır
            button.TabStop = false; // Tab ile focus almasını engelle

            // İkonu tam ortalamak için
            button.TextImageRelation = TextImageRelation.Overlay;
        }

        public static void ApplyRefreshButton(Button button)
        {
            ApplyStandardButton(button);
            button.BackColor = RefreshNormal;
            button.FlatAppearance.MouseOverBackColor = RefreshHover;
            button.FlatAppearance.MouseDownBackColor = RefreshPressed;
            button.TabStop = false; // Focus border'ını kaldır
        }

        public static void ApplyRestartButton(Button button)
        {
            ApplyStandardButton(button);
            button.BackColor = RestartNormal;
            button.FlatAppearance.MouseOverBackColor = RestartHover;
            button.FlatAppearance.MouseDownBackColor = RestartPressed;
            button.TabStop = false; // Focus border'ını kaldır
        }
        #endregion

        #region Form Styles
        public static void ApplyToolbarForm(Form form)
        {
            form.BackColor = BackgroundDark;
            form.FormBorderStyle = FormBorderStyle.None;
            form.TopMost = true;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Size = new Size(120, 50);
            form.MaximumSize = new Size(120, 50);
            form.MinimumSize = new Size(120, 50);
            form.AutoScaleMode = AutoScaleMode.None;
            form.AutoSize = false;
        }

        public static void ApplySettingsForm(Form form)
        {
            ApplyToolbarForm(form);
            form.BackColor = BackgroundMedium;
        }
        #endregion
    }
}