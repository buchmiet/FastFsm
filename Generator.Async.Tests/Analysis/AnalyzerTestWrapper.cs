using System.Collections.Immutable;
using Generator.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using Generator.Helpers;

namespace Generator.Async.Tests.Analysis;

public class AnalyzerTestWrapper : DiagnosticAnalyzer // <-- Musi być public
{
    // Callback ustawiany przez test przed uruchomieniem
    public Action<AsyncSignatureInfo>? OnResultCallback { get; set; }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray<DiagnosticDescriptor>.Empty;

    // Publiczny, bezparametrowy konstruktor jest wymagany przez framework
    public AnalyzerTestWrapper() { }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol { Name: "MethodToTest" } methodSymbol)
        {
            return;
        }

        var typeHelper = new TypeSystemHelper();
        var analyzer = new AsyncSignatureAnalyzer(typeHelper);
        var result = analyzer.Analyze(methodSymbol,context.Compilation);

        // Wywołaj callback, jeśli został ustawiony
        OnResultCallback?.Invoke(result);
    }
}