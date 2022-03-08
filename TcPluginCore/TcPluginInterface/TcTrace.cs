using System;
using System.Diagnostics;

namespace OY.TotalCommander.TcPluginInterface;

public static class TcTrace
{
    public static void TraceCall(TcPlugin plugin, TraceLevel level, string callSignature, string result)
    {
#if TRACE
        var text = callSignature + (string.IsNullOrEmpty(result) ? null : ": " + result);
        if (plugin != null)
        {
            plugin.OnTcTrace(level, text);
            if (plugin.WriteTrace || level == TraceLevel.Error)
            {
                TraceOut(level, text, plugin.TraceTitle);
            }
        }
        else
        {
            TraceOut(level, text, null);
        }
#endif
    }
#if TRACE
    public const string TraceDateTimeFormat = "MM/dd/yy HH:mm:ss.fff ";

    public static readonly TraceSwitch TcPluginTraceSwitch = new("DotNetPlugins", "All .NET plugins", "Warning");

    public static void TraceError(string text, string pluginTitle) =>
        TraceOut(TraceLevel.Error, text, string.Format("ERROR ({0})", pluginTitle));

    public static void TraceOut(TraceLevel level, string text, string category) => TraceOut(level, text, category, 0);

    public static void TraceOut(TraceLevel level, string text, string category, int indent)
    {
        if (level.Equals(TraceLevel.Error) && TcPluginTraceSwitch.TraceError
            || level.Equals(TraceLevel.Warning) && TcPluginTraceSwitch.TraceWarning
            || level.Equals(TraceLevel.Info) && TcPluginTraceSwitch.TraceInfo
            || level.Equals(TraceLevel.Verbose) && TcPluginTraceSwitch.TraceVerbose)
        {
            var timeStr = GetTraceTimeString();
            if (indent < 0 && Trace.IndentLevel > 0)
            {
                Trace.IndentLevel--;
            }

            Trace.WriteLine(text, timeStr + " - " + category);
            if (indent > 0)
            {
                Trace.IndentLevel++;
            }
        }
    }

    public static string GetTraceTimeString() => DateTime.Now.ToString(TraceDateTimeFormat);

    public static void TraceDelimiter()
    {
        if (TcPluginTraceSwitch.TraceWarning)
        {
            Trace.WriteLine("- - - - - - - - - -");
        }
    }

#endif
}
