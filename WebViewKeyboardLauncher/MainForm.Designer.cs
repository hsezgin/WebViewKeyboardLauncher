namespace WebViewKeyboardLauncher
{
    partial class MainForm
    {
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None; // Scaling problemi önlemek için
            this.ClientSize = new System.Drawing.Size(800, 450); // Bu deðer MainForm.cs'de ezilecek
            this.Name = "MainForm";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None; // Tam ekran için
            this.WindowState = System.Windows.Forms.FormWindowState.Normal; // Normal state
            this.ResumeLayout(false);
        }
    }
}