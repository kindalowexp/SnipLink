using Microsoft.Win32;

namespace SnipLink;

static class WindowsStartup
{
    const string ValueName = "SnipLink";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Could not open the startup registry key.");

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var path = Application.ExecutablePath;
        key.SetValue(ValueName, path.Contains(' ') ? "\"" + path + "\"" : path);
    }
}
