using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TotalCommander.Interface.Aot.Generator.Models;
using TotalCommander.Interface.Aot.Plugins.Bridge;
using TotalCommander.Interface.Aot.Plugins.FileSystem.Bridge;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static TotalCommander.Interface.Aot.Plugins.Shared.SyntaxFactory;
using static TotalCommander.Interface.Aot.Plugins.Infrastructure.TypeNames;

namespace TotalCommander.Interface.Aot.Plugins.FileSystem.Methods;

internal sealed class FindClose : IMethod
{
    private const string Name = "FsFindClose";

    public MethodDeclarationSyntax Create()
    {
        const string hdlVariable = "hdl";

        return MethodDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier(Name))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(UnmanagedCallersOnlyDeclaration(Name)))))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SingletonSeparatedList(Parameter(Identifier(hdlVariable)).WithType(IdentifierName(IntPtr)))))
            .WithExpressionBody(
                ArrowExpressionClause(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(BridgeApi.Name),
                                IdentifierName(FilesystemBridgeApi.FindClose)))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName(hdlVariable)))))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
