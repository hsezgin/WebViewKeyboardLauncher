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

namespace WebViewKeyboardLauncher
{
    partial class MainForm
    {
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None; // Scaling problemi �nlemek i�in
            this.ClientSize = new System.Drawing.Size(800, 450); // Bu de�er MainForm.cs'de ezilecek
            this.Name = "MainForm";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None; // Tam ekran i�in
            this.WindowState = System.Windows.Forms.FormWindowState.Normal; // Normal state
            this.ResumeLayout(false);
        }
    }
}