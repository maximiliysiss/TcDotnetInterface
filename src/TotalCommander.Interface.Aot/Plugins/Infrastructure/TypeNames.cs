using System;

namespace TotalCommander.Interface.Aot.Plugins.Infrastructure;

internal static class TypeNames
{
    public static readonly string IntPtr = typeof(IntPtr).FullName ?? throw new InvalidOperationException("Unable to get IntPtr");
}
