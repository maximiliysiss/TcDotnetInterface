using System;

namespace TcPluginInterface.Packer;

[Serializable]
public class PackerProcessEventArgs : PluginEventArgs
{
    public PackerProcessEventArgs(string fileName, int size)
    {
        FileName = fileName;
        Size = size;
        Result = 0;
    }

    #region Properties

    public string FileName { get; private set; }
    public int Size { get; private set; }

    #endregion Properties
}
