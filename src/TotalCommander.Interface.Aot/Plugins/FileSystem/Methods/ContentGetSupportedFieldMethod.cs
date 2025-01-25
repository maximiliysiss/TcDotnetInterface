using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Generator.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static TotalCommander.Interface.Aot.Plugins.Shared.SyntaxFactory;
using static TotalCommander.Interface.Aot.Plugins.Infrastructure.TypeNames;

namespace TotalCommander.Interface.Aot.Plugins.FileSystem.Methods;

internal sealed class ContentGetSupportedFieldMethod : IMethod
{
    private const string Name = "FsContentGetSupportedField";

    public MethodDeclarationSyntax Create()
    {
        var parameters = new SyntaxNodeOrToken[]
        {
            Parameter(Identifier("fieldIndex")).WithType(PredefinedType(Token(SyntaxKind.IntKeyword))),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier("fieldName")).WithType(IdentifierName(IntPtr)),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier("units")).WithType(IdentifierName(IntPtr)),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier("maxLen")).WithType(PredefinedType(Token(SyntaxKind.IntKeyword)))
        };

        return MethodDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier(Name))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(UnmanagedCallersOnlyDeclaration(Name)))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(parameters)))
            .WithExpressionBody(ArrowExpressionClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
