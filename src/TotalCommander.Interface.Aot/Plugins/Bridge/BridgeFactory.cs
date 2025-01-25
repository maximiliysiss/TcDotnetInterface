using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Generator.Models;
using TotalCommander.Interface.Aot.Plugins.FileSystem;
using TotalCommander.Interface.Aot.Plugins.FileSystem.Bridge;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TotalCommander.Interface.Aot.Plugins.Bridge;

internal static class BridgeFactory
{
    private static readonly Dictionary<Type, string> _pluginBridges = new()
    {
        { typeof(FileSystemPlugin), FilesystemBridgeApi.Type }
    };

    public static FieldDeclarationSyntax Create(IPlugin plugin)
    {
        var pluginBridgeType = _pluginBridges[plugin.GetType()];

        return FieldDeclaration(
                VariableDeclaration(IdentifierName(pluginBridgeType))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(BridgeApi.Name))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ImplicitObjectCreationExpression()
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            ObjectCreationExpression(IdentifierName(plugin.Name))
                                                                .WithArgumentList(ArgumentList()))))))))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword)));
    }
}
