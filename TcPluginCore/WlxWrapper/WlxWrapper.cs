﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using TcPluginInterface;
using TcPluginInterface.Lister;
using TcPluginTools;

namespace WlxWrapper;

public class ListerWrapper
{
    private ListerWrapper()
    {
    }

    #region Variables

    private static ListerPlugin _plugin;
    private static readonly string _pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
    private static string _callSignature;

    #endregion Variables

    #region Properties

    private static ListerPlugin Plugin =>
        _plugin ??
        (_plugin = (ListerPlugin)TcPluginLoader.GetTcPlugin(_pluginWrapperDll, PluginType.Lister));

    private static IListerHandlerBuilder ListerHandlerBuilder => TcPluginLoader.GetListerHandlerBuilder(_pluginWrapperDll);

    #endregion Properties

    #region Lister Plugin Exported Functions

    #region Mandatory Methods

    #region ListLoad

    [DllExport(EntryPoint = "ListLoad")]
    public static IntPtr Load(IntPtr parentWin, [MarshalAs(UnmanagedType.LPStr)] string fileToLoad, int flags) =>
        LoadW(parentWin, fileToLoad, flags);

    [DllExport(EntryPoint = "ListLoadW")]
    public static IntPtr LoadW(
        IntPtr parentWin,
        [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad,
        int flags)
    {
        var listerHandle = IntPtr.Zero;
        var showFlags = (ShowFlags)flags;
        _callSignature = $"Load ({fileToLoad}, {showFlags.ToString()})";
        try
        {
            var listerControl = Plugin.Load(fileToLoad, showFlags);
            listerHandle = ListerHandlerBuilder.GetHandle(listerControl, parentWin);
            if (listerHandle != IntPtr.Zero)
            {
                Plugin.ListerHandle = listerHandle;
                Plugin.ParentHandle = parentWin;
                long windowState = NativeMethods.GetWindowLong(parentWin, NativeMethods.GWL_STYLE);
                Plugin.IsQuickView = (windowState & NativeMethods.WS_CHILD) != 0;
                TcHandles.AddHandle(listerHandle, listerControl);
                NativeMethods.SetParent(listerHandle, parentWin);
            }

            TraceCall(TraceLevel.Warning, listerHandle.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return listerHandle;
    }

    #endregion ListLoad

    #endregion Mandatory Methods

    #region Optional Methods

    #region ListLoadNext

    [DllExport(EntryPoint = "ListLoadNext")]
    public static int LoadNext(
        IntPtr parentWin,
        IntPtr listWin,
        [MarshalAs(UnmanagedType.LPStr)] string fileToLoad,
        int flags) =>
        LoadNextW(parentWin, listWin, fileToLoad, flags);

    [DllExport(EntryPoint = "ListLoadNextW")]
    public static int LoadNextW(
        IntPtr parentWin,
        IntPtr listWin,
        [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad,
        int flags)
    {
        var result = ListerResult.Error;
        var showFlags = (ShowFlags)flags;
        _callSignature = $"LoadNext ({listWin.ToString()}, {fileToLoad}, {showFlags.ToString()})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            result = Plugin.LoadNext(listerControl, fileToLoad, showFlags);
            TcHandles.UpdateHandle(listWin, listerControl);
            TraceCall(TraceLevel.Warning, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ListLoadNext

    #region ListCloseWindow

    [DllExport(EntryPoint = "ListCloseWindow")]
    public static void CloseWindow(IntPtr listWin)
    {
        _callSignature = $"CloseWindow ({listWin.ToString()})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            Plugin.CloseWindow(listerControl);
            var count = TcHandles.RemoveHandle(listWin);
            NativeMethods.DestroyWindow(listWin);
            TraceCall(TraceLevel.Warning, $"{count} calls.");
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion ListCloseWindow

    #region ListGetDetectString

    // ListGetDetectString functionality is implemented here, not included to Lister Plugin interface.
    [DllExport(EntryPoint = "ListGetDetectString")]
    public static void GetDetectString(IntPtr detectString, int maxLen)
    {
        _callSignature = "GetDetectString";
        try
        {
            TcUtils.WriteStringAnsi(Plugin.DetectString, detectString, maxLen);
            TraceCall(TraceLevel.Warning, Plugin.DetectString);
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }
    }

    #endregion ListGetDetectString

    #region ListSearchText

    [DllExport(EntryPoint = "ListSearchText")]
    public static int SearchText(
        IntPtr listWin,
        [MarshalAs(UnmanagedType.LPStr)] string searchString,
        int searchParameter) =>
        SearchTextW(listWin, searchString, searchParameter);

    [DllExport(EntryPoint = "ListSearchTextW")]
    public static int SearchTextW(
        IntPtr listWin,
        [MarshalAs(UnmanagedType.LPWStr)] string searchString,
        int searchParameter)
    {
        var result = ListerResult.Error;
        var sp = (SearchParameter)searchParameter;
        _callSignature = $"SearchText ({listWin.ToString()}, {searchString}, {sp.ToString()})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            result = Plugin.SearchText(listerControl, searchString, sp);
            TcHandles.UpdateHandle(listWin, listerControl);
            TraceCall(TraceLevel.Warning, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ListSearchText

    #region ListSendCommand

    [DllExport(EntryPoint = "ListSendCommand")]
    public static int SendCommand(IntPtr listWin, int command, int parameter)
    {
        var result = ListerResult.Error;
        var cmd = (ListerCommand)command;
        var par = (ShowFlags)parameter;
        _callSignature = $"SendCommand ({listWin.ToString()}, {cmd.ToString()}, {par.ToString()})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            result = Plugin.SendCommand(listerControl, cmd, par);
            TcHandles.UpdateHandle(listWin, listerControl);
            TraceCall(TraceLevel.Info, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ListSendCommand

    #region ListPrint

    [DllExport(EntryPoint = "ListPrint")]
    public static int Print(
        IntPtr listWin,
        [MarshalAs(UnmanagedType.LPStr)] string fileToPrint,
        [MarshalAs(UnmanagedType.LPStr)] string defPrinter,
        int flags,
        PrintMargins margins) =>
        PrintW(listWin, fileToPrint, defPrinter, flags, margins);

    [DllExport(EntryPoint = "ListPrintW")]
    public static int PrintW(
        IntPtr listWin,
        [MarshalAs(UnmanagedType.LPWStr)] string fileToPrint,
        [MarshalAs(UnmanagedType.LPWStr)] string defPrinter,
        int flags,
        PrintMargins margins)
    {
        var result = ListerResult.Error;
        var printFlags = (PrintFlags)flags;
        _callSignature = $"Print ({listWin.ToString()}, {fileToPrint}, {defPrinter}, {printFlags.ToString()})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            result = Plugin.Print(listerControl, fileToPrint, defPrinter, printFlags, margins);
            TcHandles.UpdateHandle(listWin, listerControl);
            TraceCall(TraceLevel.Warning, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ListPrint

    #region ListNotificationReceived

    [DllExport(EntryPoint = "ListNotificationReceived")]
    public static int NotificationReceived(IntPtr listWin, int message, int wParam, int lParam) // 32, 64 ???
    {
        var result = 0;
        _callSignature = $"NotificationReceived ({listWin.ToString()}, {message}, {wParam}, {lParam})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            result = Plugin.NotificationReceived(listerControl, message, wParam, lParam);
            TcHandles.UpdateHandle(listWin, listerControl);
            TraceCall(TraceLevel.Info, result.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return result;
    }

    #endregion ListNotificationReceived

    #region ListSetDefaultParams

    // ListSetDefaultParams functionality is implemented here, not included to Lister Plugin interface.
    [DllExport(EntryPoint = "ListSetDefaultParams")]
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

    #endregion ListSetDefaultParams

    #region ListGetPreviewBitmap

    [DllExport(EntryPoint = "ListGetPreviewBitmap")]
    public static IntPtr GetPreviewBitmap(
        [MarshalAs(UnmanagedType.LPStr)] string fileToLoad,
        int width,
        int height,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
        byte[] contentBuf,
        int contentBufLen) =>
        GetPreviewBitmapInternal(fileToLoad, width, height, contentBuf);

    [DllExport(EntryPoint = "ListGetPreviewBitmapW")]
    public static IntPtr GetPreviewBitmapW(
        [MarshalAs(UnmanagedType.LPWStr)] string fileToLoad,
        int width,
        int height,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
        byte[] contentBuf,
        int contentBufLen) =>
        GetPreviewBitmapInternal(fileToLoad, width, height, contentBuf);

    public static IntPtr GetPreviewBitmapInternal(string fileToLoad, int width, int height, byte[] contentBuf)
    {
        IntPtr result;
        _callSignature = $"GetPreviewBitmap '{fileToLoad}' ({width} x {height})";
        try
        {
            var bitmap = Plugin.GetPreviewBitmap(fileToLoad, width, height, contentBuf);
            result = bitmap.GetHbitmap(Plugin.BitmapBackgroundColor);
            TraceCall(TraceLevel.Info, result.Equals(IntPtr.Zero) ? "OK" : "None");
        }
        catch (Exception ex)
        {
#if TRACE
            TcTrace.TraceOut(
                TraceLevel.Error,
                $"{_callSignature}: {ex.Message}",
                $"ERROR ({Plugin.TraceTitle})");
#endif
            result = IntPtr.Zero;
        }

        return result;
    }

    #endregion ListGetPreviewBitmap

    #region ListSearchDialog

    [DllExport(EntryPoint = "ListSearchDialog")]
    public static int SearchDialog(IntPtr listWin, int findNext)
    {
        var result = ListerResult.Error;
        _callSignature = $"SearchDialog ({listWin.ToString()}, {findNext})";
        try
        {
            var listerControl = TcHandles.GetObject(listWin);
            result = Plugin.SearchDialog(listerControl, findNext != 0);
            TraceCall(TraceLevel.Info, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return (int)result;
    }

    #endregion ListSearchDialog

    #endregion Optional Methods

    #endregion Lister Plugin Exported Functions

    #region Tracing & Exceptions

    public static void ProcessException(Exception ex) => TcPluginLoader.ProcessException(_plugin, false, _callSignature, ex);

    public static void TraceCall(TraceLevel level, string result)
    {
        TcTrace.TraceCall(_plugin, level, _callSignature, result);
        _callSignature = null;
    }

    #endregion Tracing & Exceptions
}
