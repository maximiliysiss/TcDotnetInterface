using System;

namespace TcPluginInterface.FileSystem;

[Serializable]
public class ProgressEventArgs : PluginEventArgs
{
    public ProgressEventArgs(int pluginNumber, string sourceName, string targetName, int percentDone)
    {
        PluginNumber = pluginNumber;
        SourceName = sourceName;
        TargetName = targetName;
        PercentDone = percentDone;
    }

    #region Properties

    public int PluginNumber { get; private set; }
    public string SourceName { get; private set; }
    public string TargetName { get; private set; }
    public int PercentDone { get; private set; }

    #endregion Properties
}
