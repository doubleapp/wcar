using System.Security.Principal;

namespace Wcar.Scripts;

public static class UacHelper
{
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool RequestElevation(string exePath, string arguments = "")
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                Verb = "runas",
                UseShellExecute = true
            };

            var proc = System.Diagnostics.Process.Start(psi);
            return proc != null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled UAC prompt
            return false;
        }
    }
}
