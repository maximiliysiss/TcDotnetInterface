using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Generator.Diagnostics;
using TotalCommander.Interface.Aot.Plugins.Bridge;
using TotalCommander.Interface.Aot.Receivers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TotalCommander.Interface.Aot.Generator;

[Generator]
internal sealed class NativeApiGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new PluginReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not PluginReceiver receiver || receiver.Plugins is [])
            return;

        if (receiver.Plugins.Count > 1)
        {
            foreach (var diagnostic in receiver.Plugins.Select(c => MapDiagnostic(c.Location)))
                context.ReportDiagnostic(diagnostic);
            return;
        }

        var (plugin, _) = receiver.Plugins[0];

        const string apiName = "Api";

        IEnumerable<MemberDeclarationSyntax> members =
        [
            BridgeFactory.Create(plugin),
            .. plugin.Methods.Select(m => m.Create()),
            .. plugin.Extensions.SelectMany(e => e.Methods.Select(m => m.Create()))
        ];

        var memberDeclarationSyntax = ClassDeclaration(apiName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithMembers(List(members));

        const string namespaceName = "TotalCommander.Api";

        var namespaceSyntax = FileScopedNamespaceDeclaration(IdentifierName(namespaceName))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(memberDeclarationSyntax));

        var source = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(namespaceSyntax))
            .NormalizeWhitespace()
            .ToFullString();

        context.AddSource("Api.g.cs", source);

        return;

        Diagnostic MapDiagnostic(Location location)
        {
            return Diagnostic.Create(
                descriptor: DiagnosticDescriptors.OnlyOnePluginInterface,
                location: location);
        }
    }
}
