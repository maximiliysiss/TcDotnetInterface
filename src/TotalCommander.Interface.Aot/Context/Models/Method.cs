using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TotalCommander.Interface.Aot.Context.Models;

internal interface IMethod
{
    MethodDeclarationSyntax Create();
}
