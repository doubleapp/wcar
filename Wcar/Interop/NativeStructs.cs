using System.Runtime.InteropServices;

namespace Wcar.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPLACEMENT
{
    public int Length;
    public int Flags;
    public int ShowCmd;
    public POINT MinPosition;
    public POINT MaxPosition;
    public RECT NormalPosition;

    public static WINDOWPLACEMENT Default
    {
        get
        {
            var wp = new WINDOWPLACEMENT();
            wp.Length = Marshal.SizeOf<WINDOWPLACEMENT>();
            return wp;
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PROCESS_BASIC_INFORMATION
{
    public IntPtr Reserved1;
    public IntPtr PebBaseAddress;
    public IntPtr Reserved2_0;
    public IntPtr Reserved2_1;
    public IntPtr UniqueProcessId;
    public IntPtr Reserved3;
}

[StructLayout(LayoutKind.Sequential)]
public struct UNICODE_STRING
{
    public ushort Length;
    public ushort MaximumLength;
    public IntPtr Buffer;
}
