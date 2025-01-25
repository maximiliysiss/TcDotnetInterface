namespace TotalCommander.Interface.Aot.Generator.Models;

internal interface IExtension
{
    string Name { get; }
    IMethod[] Methods { get; }
}
