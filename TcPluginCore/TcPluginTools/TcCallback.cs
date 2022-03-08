using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TcPluginInterface;
using TcPluginInterface.Content;
using TcPluginInterface.FileSystem;
using TcPluginInterface.Packer;
#if TRACE
#endif

namespace TcPluginTools;

public static class TcCallback
{
    #region Tracing

#if TRACE
    private static void TraceOut(TraceLevel level, string text, string category)
    {
        if (_writeTrace)
        {
            TcTrace.TraceOut(level, text, category);
        }
    }
#endif

    #endregion Tracing

    #region Constants

    public const string PluginCallbackDataName = "PluginCallbackData";
    public const int CryptPasswordMaxLen = NativeMethods.MAX_PATH_UNI;

    // Error Messages
    private const string ErrorMsg1 = "Callback: Plugin not found.";
    private const string ErrorMsg2 = "Callback: Domain error.";
    private const string ErrorMsg3 = "Callback: Wrong argument.";

    // Trace Messages
#if TRACE
    private const string TraceMsg1 = "Callback";
    private const string TraceMsg2 = "OnProgress ({0}, {1}): {2} => {3} - {4}.";
    private const string TraceMsg3 = "OnLog ({0}, {1}): {2}.";
    private const string TraceMsg4 = "DomainInfo";
    private const string TraceMsg5 = "OnRequest ({0}, {1}): {2}";
    private const string TraceMsg6 = "OnCrypt ({0}, {1}, {2}): {3}";
    private const string TraceMsg7 = "{0} - {1}.";
    private const string TraceMsg8 = "OnCompareProgress ({0}) - {1}.";
#endif

    #endregion Constants

    #region Variables

#if TRACE
    private static bool _writeTrace;

    // to trace Progress callback
    private const int ProgressTraceChunk = 25;
    private static int _prevPercDone = -ProgressTraceChunk - 1;
#endif

    #endregion Variables

    #region Main Handler

    // This handler is called in main AppDomain
    public static void TcPluginCallbackHandler()
    {
        var domain = AppDomain.CurrentDomain;
        string pluginId;
        try
        {
            pluginId = (string)domain.GetData(PluginCallbackDataName);
        }
        finally
        {
            domain.SetData(PluginCallbackDataName, null);
        }

        var tp = TcPluginLoader.GetTcPluginById(pluginId);
        if (tp == null)
        {
            throw new InvalidOperationException(ErrorMsg1);
        }

        if (!domain.Equals(tp.MainDomain))
        {
            throw new InvalidOperationException(ErrorMsg2);
        }
#if TRACE
        _writeTrace = tp.WriteTrace;
#endif
        var callbackDataBufferName = tp.DataBufferName;
        try
        {
            var o = domain.GetData(callbackDataBufferName);
            if (o == null || !(o is PluginEventArgs args))
            {
                throw new ArgumentException(ErrorMsg3);
            }

            HandleTcPluginEvent(tp, args);
        }
        finally
        {
#if TRACE
            _writeTrace = false;
#endif
        }
    }

    public static void HandleTcPluginEvent(object sender, PluginEventArgs e)
    {
        switch (e)
        {
            case CryptEventArgs args:
                CryptCallback(args);
                break;
            case ProgressEventArgs args:
                FsProgressCallback(args);
                break;
            case LogEventArgs args:
                FsLogCallback(args);
                break;
            case RequestEventArgs args:
                FsRequestCallback(args);
                break;
            case ContentProgressEventArgs args:
                ContentProgressCallback(args);
                break;
            case PackerProcessEventArgs args:
                PackerProcessCallback(args);
                break;
            case PackerChangeVolEventArgs args:
                PackerChangeVolCallback(args);
                break;
        }
    }

    #endregion Main Handler

    #region FS Callbacks

    private static ProgressCallback _progressCallback;
    private static ProgressCallbackW _progressCallbackW;
    private static LogCallback _logCallback;
    private static LogCallbackW _logCallbackW;
    private static RequestCallback _requestCallback;
    private static RequestCallbackW _requestCallbackW;
    private static FsCryptCallback _fsCryptCallback;
    private static FsCryptCallbackW _fsCryptCallbackW;

    public static void SetFsPluginCallbacks(
        ProgressCallback progress,
        ProgressCallbackW progressW,
        LogCallback log,
        LogCallbackW logW,
        RequestCallback request,
        RequestCallbackW requestW,
        FsCryptCallback crypt,
        FsCryptCallbackW cryptW)
    {
        _progressCallback ??= progress;
        _progressCallbackW ??= progressW;
        _logCallback ??= log;
        _logCallbackW ??= logW;
        _requestCallback ??= request;
        _requestCallbackW ??= requestW;
        _fsCryptCallback ??= crypt;
        _fsCryptCallbackW ??= cryptW;
    }

    public static void FsProgressCallback(ProgressEventArgs e)
    {
        if (_progressCallbackW == null && _progressCallback == null)
            return;

        var pluginNumber = e.PluginNumber;
        var sourceName = e.SourceName;
        var targetName = e.TargetName;
        var percentDone = e.PercentDone;

        if (_progressCallbackW != null)
        {
            e.Result = _progressCallbackW(pluginNumber, sourceName, targetName, percentDone);
        }
        else if (_progressCallback != null)
        {
            e.Result = _progressCallback(pluginNumber, sourceName, targetName, percentDone);
        }

#if TRACE
        if (percentDone - _prevPercDone < ProgressTraceChunk && percentDone != 100)
            return;

        TraceOut(
            TraceLevel.Verbose,
            string.Format(TraceMsg2, pluginNumber, percentDone, sourceName, targetName, e.Result),
            TraceMsg1);
        if (percentDone == 100)
        {
            _prevPercDone = -ProgressTraceChunk - 1;
        }
        else
        {
            _prevPercDone = percentDone;
        }
#endif
    }

    public static void FsLogCallback(LogEventArgs e)
    {
        if (_logCallbackW == null && _logCallback == null)
            return;

        if (_logCallbackW != null)
        {
            _logCallbackW(e.PluginNumber, e.MessageType, e.LogText);
        }
        else
        {
            _logCallback(e.PluginNumber, e.MessageType, e.LogText);
        }
#if TRACE
        TraceOut(
            TraceLevel.Info,
            string.Format(TraceMsg3, e.PluginNumber, ((LogMsgType)e.MessageType).ToString(), e.LogText),
            TraceMsg1);
#endif
    }

    public static void FsRequestCallback(RequestEventArgs e)
    {
        if (e.RequestType == (int)RequestType.DomainInfo)
        {
            e.ReturnedText = TcPluginLoader.DomainInfo;
            e.Result = 1;
#if TRACE
            TraceOut(TraceLevel.Info, e.ReturnedText, TraceMsg4);
#endif
        }
        else if (_requestCallbackW != null || _requestCallback != null)
        {
            var retText = IntPtr.Zero;
            if (e.RequestType < (int)RequestType.MsgOk)
            {
                if (_requestCallbackW != null)
                {
                    retText = Marshal.AllocHGlobal(e.MaxLen * 2);
                    Marshal.Copy(new char[e.MaxLen], 0, retText, e.MaxLen);
                }
                else
                {
                    retText = Marshal.AllocHGlobal(e.MaxLen);
                    Marshal.Copy(new byte[e.MaxLen], 0, retText, e.MaxLen);
                }
            }

            try
            {
                if (retText != IntPtr.Zero && !string.IsNullOrEmpty(e.ReturnedText))
                {
                    if (_requestCallbackW != null)
                    {
                        Marshal.Copy(e.ReturnedText.ToCharArray(), 0, retText, e.ReturnedText.Length);
                    }
                    else
                    {
                        TcUtils.WriteStringAnsi(e.ReturnedText, retText, 0);
                    }
                }

                if (_requestCallbackW != null)
                {
                    e.Result = _requestCallbackW(e.PluginNumber, e.RequestType, e.CustomTitle, e.CustomText, retText, e.MaxLen) ? 1 : 0;
                }
                else
                {
                    e.Result = _requestCallback(e.PluginNumber, e.RequestType, e.CustomTitle, e.CustomText, retText, e.MaxLen) ? 1 : 0;
                }
#if TRACE
                var traceStr = string.Format(TraceMsg5, e.PluginNumber, ((RequestType)e.RequestType).ToString(), e.ReturnedText);
#endif
                if (e.Result != 0 && retText != IntPtr.Zero)
                {
                    e.ReturnedText = _requestCallbackW != null ? Marshal.PtrToStringUni(retText) : Marshal.PtrToStringAnsi(retText);
#if TRACE
                    traceStr += $" => {e.ReturnedText}";
#endif
                }
#if TRACE
                TraceOut(TraceLevel.Verbose, string.Format(TraceMsg7, traceStr, e.Result), TraceMsg1);
#endif
            }
            finally
            {
                if (retText != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(retText);
                }
            }
        }
    }

    #endregion FS Callbacks

    #region Content Callbacks

    private static ContentProgressCallback _contentProgressCallback;

    public static void SetContentPluginCallback(ContentProgressCallback contentProgress) => _contentProgressCallback = contentProgress;

    public static void ContentProgressCallback(ContentProgressEventArgs e)
    {
        if (_contentProgressCallback == null)
            return;

        e.Result = _contentProgressCallback(e.NextBlockData);
#if TRACE
        TraceOut(TraceLevel.Verbose, string.Format(TraceMsg8, e.NextBlockData, e.Result), TraceMsg1);
#endif
    }

    #endregion Content Callbacks

    #region Packer Callbacks

    private static ChangeVolCallback _changeVolCallback;
    private static ChangeVolCallbackW _changeVolCallbackW;
    private static ProcessDataCallback _processDataCallback;
    private static ProcessDataCallbackW _processDataCallbackW;
    private static PkCryptCallback _pkCryptCallback;
    private static PkCryptCallbackW _pkCryptCallbackW;

    public static void SetPackerPluginCallbacks(
        ChangeVolCallback changeVol,
        ChangeVolCallbackW changeVolW,
        ProcessDataCallback processData,
        ProcessDataCallbackW processDataW,
        PkCryptCallback crypt,
        PkCryptCallbackW cryptW)
    {
        _changeVolCallback ??= changeVol;
        _changeVolCallbackW ??= changeVolW;
        _processDataCallback ??= processData;
        _processDataCallbackW ??= processDataW;
        _pkCryptCallback ??= crypt;
        _pkCryptCallbackW ??= cryptW;
    }

    public static void PackerProcessCallback(PackerProcessEventArgs e)
    {
        if (_processDataCallbackW == null && _processDataCallback == null)
            return;

        var fileName = e.FileName;
        var size = e.Size;

        if (_processDataCallbackW != null)
        {
            e.Result = _processDataCallbackW(fileName, size);
        }
        else if (_processDataCallback != null)
        {
            e.Result = _processDataCallback(fileName, size);
        }
#if TRACE
        TraceOut(
            TraceLevel.Verbose,
            $"OnProcessData ({fileName}, {size}) - {e.Result}.",
            TraceMsg1);
#endif
    }

    public static void PackerChangeVolCallback(PackerChangeVolEventArgs e)
    {
        if (_changeVolCallbackW == null && _changeVolCallback == null)
            return;

        var arcName = e.ArcName;
        var mode = e.Mode;

        if (_changeVolCallbackW != null)
        {
            e.Result = _changeVolCallbackW(arcName, mode);
        }
        else if (_changeVolCallback != null)
        {
            e.Result = _changeVolCallback(arcName, mode);
        }
#if TRACE
        TraceOut(
            TraceLevel.Verbose,
            $"OnChangeVol ({arcName}, {mode}) - {e.Result}.",
            TraceMsg1);
#endif
    }

    public static void CryptCallback(CryptEventArgs e)
    {
        bool isUnicode;
        var loadPassword = e.Mode == 2 || e.Mode == 3; // LoadPassword or LoadPasswordNoUI
        if (e.PluginNumber < 0)
        {
            // Packer plugin call
            if (_pkCryptCallbackW == null && _pkCryptCallback == null)
            {
                return;
            }

            isUnicode = _pkCryptCallbackW != null;
        }
        else
        {
            // File System plugin call
            if (_fsCryptCallbackW == null && _fsCryptCallback == null)
            {
                return;
            }

            isUnicode = _fsCryptCallbackW != null;
        }

        var pswText = IntPtr.Zero;
        try
        {
            if (isUnicode)
            {
                if (loadPassword)
                {
                    pswText = Marshal.AllocHGlobal(CryptPasswordMaxLen * 2);
                }
                else if (!string.IsNullOrEmpty(e.Password))
                {
                    pswText = Marshal.StringToHGlobalUni(e.Password);
                }

                e.Result = e.PluginNumber < 0
                    ? _pkCryptCallbackW(e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen)
                    : _fsCryptCallbackW(e.PluginNumber, e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen);
            }
            else
            {
                if (loadPassword)
                {
                    pswText = Marshal.AllocHGlobal(CryptPasswordMaxLen);
                }
                else if (!string.IsNullOrEmpty(e.Password))
                {
                    pswText = Marshal.StringToHGlobalAnsi(e.Password);
                }

                e.Result = e.PluginNumber < 0
                    ? _pkCryptCallback(e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen)
                    : _fsCryptCallback(e.PluginNumber, e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen);
            }

            // tracing
#if TRACE
            var traceStr = string.Format(TraceMsg6, e.PluginNumber, e.CryptoNumber, e.Mode, e.StoreName);
#endif

            if (loadPassword && e.Result == 0)
            {
                e.Password = isUnicode ? Marshal.PtrToStringUni(pswText) : Marshal.PtrToStringAnsi(pswText);
#if TRACE
                traceStr += " => (PASSWORD)"; //+ e.Password;
#endif
            }
            else
            {
                e.Password = string.Empty;
            }

            // tracing
#if TRACE
            TraceOut(
                TraceLevel.Info,
                string.Format(
                    TraceMsg7,
                    traceStr,
                    ((CryptResult)e.Result).ToString()),
                TraceMsg1);
#endif
        }
        finally
        {
            if (pswText != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pswText);
            }
        }
    }

    #endregion Packer Callbacks

    #region Lister Callbacks

    //

    #endregion Lister Callbacks
}
