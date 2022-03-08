using System;

namespace TcPluginInterface.Packer;

[Serializable]
public class PackerChangeVolEventArgs : PluginEventArgs
{
    public PackerChangeVolEventArgs(string arcName, int mode)
    {
        ArcName = arcName;
        Mode = mode;
        Result = 0;
    }

    #region Properties

    public string ArcName { get; private set; }
    public int Mode { get; private set; }

    #endregion Properties
}
