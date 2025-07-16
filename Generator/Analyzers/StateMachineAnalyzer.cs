#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Generator.Helpers;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;
using Generator.Rules.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Generator.Strings;

namespace Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateMachineAnalyzer : DiagnosticAnalyzer
{


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        DefinedRules.All
            .Select(ruleDef => DiagnosticFactory.Get(ruleDef.Id!))
            .ToImmutableArray();

    // Rule instances
    private static readonly MissingStateMachineAttributeRule _missingStateMachineAttributeRule = new();
    private static readonly InvalidTypesInAttributeRule _invalidTypesInAttributeRule = new();
    private static readonly DuplicateTransitionRule _duplicateTransitionRule = new();

    public override void Initialize(AnalysisContext context)
    {
        // Analyzer config
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeStateMachineClass, SymbolKind.NamedType);
    }

    private static void AnalyzeStateMachineClass(SymbolAnalysisContext symbolContext)
    {
        var namedTypeSymbol = (INamedTypeSymbol)symbolContext.Symbol;
        Location classLocation = namedTypeSymbol.Locations.FirstOrDefault() ?? Location.None;

        // Check if class has [StateMachine] attribute
        var fsmAttribute = namedTypeSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == StateMachineAttributeName);

        bool isPartial = namedTypeSymbol.DeclaringSyntaxReferences
            .Select(sr => sr.GetSyntax(symbolContext.CancellationToken))
            .OfType<ClassDeclarationSyntax>()
            .Any(cds => cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

        var missingAttrCtx = new MissingStateMachineAttributeValidationContext(
            fsmAttribute != null,
            fsmAttribute?.ConstructorArguments.Length ?? 0,
            namedTypeSymbol.Name,
            isPartial);
        ProcessRuleResults(_missingStateMachineAttributeRule.Validate(missingAttrCtx), classLocation, symbolContext);

        // Only continue if attribute is present, class is partial and has enough constructor args
        if (fsmAttribute != null && isPartial && fsmAttribute.ConstructorArguments.Length >= 2)
        {
            var stateTypeArgSymbol = fsmAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
            var triggerTypeArgSymbol = fsmAttribute.ConstructorArguments[1].Value as INamedTypeSymbol;

            var attributeTypeValidationCtx = new AttributeTypeValidationContext(
                stateTypeArgSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                stateTypeArgSymbol?.TypeKind == TypeKind.Enum,
                triggerTypeArgSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                triggerTypeArgSymbol?.TypeKind == TypeKind.Enum);
            ProcessRuleResults(_invalidTypesInAttributeRule.Validate(attributeTypeValidationCtx), classLocation, symbolContext);

            // Check for duplicate transitions only if state/trigger are enums
            if (stateTypeArgSymbol?.TypeKind == TypeKind.Enum && triggerTypeArgSymbol?.TypeKind == TypeKind.Enum)
            {
                ScanForDuplicateTransitions(namedTypeSymbol, symbolContext);
            }
        }
    }

    private static void ScanForDuplicateTransitions(INamedTypeSymbol namedTypeSymbol, SymbolAnalysisContext symbolContext)
    {
        // Checks for duplicate [Transition] and [InternalTransition] attributes
        var processedTransitionsForDuplicates = new HashSet<TransitionDefinition>();
        foreach (var methodSymbol in namedTypeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            ScanMemberForTransitionDuplicates(methodSymbol, namedTypeSymbol, processedTransitionsForDuplicates, symbolContext);
        }
    }

    private static void ScanMemberForTransitionDuplicates(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol containingType,
        HashSet<TransitionDefinition> processedTransitions,
        SymbolAnalysisContext symbolContext)
    {
        // Check each method for transition attributes
        var transitionAttributes = methodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == TransitionAttributeName ||
                       a.AttributeClass?.ToDisplayString() == InternalTransitionAttributeName);

        foreach (var attrData in transitionAttributes)
        {
            Location attrLocation = attrData.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken)?.GetLocation()
                ?? methodSymbol.Locations.FirstOrDefault()
                ?? containingType.Locations[0];

            // Read attribute constructor args (uses ToString() - limited)
            if (attrData.ConstructorArguments.Length >= 2)
            {
                var fromStateConst = attrData.ConstructorArguments[0];
                var triggerConst = attrData.ConstructorArguments[1];
                string? fromStateStr = fromStateConst.Value?.ToString();
                string? triggerStr = triggerConst.Value?.ToString();

                if (fromStateStr != null && triggerStr != null)
                {
                    var currentTransitionDef = new TransitionDefinition(fromStateStr, triggerStr, null);
                    var duplicateCheckCtx = new DuplicateTransitionContext(currentTransitionDef, processedTransitions);
                    ProcessRuleResults(_duplicateTransitionRule.Validate(duplicateCheckCtx), attrLocation, symbolContext);
                }
            }
        }
    }

    private static void ProcessRuleResults(
        IEnumerable<ValidationResult> ruleResults,
        Location defaultLocation,
        SymbolAnalysisContext analysisContext)
    {
        foreach (var result in ruleResults)
        {
            if (!result.IsValid && result.RuleId != null)
            {
                if (DiagnosticFactory.TryCreateDiagnostic(result, defaultLocation, out var diagnosticToReport))
                {
                    analysisContext.ReportDiagnostic(diagnosticToReport);
                }
            }
        }
    }
}
