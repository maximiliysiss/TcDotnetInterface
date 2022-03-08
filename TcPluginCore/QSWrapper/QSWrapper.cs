using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.QuickSearch;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.QSWrapper;

public class QuickSearchWrapper
{
    private QuickSearchWrapper()
    {
    }

    #region Properties

    private static QuickSearchPlugin Plugin =>
        plugin ??
        (plugin = (QuickSearchPlugin)TcPluginLoader.GetTcPlugin(pluginWrapperDll, PluginType.QuickSearch));

    #endregion Properties

    #region Variables

    private static QuickSearchPlugin plugin;
    private static readonly string pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
    private static string callSignature;

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
        callSignature = string.Format("MatchFileW(\"{0}\",\"{1}\")", fileName, filter);
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
        callSignature = string.Format("MatchGetSetOptions(\"{0}\")", status);
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

    public static void ProcessException(Exception ex) => TcPluginLoader.ProcessException(plugin, false, callSignature, ex);

    public static void TraceCall(TraceLevel level, string result)
    {
        TcTrace.TraceCall(plugin, level, callSignature, result);
        callSignature = null;
    }

    #endregion Tracing & Exceptions
}
