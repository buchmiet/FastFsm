using Generator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

/// <summary>
/// Represents the result of processing a state machine candidate through the pipeline.
/// Following Roslyn incremental generator best practices to avoid silent drops.
/// See: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
/// </summary>
internal readonly struct CandidateResult
{
    public ClassDeclarationSyntax ClassDeclaration { get; }
    public INamedTypeSymbol Symbol { get; }
    public StateMachineModel? Model { get; }
    public string? SkipReason { get; }

    public CandidateResult(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol symbol, StateMachineModel? model, string? skipReason)
    {
        ClassDeclaration = classDeclaration;
        Symbol = symbol;
        Model = model;
        SkipReason = skipReason;
    }

    public bool IsValid => SkipReason is null;
    public bool HasModel => Model is not null;
    
    public static CandidateResult Valid(ClassDeclarationSyntax classDecl, INamedTypeSymbol s, StateMachineModel m) 
        => new CandidateResult(classDecl, s, m, null);
    
    public static CandidateResult Skipped(ClassDeclarationSyntax classDecl, INamedTypeSymbol s, string reason) 
        => new CandidateResult(classDecl, s, null, reason);
}