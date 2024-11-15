using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TotalCommander.Interface.Aot.Generator.Diagnostics;
using TotalCommander.Interface.Aot.Receivers;
using TotalCommander.Interface.Aot.Receivers.Models;

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
        if (context.SyntaxContextReceiver is not PluginReceiver receiver)
            return;

        if (receiver.Contexts is [])
            return;

        if (receiver.Contexts.Count is not 1)
        {
            foreach (var diagnostic in receiver.Contexts.Select(MapDiagnostic))
                context.ReportDiagnostic(diagnostic);
            return;
        }

        var receiverContext = receiver.Contexts[0];
        if (receiverContext.Plugins.Length > 1)
        {
            context.ReportDiagnostic(MapDiagnostic(receiverContext));
            return;
        }

        var compilationUnitSyntax = SyntaxFactory.CompilationUnit();

        return;

        Diagnostic MapDiagnostic(PluginReceiverContext ctx)
        {
            return Diagnostic.Create(
                descriptor: DiagnosticDescriptors.OnlyOnePluginInterface,
                location: ctx.Location);
        }
    }
}
