namespace TotalCommander.Interface.Aot.Plugins.FileSystem.Bridge;

internal static class FilesystemBridgeApi
{
    public const string Type = "TotalCommander.Interface.Abstraction.FileSystem.Native.Bridge.Bridge";

    public const string FindFirst = nameof(Abstraction.FileSystem.Native.Bridge.Bridge.FindFirst);
    public const string FindNext = nameof(Abstraction.FileSystem.Native.Bridge.Bridge.FindNext);
    public const string FindClose = nameof(Abstraction.FileSystem.Native.Bridge.Bridge.FindClose);
}
