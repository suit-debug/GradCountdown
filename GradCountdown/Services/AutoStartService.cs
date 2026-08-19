using System.IO;
using Microsoft.Win32;

namespace GradCountdown.Services;

public static class AutoStartService
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "GradCountdown";

    public static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                // 动态获取当前运行进程路径，不依赖固定绝对路径
                string targetPath = Environment.ProcessPath ??
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GradCountdown.exe");

                key.SetValue(AppName, $"\"{targetPath}\"");
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
        catch (Exception ex)
        {
            App.Log($"AutoStartService.SetAutoStart error: {ex.Message}");
        }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            if (key == null) return false;
            var val = key.GetValue(AppName);
            return val != null && !string.IsNullOrWhiteSpace(val.ToString());
        }
        catch
        {
            return false;
        }
    }
}
