using System;

namespace TotalCommander.Interface.Abstraction.FileSystem.Native.Infrastructure;

internal static class NativeConstants
{
    public static readonly IntPtr InvalidHandle = new(-1);

    public const int NoContent = 18;

    public const int MaxPathUni = 1024;
    public const int MaxPathAnsi = 260;
}
