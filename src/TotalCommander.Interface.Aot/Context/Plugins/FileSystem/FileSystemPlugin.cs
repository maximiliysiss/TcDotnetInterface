using TotalCommander.Interface.Aot.Context.Models;
using TotalCommander.Interface.Aot.Context.Plugins.FileSystem.Methods;

namespace TotalCommander.Interface.Aot.Context.Plugins.FileSystem;

internal sealed record FileSystemPlugin(string name) : IPlugin
{
    public const string Type = "TotalCommander.Interface.Abstraction.FileSystem.Interface.IFileSystemPlugin";

    public string Name => name;

    public IMethod[] Methods { get; } =
    [
        new FsInitMethod(name: "FsInit"),
        new FsInitMethod(name: "FsInitW"),
        new FindFirstMethod(name: "FsFindFirst", isUnicode: false),
        new FindFirstMethod(name: "FsFindFirstW", isUnicode: true),
        new FindNextMethod(name: "FsFindNext", isUnicode: false),
        new FindNextMethod(name: "FsFindNextW", isUnicode: true),
    ];

    public Extension[] Extensions { get; } = [];
}
