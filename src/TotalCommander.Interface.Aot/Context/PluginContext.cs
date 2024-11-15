using TotalCommander.Interface.Aot.Context.Plugins;
using TotalCommander.Interface.Aot.Context.Plugins.FileSystem;

namespace TotalCommander.Interface.Aot.Context;

internal static class PluginContext
{
    public static readonly Plugin[] Plugins =
    [
        new FileSystemPlugin()
    ];
}
