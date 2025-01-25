using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TotalCommander.Interface.Aot.Generator.Models;
using TotalCommander.Interface.Aot.Plugins.FileSystem;
using TotalCommander.Interface.Aot.Plugins.FileSystem.Extensions;
using TotalCommander.Interface.Aot.Plugins.FileSystem.Extensions.FileHub;

namespace TotalCommander.Interface.Aot.Plugins;

internal static class PluginFactory
{
    private static readonly Dictionary<string, FactoryContext> _factories = new()
    {
        {
            FileSystemPlugin.Type,
            new FactoryContext(
                Factory: (name, extensions) => new FileSystemPlugin(name, extensions),
                Extensions: [new FileHub()])
        }
    };

    public static IPlugin? CreatePlugin(GeneratorSyntaxContext context, INamedTypeSymbol symbol)
    {
        var factories = _factories
            .Select(c => new { Key = context.SemanticModel.Compilation.GetTypeByMetadataName(c.Key)!, c.Value })
            .ToArray();

        var factoryContext = factories.FirstOrDefault(c => symbol.Interfaces.Contains(c.Key));

        var extensions = factoryContext?.Value.Extensions
            .Select(c => new { Key = context.SemanticModel.Compilation.GetTypeByMetadataName(c.Name)!, Value = c })
            .Where(c => symbol.Interfaces.Contains(c.Key))
            .ToArray();

        return factoryContext?.Value.Factory(
            symbol.ToDisplayString(),
            extensions?.Select(c => c.Value).ToArray() ?? []);
    }

    private sealed record FactoryContext(Func<string, IExtension[], IPlugin> Factory, IExtension[] Extensions);
}
