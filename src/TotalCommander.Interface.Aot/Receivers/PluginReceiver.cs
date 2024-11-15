using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Context;
using TotalCommander.Interface.Aot.Receivers.Models;

namespace TotalCommander.Interface.Aot.Receivers;

internal sealed class PluginReceiver : ISyntaxContextReceiver
{
    public List<PluginReceiverContext> Contexts { get; } = new(1);

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax node)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
            return;

        var plugins = PluginContext.Plugins
            .Select(c => new { Plugin = c, Type = context.SemanticModel.Compilation.GetTypeByMetadataName(c.Name) })
            .Where(c => c.Type is not null)
            .ToArray();

        var linkedPlugins = plugins
            .Where(c => symbol.Interfaces.Contains(c.Type!))
            .ToArray();

        if (linkedPlugins is [])
            return;

        var pluginReceiverContext = new PluginReceiverContext(
            Symbol: symbol,
            Location: context.Node.GetLocation(),
            Plugins: [.. linkedPlugins.Select(c => c.Plugin)]);

        Contexts.Add(pluginReceiverContext);
    }
}
