using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TotalCommander.Interface.Aot.Generator.Models;

internal interface IMethod
{
    MethodDeclarationSyntax Create();
}
