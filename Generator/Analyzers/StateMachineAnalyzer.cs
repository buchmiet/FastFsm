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

/// <summary>
/// Roslyn analyzer for validating state machine definitions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateMachineAnalyzer : DiagnosticAnalyzer
{


    /// <summary>
    /// Gets the diagnostics supported by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            ..DefinedRules.All
                .Select(ruleDef => DiagnosticFactory.Get(ruleDef.Id!))
        ];

    // Rule instances
    private static readonly MissingStateMachineAttributeRule _missingStateMachineAttributeRule = new();
    private static readonly InvalidTypesInAttributeRule _invalidTypesInAttributeRule = new();
    private static readonly DuplicateTransitionRule _duplicateTransitionRule = new();

    /// <summary>
    /// Initializes the analyzer.
    /// </summary>
    /// <param name="context">The analysis context.</param>
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

        // Robust match for [StateMachine] attribute:
        // - accept fully-qualified name (StateMachineAttributeName)
        // - accept short metadata name "StateMachineAttribute"
        // - tolerate "Attribute" suffix conventions
        static bool MatchesAttr(ITypeSymbol? cls, string? expectedFull, string expectedShort)
        {
            if (cls is null) return false;
            // Full name (error-message format is more stable across symbol kinds)
            var full = cls.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (!string.IsNullOrEmpty(expectedFull) && full == expectedFull) return true;
            // Short names
            var name = cls.Name; // e.g., "StateMachineAttribute"
            if (name == expectedShort || name == expectedShort + "Attribute") return true;
            return false;
        }
        var fsmAttribute = namedTypeSymbol.GetAttributes()
            .FirstOrDefault(attr =>
                MatchesAttr(attr.AttributeClass, StateMachineAttributeName, "StateMachine"));

        // Narrow FSM004 candidates: classes that actually look like FSMs
        // Accept both fully-qualified and short names for related attributes.
        static bool IsRelatedAttr(ITypeSymbol? cls, string? full, string shortName)
            => MatchesAttr(cls, full, shortName);
        bool ClassHasRelatedAttrs() =>
            namedTypeSymbol.GetAttributes().Any(a =>
                IsRelatedAttr(a.AttributeClass, StateAttributeFullName, "State") ||
                IsRelatedAttr(a.AttributeClass, PayloadTypeAttributeFullName, "PayloadType"));
        bool MembersHaveRelatedAttrs() =>
            namedTypeSymbol.GetMembers().OfType<IMethodSymbol>().Any(m =>
                m.GetAttributes().Any(a =>
                    IsRelatedAttr(a.AttributeClass, /* full may be null/unknown */ null, "Transition") ||
                    IsRelatedAttr(a.AttributeClass, /* full may be null/unknown */ null, "InternalTransition")));
        bool hasFsmRelatedAttributes = ClassHasRelatedAttrs() || MembersHaveRelatedAttrs();

        // Additional machine-like heuristics: inherits StateMachineBase<,> / AsyncStateMachineBase<,>
        // or implements IStateMachine*/IExtensibleStateMachine* from StateMachine.Contracts
        bool IsMachineLikeByInheritance()
        {
            static bool IsRuntimeBase(INamedTypeSymbol t)
            {
                var name = t.ConstructedFrom?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           ?? t.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                return name is "StateMachine.Runtime.StateMachineBase<TState, TTrigger>"
                             or "StateMachine.Runtime.StateMachineBase`2"
                             or "StateMachine.Runtime.AsyncStateMachineBase<TState, TTrigger>"
                             or "StateMachine.Runtime.AsyncStateMachineBase`2";
            }
            for (var bt = namedTypeSymbol.BaseType; bt != null; bt = bt.BaseType)
            {
                if (IsRuntimeBase(bt)) return true;
            }
            return false;
        }

        bool IsMachineLikeByInterfaces()
        {
            foreach (var itf in namedTypeSymbol.AllInterfaces)
            {
                var n = itf.ConstructedFrom?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                        ?? itf.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                if (n is "StateMachine.Contracts.IStateMachineSync<TState, TTrigger>"
                       or "StateMachine.Contracts.IStateMachineSync`2"
                       or "StateMachine.Contracts.IStateMachineAsync<TState, TTrigger>"
                       or "StateMachine.Contracts.IStateMachineAsync`2"
                       or "StateMachine.Contracts.IExtensibleStateMachineSync<TState, TTrigger>"
                       or "StateMachine.Contracts.IExtensibleStateMachineSync`2"
                       or "StateMachine.Contracts.IExtensibleStateMachineAsync<TState, TTrigger>"
                       or "StateMachine.Contracts.IExtensibleStateMachineAsync`2")
                    return true;
            }
            return false;
        }

        bool isClass = namedTypeSymbol.TypeKind == TypeKind.Class;
        
        // Check if this is a framework base class that should be excluded
        bool isFrameworkBaseClass = namedTypeSymbol.Name == "StateMachineBase" || 
                                   namedTypeSymbol.Name == "AsyncStateMachineBase" ||
                                   (namedTypeSymbol.ContainingNamespace?.ToDisplayString() == "StateMachine.Runtime");
        
        bool machineLike = hasFsmRelatedAttributes || IsMachineLikeByInheritance() || IsMachineLikeByInterfaces();

        bool isPartial = namedTypeSymbol.DeclaringSyntaxReferences
            .Select(sr => sr.GetSyntax(symbolContext.CancellationToken))
            .OfType<ClassDeclarationSyntax>()
            .Any(cds => cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

        // Emit FSM004 only for real machine-like classes that truly miss the attribute
        if (isClass && machineLike && fsmAttribute == null && !isFrameworkBaseClass)
        {
            var missingAttrCtx = new MissingStateMachineAttributeValidationContext(
                false,
                0,
                namedTypeSymbol.Name,
                isPartial);
            ProcessRuleResults(_missingStateMachineAttributeRule.Validate(missingAttrCtx), classLocation, symbolContext);
        }

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
