namespace Wcar.Interop;

public static class NativeConstants
{
    // ShowWindow commands
    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;
    public const int SW_RESTORE = 9;

    // Process access rights
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    // Window styles
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_CAPTION = 0x00C00000;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_APPWINDOW = 0x00040000;

    // GetWindow constants
    public const uint GW_OWNER = 4;

    // WINDOWPLACEMENT length
    public const int WPL_LENGTH = 44;

    // ProcessBasicInformation class
    public const int ProcessBasicInformation = 0;
}
