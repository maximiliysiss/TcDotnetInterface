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

internal sealed class FindNextMethod(string name, bool isUnicode) : IMethod
{
    public MethodDeclarationSyntax Create()
    {
        const string hdlVariable = "hdl";
        const string findFileVariable = "findFile";

        var parameters = new SyntaxNodeOrToken[]
        {
            Parameter(Identifier(hdlVariable))
                .WithType(IdentifierName(IntPtr)),
            Token(SyntaxKind.CommaToken),
            Parameter(Identifier(findFileVariable))
                .WithType(IdentifierName(IntPtr))
        };

        var arguments = new SyntaxNodeOrToken[]
        {
            Argument(IdentifierName(hdlVariable)),
            Token(SyntaxKind.CommaToken),
            Argument(IdentifierName(findFileVariable)),
            Token(SyntaxKind.CommaToken),
            Argument(LiteralExpression(isUnicode.AsLiteral()))
        };

        return MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), Identifier(name))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(UnmanagedCallersOnlyDeclaration(name)))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(parameters)))
            .WithExpressionBody(
                ArrowExpressionClause(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(BridgeApi.Name),
                                IdentifierName(FilesystemBridgeApi.FindNext)))
                        .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(arguments)))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .NormalizeWhitespace();
    }
}
