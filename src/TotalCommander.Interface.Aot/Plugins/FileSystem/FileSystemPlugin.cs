using TotalCommander.Interface.Aot.Generator.Models;
using TotalCommander.Interface.Aot.Plugins.FileSystem.Methods;

namespace TotalCommander.Interface.Aot.Plugins.FileSystem;

internal sealed record FileSystemPlugin(string name, IExtension[] extensions) : IPlugin
{
    public const string Type = "TotalCommander.Interface.Abstraction.FileSystem.Interface.IFileSystemPlugin";

    public string Name => name;

    public IMethod[] Methods { get; } =
    [
        new InitMethod(name: "FsInit"),
        new InitMethod(name: "FsInitW"),
        new FindFirstMethod(name: "FsFindFirst", isUnicode: false),
        new FindFirstMethod(name: "FsFindFirstW", isUnicode: true),
        new FindNextMethod(name: "FsFindNext", isUnicode: false),
        new FindNextMethod(name: "FsFindNextW", isUnicode: true),
        new FindClose(),
        new ContentGetSupportedFieldMethod(),
    ];

    public IExtension[] Extensions { get; } = extensions;
}
