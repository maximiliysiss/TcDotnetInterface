using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Generator.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static TotalCommander.Interface.Aot.Plugins.Shared.SyntaxFactory;
using static TotalCommander.Interface.Aot.Plugins.Infrastructure.TypeNames;

namespace TotalCommander.Interface.Aot.Plugins.FileSystem.Methods;

internal sealed class InitMethod(string name) : IMethod
{
    public MethodDeclarationSyntax Create()
    {
        var parameters = new SyntaxNodeOrToken[]
        {
            Parameter(Identifier("pluginNumber")).WithType(PredefinedType(Token(SyntaxKind.IntKeyword))),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier("progressProc")).WithType(IdentifierName(IntPtr)),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier("logProc")).WithType(IdentifierName(IntPtr)),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier("requestProc")).WithType(IdentifierName(IntPtr))
        };

        return MethodDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier(name))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(UnmanagedCallersOnlyDeclaration(name)))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(parameters)))
            .WithExpressionBody(ArrowExpressionClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
