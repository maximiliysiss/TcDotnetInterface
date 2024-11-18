using Microsoft.CodeAnalysis.CSharp;

namespace TotalCommander.Interface.Aot.Context.Plugins.Infrastructure;

internal static class BoolExtensions
{
    public static SyntaxKind AsLiteral(this bool value) => value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;
}
