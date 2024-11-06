using System.Runtime.InteropServices;

namespace TotalCommander.Interface.Abstraction.FileSystem.Native.Infrastructure;

internal static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern void SetLastError(uint errCode);
}
