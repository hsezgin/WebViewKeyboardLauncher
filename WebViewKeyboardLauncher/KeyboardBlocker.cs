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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebViewKeyboardLauncher
{
    /// <summary>
    /// Kiosk mode'da tüm sistem tuş kombinasyonlarını engelleyen class
    /// </summary>
    public class KeyboardBlocker
    {
        private readonly WebViewManager _webViewManager;
        private readonly Form _parentForm;

        // Windows API imports
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSCOMMAND = 0x0112;

        // Virtual key codes
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        // System command codes
        private const int SC_MOVE = 0xF010;
        private const int SC_SIZE = 0xF000;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_CLOSE = 0xF060;

        // Hook variables
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static WebViewManager? _staticWebViewManager;

        // Events
        public event Action? EmergencyExitRequested;

        public KeyboardBlocker(WebViewManager webViewManager, Form parentForm)
        {
            _webViewManager = webViewManager;
            _parentForm = parentForm;
        }

        /// <summary>
        /// Kiosk mode tuş engellemesini başlat
        /// </summary>
        public void StartBlocking()
        {
            if (!_webViewManager.IsKioskMode) return;

            // Form event'lerini bağla
            _parentForm.KeyPreview = true;
            _parentForm.KeyDown += OnKeyDown;

            // Global hook'u kur
            _staticWebViewManager = _webViewManager;
            _hookID = SetHook();

            Debug.WriteLine("[KeyboardBlocker] Kiosk mode key blocking started");
            Debug.WriteLine("[KeyboardBlocker] Global hook installed: " + _hookID);
        }

        /// <summary>
        /// Tuş engellemesini durdur
        /// </summary>
        public void StopBlocking()
        {
            // Form event'lerini kaldır
            _parentForm.KeyDown -= OnKeyDown;

            // Global hook'u kaldır
            RemoveHook();
            _staticWebViewManager = null;

            Debug.WriteLine("[KeyboardBlocker] Key blocking stopped");
        }

        /// <summary>
        /// Form KeyDown event handler
        /// </summary>
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_webViewManager.IsKioskMode) return;

            // Tek Windows tuşları
            switch (e.KeyCode)
            {
                case Keys.LWin:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Left Windows Key");
                    return;

                case Keys.RWin:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Right Windows Key");
                    return;
            }

            // Windows kombinasyonları
            if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin ||
                Control.ModifierKeys.HasFlag(Keys.LWin) || Control.ModifierKeys.HasFlag(Keys.RWin))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Debug.WriteLine("[BLOCKED] Windows Key + " + e.KeyCode + " combination");
                return;
            }

            // Diğer sistem tuşları
            switch (e.KeyCode)
            {
                case Keys.F4 when e.Alt:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Alt+F4 - Close window attempt (KeyDown)");
                    break;

                case Keys.Tab when e.Alt:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Alt+Tab - Task switching (KeyDown)");
                    break;

                case Keys.Tab when e.Alt && e.Shift: // ✅ YENİ: Alt+Shift+Tab ek güvenlik
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Alt+Shift+Tab - Reverse task switching (KeyDown)");
                    break;

                case Keys.Escape when e.Control:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Ctrl+Esc - Start menu access");
                    break;

                case Keys.Escape when e.Control && e.Shift:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Ctrl+Shift+Esc - Task Manager (KeyDown)");
                    break;

                case Keys.PrintScreen when e.Alt:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Alt+Print Screen - Window screenshot (KeyDown)");
                    break;

                case Keys.PrintScreen:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Print Screen - Screenshot (KeyDown)");
                    break;

                case Keys.F1:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] F1 - Help blocked (KeyDown)");
                    break;

                case Keys.F5 when e.Control:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Ctrl+F5 - Hard refresh (KeyDown)");
                    break;

                case Keys.F11:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] F11 - Fullscreen toggle (KeyDown)");
                    break;

                case Keys.F12:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] F12 - Developer tools (KeyDown)");
                    break;

                case Keys.Apps: // ✅ YENİ: Menu/Application key (klavyedeki sağ tık tuşu)
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Debug.WriteLine("[BLOCKED] Menu/Application key - Context menu (KeyDown)");
                    break;

                // Emergency exit: Ctrl+Shift+Alt+E
                case Keys.E when e.Control && e.Shift && e.Alt:
                    Debug.WriteLine("[EMERGENCY] Ctrl+Shift+Alt+E - Emergency exit triggered");
                    EmergencyExitRequested?.Invoke();
                    e.Handled = true;
                    break;

                default:
                    if (e.Control || e.Alt || e.Shift)
                    {
                        Debug.WriteLine("[KEY] Combination: " + e.KeyCode + " (Ctrl:" + e.Control + ", Alt:" + e.Alt + ", Shift:" + e.Shift + ")");
                    }
                    break;
            }
        }

        /// <summary>
        /// ProcessCmdKey override için metod
        /// </summary>
        public bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!_webViewManager.IsKioskMode) return false;

            // Windows kombinasyonları
            bool isWinPressed = (keyData & Keys.LWin) == Keys.LWin || (keyData & Keys.RWin) == Keys.RWin;

            if (isWinPressed)
            {
                Keys baseKey = keyData & ~Keys.LWin & ~Keys.RWin;

                switch (baseKey)
                {
                    case Keys.D:
                        Debug.WriteLine("[BLOCKED] Win+D - Show Desktop");
                        return true;
                    case Keys.L:
                        Debug.WriteLine("[BLOCKED] Win+L - Lock Screen");
                        return true;
                    case Keys.R:
                        Debug.WriteLine("[BLOCKED] Win+R - Run Dialog");
                        return true;
                    case Keys.E:
                        Debug.WriteLine("[BLOCKED] Win+E - File Explorer");
                        return true;
                    case Keys.I:
                        Debug.WriteLine("[BLOCKED] Win+I - Settings");
                        return true;
                    case Keys.X:
                        Debug.WriteLine("[BLOCKED] Win+X - Power User Menu");
                        return true;
                    case Keys.A:
                        Debug.WriteLine("[BLOCKED] Win+A - Action Center");
                        return true;
                    case Keys.S:
                        Debug.WriteLine("[BLOCKED] Win+S - Search");
                        return true;
                    case Keys.PrintScreen:
                        Debug.WriteLine("[BLOCKED] Win+Print Screen - Screenshot");
                        return true;
                    case Keys.Tab:
                        Debug.WriteLine("[BLOCKED] Win+Tab - Task View");
                        return true;
                    case Keys.M:
                        Debug.WriteLine("[BLOCKED] Win+M - Minimize All");
                        return true;
                    case Keys.U:
                        Debug.WriteLine("[BLOCKED] Win+U - Ease of Access");
                        return true;
                    case Keys.P:
                        Debug.WriteLine("[BLOCKED] Win+P - Project Display");
                        return true;
                    default:
                        Debug.WriteLine("[BLOCKED] Windows combination: Win+" + baseKey);
                        return true;
                }
            }

            // Diğer kombinasyonlar
            switch (keyData)
            {
                case Keys.Control | Keys.Escape:
                    Debug.WriteLine("[BLOCKED] Ctrl+Esc (ProcessCmdKey)");
                    return true;
                case Keys.Control | Keys.Shift | Keys.Escape:
                    Debug.WriteLine("[BLOCKED] Ctrl+Shift+Esc - Task Manager");
                    return true;
                case Keys.Control | Keys.Alt | Keys.Delete:
                    Debug.WriteLine("[BLOCKED] Ctrl+Alt+Del - Security screen");
                    return true;
                case Keys.Alt | Keys.F4:
                    Debug.WriteLine("[BLOCKED] Alt+F4 (ProcessCmdKey)");
                    return true;
                case Keys.Alt | Keys.Tab:
                    Debug.WriteLine("[BLOCKED] Alt+Tab (ProcessCmdKey)");
                    return true;
                case Keys.Alt | Keys.Shift | Keys.Tab:
                    Debug.WriteLine("[BLOCKED] Alt+Shift+Tab");
                    return true;
                case Keys.PrintScreen:
                    Debug.WriteLine("[BLOCKED] Print Screen (ProcessCmdKey)");
                    return true;
                case Keys.Alt | Keys.PrintScreen:
                    Debug.WriteLine("[BLOCKED] Alt+Print Screen (ProcessCmdKey)");
                    return true;
                case Keys.F11:
                    Debug.WriteLine("[BLOCKED] F11 - Fullscreen toggle (ProcessCmdKey)");
                    return true;
                case Keys.F12:
                    Debug.WriteLine("[BLOCKED] F12 - Developer tools (ProcessCmdKey)");
                    return true;
                case Keys.Control | Keys.F5:
                    Debug.WriteLine("[BLOCKED] Ctrl+F5 (ProcessCmdKey)");
                    return true;
                case Keys.Apps: // ✅ YENİ: Menu/Application key
                    Debug.WriteLine("[BLOCKED] Menu/Application key (ProcessCmdKey)");
                    return true;
                default:
                    if ((keyData & Keys.Control) == Keys.Control ||
                        (keyData & Keys.Alt) == Keys.Alt ||
                        (keyData & Keys.Shift) == Keys.Shift)
                    {
                        Debug.WriteLine("[CMDKEY] Key combination: " + keyData);
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// WndProc override için metod
        /// </summary>
        public bool ProcessWndProc(ref Message m)
        {
            if (!_webViewManager.IsKioskMode) return false;

            switch (m.Msg)
            {
                case WM_SYSCOMMAND:
                    int command = m.WParam.ToInt32() & 0xFFF0;
                    switch (command)
                    {
                        case SC_MOVE:
                            Debug.WriteLine("[BLOCKED] WM_SYSCOMMAND - Window move blocked");
                            return true;
                        case SC_SIZE:
                            Debug.WriteLine("[BLOCKED] WM_SYSCOMMAND - Window resize blocked");
                            return true;
                        case SC_MINIMIZE:
                            Debug.WriteLine("[BLOCKED] WM_SYSCOMMAND - Window minimize blocked");
                            return true;
                        case SC_CLOSE:
                            Debug.WriteLine("[BLOCKED] WM_SYSCOMMAND - Window close blocked");
                            return true;
                    }
                    break;

                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    int vkCode = m.WParam.ToInt32();
                    if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                    {
                        Debug.WriteLine("[BLOCKED] WndProc - Windows Key (VK: " + vkCode.ToString("X") + ")");
                        return true;
                    }
                    break;

                case WM_KEYUP:
                case WM_SYSKEYUP:
                    int vkCodeUp = m.WParam.ToInt32();
                    if (vkCodeUp == VK_LWIN || vkCodeUp == VK_RWIN)
                    {
                        Debug.WriteLine("[BLOCKED] WndProc - Windows Key Up (VK: " + vkCodeUp.ToString("X") + ")");
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Global keyboard hook kurma
        /// </summary>
        private static IntPtr SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName!), 0);
            }
        }

        /// <summary>
        /// Global hook callback
        /// </summary>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _staticWebViewManager?.IsKioskMode == true)
            {
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    // Windows tuşlarını engelle
                    if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                    {
                        Debug.WriteLine("[GLOBAL HOOK] Windows Key blocked (VK: " + vkCode.ToString("X") + ")");
                        return (IntPtr)1; // Block the key
                    }

                    // Ctrl+Esc kontrolü
                    if (vkCode == 0x1B) // VK_ESCAPE
                    {
                        // Ctrl basılı mı kontrol et
                        if ((GetAsyncKeyState(0x11) & 0x8000) != 0) // VK_CONTROL
                        {
                            Debug.WriteLine("[GLOBAL HOOK] Ctrl+Esc blocked");
                            return (IntPtr)1; // Block the key
                        }
                    }

                    // ✅ YENİ: Alt+Tab kontrolü
                    if (vkCode == 0x09) // VK_TAB
                    {
                        // Alt basılı mı kontrol et
                        if ((GetAsyncKeyState(0x12) & 0x8000) != 0) // VK_MENU (Alt)
                        {
                            Debug.WriteLine("[GLOBAL HOOK] Alt+Tab blocked");
                            return (IntPtr)1; // Block the key
                        }
                    }

                    // ✅ YENİ: Alt+F4 kontrolü
                    if (vkCode == 0x73) // VK_F4
                    {
                        // Alt basılı mı kontrol et
                        if ((GetAsyncKeyState(0x12) & 0x8000) != 0) // VK_MENU (Alt)
                        {
                            Debug.WriteLine("[GLOBAL HOOK] Alt+F4 blocked");
                            return (IntPtr)1; // Block the key
                        }
                    }

                    // ✅ YENİ: Alt tuşlarını da engelle (çok agresif)
                    if (vkCode == 0x12) // VK_MENU (Alt tuşu)
                    {
                        Debug.WriteLine("[GLOBAL HOOK] Alt key blocked");
                        return (IntPtr)1; // Block the key
                    }

                    // ✅ YENİ: Print Screen engelleme
                    if (vkCode == 0x2C) // VK_SNAPSHOT (Print Screen)
                    {
                        Debug.WriteLine("[GLOBAL HOOK] Print Screen blocked");
                        return (IntPtr)1; // Block the key
                    }

                    // ✅ YENİ: Menu/Application key engelleme
                    if (vkCode == 0x5D) // VK_APPS (Menu/Application key)
                    {
                        Debug.WriteLine("[GLOBAL HOOK] Menu/Application key blocked");
                        return (IntPtr)1; // Block the key
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Hook'u kaldır
        /// </summary>
        private void RemoveHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Debug.WriteLine("[KeyboardBlocker] Global keyboard hook removed");
            }
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Dispose()
        {
            StopBlocking();
        }
    }
}