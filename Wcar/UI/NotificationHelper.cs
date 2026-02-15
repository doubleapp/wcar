namespace Wcar.UI;

public static class NotificationHelper
{
    public static void Show(NotifyIcon icon, string title, string message,
        ToolTipIcon tipIcon = ToolTipIcon.Info, int timeout = 3000)
    {
        icon.BalloonTipTitle = title;
        icon.BalloonTipText = message;
        icon.BalloonTipIcon = tipIcon;
        icon.ShowBalloonTip(timeout);
    }

    public static void ShowError(NotifyIcon icon, string message)
    {
        Show(icon, "WCAR Error", message, ToolTipIcon.Error);
    }

    public static void ShowWarning(NotifyIcon icon, string message)
    {
        Show(icon, "WCAR", message, ToolTipIcon.Warning);
    }

    public static void ShowInfo(NotifyIcon icon, string message)
    {
        Show(icon, "WCAR", message, ToolTipIcon.Info);
    }
}
