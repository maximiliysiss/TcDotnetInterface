using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Context.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static TotalCommander.Interface.Aot.Context.Plugins.Shared.SyntaxFactory;
using static TotalCommander.Interface.Aot.Context.Plugins.Infrastructure.Constants;

namespace TotalCommander.Interface.Aot.Context.Plugins.FileSystem.Methods;

internal sealed class FsInitMethod(string name) : IMethod
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
