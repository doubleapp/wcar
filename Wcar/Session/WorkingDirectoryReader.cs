using System.Runtime.InteropServices;
using Wcar.Interop;

namespace Wcar.Session;

public static class WorkingDirectoryReader
{
    // x64 PEB offsets
    private const int ProcessParametersOffset = 0x20;
    private const int CurrentDirectoryPathOffset = 0x38;

    public static string? GetWorkingDirectory(uint processId)
    {
        var hProcess = NativeMethods.OpenProcess(
            NativeConstants.PROCESS_QUERY_INFORMATION | NativeConstants.PROCESS_VM_READ,
            false, processId);

        if (hProcess == IntPtr.Zero)
        {
            hProcess = NativeMethods.OpenProcess(
                NativeConstants.PROCESS_QUERY_LIMITED_INFORMATION | NativeConstants.PROCESS_VM_READ,
                false, processId);
        }

        if (hProcess == IntPtr.Zero)
            return null;

        try
        {
            return ReadCwdFromPeb(hProcess);
        }
        catch
        {
            return null;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }

    private static string? ReadCwdFromPeb(IntPtr hProcess)
    {
        // Get PEB address
        var pbi = new PROCESS_BASIC_INFORMATION();
        int status = NativeMethods.NtQueryInformationProcess(hProcess,
            NativeConstants.ProcessBasicInformation,
            ref pbi, Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out _);

        if (status != 0 || pbi.PebBaseAddress == IntPtr.Zero)
            return null;

        // Read ProcessParameters pointer from PEB
        var processParamsPtr = ReadPointer(hProcess,
            pbi.PebBaseAddress + ProcessParametersOffset);
        if (processParamsPtr == IntPtr.Zero)
            return null;

        // Read CurrentDirectory.DosPath UNICODE_STRING from RTL_USER_PROCESS_PARAMETERS
        var cwdAddress = processParamsPtr + CurrentDirectoryPathOffset;

        var unicodeStr = ReadUnicodeString(hProcess, cwdAddress);
        if (unicodeStr == null)
            return null;

        return unicodeStr;
    }

    private static IntPtr ReadPointer(IntPtr hProcess, IntPtr address)
    {
        var buffer = new byte[IntPtr.Size];
        if (!NativeMethods.ReadProcessMemory(hProcess, address, buffer, buffer.Length, out _))
            return IntPtr.Zero;

        return IntPtr.Size == 8
            ? new IntPtr(BitConverter.ToInt64(buffer, 0))
            : new IntPtr(BitConverter.ToInt32(buffer, 0));
    }

    private static string? ReadUnicodeString(IntPtr hProcess, IntPtr address)
    {
        // Read UNICODE_STRING structure (Length, MaxLength, Buffer pointer)
        var headerSize = Marshal.SizeOf<UNICODE_STRING>();
        var headerBuffer = new byte[headerSize];

        if (!NativeMethods.ReadProcessMemory(hProcess, address, headerBuffer, headerSize, out _))
            return null;

        var length = BitConverter.ToUInt16(headerBuffer, 0);
        if (length == 0)
            return null;

        // Buffer pointer is at offset 8 on x64 (after Length + MaximumLength + padding)
        var bufferPtrOffset = IntPtr.Size == 8 ? 8 : 4;
        var bufferPtr = IntPtr.Size == 8
            ? new IntPtr(BitConverter.ToInt64(headerBuffer, bufferPtrOffset))
            : new IntPtr(BitConverter.ToInt32(headerBuffer, bufferPtrOffset));

        if (bufferPtr == IntPtr.Zero)
            return null;

        // Read the actual string data
        var strBuffer = new byte[length];
        if (!NativeMethods.ReadProcessMemory(hProcess, bufferPtr, strBuffer, length, out _))
            return null;

        var result = System.Text.Encoding.Unicode.GetString(strBuffer);

        // Remove trailing backslash if present (except for root like "C:\")
        if (result.Length > 3 && result.EndsWith('\\'))
            result = result[..^1];

        return result;
    }
}
