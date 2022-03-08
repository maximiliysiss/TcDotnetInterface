using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TcPluginInterface.Packer;

public class PackerPlugin : TcPlugin, IPackerPlugin
{
    #region Constructors

    public PackerPlugin(StringDictionary pluginSettings) : base(pluginSettings)
    {
        BackgroundFlags = PackBackgroundFlags.None;
        Capabilities = PackerCapabilities.None;
    }

    #endregion Constructors

    public override void CreatePassword(int cryptoNumber, int flags) => Password ??= new PackerPassword(this, cryptoNumber, flags);

    #region Properties

    public PackerCapabilities Capabilities { get; set; }

    public PackBackgroundFlags BackgroundFlags { get; set; }

    #endregion Properties

    #region IPackerPlugin Members

    #region Mandatory Methods

    public virtual object OpenArchive(ref OpenArchiveData archiveData) => throw new MethodNotSupportedException("OpenArchive", true);

    [CLSCompliant(false)]
    public virtual PackerResult ReadHeader(ref object arcData, out HeaderData headerData) =>
        throw new MethodNotSupportedException("ReadHeader", true);

    public virtual PackerResult ProcessFile(object arcData, ProcessFileOperation operation, string destFile) =>
        throw new MethodNotSupportedException("ProcessFile", true);

    public virtual PackerResult CloseArchive(object arcData) => throw new MethodNotSupportedException("CloseArchive", true);

    #endregion Mandatory Methods

    #region Optional Methods

    public virtual PackerResult PackFiles(string packedFile, string subPath, string srcPath, List<string> addList, PackFilesFlags flags)
        => PackerResult.NotSupported;

    public virtual PackerResult DeleteFiles(string packedFile, List<string> deleteList) => PackerResult.NotSupported;

    public virtual void ConfigurePacker(TcWindow parentWin)
    {
    }

    public virtual object StartMemPack(MemPackOptions options, string fileName) => null;

    public virtual PackerResult PackToMem(ref object memData, byte[] bufIn, ref int taken, byte[] bufOut, ref int written, int seekBy)
        => PackerResult.NotSupported;

    public virtual PackerResult DoneMemPack(object memData) => PackerResult.NotSupported;

    public virtual bool CanYouHandleThisFile(string fileName) => false;

    #endregion Optional Methods

    #endregion IPackerPlugin Members

    #region Callback Procedures

    protected int ProcessDataProc(string fileName, int size) => OnTcPluginEvent(new PackerProcessEventArgs(fileName, size));

    protected int ChangeVolProc(string arcName, ChangeValueProcMode mode) =>
        OnTcPluginEvent(new PackerChangeVolEventArgs(arcName, (int)mode));

    #endregion Callback Procedures
}
