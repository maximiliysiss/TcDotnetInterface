using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TotalCommander.Interface.Abstraction.FileSystem.Native.Infrastructure;

namespace TotalCommander.Interface.Abstraction.FileSystem.Native.Models;

[Serializable]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct UnicodeTcFindData
{
    public int FileAttributes;

    public FILETIME CreationTime;
    public FILETIME LastAccessTime;
    public FILETIME LastWriteTime;

    public uint FileSizeHigh;
    public uint FileSizeLow;

    public uint Reserved0;
    public uint Reserved1;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeConstants.MaxPathUni)]
    public string FileName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public string AlternateFileName;
}
