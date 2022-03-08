using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using OY.TotalCommander.TcPluginInterface.Content;
using OY.TotalCommander.TcPluginInterface.FileSystem;
using OY.TotalCommander.TcPluginInterface.Lister;
using OY.TotalCommander.TcPluginInterface.Packer;
using OY.TotalCommander.TcPluginInterface.QuickSearch;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace OY.TotalCommander.TcPluginInterface;

public static class TcUtils
{
    private const uint EmptyDateTimeHi = 0xFFFFFFFF;
    private const uint EmptyDateTimeLo = 0xFFFFFFFE;

    #region Common Dictionaries

    public static Dictionary<PluginType, string> PluginInterfaces =
        new()
        {
            { PluginType.Content, typeof(IContentPlugin).FullName },
            { PluginType.FileSystem, typeof(IFsPlugin).FullName },
            { PluginType.Lister, typeof(IListerPlugin).FullName },
            { PluginType.Packer, typeof(IPackerPlugin).FullName },
            { PluginType.QuickSearch, typeof(IQuickSearchPlugin).FullName }
        };

    public static Dictionary<PluginType, string> PluginNames =
        new()
        {
            { PluginType.Content, "Content " },
            { PluginType.FileSystem, "File System " },
            { PluginType.Lister, "Lister " },
            { PluginType.Packer, "Packer " },
            { PluginType.QuickSearch, "QuickSearch " }
        };

    #endregion Common Dictionaries

    #region Class Loading Methods

    public static object CreateInstance(Type classType, string assemblyPath, Type interfaceType, string className)
    {
        if (string.IsNullOrEmpty(assemblyPath) ||
            string.IsNullOrEmpty(className) && interfaceType == null)
        {
            return null;
        }

        if (classType == null)
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
            try
            {
                classType = FindClass(assemblyPath, interfaceType, className);
                if (classType == null)
                {
                    return null;
                }
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveEventHandler;
            }
        }

        try
        {
            var result = classType.InvokeMember(
                null,
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance,
                null,
                null,
                null);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(className + " - Class loading error.", ex);
        }
    }

    private static Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args) => Assembly.Load(args.Name);

    private static Type FindClass(string assemblyPath, Type interfaceType, string className)
    {
        var interfaceTypeName = interfaceType == null ? null : interfaceType.FullName;
        if (!Path.IsPathRooted(assemblyPath))
        {
            assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyPath);
        }

        var assembly = Assembly.LoadFrom(assemblyPath);
        foreach (var type in assembly.GetExportedTypes())
        {
            if (!string.IsNullOrEmpty(interfaceTypeName) && type.GetInterface(interfaceTypeName) == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(className) && !type.FullName.EndsWith(className))
            {
                continue;
            }

            return type;
        }

        return null;
    }

    #endregion Class Loading Methods

    #region Long Conversion Methods

    public static int GetHigh(long value) => (int)(value >> 32);

    public static int GetLow(long value) => (int)(value & uint.MaxValue);

    public static long GetLong(int high, int low) => ((long)high << 32) + low;

    [CLSCompliant(false)]
    public static uint GetUHigh(ulong value) => (uint)(value >> 32);

    [CLSCompliant(false)]
    public static uint GetULow(ulong value) => (uint)(value & uint.MaxValue);

    [CLSCompliant(false)]
    public static ulong GetULong(uint high, uint low) => ((ulong)high << 32) + low;

    #endregion Long Conversion Methods

    #region DateTime Conversion Methods

    public static FileTime GetFileTime(DateTime? dateTime)
    {
        var longTime =
            dateTime.HasValue && dateTime.Value != DateTime.MinValue
                ? dateTime.Value.ToFileTime()
                : long.MaxValue << 1;
        return new FileTime
        {
            dwHighDateTime = GetHigh(longTime),
            dwLowDateTime = GetLow(longTime)
        };
    }

    [CLSCompliant(false)]
    public static ulong GetULong(DateTime? dateTime)
    {
        if (dateTime.HasValue && dateTime.Value != DateTime.MinValue)
        {
            var ulongTime = Convert.ToUInt64(dateTime.Value.ToFileTime());
            return ulongTime;
        }

        return GetULong(EmptyDateTimeHi, EmptyDateTimeLo);
    }

    public static DateTime? FromFileTime(FileTime fileTime)
    {
        try
        {
            var longTime = Convert.ToInt64(fileTime);
            return DateTime.FromFileTime(longTime);
        }
        catch (Exception)
        {
            return null;
        }
    }

    [CLSCompliant(false)]
    public static DateTime? FromULong(ulong fileTime)
    {
        var longTime = Convert.ToInt64(fileTime);
        return longTime != 0
            ? DateTime.FromFileTime(longTime)
            : null;
    }

    public static DateTime? ReadDateTime(IntPtr addr) =>
        addr == IntPtr.Zero
            ? null
            : DateTime.FromFileTime(Marshal.ReadInt64(addr));

    public static int GetArchiveHeaderTime(DateTime dt)
    {
        if (dt.Year < 1980 || dt.Year > 2100)
        {
            return 0;
        }

        return
            ((dt.Year - 1980) << 25)
            | (dt.Month << 21)
            | (dt.Day << 16)
            | (dt.Hour << 11)
            | (dt.Minute << 5)
            | (dt.Second / 2);
    }

    #endregion DateTime Conversion Methods

    #region Unmanaged String Methods

    public static string ReadStringAnsi(IntPtr addr) =>
        addr == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringAnsi(addr);

    public static List<string> ReadStringListAnsi(IntPtr addr)
    {
        var result = new List<string>();
        if (addr != IntPtr.Zero)
        {
            while (true)
            {
                var s = ReadStringAnsi(addr);
                if (string.IsNullOrEmpty(s))
                {
                    break;
                }

                result.Add(s);
                addr = new IntPtr(addr.ToInt64() + s.Length + 1);
            }
        }

        return result;
    }

    public static string ReadStringUni(IntPtr addr) =>
        addr == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringUni(addr);

    public static List<string> ReadStringListUni(IntPtr addr)
    {
        var result = new List<string>();
        if (addr != IntPtr.Zero)
        {
            while (true)
            {
                var s = ReadStringUni(addr);
                if (string.IsNullOrEmpty(s))
                {
                    break;
                }

                result.Add(s);
                addr = new IntPtr(addr.ToInt64() + (s.Length + 1) * 2);
            }
        }

        return result;
    }

    public static void WriteStringAnsi(string str, IntPtr addr, int length)
    {
        if (string.IsNullOrEmpty(str))
        {
            Marshal.WriteIntPtr(addr, IntPtr.Zero);
        }
        else
        {
            var strLen = str.Length;
            if (length > 0 && strLen >= length)
            {
                strLen = length - 1;
            }

            var i = 0;
            var bytes = new byte[strLen + 1];
            foreach (var ch in str.Substring(0, strLen))
            {
                bytes[i++] = Convert.ToByte(ch);
            }

            bytes[strLen] = 0;
            Marshal.Copy(bytes, 0, addr, strLen + 1);
        }
    }

    public static void WriteStringUni(string str, IntPtr addr, int length)
    {
        if (string.IsNullOrEmpty(str))
        {
            Marshal.WriteIntPtr(addr, IntPtr.Zero);
        }
        else
        {
            var strLen = str.Length;
            if (length > 0 && strLen >= length)
            {
                strLen = length - 1;
            }

            Marshal.Copy((str + (char)0).ToCharArray(0, strLen + 1), 0, addr, strLen + 1);
        }
    }

    #endregion Unmanaged String Methods
}
