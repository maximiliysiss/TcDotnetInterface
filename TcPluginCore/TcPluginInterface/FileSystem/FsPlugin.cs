using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using TcPluginInterface.Content;
#if TRACE
#endif

namespace TcPluginInterface.FileSystem;

public class FsPlugin : TcPlugin, IFsPlugin
{
    #region MarshalByRefObject - Lifetime initialization

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public override object InitializeLifetimeService()
    {
        var lease = (ILease)base.InitializeLifetimeService();
        if (lease == null || lease.CurrentState != LeaseState.Initial)
            return lease;

        // By default we set infinite lifetime for each created plugin (initialLifeTime = 0)
        // To change, set "initialLifeTime" key in plugin configuration file
        var initialLifeTime = TimeSpan.Zero;
        try
        {
            initialLifeTime = TimeSpan.Parse(Settings["initialLifeTime"]);
        }
        catch (ArgumentNullException)
        {
        }
        catch (FormatException)
        {
        }

        // set the Initial lease time if it's configured
        if (!initialLifeTime.Equals(TimeSpan.MinValue))
        {
            lease.InitialLeaseTime = initialLifeTime;
        }

        // lease.InitialLeaseTime = 0 means infinite lifetime for the remote object, it's enough.
        if (lease.InitialLeaseTime.Equals(TimeSpan.Zero))
            return lease;

        var renewOnCallTime = TimeSpan.MinValue;
        try
        {
            renewOnCallTime = TimeSpan.Parse(Settings["renewOnCallTime"]);
        }
        catch (ArgumentNullException)
        {
        }
        catch (FormatException)
        {
        }

        // Set the RenewOnCall lease time - time added to object's lifetime after each client call
        if (!renewOnCallTime.Equals(TimeSpan.MinValue))
        {
            lease.RenewOnCallTime = renewOnCallTime;
        }

        return lease;
    }

    #endregion

    public override void CreatePassword(int cryptoNumber, int flags) => Password ??= new FsPassword(this, cryptoNumber, flags);

    #region Properties

    public FsBackgroundFlags BackgroundFlags { get; set; }

    public ContentPlugin ContentPlgn { get; set; }

    public bool IsTempFilePanel { get; protected set; }

    public override string TraceTitle =>
        Convert.ToBoolean(Settings["useTitleForTrace"]) ? Title : PluginNumber.ToString(CultureInfo.InvariantCulture);

    public bool UnloadExpired { get; }

    public bool WriteStatusInfo { get; }

    #endregion Properties

    #region Constructors

    public FsPlugin(StringDictionary pluginSettings)
        : base(pluginSettings)
    {
        BackgroundFlags = FsBackgroundFlags.None;
        IsTempFilePanel = false;
        PluginNumber = -1;
        UnloadExpired = pluginSettings["unloadExpired"] == null || Convert.ToBoolean(pluginSettings["unloadExpired"]);
        SetPluginFolder("iconFolder", Path.Combine(PluginFolder, "img"));
#if TRACE
        WriteStatusInfo = Convert.ToBoolean(pluginSettings["writeStatusInfo"]);
#endif
    }

    ~FsPlugin()
    {
#if TRACE
        if (WriteTrace)
        {
            TraceProc(TraceLevel.Warning, "FsPlugin's destructor is called.");
        }
#endif
    }

    #endregion Constructors

    #region IFsPlugin Members

    #region Mandatory Methods

    [CLSCompliant(false)]
    public virtual object FindFirst(string path, out FindData findData) => throw new MethodNotSupportedException("FindFirst", true);

    [CLSCompliant(false)]
    public virtual bool FindNext(ref object o, out FindData findData) => throw new MethodNotSupportedException("FindNext", true);

    public virtual int FindClose(object o) => 0;

    #endregion Mandatory Methods

    #region Optional Methods

    [CLSCompliant(false)]
    public virtual FileSystemExitCode GetFile(
        string remoteName,
        ref string localName,
        CopyFlags copyFlags,
        RemoteInfo remoteInfo) =>
        FileSystemExitCode.NotSupported;

    public virtual FileSystemExitCode PutFile(string localName, ref string remoteName, CopyFlags copyFlags) =>
        FileSystemExitCode.NotSupported;

    [CLSCompliant(false)]
    public virtual FileSystemExitCode RenMovFile(
        string oldName,
        string newName,
        bool move,
        bool overwrite,
        RemoteInfo remoteInfo) =>
        FileSystemExitCode.NotSupported;

    public virtual bool DeleteFile(string fileName) => false;

    public virtual bool RemoveDir(string dirName) => false;

    public virtual bool MkDir(string dir) => false;

    public ExecResult ExecuteFile(TcWindow mainWin, ref string remoteName, string verb)
    {
        if (string.IsNullOrEmpty(verb))
        {
            return ExecResult.Error;
        }

        if (verb.Equals("open", StringComparison.CurrentCultureIgnoreCase))
        {
            return ExecuteOpen(mainWin, ref remoteName);
        }

        if (verb.Equals("properties", StringComparison.CurrentCultureIgnoreCase))
        {
            return ExecuteProperties(mainWin, remoteName);
        }

        if (verb.StartsWith("chmod ", StringComparison.CurrentCultureIgnoreCase))
        {
            return ExecuteCommand(mainWin, ref remoteName, verb.Trim());
        }

        if (verb.StartsWith("quote ", StringComparison.CurrentCultureIgnoreCase))
        {
            return ExecuteCommand(mainWin, ref remoteName, verb.Substring(6).Trim());
        }

        return ExecResult.Yourself;
    }

    public virtual ExecResult ExecuteOpen(TcWindow mainWin, ref string remoteName) => ExecResult.Yourself;

    public virtual ExecResult ExecuteProperties(TcWindow mainWin, string remoteName) => ExecResult.Yourself;

    public virtual ExecResult ExecuteCommand(TcWindow mainWin, ref string remoteName, string command) => ExecResult.Yourself;

    public virtual bool SetAttr(string remoteName, FileAttributes attr) => false;

    public virtual bool SetTime(
        string remoteName,
        DateTime? creationTime,
        DateTime? lastAccessTime,
        DateTime? lastWriteTime) =>
        false;

    public virtual bool Disconnect(string disconnectRoot) => false;

    protected InfoOperation CurrentTcOperation;

    public virtual void StatusInfo(string remoteDir, InfoStartEnd startEnd, InfoOperation infoOperation) =>
        CurrentTcOperation = startEnd == InfoStartEnd.Start ? infoOperation : InfoOperation.None;

    public virtual ExtractIconResult ExtractCustomIcon(
        ref string remoteName,
        ExtractIconFlags extractFlags,
        out Icon icon)
    {
        icon = null;
        return ExtractIconResult.UseDefault;
    }

    public virtual PreviewBitmapResult GetPreviewBitmap(
        ref string remoteName,
        int width,
        int height,
        out Bitmap returnedBitmap)
    {
        returnedBitmap = null;
        return PreviewBitmapResult.None;
    }

    public virtual bool GetLocalName(ref string remoteName, int maxLen) => false;

    #endregion Optional Methods

    #endregion IFsPlugin Members

    #region Callback Procedures

    protected int ProgressProc(string source, string destination, int percentDone) =>
        OnTcPluginEvent(new ProgressEventArgs(PluginNumber, source, destination, percentDone));

    protected void LogProc(LogMsgType msgType, string logText) =>
        OnTcPluginEvent(new LogEventArgs(PluginNumber, (int)msgType, logText));

    protected bool RequestProc(
        RequestType requestType,
        string customTitle,
        string customText,
        ref string returnedText,
        int maxLen)
    {
        var e =
            new RequestEventArgs(PluginNumber, (int)requestType, customTitle, customText, returnedText, maxLen);
        if (OnTcPluginEvent(e) != 0)
        {
            returnedText = e.ReturnedText;
            return true;
        }

        return false;
    }

    #endregion Callback Procedures
}
