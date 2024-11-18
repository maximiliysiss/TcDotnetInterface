using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Context.Plugins.Bridge;
using TotalCommander.Interface.Aot.Generator.Diagnostics;
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
            foreach (var diagnostic in receiver.Plugins.Select(c => MapDiagnostic(c.Item2)))
                context.ReportDiagnostic(diagnostic);
            return;
        }

        var (plugin, _) = receiver.Plugins[0];

        var memberDeclarationSyntax = ClassDeclaration("Api")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithMembers(List<MemberDeclarationSyntax>([BridgeFactory.Create(plugin), .. plugin.Methods.Select(c => c.Create())]));

        var namespaceSyntax = FileScopedNamespaceDeclaration(IdentifierName("TotalCommander.Api"))
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
