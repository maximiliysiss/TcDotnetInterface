using System;
using System.Runtime.InteropServices;

namespace OY.TotalCommander.TcPluginInterface;

// This structure is used in SetDefaultParams method (all TC plugins)
[Serializable]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct PluginDefaultParams
{
    public int size;
    public int pluginInterfaceVersionLow;
    public int pluginInterfaceVersionHi;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.MAX_PATH_ANSI)]
    public string defaultIniName;
}
