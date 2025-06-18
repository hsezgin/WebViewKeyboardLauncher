using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace WebViewKeyboardLauncher;

public class KeyboardManager
{
    public KeyboardManager()
    {
        // Hiçbir şey yapmıyoruz, sadece On event'ını bekliyoruz
    }

    // Tek event - sadece On
    public void On()
    {
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