using System;

namespace TcPluginInterface;

[Serializable]
public enum PluginType
{
    Content,
    FileSystem,
    Lister,
    Packer,
    QuickSearch,
    Unknown
}
