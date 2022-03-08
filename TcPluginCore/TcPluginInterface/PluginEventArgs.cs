using System;
using System.Diagnostics;

namespace TcPluginInterface;

[Serializable]
public class PluginEventArgs : EventArgs
{
    public PluginEventArgs() => Result = 0;

    public int Result { get; set; }
}

[Serializable]
public class CryptEventArgs : PluginEventArgs
{
    public CryptEventArgs(int pluginNumber, int cryptoNumber, int mode, string storeName, string password)
    {
        PluginNumber = pluginNumber;
        CryptoNumber = cryptoNumber;
        Mode = mode;
        StoreName = storeName;
        Password = password;
    }

    #region Properties

    public int PluginNumber { get; private set; }
    public int CryptoNumber { get; private set; }
    public int Mode { get; private set; }
    public string StoreName { get; private set; }
    public string Password { get; set; }

    #endregion Properties
}

[Serializable]
public class TraceEventArgs : EventArgs
{
    public TraceEventArgs(TraceLevel level, string text)
    {
        Level = level;
        Text = text;
    }

    #region Properties

    public TraceLevel Level { get; private set; }
    public string Text { get; private set; }

    #endregion Properties
}
