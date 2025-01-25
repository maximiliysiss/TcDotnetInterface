namespace TotalCommander.Interface.Aot.Generator.Models;

internal interface IPlugin
{
    string Name { get; }
    IMethod[] Methods { get; }
    IExtension[] Extensions { get; }
}
