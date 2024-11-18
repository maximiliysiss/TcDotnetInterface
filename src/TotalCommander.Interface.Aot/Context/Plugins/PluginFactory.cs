using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TotalCommander.Interface.Aot.Context.Plugins.FileSystem;

namespace TotalCommander.Interface.Aot.Context.Plugins;

internal static class PluginFactory
{
    private static readonly Dictionary<string, Func<string, IPlugin>> _factories = new()
    {
        { FileSystemPlugin.Type, n => new FileSystemPlugin(n) }
    };

    public static IPlugin? CreatePlugin(GeneratorSyntaxContext context, INamedTypeSymbol symbol)
    {
        var factories = _factories
            .Select(c => new { Key = context.SemanticModel.Compilation.GetTypeByMetadataName(c.Key)!, c.Value })
            .ToArray();

        var factory = factories.FirstOrDefault(c => symbol.Interfaces.Contains(c.Key));

        return factory?.Value(symbol.ToDisplayString());
    }
}
