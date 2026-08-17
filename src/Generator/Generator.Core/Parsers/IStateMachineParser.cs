using System;
using Generator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Parsers
{
    internal interface IStateMachineParser
    {
        bool TryParse(
            ClassDeclarationSyntax classDeclaration,
            out StateMachineModel? model,
            Action<string>? report = null);
    }
}