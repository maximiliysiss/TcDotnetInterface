﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using TcPluginInterface;
using TcPluginInterface.QuickSearch;
using TcPluginTools;

namespace QSWrapper;

public class QuickSearchWrapper
{
    private QuickSearchWrapper()
    {
    }

    #region Properties

    private static QuickSearchPlugin Plugin =>
        _plugin ??= (QuickSearchPlugin)TcPluginLoader.GetTcPlugin(_pluginWrapperDll, PluginType.QuickSearch);

    #endregion Properties

    #region Variables

    private static QuickSearchPlugin _plugin;
    private static readonly string _pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
    private static string _callSignature;

    #endregion Variables

    #region QuickSearch Exported Functions

    #region Mandatory Methods

    [DllExport(EntryPoint = "MatchFileW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static bool MatchFile(IntPtr wcFilter, IntPtr wcFileName)
    {
        var filter = Marshal.PtrToStringUni(wcFilter);
        var fileName = Marshal.PtrToStringUni(wcFileName);

        var result = false;
        _callSignature = $"MatchFileW(\"{fileName}\",\"{filter}\")";
        try
        {
            result = Plugin.MatchFile(filter, fileName);

            // !!! may produce much trace info !!!
            TraceCall(TraceLevel.Verbose, result ? "Yes" : "No");
        }
        catch (Exception ex)
        {
            ProcessException(ex);
        }

        return result;
    }

    [DllExport]
    public static int MatchGetSetOptions(int status)
    {
        MatchOptions result;
        _callSignature = $"MatchGetSetOptions(\"{status}\")";
        try
        {
            result = Plugin.MatchGetSetOptions((ExactNameMatch)status);

            TraceCall(TraceLevel.Info, result.ToString());
        }
        catch (Exception ex)
        {
            ProcessException(ex);
            result = MatchOptions.None;
        }

        return (int)result;
    }

    #endregion Mandatory Methods

    #endregion QuickSearch Exported Functions

    #region Tracing & Exceptions

    public static void ProcessException(Exception ex) => TcPluginLoader.ProcessException(_plugin, false, _callSignature, ex);

    public static void TraceCall(TraceLevel level, string result)
    {
        TcTrace.TraceCall(_plugin, level, _callSignature, result);
        _callSignature = null;
    }

    #endregion Tracing & Exceptions
}
