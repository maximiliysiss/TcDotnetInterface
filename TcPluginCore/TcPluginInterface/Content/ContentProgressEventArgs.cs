using System;

namespace TcPluginInterface.Content;

[Serializable]
public class ContentProgressEventArgs : PluginEventArgs
{
    public ContentProgressEventArgs(int nextBlockData) => NextBlockData = nextBlockData;

    #region Properties

    public int NextBlockData { get; private set; }

    #endregion Properties
}
