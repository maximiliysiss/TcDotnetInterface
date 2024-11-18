using TotalCommander.Interface.Aot.Context.Models;

namespace TotalCommander.Interface.Aot.Context.Plugins;

internal interface IPlugin
{
    string Name { get; }
    IMethod[] Methods { get; }
    Extension[] Extensions { get; }
}
