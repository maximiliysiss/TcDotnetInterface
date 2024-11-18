using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Context.Models;
using TotalCommander.Interface.Aot.Context.Plugins.Bridge;
using TotalCommander.Interface.Aot.Context.Plugins.FileSystem.Bridge;
using TotalCommander.Interface.Aot.Context.Plugins.Infrastructure;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static TotalCommander.Interface.Aot.Context.Plugins.Shared.SyntaxFactory;
using static TotalCommander.Interface.Aot.Context.Plugins.Infrastructure.Constants;

namespace TotalCommander.Interface.Aot.Context.Plugins.FileSystem.Methods;

internal sealed class FindFirstMethod(string name, bool isUnicode) : IMethod
{
    public MethodDeclarationSyntax Create()
    {
        const string pathVariable = "path";
        const string findFileVariable = "findFile";

        var parameters = new SyntaxNodeOrToken[]
        {
            Parameter(Identifier(pathVariable)).WithType(IdentifierName(IntPtr)),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier(findFileVariable)).WithType(IdentifierName(IntPtr))
        };

        var bridgeArguments = new SyntaxNodeOrToken[]
        {
            Argument(MarshalingIntoString(pathVariable)),
            Token(SyntaxKind.CommaToken),
            Argument(IdentifierName(findFileVariable)),
            Token(SyntaxKind.CommaToken),
            Argument(LiteralExpression(isUnicode.AsLiteral()))
        };

        return MethodDeclaration(IdentifierName(IntPtr), Identifier(name))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(UnmanagedCallersOnlyDeclaration(name)))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(parameters)))
            .WithExpressionBody(
                ArrowExpressionClause(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(BridgeApi.Name),
                                IdentifierName(FilesystemBridgeApi.FindFirst)))
                        .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(bridgeArguments)))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
