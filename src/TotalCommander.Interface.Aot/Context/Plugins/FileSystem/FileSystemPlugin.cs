using TotalCommander.Interface.Aot.Context.Models;

namespace TotalCommander.Interface.Aot.Context.Plugins.FileSystem;

internal sealed record FileSystemPlugin() : Plugin(PluginName, _pluginMethods, [])
{
    private const string PluginName = "TotalCommander.Interface.Abstraction.FileSystem.Interface.IFileSystemPlugin";

    private static readonly Method[] _pluginMethods =
    [
        new Method(
            Name: "FsInit",
            DefaultBody: "return 0;",
            ReturnType: Parameter.Int,
            Parameters: [Parameter.Int, Parameter.IntPtr, Parameter.IntPtr, Parameter.IntPtr]),
        new Method(
            Name: "FsInitW",
            DefaultBody: "return 0;",
            ReturnType: Parameter.Int,
            Parameters: [Parameter.Int, Parameter.IntPtr, Parameter.IntPtr, Parameter.IntPtr]),
        new Method(
            Name: "FsFindFirst",
            ReturnType: Parameter.IntPtr,
            Parameters: [Parameter.String, Parameter.IntPtr]),
        new Method(
            Name: "FsFindFirstW",
            ReturnType: Parameter.IntPtr,
            Parameters: [Parameter.String, Parameter.IntPtr])
    ];
}
