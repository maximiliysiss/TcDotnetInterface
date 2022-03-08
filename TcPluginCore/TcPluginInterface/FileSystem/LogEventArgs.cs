using System;

namespace OY.TotalCommander.TcPluginInterface.FileSystem;

[Serializable]
public class LogEventArgs : PluginEventArgs
{
    public LogEventArgs(int pluginNumber, int messageType, string logText)
    {
        PluginNumber = pluginNumber;
        MessageType = messageType;
        LogText = logText;
    }

    #region Properties

    public int PluginNumber { get; private set; }
    public int MessageType { get; private set; }
    public string LogText { get; private set; }

    #endregion Properties
}
