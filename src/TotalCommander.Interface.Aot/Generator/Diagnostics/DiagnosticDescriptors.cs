using Microsoft.CodeAnalysis;

namespace TotalCommander.Interface.Aot.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor OnlyOnePluginInterface = new(
        id: "TC001",
        title: "Several interfaces",
        messageFormat: "There are several plugin's interface implementations",
        category: "InvalidOperation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
