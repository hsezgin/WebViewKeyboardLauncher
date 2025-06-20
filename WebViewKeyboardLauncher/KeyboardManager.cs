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
using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace WebViewKeyboardLauncher;

public class KeyboardManager
{
    // ✅ THROTTLE - Aynı anda birden fazla TabTip açılmasını engelle
    private DateTime _lastTabTipOpen = DateTime.MinValue;
    private const int TABTIP_COOLDOWN_MS = 500; // 500ms cooldown

    public KeyboardManager()
    {
        // Hiçbir şey yapmıyoruz, sadece On event'ını bekliyoruz
    }

    // Tek event - sadece On
    public void On()
    {
        // ✅ THROTTLE CHECK - Son 500ms içinde TabTip açıldıysa ignore et
        var now = DateTime.Now;
        if ((now - _lastTabTipOpen).TotalMilliseconds < TABTIP_COOLDOWN_MS)
        {
            Debug.WriteLine($"[THROTTLE] TabTip ignored - cooldown active ({(now - _lastTabTipOpen).TotalMilliseconds}ms)");
            return;
        }

        _lastTabTipOpen = now;
        ShowKeyboard();
        Debug.WriteLine("TabTip açıldı");
    }

    private void ShowKeyboard()
    {
        try
        {
            // Registry ayarı - TabTip'in desktop'ta açılabilmesi için
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\TabletTip\1.7", true);
            key?.SetValue("EnableDesktopModeAutoInvoke", 1, RegistryValueKind.DWord);

            // TabTip'i başlat
            string? tabTipPath = GetTabTipPath();
            if (!string.IsNullOrEmpty(tabTipPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = tabTipPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                });
                Debug.WriteLine($"TabTip başlatıldı: {tabTipPath}");
            }
            else
            {
                // Fallback - OSK
                Process.Start(new ProcessStartInfo
                {
                    FileName = "osk.exe",
                    UseShellExecute = true
                });
                Debug.WriteLine("OSK fallback başlatıldı");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Klavye başlatma hatası: {ex.Message}");
        }
    }

    private string? GetTabTipPath()
    {
        string[] possiblePaths = {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "Microsoft Shared", "ink", "TabTip.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "Microsoft Shared", "ink", "TabTip.exe"),
            @"C:\Program Files\Common Files\Microsoft Shared\ink\TabTip.exe",
            @"C:\Program Files (x86)\Common Files\Microsoft Shared\ink\TabTip.exe"
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    public void Cleanup()
    {
        // Hiçbir şey yapmıyoruz - TabTip kendi halinde
        Debug.WriteLine("KeyboardManager cleanup - hiçbir şey yapmıyor");
    }
}