using Microsoft.Toolkit.Uwp.Notifications;

namespace dashboard.Services;

// Native Windows toast notifications, in-process — no shelling out to PowerShell. The
// library registers an AppUserModelID for this unpackaged app automatically the first
// time a toast is shown, so no manifest/Start Menu shortcut setup is needed.
public static class DesktopNotifier
{
    public static void Send(string title, string message)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .Show();
    }
}
