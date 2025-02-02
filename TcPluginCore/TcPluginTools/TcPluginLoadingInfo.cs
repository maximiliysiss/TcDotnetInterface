﻿using System;
using TcPluginInterface;

namespace TcPluginTools;

public class TcPluginLoadingInfo
{
    public TcPluginLoadingInfo(string wrapperFileName, TcPlugin tcPlugin, AppDomain domain)
    {
        WrapperFileName = wrapperFileName;
        Plugin = tcPlugin;
        Domain = domain;
        PluginNumber = -1;
        CryptoNumber = -1;
        CryptoFlags = 0;
        UnloadExpired = true;
        LifetimeStatus = PluginLifetimeStatus.Active;
    }

    public string WrapperFileName { get; }
    public TcPlugin Plugin { get; set; }
    public AppDomain Domain { get; set; }
    public int PluginNumber { get; set; }
    public int CryptoNumber { get; set; }
    public int CryptoFlags { get; set; }
    public bool UnloadExpired { get; set; }
    public PluginLifetimeStatus LifetimeStatus { get; set; }
}
