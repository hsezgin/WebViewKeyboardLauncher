namespace WebViewKeyboardLauncher
{
    partial class FloatingToolbar
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private WebViewKeyboardLauncher.KeyboardButton keyboardButton;
        private System.Windows.Forms.Button settingsButton;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.keyboardButton = new WebViewKeyboardLauncher.KeyboardButton();
            this.settingsButton = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // 
            // keyboardButton - Stil AppStyles'dan uygulanacak
            // 
            this.keyboardButton.Location = new System.Drawing.Point(5, 5);
            this.keyboardButton.Name = "keyboardButton";
            this.keyboardButton.Size = new System.Drawing.Size(50, 40);
            this.keyboardButton.TabIndex = 0;

            // 
            // settingsButton - Stil AppStyles'dan uygulanacak
            // 
            this.settingsButton.Location = new System.Drawing.Point(65, 5);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(50, 40);
            this.settingsButton.TabIndex = 1;
            this.settingsButton.Text = "⚙️";

            // 
            // FloatingToolbar - Stil AppStyles'dan uygulanacak
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = false;
            this.ClientSize = new System.Drawing.Size(120, 50);
            this.Controls.Add(this.keyboardButton);
            this.Controls.Add(this.settingsButton);
            this.Name = "FloatingToolbar";
            this.ResumeLayout(false);
        }

        #endregion
    }
}