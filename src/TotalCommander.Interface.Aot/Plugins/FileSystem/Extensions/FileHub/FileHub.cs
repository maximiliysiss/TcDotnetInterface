using TotalCommander.Interface.Aot.Generator.Models;

namespace TotalCommander.Interface.Aot.Plugins.FileSystem.Extensions.FileHub;

internal sealed class FileHub : IExtension
{
    public string Name => "TotalCommander.Interface.Abstraction.FileSystem.Interface.Extensions.IFileHub";
    public IMethod[] Methods { get; } = [];
}
