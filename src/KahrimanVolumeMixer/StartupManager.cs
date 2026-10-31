using System;
using Microsoft.Win32;

namespace KahrimanVolumeMixer;

/// <summary>Windows başlangıcında çalıştırma (HKCU\...\Run — yönetici izni gerektirmez).</summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KahrimanVolumeMixer";

    public static bool Apply(bool enable)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return false;
                key.SetValue(ValueName, "\"" + exePath + "\"", RegistryValueKind.String);
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            App.LogError(ex);
            return false;
        }
    }
}
