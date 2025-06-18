namespace WebViewKeyboardLauncher
{
    partial class SettingsButtonForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnRestart;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _restartTimer?.Stop();
                _restartTimer?.Dispose();
                _autoCloseTimer?.Stop();
                _autoCloseTimer?.Dispose();

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnRestart = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // 
            // btnRefresh - Stil AppStyles'dan uygulanacak
            // 
            this.btnRefresh.Location = new System.Drawing.Point(5, 5);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(50, 40);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "🔄";

            // 
            // btnRestart - Stil AppStyles'dan uygulanacak
            // 
            this.btnRestart.Location = new System.Drawing.Point(65, 5);
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Size = new System.Drawing.Size(50, 40);
            this.btnRestart.TabIndex = 1;
            this.btnRestart.Text = "⏻";

            // 
            // SettingsButtonForm - Stil AppStyles'dan uygulanacak
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = false;
            this.ClientSize = new System.Drawing.Size(120, 50);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnRestart);
            this.Name = "SettingsButtonForm";
            this.ResumeLayout(false);
        }
    }
}