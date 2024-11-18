using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Context.Plugins;

namespace TotalCommander.Interface.Aot.Receivers;

internal sealed class PluginReceiver : ISyntaxContextReceiver
{
    public List<(IPlugin plugin, Location)> Plugins { get; } = new(1);

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax node)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
            return;

        var plugin = PluginFactory.CreatePlugin(context, symbol);
        if (plugin is null)
            return;

        Plugins.Add((plugin, node.GetLocation()));
    }
}
