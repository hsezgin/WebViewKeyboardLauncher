using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WebViewKeyboardLauncher;

public class SettingsController
{
    private WebViewManager? _webViewManager;
    private bool _testMode = false;

    // Windows API imports
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out long lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const uint EWX_REBOOT = 0x00000002;
    private const uint EWX_FORCE = 0x00000004;
    private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public long Luid;
        public uint Attributes;
    }

    public void SetWebViewManager(WebViewManager manager)
    {
        _webViewManager = manager;
    }

    public void ReloadWebView()
    {
        if (_webViewManager != null)
        {
            _webViewManager.Reload();
            Debug.WriteLine("[SettingsController] WebView yenilendi");
        }
        else
        {
            Debug.WriteLine("[SettingsController] WebViewManager tanımlı değil");
        }
    }

    public void NavigateHomepage()
    {
        if (_webViewManager != null)
        {
            _webViewManager.NavigateHomepage();
            Debug.WriteLine("[SettingsController] Homepage'e yönlendirildi");
        }
        else
        {
            Debug.WriteLine("[SettingsController] WebViewManager tanımlı değil");
        }
    }

    public void RestartSystem()
    {
        Debug.WriteLine("[SettingsController] Sistem restart komutu gönderiliyor...");

        if (_testMode)
        {
            Console.WriteLine("🚀 TEST MODE: Windows API Restart komutu çalıştırıldı!");
            Console.WriteLine("Gerçek durumda ExitWindowsEx() API çağrısı yapılacaktı.");
            Debug.WriteLine("[SettingsController] *** TEST MODE: Windows API Restart simüle edildi ***");
            return;
        }

        // GERÇEK RESTART - Windows API kullanarak
        try
        {
            if (EnableShutdownPrivilege())
            {
                // Force restart - init 1 benzeri
                bool result = ExitWindowsEx(EWX_REBOOT | EWX_FORCE, 0);
                if (!result)
                {
                    Debug.WriteLine($"[SettingsController] ExitWindowsEx hatası: {Marshal.GetLastWin32Error()}");
                    FallbackRestart(); // Başarısızsa eski yönteme dön
                }
            }
            else
            {
                Debug.WriteLine("[SettingsController] Shutdown privilege alınamadı, fallback kullanılıyor");
                FallbackRestart();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsController] Windows API restart hatası: {ex.Message}");
            Console.WriteLine($"Windows API restart başarısız: {ex.Message}");
            FallbackRestart();
        }
    }

    private bool EnableShutdownPrivilege()
    {
        try
        {
            IntPtr tokenHandle;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
                return false;

            if (!LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out long luid))
                return false;

            TOKEN_PRIVILEGES tokenPrivileges = new()
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            return AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            return false;
        }
    }

    private void FallbackRestart()
    {
        // Eski yöntem - backup olarak
        try
        {
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsController] Fallback restart hatası: {ex.Message}");
        }
    }

    public void SetTestMode(bool testMode)
    {
        _testMode = testMode;
        Debug.WriteLine($"[SettingsController] Test modu: {(_testMode ? "AÇIK" : "KAPALI")}");
    }
}