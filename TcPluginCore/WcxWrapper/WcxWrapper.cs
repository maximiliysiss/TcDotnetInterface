﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TcPluginInterface;
using TcPluginInterface.Packer;
using TcPluginTools;

namespace WcxWrapper;

public class PackerWrapper
{
    private PackerWrapper()
    {
    }

    #region Properties

    private static PackerPlugin Plugin =>
        _plugin ??
        (_plugin = (PackerPlugin)TcPluginLoader.GetTcPlugin(_pluginWrapperDll, PluginType.Packer));

    #endregion Properties

    #region Variables

    private static PackerPlugin _plugin;
    private static readonly string _pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
    private static string _callSignature;

    #endregion Variables

    #region Packer Plugin Exported Functions

    #region Mandatory Methods

    #region OpenArchive

    [DllExport(EntryPoint = "OpenArchive")]
    public static IntPtr OpenArchive(IntPtr archiveData)
    {
        var data = new OpenArchiveData(archiveData, false);
        return OpenArchiveInternal(data);
    }

    [DllExport(EntryPoint = "OpenArchiveW")]
    public static IntPtr OpenArchiveW(IntPtr archiveData)
    {
        var data = new OpenArchiveData(archiveData, true);
        return OpenArchiveInternal(data);
    }

    public static IntPtr OpenArchiveInternal(OpenArchiveData data)
    {
        var result = IntPtr.Zero;
        _callSignature = $"OpenArchive {data.ArchiveName} ({data.Mode.ToString()})";
        try
        {
            var o = Plugin.OpenArchive(ref data);
            if (o != null && data.Result == PackerResult.Ok)
            {
                result = TcHandles.AddHandle(o);
                data.Update();
            }

            TraceCall(TraceLevel.Info, result == IntPtr.Zero ? $"Error ({data.Result.ToString()})" : result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
            result = IntPtr.Zero;
        }

        return result;
    }

    #endregion OpenArchive

    #region ReadHeader

    [DllExport(EntryPoint = "ReadHeader")]
    public static int ReadHeader(IntPtr arcData, IntPtr headerData) => ReadHeaderInternal(arcData, headerData, HeaderDataMode.Ansi);

    #endregion ReadHeader

    #region ReadHeaderEx

    [DllExport(EntryPoint = "ReadHeaderEx")]
    public static int ReadHeaderEx(IntPtr arcData, IntPtr headerData) => ReadHeaderInternal(arcData, headerData, HeaderDataMode.ExAnsi);

    [DllExport(EntryPoint = "ReadHeaderExW")]
    public static int ReadHeaderExW(IntPtr arcData, IntPtr headerData) => ReadHeaderInternal(arcData, headerData, HeaderDataMode.ExUnicode);

    public static int ReadHeaderInternal(IntPtr arcData, IntPtr headerData, HeaderDataMode mode)
    {
        var result = PackerResult.EndArchive;
        _callSignature = $"ReadHeader ({arcData.ToString()})";
        try
        {
            var o = TcHandles.GetObject(arcData);
            if (o == null)
            {
                return (int)PackerResult.ErrorOpen;
            }

            result = Plugin.ReadHeader(ref o, out var header);
            if (result == PackerResult.Ok)
            {
                header.CopyTo(headerData, mode);
                TcHandles.UpdateHandle(arcData, o);
            }

            // !!! may produce much trace info !!!
            TraceCall(
                TraceLevel.Verbose,
                $"{result.ToString()} ({(result == PackerResult.Ok ? header.FileName : null)})");
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ReadHeaderEx

    #region ProcessFile

    [DllExport(EntryPoint = "ProcessFile")]
    public static int ProcessFile(
        IntPtr arcData,
        int operation,
        [MarshalAs(UnmanagedType.LPStr)] string destPath,
        [MarshalAs(UnmanagedType.LPStr)] string destName) =>
        ProcessFileW(arcData, operation, destPath, destName);

    [DllExport(EntryPoint = "ProcessFileW")]
    public static int ProcessFileW(
        IntPtr arcData,
        int operation,
        [MarshalAs(UnmanagedType.LPWStr)] string destPath,
        [MarshalAs(UnmanagedType.LPWStr)] string destName)
    {
        var result = PackerResult.NotSupported;
        var oper = (ProcessFileOperation)operation;
        var fileName = string.IsNullOrEmpty(destPath) ? destName : Path.Combine(destPath, destName);
        _callSignature = $"ProcessFile ({arcData.ToString()}, {oper.ToString()}, {fileName})";
        try
        {
            var o = TcHandles.GetObject(arcData);
            if (o != null)
            {
                result = Plugin.ProcessFile(o, oper, fileName);
                if (result == PackerResult.Ok)
                {
                    TcHandles.UpdateHandle(arcData, o);
                }
            }

            // !!! may produce much trace info !!!
            TraceCall(TraceLevel.Verbose, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ProcessFile

    #region CloseArchive

    [DllExport(EntryPoint = "CloseArchive")]
    public static int CloseArchive(IntPtr arcData)
    {
        var result = PackerResult.ErrorClose;
        _callSignature = $"FindClose ({arcData.ToString()})";
        try
        {
            var o = TcHandles.GetObject(arcData);
            if (o != null)
            {
                result = Plugin.CloseArchive(o);
                if (o is IDisposable disp)
                    disp.Dispose();

                var count = (TcHandles.RemoveHandle(arcData) - 1) / 2;

                TraceCall(TraceLevel.Info, $"{count} items.");
            }
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion CloseArchive

    #region SetChangeVolProc

    // SetChangeVolProc & SetChangeVolProcW functionality is implemented here, not included to Packer Plugin interface.
    [DllExport(EntryPoint = "SetChangeVolProc")]
    public static void SetChangeVolProc(IntPtr arcData, ChangeVolCallback changeVolProc)
    {
        _callSignature = $"SetChangeVolProc ({arcData.ToString()})";
        try
        {
            TcCallback.SetPackerPluginCallbacks(changeVolProc, null, null, null, null, null);

            TraceCall(
                TraceLevel.Warning,
                changeVolProc.Method.MethodHandle.GetFunctionPointer().ToString("X"));
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    [DllExport(EntryPoint = "SetChangeVolProcW")]
    public static void SetChangeVolProcW(IntPtr arcData, ChangeVolCallbackW changeVolProcW)
    {
        _callSignature = $"SetChangeVolProcW ({arcData.ToString()})";
        try
        {
            TcCallback.SetPackerPluginCallbacks(null, changeVolProcW, null, null, null, null);

            TraceCall(
                TraceLevel.Warning,
                changeVolProcW.Method.MethodHandle.GetFunctionPointer().ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion SetChangeVolProc

    #region SetProcessDataProc

    // SetProcessDataProc & SetProcessDataProcW functionality is implemented here, not included to Packer Plugin interface.
    [DllExport(EntryPoint = "SetProcessDataProc")]
    public static void SetProcessDataProc(IntPtr arcData, ProcessDataCallback processDataProc)
    {
        _callSignature = $"SetProcessDataProc ({arcData.ToString()})";
        try
        {
            TcCallback.SetPackerPluginCallbacks(null, null, processDataProc, null, null, null);

            TraceCall(
                TraceLevel.Warning,
                processDataProc.Method.MethodHandle.GetFunctionPointer().ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    [DllExport(EntryPoint = "SetProcessDataProcW")]
    public static void SetProcessDataProcW(IntPtr arcData, ProcessDataCallbackW processDataProcW)
    {
        _callSignature = $"SetProcessDataProcW ({arcData.ToString()})";
        try
        {
            TcCallback.SetPackerPluginCallbacks(null, null, null, processDataProcW, null, null);
            TraceCall(
                TraceLevel.Warning,
                processDataProcW.Method.MethodHandle.GetFunctionPointer().ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion SetProcessDataProc

    #endregion Mandatory Methods

    #region Optional Methods

    #region PackFiles

    [DllExport(EntryPoint = "PackFiles")]
    public static int PackFiles(
        [MarshalAs(UnmanagedType.LPStr)] string packedFile,
        [MarshalAs(UnmanagedType.LPStr)] string subPath,
        [MarshalAs(UnmanagedType.LPStr)] string srcPath,
        IntPtr addListPtr,
        int flags)
    {
        var addList = TcUtils.ReadStringListAnsi(addListPtr);
        return PackFilesInternal(packedFile, subPath, srcPath, addList, (PackFilesFlags)flags);
    }

    [DllExport(EntryPoint = "PackFilesW")]
    public static int PackFilesW(
        [MarshalAs(UnmanagedType.LPWStr)] string packedFile,
        [MarshalAs(UnmanagedType.LPWStr)] string subPath,
        [MarshalAs(UnmanagedType.LPWStr)] string srcPath,
        IntPtr addListPtr,
        int flags)
    {
        var addList = TcUtils.ReadStringListUni(addListPtr);
        return PackFilesInternal(packedFile, subPath, srcPath, addList, (PackFilesFlags)flags);
    }

    public static int PackFilesInternal(
        string packedFile,
        string subPath,
        string srcPath,
        List<string> addList,
        PackFilesFlags flags)
    {
        var result = PackerResult.NotSupported;
        _callSignature = $"PackFiles ({packedFile}, {subPath}, {srcPath}, {flags.ToString()}) - {addList.Count} files)";
        try
        {
            result = Plugin.PackFiles(packedFile, subPath, srcPath, addList, flags);

            TraceCall(TraceLevel.Info, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion PackFiles

    #region DeleteFiles

    [DllExport(EntryPoint = "DeleteFiles")]
    public static int DeleteFiles([MarshalAs(UnmanagedType.LPStr)] string packedFile, IntPtr deleteListPtr)
    {
        var deleteList = TcUtils.ReadStringListAnsi(deleteListPtr);
        return DeleteFilesInternal(packedFile, deleteList);
    }

    [DllExport(EntryPoint = "DeleteFilesW")]
    public static int DeleteFilesW([MarshalAs(UnmanagedType.LPWStr)] string packedFile, IntPtr deleteListPtr)
    {
        var deleteList = TcUtils.ReadStringListUni(deleteListPtr);
        return DeleteFilesInternal(packedFile, deleteList);
    }

    public static int DeleteFilesInternal(string packedFile, List<string> deleteList)
    {
        var result = PackerResult.NotSupported;
        _callSignature = $"DeleteFiles ({packedFile}) - {deleteList.Count} files)";
        try
        {
            result = Plugin.DeleteFiles(packedFile, deleteList);

            TraceCall(TraceLevel.Info, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion DeleteFiles

    #region GetPackerCaps

    // GetPackerCaps functionality is implemented here, not included to Packer Plugin interface.
    [DllExport(EntryPoint = "GetPackerCaps")]
    public static int GetPackerCaps()
    {
        _callSignature = "GetPackerCaps";

        TraceCall(TraceLevel.Info, Plugin.Capabilities.ToString());
        return (int)Plugin.Capabilities;
    }

    #endregion GetPackerCaps

    #region ConfigurePacker

    [DllExport(EntryPoint = "ConfigurePacker")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static void ConfigurePacker(IntPtr parentWin, IntPtr dllInstance)
    {
        _callSignature = "ConfigurePacker";
        try
        {
            Plugin.ConfigurePacker(new TcWindow(parentWin));

            TraceCall(TraceLevel.Info, null);
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion ConfigurePacker

    #region StartMemPack

    [DllExport(EntryPoint = "StartMemPack")]
    public static IntPtr StartMemPack(int options, [MarshalAs(UnmanagedType.LPStr)] string fileName) => StartMemPackW(options, fileName);

    [DllExport(EntryPoint = "StartMemPackW")]
    public static IntPtr StartMemPackW(int options, [MarshalAs(UnmanagedType.LPWStr)] string fileName)
    {
        var result = IntPtr.Zero;
        var mpOptions = (MemPackOptions)options;
        _callSignature = $"StartMemPack {fileName} ({mpOptions.ToString()})";
        try
        {
            var o = Plugin.StartMemPack(mpOptions, fileName);
            if (o != null)
            {
                result = TcHandles.AddHandle(o);
            }

            TraceCall(TraceLevel.Warning, result == IntPtr.Zero ? "ERROR" : result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return result;
    }

    #endregion StartMemPack

    #region PackToMem

    [DllExport(EntryPoint = "PackToMem")]
    public static int PackToMem(
        IntPtr hMemPack,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
        byte[] bufIn,
        int inLen,
        ref int taken,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
        byte[] bufOut,
        int outLen,
        ref int written,
        int seekBy)
    {
        var result = PackerResult.NotSupported;
        _callSignature = $"PackToMem ({hMemPack.ToString()} - {inLen}, {outLen}, {seekBy})";
        string traceRes = null;
        try
        {
            var o = TcHandles.GetObject(hMemPack);
            if (o != null)
            {
                result = Plugin.PackToMem(ref o, bufIn, ref taken, bufOut, ref written, seekBy);
                traceRes = result.ToString();
                if (result == PackerResult.Ok)
                {
                    TcHandles.UpdateHandle(hMemPack, o);
                    traceRes += $" - {taken}, {written}";
                }
            }

            TraceCall(TraceLevel.Verbose, traceRes);
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion PackToMem

    #region DoneMemPack

    [DllExport(EntryPoint = "DoneMemPack")]
    public static int DoneMemPack(IntPtr hMemPack)
    {
        var result = PackerResult.ErrorClose;
        _callSignature = $"DoneMemPack ({hMemPack.ToString()})";
        try
        {
            var o = TcHandles.GetObject(hMemPack);
            if (o != null)
            {
                result = Plugin.DoneMemPack(o);
                if (o is IDisposable disp)
                {
                    disp.Dispose();
                }

                var count = TcHandles.RemoveHandle(hMemPack);
                TraceCall(TraceLevel.Warning, $"{count} calls.");
            }
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion DoneMemPack

    #region CanYouHandleThisFile

    [DllExport(EntryPoint = "CanYouHandleThisFile")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static bool CanYouHandleThisFile([MarshalAs(UnmanagedType.LPStr)] string fileName) => CanYouHandleThisFileW(fileName);

    [DllExport(EntryPoint = "CanYouHandleThisFileW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static bool CanYouHandleThisFileW([MarshalAs(UnmanagedType.LPWStr)] string fileName)
    {
        var result = false;
        _callSignature = $"CanYouHandleThisFile ({fileName})";
        try
        {
            result = Plugin.CanYouHandleThisFile(fileName);

            TraceCall(TraceLevel.Warning, result ? "Yes" : "No");
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return result;
    }

    #endregion CanYouHandleThisFile

    #region PackSetDefaultParams

    // PackSetDefaultParams functionality is implemented here, not included to Packer Plugin interface.
    [DllExport(EntryPoint = "PackSetDefaultParams")]
    public static void SetDefaultParams(ref PluginDefaultParams defParams)
    {
        _callSignature = "SetDefaultParams";
        try
        {
            Plugin.DefaultParams = defParams;

            TraceCall(TraceLevel.Info, null);
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion PackSetDefaultParams

    #region PkSetCryptCallback

    // PkSetCryptCallback & PkSetCryptCallbackW functionality is implemented here, not included to Packer Plugin interface.
    [DllExport(EntryPoint = "PkSetCryptCallback")]
    public static void SetCryptCallback(PkCryptCallback cryptProc, int cryptNumber, int flags)
    {
        _callSignature = $"PkSetCryptCallback ({cryptNumber}, {flags})";
        try
        {
            TcCallback.SetPackerPluginCallbacks(null, null, null, null, cryptProc, null);
            Plugin.CreatePassword(cryptNumber, flags);

            TraceCall(
                TraceLevel.Info,
                cryptProc.Method.MethodHandle.GetFunctionPointer().ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    [DllExport(EntryPoint = "PkSetCryptCallbackW")]
    public static void SetCryptCallbackW(PkCryptCallbackW cryptProcW, int cryptNumber, int flags)
    {
        _callSignature = $"PkSetCryptCallbackW ({cryptNumber}, {flags})";
        try
        {
            TcCallback.SetPackerPluginCallbacks(null, null, null, null, null, cryptProcW);
            Plugin.CreatePassword(cryptNumber, flags);

            TraceCall(
                TraceLevel.Info,
                cryptProcW.Method.MethodHandle.GetFunctionPointer().ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion PkSetCryptCallback

    #region GetBackgroundFlags

    // GetBackgroundFlags functionality is implemented here, not included to Packer Plugin interface.
    [DllExport(EntryPoint = "GetBackgroundFlags")]
    public static int GetBackgroundFlags()
    {
        var result = PackBackgroundFlags.None;
        _callSignature = "GetBackgroundFlags";
        try
        {
            result = Plugin.BackgroundFlags;

            TraceCall(TraceLevel.Info, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion FsGetBackgroundFlags

    #endregion Optional Methods

    #endregion Packer Plugin Exported Functions

    #region Tracing & Exceptions

    private static void ProcessException(Exception ex) => TcPluginLoader.ProcessException(_plugin, false, _callSignature, ex);

    private static void TraceCall(TraceLevel level, string result)
    {
        TcTrace.TraceCall(_plugin, level, _callSignature, result);
        _callSignature = null;
    }

    #endregion Tracing & Exceptions
}
