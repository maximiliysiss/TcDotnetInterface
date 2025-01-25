using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TotalCommander.Interface.Aot.Plugins.Shared;

internal static class SyntaxFactory
{
    private static readonly string _unmanagedCallersOnlyAttribute = typeof(UnmanagedCallersOnlyAttribute).FullName!;
    private static readonly string _marshal = typeof(Marshal).FullName!;

    public static AttributeSyntax UnmanagedCallersOnlyDeclaration(string entryPoint)
    {
        return Attribute(IdentifierName(_unmanagedCallersOnlyAttribute))
            .WithArgumentList(
                AttributeArgumentList(
                    SingletonSeparatedList(
                        AttributeArgument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(entryPoint)))
                            .WithNameEquals(NameEquals(IdentifierName("EntryPoint"))))));
    }

    public static InvocationExpressionSyntax MarshalingIntoString(string variableName)
    {
        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(_marshal),
                    IdentifierName("PtrToStringAuto")))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName(variableName)))));
    }
}
