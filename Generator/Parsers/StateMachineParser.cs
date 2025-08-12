using System;
using Generator.Infrastructure;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;
using Generator.Rules.Rules;
using Generator.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Abstractions.Attributes;
using Generator.Model;
using static Generator.Strings;
using static Microsoft.CodeAnalysis.SpecialType;
using Generator.Helpers;

namespace Generator.Parsers;

public class StateMachineParser(Compilation compilation, SourceProductionContext context)
{
    private readonly InvalidMethodSignatureRule _invalidMethodSignatureRule = new();
    private readonly InvalidTypesInAttributeRule _invalidTypesInAttributeRule = new();
    private readonly InvalidEnumValueInTransitionRule _invalidEnumValueRule = new();
    private readonly DuplicateTransitionRule _duplicateTransitionRule = new();
    private readonly MissingStateMachineAttributeRule _missingStateMachineAttributeRule = new();
    private readonly UnreachableStateRule _unreachableStateRule = new();
    private readonly GuardWithPayloadInNonPayloadMachineRule _guardWithPayloadRule = new();
    private readonly MissingPayloadTypeRule _missingPayloadTypeRule = new();
    private readonly ConflictingPayloadRule _conflictingPayloadRule = new();
    private readonly InvalidVariantConfigRule _invalidVariantConfigRule = new();
    private readonly InvalidGuardTaskReturnTypeRule _invalidGuardTaskReturnTypeRule = new();
    private readonly AsyncCallbackInSyncMachineRule _asyncCallbackInSyncMachineRule = new();
    private readonly InvalidAsyncVoidRule _invalidAsyncVoidRule = new();
    private readonly CircularHierarchyRule _circularHierarchyRule = new();
    private readonly OrphanSubstateRule _orphanSubstateRule = new();
    private readonly InvalidHierarchyConfigurationRule _invalidHierarchyConfigRule = new();
    private readonly MultipleInitialSubstatesRule _multipleInitialSubstatesRule = new();
    private readonly InvalidHistoryConfigurationRule _invalidHistoryConfigRule = new();
    private readonly ConflictingTransitionTargetsRule _conflictingTransitionTargetsRule = new();
    private readonly TypeSystemHelper _typeHelper = new();
    private readonly AsyncSignatureAnalyzer _asyncAnalyzer = new(new TypeSystemHelper());
    private readonly HashSet<TransitionDefinition> _processedTransitionsInCurrentFsm = [];
    private readonly MixedModeRule _mixedModeRule = new();

    // Maps our RuleSeverity to Roslyn DiagnosticSeverity locally in the parser
    private static DiagnosticSeverity Map(RuleSeverity s) => s switch
    {
        RuleSeverity.Error   => DiagnosticSeverity.Error,
        RuleSeverity.Warning => DiagnosticSeverity.Warning,
        _                    => DiagnosticSeverity.Info
    };

    // Emit for catalogued rules (present in RuleLookup)
    private static void EmitRule(
        SourceProductionContext context,
        string ruleId,
        Location? location,
        params object?[] args)
    {
        var def = RuleLookup.Get(ruleId);
        var descriptor = new DiagnosticDescriptor(
            id: def.Id,
            title: def.Title,
            messageFormat: def.MessageFormat,
            category: def.Category,
            defaultSeverity: Map(def.DefaultSeverity),
            isEnabledByDefault: def.IsEnabledByDefault,
            description: def.Description
        );
        var diag = Diagnostic.Create(descriptor, location, args ?? System.Array.Empty<object?>());
        context.ReportDiagnostic(diag);
    }

    // Emit for legacy, ad-hoc IDs that are NOT in RuleLookup (behavior 1:1)
    private static void EmitLegacy(
        SourceProductionContext context,
        string id,
        string title,
        string messageFormat,
        string category,
        RuleSeverity severity,
        Location? location,
        params object?[] args)
    {
        var descriptor = new DiagnosticDescriptor(
            id: id,
            title: title,
            messageFormat: messageFormat,
            category: category,
            defaultSeverity: Map(severity),
            isEnabledByDefault: true,
            description: null
        );
        var diag = Diagnostic.Create(descriptor, location, args ?? System.Array.Empty<object?>());
        context.ReportDiagnostic(diag);
    }

    // Emit for catalogued rules when we already have a preformatted message (no string.Format args).
    private static void EmitRulePreformatted(
        SourceProductionContext context,
        string ruleId,
        Location? location,
        string message,
        RuleSeverity? severityOverride = null)
    {
        var def = RuleLookup.Get(ruleId);
        var descriptor = new DiagnosticDescriptor(
            id: def.Id,
            title: def.Title,
            messageFormat: "{0}", // pass the message as the only argument
            category: def.Category,
            defaultSeverity: Map(severityOverride ?? def.DefaultSeverity),
            isEnabledByDefault: def.IsEnabledByDefault,
            description: def.Description
        );
        var diag = Diagnostic.Create(descriptor, location, message);
        context.ReportDiagnostic(diag);
    }

    private void ProcessRuleResults(IEnumerable<ValidationResult> ruleResults,
        Location defaultLocation,
        ref bool criticalErrorOccurredFlag)
    {
        foreach (var result in ruleResults)
        {
            if (!result.IsValid && result.RuleId != null)
            {
                // Try to get from catalog first
                if (RuleLookup.TryGet(result.RuleId, out var def))
                {
                    // Use the new preformatted helper for catalogued rules
                    EmitRulePreformatted(context, result.RuleId, defaultLocation, 
                        result.Message ?? string.Empty, result.Severity);
                }
                else
                {
                    // Legacy handling for non-catalogued rules
                    // This shouldn't happen if all rules are in catalog, but kept for safety
                    EmitLegacy(context, result.RuleId, result.RuleId, result.Message ?? "", 
                        "FSM.Generator", result.Severity, defaultLocation);
                }

                if (result.Severity == RuleSeverity.Error)
                {
                    criticalErrorOccurredFlag = true;
                }
            }
        }
    }



    public bool TryParse(
     ClassDeclarationSyntax classDeclaration,
     out StateMachineModel? model,
     Action<string>? report = null)
    {
        model = null;
        _processedTransitionsInCurrentFsm.Clear();
        bool criticalErrorOccurred = false;
        bool? isMachineAsyncMode = null;

        report?.Invoke("=== START TryParse ===");
        report?.Invoke($"Parsing class: {classDeclaration.Identifier.Text}");
        
        bool isAsyncOce = classDeclaration.Identifier.Text.Contains("AsyncOceOnEntryMachine");
        if (isAsyncOce)
            report?.Invoke("[DEBUG AOE] Starting parse of AsyncOceOnEntryMachine");

        // === SEKCJA 1: Pobieranie semantic model i class symbol ===
        report?.Invoke("Section 1: Getting semantic model and class symbol");
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            report?.Invoke("ERROR: Failed to get class symbol");
            return false;
        }
        report?.Invoke($"Class symbol obtained: {classSymbol.Name}");
        if (isAsyncOce)
            report?.Invoke("[DEBUG AOE] Got class symbol");

        // === SEKCJA 2: Tworzenie początkowego modelu ===
        report?.Invoke("Section 2: Creating initial model");
        var currentModel = new StateMachineModel
        {
            Namespace = classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name
        };
        // Capture nested containing types (outer classes) to mirror nested partials
        {
            var containers = new List<string>();
            var containerSymbol = classSymbol.ContainingType;
            while (containerSymbol != null)
            {
                containers.Insert(0, containerSymbol.Name);
                containerSymbol = containerSymbol.ContainingType;
            }
            currentModel.ContainerClasses = containers;
        }
        report?.Invoke($"Model created - Namespace: {currentModel.Namespace}, ClassName: {currentModel.ClassName}");

        // === SEKCJA 3: Pobieranie atrybutu StateMachine ===
        report?.Invoke("Section 3: Getting StateMachine attribute");
        var fsmAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);
        report?.Invoke($"StateMachine attribute found: {fsmAttribute != null}");
        if (isAsyncOce)
            report?.Invoke($"[DEBUG AOE] StateMachine attribute found: {fsmAttribute != null}");

        // === SEKCJA 4: Odczyt argumentów z atrybutu [StateMachine] ===
        if (fsmAttribute is not null)
        {
            report?.Invoke("Section 4: Reading StateMachine attribute arguments");

            // Odczyt DefaultPayloadType
            var defaultPayloadArg = fsmAttribute.NamedArguments
                .FirstOrDefault(na => na.Key == nameof(StateMachineAttribute.DefaultPayloadType));
            if (defaultPayloadArg.Key is not null && defaultPayloadArg.Value.Value is INamedTypeSymbol payloadSym)
            {
                currentModel.DefaultPayloadType = _typeHelper.BuildFullTypeName(payloadSym);
                currentModel.GenerationConfig.HasPayload = true;
                report?.Invoke($"DefaultPayloadType set: {currentModel.DefaultPayloadType}");
            }

            // Odczyt GenerateStructuralApi
            var structuralApiArg = fsmAttribute.NamedArguments
                .FirstOrDefault(kv => kv.Key == "GenerateStructuralApi");
            if (structuralApiArg.Key is not null && structuralApiArg.Value.Value is bool flag)
            {
                currentModel.EmitStructuralHelpers = flag;
                report?.Invoke($"GenerateStructuralApi set: {flag}");
            }

            // Odczyt ContinueOnCapturedContext
            var continueContextArg = fsmAttribute.NamedArguments
                .FirstOrDefault(na => na.Key == "ContinueOnCapturedContext");
            if (continueContextArg.Key is not null && continueContextArg.Value.Value is bool continueOnContext)
            {
                currentModel.ContinueOnCapturedContext = continueOnContext;
                report?.Invoke($"ContinueOnCapturedContext set: {continueOnContext}");
            }

            // Odczyt EnableHierarchy (HSM)
            var enableHierarchyArg = fsmAttribute.NamedArguments
                .FirstOrDefault(na => na.Key == "EnableHierarchy");
            if (enableHierarchyArg.Key is not null && enableHierarchyArg.Value.Value is bool enableHierarchy)
            {
                currentModel.HierarchyEnabled = enableHierarchy;
                report?.Invoke($"EnableHierarchy set: {enableHierarchy}");
            }
        }

        // === SEKCJA 5: Podstawowa walidacja atrybutu i klasy ===
        report?.Invoke("Section 5: Basic attribute and class validation");
        Location fsmAttributeLocation = fsmAttribute?.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? classDeclaration.Identifier.GetLocation();
        bool isPartial = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        report?.Invoke($"Class is partial: {isPartial}");

        var missingAttrCtx = new MissingStateMachineAttributeValidationContext(
            fsmAttribute != null,
            fsmAttribute?.ConstructorArguments.Length ?? 0,
            classSymbol.Name,
            isPartial
        );

        report?.Invoke("Validating missing attribute rule");
        ProcessRuleResults(_missingStateMachineAttributeRule.Validate(missingAttrCtx), fsmAttributeLocation, ref criticalErrorOccurred);
        report?.Invoke($"Critical error after missing attribute validation: {criticalErrorOccurred}");

        if (fsmAttribute == null || fsmAttribute.ConstructorArguments.Length < 2 || !isPartial)
        {
            report?.Invoke("ERROR: Invalid attribute or not partial class - returning false");
            return false;
        }

        // === SEKCJA 6: Walidacja typów State i Trigger ===
        report?.Invoke("Section 6: Validating State and Trigger types");
        var stateTypeArg = fsmAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
        var triggerTypeArg = fsmAttribute.ConstructorArguments[1].Value as INamedTypeSymbol;

        report?.Invoke($"State type: {stateTypeArg?.ToDisplayString() ?? "null"}");
        report?.Invoke($"Trigger type: {triggerTypeArg?.ToDisplayString() ?? "null"}");

        var attributeTypeValidationCtx = new AttributeTypeValidationContext(
            stateTypeArg?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            stateTypeArg?.TypeKind == TypeKind.Enum,
            triggerTypeArg?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            triggerTypeArg?.TypeKind == TypeKind.Enum
        );

        report?.Invoke("Validating attribute types");
        ProcessRuleResults(_invalidTypesInAttributeRule.Validate(attributeTypeValidationCtx), fsmAttributeLocation, ref criticalErrorOccurred);
        report?.Invoke($"Critical error after type validation: {criticalErrorOccurred}");

        if (stateTypeArg is not { TypeKind: TypeKind.Enum } || triggerTypeArg is not { TypeKind: TypeKind.Enum })
        {
            report?.Invoke("ERROR: State or Trigger type is not enum - returning false");
            return false;
        }

        // === SEKCJA 7: Budowanie podstawowego modelu ===
        report?.Invoke("Section 7: Building basic model");
        currentModel.StateType = _typeHelper.BuildFullTypeName(stateTypeArg);
        currentModel.TriggerType = _typeHelper.BuildFullTypeName(triggerTypeArg);
        report?.Invoke($"StateType: {currentModel.StateType}");
        report?.Invoke($"TriggerType: {currentModel.TriggerType}");

        report?.Invoke("Enumerating states from enum");
        int stateCount = 0;
        foreach (var member in stateTypeArg.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst)
            {
                if (!currentModel.States.ContainsKey(member.Name))
                {
                    currentModel.States[member.Name] = new StateModel { Name = member.Name };
                    stateCount++;
                }
            }
        }
        report?.Invoke($"Found {stateCount} states");

        // === SEKCJA 8: Parsowanie atrybutów z memberów klasy ===
        report?.Invoke("Section 8: Parsing member attributes");
        report?.Invoke("Calling ParseMemberAttributes");
        if (isAsyncOce)
            report?.Invoke("[DEBUG AOE] Before ParseMemberAttributes");
        ParseMemberAttributes(classSymbol, currentModel, stateTypeArg, triggerTypeArg, ref criticalErrorOccurred, ref isMachineAsyncMode, report);
        report?.Invoke($"ParseMemberAttributes completed. Critical error: {criticalErrorOccurred}, IsAsync: {isMachineAsyncMode}");
        if (isAsyncOce)
            report?.Invoke($"[DEBUG AOE] After ParseMemberAttributes - criticalError: {criticalErrorOccurred}, isAsync: {isMachineAsyncMode}");

        // === SEKCJA 8.4: Enum-only fallback ===
        // If no [State] attributes were found, ensure all enum members are in the model
        bool hasStateAttributes = currentModel.States.Values.Any(s => 
            !string.IsNullOrEmpty(s.OnEntryMethod) || 
            !string.IsNullOrEmpty(s.OnExitMethod) ||
            s.ParentState != null ||
            s.History != Generator.Model.HistoryMode.None ||
            s.IsInitial);
            
        if (!hasStateAttributes)
        {
            report?.Invoke("Section 8.4: Enum-only fallback - no [State] attributes found");
            
            // Rebuild states from enum to ensure they're all present
            currentModel.States.Clear();
            foreach (var member in stateTypeArg.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.IsConst)
                {
                    currentModel.States[member.Name] = new StateModel 
                    { 
                        Name = member.Name,
                        OnEntryMethod = null,
                        OnExitMethod = null,
                        ParentState = null,
                        History = Generator.Model.HistoryMode.None,
                        IsInitial = false
                    };
                }
            }
            
            currentModel.UsedEnumOnlyFallback = true;
            report?.Invoke($"Enum-only fallback applied: {currentModel.States.Count} states from enum");
            
            // Report FSM994 diagnostic
            EmitLegacy(context, "FSM994", "Enum-only states fallback",
                "Enum-only states fallback applied for '{0}' — 0 [State] attributes found; using all enum members as states",
                "FSM.Generator", RuleSeverity.Info, classDeclaration.GetLocation(),
                string.IsNullOrEmpty(currentModel.Namespace) ? currentModel.ClassName : $"{currentModel.Namespace}.{currentModel.ClassName}");
        }

        // === SEKCJA 8.5: Build HSM hierarchy if needed ===
        report?.Invoke("Section 8.5: Building HSM hierarchy");
        BuildHierarchy(currentModel, ref criticalErrorOccurred, report);
        report?.Invoke($"Hierarchy built. HierarchyEnabled: {currentModel.HierarchyEnabled}");

        // === SEKCJA 9: Określenie wariantu generacji ===
        report?.Invoke("Section 9: Determining generation variant");
        var variantSelector = new VariantSelector();
        report?.Invoke("Calling DetermineVariant");
        variantSelector.DetermineVariant(currentModel, classSymbol);
        report?.Invoke($"Variant determined: {currentModel.GenerationConfig.Variant}");
        
        // FSM990_HSM_FLAG: Log after variant selection
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "FSM990_HSM_FLAG",
                "HSM Flag Tracking",
                "[2-AfterVariant] {0}: HierarchyEnabled={1}, Variant={2}",
                "FSM.Generator",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true),
            classDeclaration.GetLocation(),
            currentModel.ClassName,
            currentModel.HierarchyEnabled,
            currentModel.GenerationConfig.Variant));

        // === SEKCJA 10: Walidacje po ustaleniu wariantu ===
        report?.Invoke("Section 10: Post-variant validations");
        
        // Validate FSM007-009: Payload configuration with forced variants
        if (currentModel.GenerationConfig.IsForced)
        {
            report?.Invoke($"Validating forced variant: {currentModel.GenerationConfig.Variant}");
            
            // FSM007: MissingPayloadType - forced payload variant needs payload type
            var missingPayloadCtx = new MissingPayloadTypeContext(
                variant: currentModel.GenerationConfig.Variant.ToString(),
                hasDefaultPayloadType: !string.IsNullOrEmpty(currentModel.DefaultPayloadType),
                hasTriggerPayloadTypes: currentModel.TriggerPayloadTypes.Any(),
                isForced: true);
            
            var missingPayloadResults = _missingPayloadTypeRule.Validate(missingPayloadCtx);
            ProcessRuleResults(missingPayloadResults, classDeclaration.GetLocation(), ref criticalErrorOccurred);
            
            // FSM008: ConflictingPayloadConfiguration - WithPayload variant can't have trigger-specific payloads
            if (currentModel.GenerationConfig.Variant == GenerationVariant.WithPayload)
            {
                var conflictingPayloadCtx = new ConflictingPayloadContext(
                    isWithPayloadVariant: true,
                    triggerSpecificPayloadCount: currentModel.TriggerPayloadTypes.Count);
                
                var conflictingPayloadResults = _conflictingPayloadRule.Validate(conflictingPayloadCtx);
                ProcessRuleResults(conflictingPayloadResults, classDeclaration.GetLocation(), ref criticalErrorOccurred);
            }
            
            // FSM009: InvalidForcedVariantConfiguration - forced variant conflicts with actual usage
            string? conflictType = null;
            bool hasConflict = false;
            
            if (currentModel.GenerationConfig.Variant == GenerationVariant.Pure)
            {
                // Pure variant can't have callbacks, payloads, or extensions
                if (currentModel.GenerationConfig.HasOnEntryExit)
                {
                    conflictType = "OnEntryExit";
                    hasConflict = true;
                }
                else if (currentModel.GenerationConfig.HasPayload || currentModel.TriggerPayloadTypes.Any())
                {
                    conflictType = "PayloadTypes";
                    hasConflict = true;
                }
                else if (currentModel.GenerationConfig.HasExtensions)
                {
                    conflictType = "Extensions";
                    hasConflict = true;
                }
            }
            else if (currentModel.GenerationConfig.Variant == GenerationVariant.Basic)
            {
                // Basic variant can have callbacks but not payloads or extensions
                if (currentModel.GenerationConfig.HasPayload || currentModel.TriggerPayloadTypes.Any())
                {
                    conflictType = "PayloadTypes";
                    hasConflict = true;
                }
                else if (currentModel.GenerationConfig.HasExtensions)
                {
                    conflictType = "Extensions";
                    hasConflict = true;
                }
            }
            else if (currentModel.GenerationConfig.Variant == GenerationVariant.WithPayload)
            {
                // WithPayload variant can't have extensions
                if (currentModel.GenerationConfig.HasExtensions)
                {
                    conflictType = "Extensions";
                    hasConflict = true;
                }
            }
            else if (currentModel.GenerationConfig.Variant == GenerationVariant.WithExtensions)
            {
                // WithExtensions variant can't have payloads
                if (currentModel.GenerationConfig.HasPayload || currentModel.TriggerPayloadTypes.Any())
                {
                    conflictType = "PayloadTypes";
                    hasConflict = true;
                }
            }
            // Full variant can have everything, so no conflicts
            
            if (hasConflict)
            {
                var invalidVariantCtx = new InvalidVariantConfigContext(
                    variantName: currentModel.GenerationConfig.Variant.ToString(),
                    conflictType: conflictType!,
                    hasConflict: true);
                
                var invalidVariantResults = _invalidVariantConfigRule.Validate(invalidVariantCtx);
                ProcessRuleResults(invalidVariantResults, classDeclaration.GetLocation(), ref criticalErrorOccurred);
            }
        }

        if (criticalErrorOccurred)
        {
            report?.Invoke("ERROR: Critical error occurred during validations - returning false");
            return false;
        }

        // === SEKCJA 11: Walidacja osiągalności stanów ===
        report?.Invoke("Section 11: Validating state reachability");
        var allStateNames = currentModel.States.Keys.ToList();
        report?.Invoke($"Total states count: {allStateNames.Count}");

        // Calculate transition metrics
        int internalCount = currentModel.Transitions.Count(t => t.IsInternal);
        int externalCount = currentModel.Transitions.Count(t => !t.IsInternal);
        int totalCount = internalCount + externalCount;
        bool isInternalOnly = internalCount > 0 && externalCount == 0;
        bool hasAnyTransitions = totalCount > 0;
        
        report?.Invoke($"Transition metrics: internal={internalCount}, external={externalCount}, total={totalCount}");

        var transitionsForReachability = currentModel.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ToState))
            .Select(t => new TransitionDefinition(t.FromState, t.Trigger, t.ToState!))
            .ToList();
        
        // Report diagnostic for internal-only or no-transitions machines
        if (externalCount == 0 && internalCount > 0)
        {
            report?.Invoke($"Internal-only machine detected: {internalCount} internal transitions");
            EmitLegacy(context, "FSM982", "Internal-only machine",
                "Machine '{0}' has only internal transitions ({1}) - generating with internal-only support",
                "FSM.Generator.Parser", RuleSeverity.Info, classDeclaration.GetLocation(),
                currentModel.ClassName, internalCount);
        }
        else if (totalCount == 0)
        {
            report?.Invoke("No-transitions machine detected");
            EmitLegacy(context, "FSM981", "No transitions",
                "Machine '{0}' has no transitions - generating minimal API skeleton",
                "FSM.Generator.Parser", RuleSeverity.Info, classDeclaration.GetLocation(),
                currentModel.ClassName);
        }
        
        // Skip reachability validation for internal-only or no-transitions machines
        if (isInternalOnly || totalCount == 0)
        {
            report?.Invoke($"Skipping reachability check for internal-only or no-transitions machine");
            // Don't run unreachable state validation for these cases
        }
        else if (externalCount > 0)
        {
            // Only validate reachability when there are external transitions
            string initialStateForReachability = allStateNames.FirstOrDefault() ?? string.Empty;
            report?.Invoke($"Initial state: {initialStateForReachability}");

            var unreachableCtx = new UnreachableStateContext(initialStateForReachability, allStateNames, transitionsForReachability);
            report?.Invoke("Validating unreachable states");
            ProcessRuleResults(_unreachableStateRule.Validate(unreachableCtx), classDeclaration.Identifier.GetLocation(), ref criticalErrorOccurred);
            report?.Invoke($"Critical error after reachability validation: {criticalErrorOccurred}");
        }

        // === SEKCJA 12: Finalizacja ===
        report?.Invoke("Section 12: Finalization");
        if (criticalErrorOccurred)
        {
            report?.Invoke($"ERROR: Critical error occurred for {currentModel.ClassName} - returning false");
            EmitLegacy(context, "FSM999", "Parser critical error",
                $"Critical error occurred while parsing {currentModel.ClassName}, code generation skipped",
                "FSM.Generator", RuleSeverity.Warning, classDeclaration.GetLocation());
            return false;
        }

        currentModel.GenerationConfig.IsAsync = isMachineAsyncMode ?? false;
        report?.Invoke($"Final IsAsync: {currentModel.GenerationConfig.IsAsync}");
        
        if (isAsyncOce)
            report?.Invoke($"[DEBUG AOE] Final state - IsAsync: {currentModel.GenerationConfig.IsAsync}, ExceptionHandler: {currentModel.ExceptionHandler != null}");

        // FSM990_HSM_FLAG: Log at parser output
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "FSM990_HSM_FLAG",
                "HSM Flag Tracking",
                "[1-Parser] {0}: HierarchyEnabled={1}, UsedEnumOnlyFallback={2}, HasPayload={3}, Variant={4}",
                "FSM.Generator",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true),
            classDeclaration.GetLocation(),
            currentModel.ClassName,
            currentModel.HierarchyEnabled,
            currentModel.UsedEnumOnlyFallback,
            currentModel.GenerationConfig.HasPayload,
            currentModel.GenerationConfig.Variant));
        
        model = currentModel;
        report?.Invoke("=== SUCCESS: TryParse completed successfully ===");
        return true;
    }


    private void ParseMemberAttributes(INamedTypeSymbol classSymbol, StateMachineModel model,
        INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol,
        ref bool criticalErrorOccurred,
        ref bool? isMachineAsyncMode,
        Action<string>? report = null)
    {
        // Collect configuration sections for FSM989 diagnostic
        var stateMethodNames = new HashSet<string>();
        var transitionMethodNames = new HashSet<string>();
        var internalMethodNames = new HashSet<string>();
        var payloadTypeCount = 0;
        
        // Scan all methods to collect configuration info
        foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            var hasStateAttrs = method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == StateAttributeFullName);
            var hasTransitionAttrs = method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == TransitionAttributeFullName);
            var hasInternalAttrs = method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == InternalTransitionAttributeFullName);
            
            if (hasStateAttrs) stateMethodNames.Add(method.Name);
            if (hasTransitionAttrs) transitionMethodNames.Add(method.Name);
            if (hasInternalAttrs) internalMethodNames.Add(method.Name);
        }
        
        // Count payload type attributes on class
        payloadTypeCount = classSymbol.GetAttributes()
            .Count(a => a.AttributeClass?.ToDisplayString() == PayloadTypeAttributeFullName);
        
        ParsePayloadTypeAttributes(classSymbol, model, ref criticalErrorOccurred);
        ParseTransitionAttributes(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode);
        ParseInternalTransitionAttributes(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode);
        ParseStateAttributes(classSymbol, model, stateTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode);
        ParseOnExceptionAttribute(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode, report);
        
        // Report FSM989 diagnostic after parsing to get accurate counts
        var externalCount = model.Transitions.Count(t => !t.IsInternal);
        var internalCount = model.Transitions.Count(t => t.IsInternal);
        
        var fullName = string.IsNullOrEmpty(model.Namespace) 
            ? model.ClassName 
            : $"{model.Namespace}.{model.ClassName}";
        
        var stateMethods = string.Join(",", stateMethodNames);
        var transitionMethods = string.Join(",", transitionMethodNames);
        var internalMethods = string.Join(",", internalMethodNames);
        
        if (string.IsNullOrEmpty(stateMethods)) stateMethods = "(none)";
        if (string.IsNullOrEmpty(transitionMethods)) transitionMethods = "(none)";
        if (string.IsNullOrEmpty(internalMethods)) internalMethods = "(none)";
        
        // Report FSM989 diagnostic - use the descriptor directly from Generator.cs
        var location = classSymbol.Locations.FirstOrDefault() ?? Location.None;
        
        // Report FSM989 diagnostic
        EmitLegacy(context, "FSM989", "Configuration sections",
            "{0} - StatesFrom: {1} | TransitionsFrom: {2} (ext={3}) | InternalFrom: {4} (int={5}) | PayloadTypes: {6}",
            "FSM.Generator.Parser", RuleSeverity.Info, location,
            fullName, stateMethods, transitionMethods, externalCount,
            internalMethods, internalCount, payloadTypeCount);
    }


    private void ParseTransitionAttributes(INamedTypeSymbol classSymbolContainingMethods, StateMachineModel model,
                                       INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol localTriggerTypeSymbol,
                                       ref bool criticalErrorOccurred,
                                       ref bool? isMachineAsyncMode)
    {
        foreach (var methodSymbol in classSymbolContainingMethods.GetMembers().OfType<IMethodSymbol>())
        {
            var transitionAttributesData = methodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == TransitionAttributeFullName);

            foreach (var attrData in transitionAttributesData)
            {
                Location attrLocation = attrData.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? methodSymbol.Locations.FirstOrDefault() ?? Location.None;

                if (attrData.ConstructorArguments.Length < 3)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                var fromState = GetEnumMemberName(attrData.ConstructorArguments[0], stateTypeSymbol, attrData, ref criticalErrorOccurred);
                var trigger = GetEnumMemberName(attrData.ConstructorArguments[1], localTriggerTypeSymbol, attrData, ref criticalErrorOccurred);
                var toState = GetEnumMemberName(attrData.ConstructorArguments[2], stateTypeSymbol, attrData, ref criticalErrorOccurred);

                if (fromState == null || trigger == null || toState == null)
                {
                    // GetEnumMemberName już zgłosił błąd i ustawił criticalErrorOccurred
                    continue;
                }

                var currentTransitionDef = new TransitionDefinition(fromState, trigger, toState);
                var duplicateCheckCtx = new DuplicateTransitionContext(currentTransitionDef, _processedTransitionsInCurrentFsm);

                var validationResults = _duplicateTransitionRule.Validate(duplicateCheckCtx).ToList();
                ProcessRuleResults(validationResults, attrLocation, ref criticalErrorOccurred);

                bool isDuplicate = validationResults.Any(r => !r.IsValid && r.RuleId == RuleIdentifiers.DuplicateTransition);

                var transition = new TransitionModel { FromState = fromState, Trigger = trigger, ToState = toState };

                if (model.TriggerPayloadTypes.TryGetValue(trigger, out var triggerPayloadType))
                {
                    transition.ExpectedPayloadType = triggerPayloadType;
                }
                else if (model.DefaultPayloadType != null)
                {
                    transition.ExpectedPayloadType = model.DefaultPayloadType;
                }

                bool guardValid = true;
                bool actionValid = true;

                foreach (var namedArg in attrData.NamedArguments)
                {
                    // Priority (HSM)
                    if (namedArg is { Key: "Priority", Value.Value: int priorityValue })
                    {
                        transition.Priority = priorityValue;
                    }
                    
                    if (namedArg is { Key: GuardCallbackType, Value.Value: string guardMethodName })
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, guardMethodName, GuardCallbackType,
                            attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                            out bool guardIsAsync, out var guardExpectsPayload,
                            model.GenerationConfig.HasPayload,
                            out var guardMethodSymbol,
                            transition.ExpectedPayloadType))
                        {
                            guardValid = false;
                        }

                        if (guardValid)
                        {
                            transition.GuardMethod = guardMethodName;
                            transition.GuardIsAsync = guardIsAsync;
                            transition.GuardExpectsPayload = guardExpectsPayload;

                            var parameterlessGuard = classSymbolContainingMethods
                                .GetMembers(guardMethodName).OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);
                            transition.GuardHasParameterlessOverload = parameterlessGuard != null;

                            // Analyze full signature using selected method symbol
                            if (guardMethodSymbol != null)
                            {
                                AnalyzeAndSetCallbackSignature(
                                    guardMethodSymbol,
                                    "Guard",
                                    sig => transition.GuardSignature = sig
                                );
                            }
                        }
                    }
                    if (namedArg is { Key: ActionCallbackType, Value.Value: string actionMethodName })
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, actionMethodName, ActionCallbackType,
                            attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                            out bool actionIsAsync, out var actionExpectsPayload,
                            model.GenerationConfig.HasPayload,
                            out var actionMethodSymbol,
                            transition.ExpectedPayloadType))
                        {
                            actionValid = false;
                        }
                        if (actionValid)
                        {
                            transition.ActionMethod = actionMethodName;
                            transition.ActionIsAsync = actionIsAsync;
                            transition.ActionExpectsPayload = actionExpectsPayload;

                            var parameterlessAction = classSymbolContainingMethods
                                .GetMembers(actionMethodName).OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);
                            transition.ActionHasParameterlessOverload = parameterlessAction != null;

                            // Analyze full signature using selected method symbol
                            if (actionMethodSymbol != null)
                            {
                                AnalyzeAndSetCallbackSignature(
                                    actionMethodSymbol,
                                    "Action",
                                    sig => transition.ActionSignature = sig
                                );
                            }
                        }
                    }
                }
                if (!isDuplicate && guardValid && actionValid)
                {
                    model.Transitions.Add(transition);
                }
            }
        }
    }


    private void ParseInternalTransitionAttributes(INamedTypeSymbol classSymbolContainingMethods, StateMachineModel model,
                                                INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol localTriggerTypeSymbol,
                                                ref bool criticalErrorOccurred,
                                                ref bool? isMachineAsyncMode)
    {
        foreach (var methodSymbol in classSymbolContainingMethods.GetMembers().OfType<IMethodSymbol>())
        {
            var internalTransitionAttributesData = methodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == InternalTransitionAttributeFullName);

            foreach (var attrData in internalTransitionAttributesData)
            {
                Location attrLocation = attrData.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? methodSymbol.Locations.FirstOrDefault() ?? Location.None;

                // Get action method name from either constructor argument or named parameter
                string? actionMethodNameFromCtor = null;
                
                // First try to get from constructor argument (backward compatibility)
                if (attrData.ConstructorArguments.Length >= 3 && attrData.ConstructorArguments[2].Value is string ctorAction)
                {
                    actionMethodNameFromCtor = ctorAction;
                }
                
                // If not found in constructor, check named arguments
                if (actionMethodNameFromCtor == null)
                {
                    foreach (var namedArg in attrData.NamedArguments)
                    {
                        if (namedArg.Key == "Action" && namedArg.Value.Value is string namedAction)
                        {
                            actionMethodNameFromCtor = namedAction;
                            break;
                        }
                    }
                }
                
                // If still no action method found, it's an error
                if (actionMethodNameFromCtor == null)
                {
                    criticalErrorOccurred = true;
                    EmitLegacy(context, "FSM983", "Missing action method",
                        "InternalTransition attribute requires an Action method name, either as third constructor argument or Action named parameter",
                        "FSM.Generator.Parser", RuleSeverity.Error, attrLocation);
                    continue;
                }

                var state = GetEnumMemberName(attrData.ConstructorArguments[0], stateTypeSymbol, attrData, ref criticalErrorOccurred);
                var trigger = GetEnumMemberName(attrData.ConstructorArguments[1], localTriggerTypeSymbol, attrData, ref criticalErrorOccurred);

                if (state == null || trigger == null)
                {
                    continue;
                }

                var currentTransitionDef = new TransitionDefinition(state, trigger, state); // toState jest takie samo jak fromState
                var duplicateCheckCtx = new DuplicateTransitionContext(currentTransitionDef, _processedTransitionsInCurrentFsm);

                var validationResults = _duplicateTransitionRule.Validate(duplicateCheckCtx).ToList();
                ProcessRuleResults(validationResults, attrLocation, ref criticalErrorOccurred);

                bool isDuplicate = validationResults.Any(r => !r.IsValid && r.RuleId == RuleIdentifiers.DuplicateTransition);

                var transition = new TransitionModel
                {
                    FromState = state,
                    Trigger = trigger,
                    ToState = state,
                    IsInternal = true
                };

                if (model.TriggerPayloadTypes.TryGetValue(trigger, out var triggerPayloadType))
                {
                    transition.ExpectedPayloadType = triggerPayloadType;
                }
                else if (model.DefaultPayloadType != null)
                {
                    transition.ExpectedPayloadType = model.DefaultPayloadType;
                }

                bool actionValid = ValidateCallbackMethodSignature(classSymbolContainingMethods, actionMethodNameFromCtor,
                    ActionCtorCallbackType, attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                    out bool actionIsAsync, out var actionExpectsPayload,
                    model.GenerationConfig.HasPayload,
                    out var actionMethodSymbol,
                    transition.ExpectedPayloadType);

                if (actionValid)
                {
                    transition.ActionMethod = actionMethodNameFromCtor;
                    transition.ActionIsAsync = actionIsAsync;
                    transition.ActionExpectsPayload = actionExpectsPayload;

                    var parameterlessAction = classSymbolContainingMethods
                        .GetMembers(actionMethodNameFromCtor).OfType<IMethodSymbol>()
                        .FirstOrDefault(m => m.Parameters.IsEmpty);
                    transition.ActionHasParameterlessOverload = parameterlessAction != null;

                    // Analyze full signature using selected method symbol
                    if (actionMethodSymbol != null)
                    {
                        AnalyzeAndSetCallbackSignature(
                            actionMethodSymbol,
                            "Action",
                            sig => transition.ActionSignature = sig
                        );
                    }
                }

                bool guardValid = true;
                foreach (var namedArg in attrData.NamedArguments)
                {
                    // Priority (HSM)
                    if (namedArg is { Key: "Priority", Value.Value: int priorityValue })
                    {
                        transition.Priority = priorityValue;
                    }
                    
                    if (namedArg.Key == GuardCallbackType && namedArg.Value.Value is string guardMethodName)
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, guardMethodName, GuardCallbackType,
                            attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                            out bool guardIsAsync, out var guardExpectsPayload,
                            model.GenerationConfig.HasPayload,
                            out var guardMethodSymbol,
                            transition.ExpectedPayloadType))
                        {
                            guardValid = false;
                        }
                        if (guardValid)
                        {
                            transition.GuardMethod = guardMethodName;
                            transition.GuardIsAsync = guardIsAsync;
                            transition.GuardExpectsPayload = guardExpectsPayload;

                            var parameterlessGuard = classSymbolContainingMethods
                                .GetMembers(guardMethodName).OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);
                            transition.GuardHasParameterlessOverload = parameterlessGuard != null;

                            // Analyze full signature using selected method symbol
                            if (guardMethodSymbol != null)
                            {
                                AnalyzeAndSetCallbackSignature(
                                    guardMethodSymbol,
                                    "Guard",
                                    sig => transition.GuardSignature = sig
                                );
                            }
                        }
                    }
                }
                if (!isDuplicate && actionValid && guardValid)
                {
                    model.Transitions.Add(transition);
                }
            }
        }
    }
    private void ParseStateAttributes(
      INamedTypeSymbol classSymbolContainingMethods,
      StateMachineModel model,
      INamedTypeSymbol stateTypeSymbol,
      ref bool criticalErrorOccurred,
      ref bool? isMachineAsyncMode)
    {
        bool isAsyncOce = classSymbolContainingMethods.Name.Contains("AsyncOceOnEntryMachine");
            
        // Potrzebne tylko do walidacji (brak zmian)
        var voidType = compilation.GetSpecialType(System_Void);

        // Przechodzimy po wszystkich metodach klasy
        foreach (var methodSymbol in classSymbolContainingMethods.GetMembers()
                                                                 .OfType<IMethodSymbol>())
        {
            var stateAttributesData = methodSymbol.GetAttributes()
                                                  .Where(a => a.AttributeClass?.ToDisplayString() ==
                                                              StateAttributeFullName);

            foreach (var attrData in stateAttributesData)
            {
                // [State] bez argumentu → błąd krytyczny
                if (attrData.ConstructorArguments.Length < 1)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                // Nazwa stanu z argumentu atrybutu
                var stateName = GetEnumMemberName(attrData.ConstructorArguments[0],
                                                  stateTypeSymbol,
                                                  attrData,
                                                  ref criticalErrorOccurred);
                if (stateName is null)
                    continue;

                // Pobierz lub utwórz definicję stanu w modelu
                if (!model.States.TryGetValue(stateName, out var stateModel))
                {
                    stateModel = new StateModel { Name = stateName };
                    model.States[stateName] = stateModel;
                }

                // Przetwarzamy argumenty nazwane (OnEntry / OnExit)
                foreach (var namedArg in attrData.NamedArguments)
                {
                    // ---------- OnEntry ----------
                    if (namedArg is { Key: OnEntryCallbackType, Value.Value: string onEntryMethodName })
                    {
                        string expectedPayloadType = GetExpectedPayloadForStateCallback(model);

                        if (ValidateCallbackMethodSignature(classSymbolContainingMethods,
                                                            onEntryMethodName,
                                                            OnEntryCallbackType,
                                                            attrData,
                                                            ref criticalErrorOccurred,
                                                            ref isMachineAsyncMode,
                                                            out bool onEntryIsAsync,
                                                            out var onEntryExpectsPayload,
                                                            model.GenerationConfig.HasPayload,
                                                            out var onEntryMethodSymbol,
                                                            expectedPayloadType))
                        {
                            stateModel.OnEntryMethod = onEntryMethodName;
                            stateModel.OnEntryIsAsync = onEntryIsAsync;
                            stateModel.OnEntryExpectsPayload = onEntryExpectsPayload;
                            

                            var parameterlessOverload = classSymbolContainingMethods
                                .GetMembers(onEntryMethodName)
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);

                            stateModel.OnEntryHasParameterlessOverload = parameterlessOverload != null;

                            // Analyze full signature using selected method symbol
                            if (onEntryMethodSymbol != null)
                            {
                                AnalyzeAndSetCallbackSignature(
                                    onEntryMethodSymbol,
                                    "OnEntry",
                                    sig => stateModel.OnEntrySignature = sig
                                );
                            }
                        }
                    }

                    // ---------- OnExit ----------
                    if (namedArg is { Key: OnExitCallbackType, Value.Value: string onExitMethodName })
                    {
                        string expectedPayloadType = GetExpectedPayloadForStateCallback(model);

                        if (ValidateCallbackMethodSignature(classSymbolContainingMethods,
                                                            onExitMethodName,
                                                            OnExitCallbackType,
                                                            attrData,
                                                            ref criticalErrorOccurred,
                                                            ref isMachineAsyncMode,
                                                            out bool onExitIsAsync,
                                                            out var onExitExpectsPayload,
                                                            model.GenerationConfig.HasPayload,
                                                            out var onExitMethodSymbol,
                                                            expectedPayloadType))
                        {
                            stateModel.OnExitMethod = onExitMethodName;
                            stateModel.OnExitIsAsync = onExitIsAsync;
                            stateModel.OnExitExpectsPayload = onExitExpectsPayload;

                            var parameterlessOverload = classSymbolContainingMethods
                                .GetMembers(onExitMethodName)
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);

                            stateModel.OnExitHasParameterlessOverload = parameterlessOverload != null;

                            // Analyze full signature using selected method symbol
                            if (onExitMethodSymbol != null)
                            {
                                AnalyzeAndSetCallbackSignature(
                                    onExitMethodSymbol,
                                    "OnExit",
                                    sig => stateModel.OnExitSignature = sig
                                );
                            }
                        }
                    }

                    // ---------- Parent (HSM) ----------
                    if (namedArg is { Key: "Parent", Value.Value: { } parentValue })
                    {
                        var parentStateName = GetEnumMemberName(
                            namedArg.Value,
                            stateTypeSymbol,
                            attrData,
                            ref criticalErrorOccurred);
                        
                        if (parentStateName != null)
                        {
                            stateModel.ParentState = parentStateName;
                        }
                        else if (parentValue != null)
                        {
                            // Parent był podany ale nie jest valid - to błąd!
                            // GetEnumMemberName już zgłosił błąd FSM002
                            // criticalErrorOccurred jest już ustawione przez GetEnumMemberName
                        }
                    }

                    // ---------- History (HSM) ----------
                    if (namedArg is { Key: "History", Value.Value: { } historyValue })
                    {
                        if (historyValue is int historyInt)
                        {
                            stateModel.History = (Generator.Model.HistoryMode)historyInt;
                        }
                    }

                    // ---------- IsInitial (HSM) ----------
                    if (namedArg is { Key: "IsInitial", Value.Value: bool isInitial })
                    {
                        stateModel.IsInitial = isInitial;
                    }
                }
            }
        }
    }


    private string? GetExpectedPayloadForStateCallback(StateMachineModel model)
    {
        string? expectedPayloadType = model.DefaultPayloadType;
        if (model.GenerationConfig.HasPayload && expectedPayloadType == null)
        {
            // Maszyna multi-payload - OnEntry/OnExit może przyjąć dowolny payload
            expectedPayloadType = "*";
        }
        return expectedPayloadType;
    }

    /// <summary>
    /// Waliduje sygnaturę metody zwrotnej (callback) opisanej w atrybucie FSM.
    /// Zwraca <c>true</c>, gdy nie wykryto błędów krytycznych.
    /// </summary>
    private bool ValidateCallbackMethodSignature(
        INamedTypeSymbol classSymbol,
        string methodName,
        string callbackType,
        AttributeData attributeData,
        ref bool criticalErrorOccurred,
        ref bool? isMachineAsyncMode,
        out bool isAsync,
        out bool expectsPayload,
        bool machineHasPayload,
        out IMethodSymbol? selectedMethod,
        string? expectedPayloadType = null)
    {
        isAsync = false;
        expectsPayload = false;

        // Lokalizacja dla wszystkich diagnostyk zgłaszanych w tej metodzie
        Location loc = attributeData.ApplicationSyntaxReference?
                           .GetSyntax(context.CancellationToken)
                           .GetLocation()
                       ?? Location.None;

        // ---------------------------------------------------------------------
        // 1. Wyszukujemy wszystkie przeciążenia metody w klasie
        // ---------------------------------------------------------------------
        var overloads = classSymbol.GetMembers(methodName)
                                   .OfType<IMethodSymbol>()
                                   .ToList();

        if (!overloads.Any())
        {
            // Metoda nie znaleziona - FSM003 zgłosi błąd
            var notFoundCtx = new MethodSignatureValidationContext(methodName, callbackType, "", false)
            {
                MethodFound = false
            };
            ProcessRuleResults(_invalidMethodSignatureRule.Validate(notFoundCtx), loc, ref criticalErrorOccurred);
            selectedMethod = null;
            return false;
        }

        // ---------------------------------------------------------------------
        // 2. Analiza sygnatur i wybór najlepszego dopasowania
        // ---------------------------------------------------------------------
        IMethodSymbol? matching = null;

        // a) preferuj wariant z 1 parametrem (payload), jeśli jest spodziewany
        if (expectedPayloadType is not null && expectedPayloadType != "*")
        {
            var payloadSymbol = compilation.GetTypeByMetadataName(expectedPayloadType);
            if (payloadSymbol is not null)
            {
                matching = overloads.FirstOrDefault(m =>
                           m.Parameters.Length == 1 &&
                           SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, payloadSymbol));
            }
        }
        else if (expectedPayloadType == "*")
        {
            // Multi-payload - akceptuj dowolny jednoparametrowy wariant
            matching = overloads.FirstOrDefault(m => m.Parameters.Length == 1);
        }

        // b) jeśli nic nie znaleziono, spróbuj bezparametrowego
        matching ??= overloads.FirstOrDefault(m => m.Parameters.IsEmpty);

        // c) ostatecznie – pierwszy lepszy
        matching ??= overloads.FirstOrDefault();

        if (matching is null)
        {
            selectedMethod = null;
            return false; // safeguard
        }

        selectedMethod = matching;

        // -----------------------------------------------------------------
        // Rozróżniamy CancellationToken od payloadu ------------------------
        // -----------------------------------------------------------------
        var cancellationTokenType =
            compilation.GetTypeByMetadataName("System.Threading.CancellationToken");


        matching.Parameters.Any(p =>_typeHelper.IsCancellationToken(p.Type,compilation));

        // Jeśli jakikolwiek parametr ≠ CancellationToken → to payload
        expectsPayload =
            matching.Parameters.Any(p => !_typeHelper.IsCancellationToken(p.Type,compilation));

        // ---------------------------------------------------------------------
        // 3. Analiza sygnatury wybranego przeciążenia
        // ---------------------------------------------------------------------
        var signatureInfo = _asyncAnalyzer.AnalyzeCallback(matching, callbackType, compilation);
        isAsync = signatureInfo.IsAsync;

        // ---------------------------------------------------------------------
        // 4. Spójność trybu maszyny (SYNC/ASYNC)
        //    Dozwolone:
        //      • maszyna ASYNC  -> callback SYNC lub ASYNC
        //    Niedozwolone:
        //      • maszyna SYNC   -> callback ASYNC
        // ---------------------------------------------------------------------
        if (isMachineAsyncMode is null)
        {
            // Pierwszy napotkany callback definiuje tryb maszyny
            isMachineAsyncMode = signatureInfo.IsAsync;
        }
        else if (isMachineAsyncMode.Value == false && signatureInfo.IsAsync)
        {
            // Maszyna była SYNC, a trafiliśmy na ASYNC callback → błąd FSM011
            var callbackMode = "asynchronous";
            var machineMode = "synchronous";

            var mixedModeCtx = new MixedModeValidationContext(methodName, callbackMode, machineMode);
            ProcessRuleResults(_mixedModeRule.Validate(mixedModeCtx), loc, ref criticalErrorOccurred);
            return false;
        }
        // jeśli maszyna jest ASYNC, sync callback jest OK – nic nie robimy

        // ---------------------------------------------------------------------
        // 5. Walidacja detali async (async void, Task<bool> dla guarda)
        // ---------------------------------------------------------------------
        
        // FSM014: Check for async void
        if (signatureInfo.IsInvalidAsyncVoid)
        {
            var asyncVoidCtx = new InvalidAsyncVoidContext(
                methodName: methodName,
                returnType: "void",
                isAsync: true);
            
            var asyncVoidResults = _invalidAsyncVoidRule.Validate(asyncVoidCtx);
            ProcessRuleResults(asyncVoidResults, loc, ref criticalErrorOccurred);
            // Warning - don't stop processing
        }

        // FSM012: Check for Task<bool> instead of ValueTask<bool> for guards
        if (callbackType == GuardCallbackType && signatureInfo.IsInvalidGuardTask)
        {
            var guardTaskCtx = new InvalidGuardTaskReturnTypeContext(
                methodName: methodName,
                actualReturnType: "System.Threading.Tasks.Task<bool>",
                isAsync: signatureInfo.IsAsync);
            
            var guardTaskResults = _invalidGuardTaskReturnTypeRule.Validate(guardTaskCtx);
            ProcessRuleResults(guardTaskResults, loc, ref criticalErrorOccurred);
            
            if (criticalErrorOccurred)
                return false; // critical error
        }
        
        // FSM013: Check for async callback in established sync machine
        // This is stricter than FSM011 - it's for when machine mode is already established
        if (isMachineAsyncMode.HasValue && !isMachineAsyncMode.Value && signatureInfo.IsAsync)
        {
            var asyncInSyncCtx = new AsyncCallbackInSyncMachineContext(
                methodName: methodName,
                isCallbackAsync: signatureInfo.IsAsync,
                isMachineAsync: isMachineAsyncMode.Value,
                isMachineEstablished: true);
            
            var asyncInSyncResults = _asyncCallbackInSyncMachineRule.Validate(asyncInSyncCtx);
            ProcessRuleResults(asyncInSyncResults, loc, ref criticalErrorOccurred);
            
            if (criticalErrorOccurred)
                return false; // critical error
        }

        // ---------------------------------------------------------------------
        // 6. Ogólna poprawność sygnatury (FSM003)
        // ---------------------------------------------------------------------
        bool isReturnTypeCorrect = (callbackType == GuardCallbackType && signatureInfo.IsBoolEquivalent) ||
                                   (callbackType != GuardCallbackType && signatureInfo.IsVoidEquivalent);

        // Dozwolone kombinacje parametrów:
        // () | (CancellationToken) | (payload) | (payload, CancellationToken)
        int nonCtParamCount = matching.Parameters.Count(p => !_typeHelper.IsCancellationToken(p.Type,compilation));
        int ctParamCount = matching.Parameters.Count(p =>_typeHelper.IsCancellationToken(p.Type,compilation));


        bool hasTooManyParams =
            nonCtParamCount > 1           // >1 payload
            || ctParamCount   > 1         // >1 CancellationToken
            || (nonCtParamCount + ctParamCount) > 2; // razem >2

        if (!isReturnTypeCorrect || hasTooManyParams)
        {
            var expectedReturn = _asyncAnalyzer.GetExpectedReturnType(callbackType, isAsync);

            var ctx = new MethodSignatureValidationContext(methodName, callbackType, expectedReturn, expectsPayload)
            {
                MethodFound = true,
                ActualReturnType = matching.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                ActualParameterCount = matching.Parameters.Length
            };

            ProcessRuleResults(_invalidMethodSignatureRule.Validate(ctx), loc, ref criticalErrorOccurred);
            if (criticalErrorOccurred) return false;
        }

        // ---------------------------------------------------------------------
        // 7. FSM010 – guard z payloadem w maszynie bez payloadu
        // ---------------------------------------------------------------------
        if (callbackType == GuardCallbackType && expectsPayload && !machineHasPayload)
        {
            var guardCtx = new GuardWithPayloadContext(methodName, true, false);
            ProcessRuleResults(_guardWithPayloadRule.Validate(guardCtx), loc, ref criticalErrorOccurred);
        }

        // ---------------------------------------------------------------------
        // 8. Wynik
        // ---------------------------------------------------------------------
        return !criticalErrorOccurred;
    }


    private string? GetEnumMemberName(TypedConstant enumValueConstant, INamedTypeSymbol enumTypeSymbol,
      AttributeData attributeDataForLocation, ref bool criticalErrorOccurred)
    {
        bool isValueDefinedInEnum = false;
        string? providedValueString = enumValueConstant.Value?.ToString();
        string? foundMemberName = null;

        if (enumValueConstant.Kind != TypedConstantKind.Error && enumValueConstant.Value != null)
        {
            foreach (var member in enumTypeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.IsConst && member is { HasConstantValue: true, ConstantValue: not null } &&
                    Equals(member.ConstantValue, enumValueConstant.Value))
                {
                    isValueDefinedInEnum = true;
                    foundMemberName = member.Name;
                    break;
                }
            }
        }

        var validationCtx = new EnumValueValidationContext(
            enumTypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            providedValueString ?? NullString,
            isValueDefinedInEnum
        );

        Location errorLocation = attributeDataForLocation.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? Location.None;

        bool isValidOverall = true;
        var ruleResults = _invalidEnumValueRule.Validate(validationCtx);
        foreach (var result in ruleResults)
        {
            if (!result.IsValid && result.RuleId != null)
            {
                // Try to get from catalog first
                if (RuleLookup.TryGet(result.RuleId!, out var def))
                {
                    // Use the preformatted helper for catalogued rules
                    EmitRulePreformatted(context, result.RuleId!, errorLocation, 
                        result.Message ?? string.Empty, result.Severity);
                }
                else
                {
                    // Legacy handling
                    EmitLegacy(context, result.RuleId!, result.RuleId!, result.Message ?? "", 
                        "FSM.Generator", result.Severity, errorLocation);
                }

                isValidOverall = false;
                if (result.Severity == RuleSeverity.Error)
                {
                    criticalErrorOccurred = true;
                }
            }
        }

        return isValidOverall && isValueDefinedInEnum ? foundMemberName : null;
    }

    /// <summary>
    ///  Odczytuje atrybuty <see cref="PayloadTypeAttribute"/> i wypełnia
    ///  <c>model.DefaultPayloadType</c> oraz <c>model.TriggerPayloadTypes</c>.
    /// </summary>
    private void ParsePayloadTypeAttributes(
        INamedTypeSymbol classSymbol,
        StateMachineModel model,
        ref bool criticalErrorOccurred)
    {

        // ---------- 1. Atrybuty na poziomie klasy ---------------------------------
        var classPayloadAttrs = classSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == PayloadTypeAttributeFullName);

        foreach (var attr in classPayloadAttrs)
        {
            Location attrLocation = attr.ApplicationSyntaxReference?
                                        .GetSyntax(context.CancellationToken)
                                        .GetLocation()
                                    ?? classSymbol.Locations.FirstOrDefault()
                                    ?? Location.None;

            // ── [PayloadType(typeof(SomePayload))] ──────────────────────────────────
            if (attr.ConstructorArguments.Length == 1)
            {
                if (attr.ConstructorArguments[0].Value is INamedTypeSymbol payloadType)
                {
                    string fqName = _typeHelper.BuildFullTypeName(payloadType);
                    model.DefaultPayloadType = fqName;
                }
                else
                {
                    EmitRule(context, RuleIdentifiers.InvalidTypesInAttribute,
                        attrLocation,
                        PayloadTypeArgName,
                        attr.ConstructorArguments[0].Value?.ToString() ?? NullString);
                    criticalErrorOccurred = true;
                }

                continue;
            }

            // ── [PayloadType(Trigger.X, typeof(SomePayload))] ───────────────────────
            if (attr.ConstructorArguments.Length == 2)
            {
                var triggerArg = attr.ConstructorArguments[0];
                var payloadTypeArg = attr.ConstructorArguments[1];

                var triggerEnum = compilation.GetTypeByMetadataName(model.TriggerType)
                                    as INamedTypeSymbol;
                if (triggerEnum is null)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                var triggerName = GetEnumMemberName(triggerArg, triggerEnum, attr, ref criticalErrorOccurred);
                if (triggerName is null) continue;

                if (payloadTypeArg.Value is INamedTypeSymbol payloadType)
                {
                    string fqName = _typeHelper.BuildFullTypeName(payloadType);
                    model.TriggerPayloadTypes[triggerName] = fqName;
                }
                else
                {
                    EmitRule(context, RuleIdentifiers.InvalidTypesInAttribute,
                        attrLocation,
                        $"{PayloadTypeForTriggerArgName}{triggerName}",
                        payloadTypeArg.Value?.ToString() ?? NullString);
                    criticalErrorOccurred = true;
                }
            }
        }

        // ---------- 2. Atrybuty na poziomie metod (mogą nadpisać klasowe) ---------
        foreach (var methodSymbol in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            var methodPayloadAttrs = methodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == PayloadTypeAttributeFullName);

            foreach (var attr in methodPayloadAttrs)
            {
                Location attrLocation = attr.ApplicationSyntaxReference?
                                            .GetSyntax(context.CancellationToken)
                                            .GetLocation()
                                        ?? methodSymbol.Locations.FirstOrDefault()
                                        ?? Location.None;

                if (attr.ConstructorArguments.Length != 2) continue;

                var triggerArg = attr.ConstructorArguments[0];
                var payloadTypeArg = attr.ConstructorArguments[1];

                var triggerEnum = compilation.GetTypeByMetadataName(model.TriggerType);
                if (triggerEnum is null)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                var triggerName = GetEnumMemberName(triggerArg, triggerEnum, attr, ref criticalErrorOccurred);
                if (triggerName is null) continue;

                if (payloadTypeArg.Value is INamedTypeSymbol payloadType)
                {
                    string fqName = _typeHelper.BuildFullTypeName(payloadType);

                    // Konflikt? – zgłoś diagnostykę.
                    if (model.TriggerPayloadTypes.TryGetValue(triggerName, out var existing) &&
                        existing != fqName)
                    {
                        EmitRule(context, RuleIdentifiers.InvalidMethodSignature,
                            attrLocation,
                            string.Format(PayloadTypeForTriggerConflictArgName, triggerName),
                            ConflictsWithAlreadyDefinedType);
                        criticalErrorOccurred = true;
                    }
                    else
                    {
                        model.TriggerPayloadTypes[triggerName] = fqName;
                    }
                }
            }
        }

        // ---------- 3. Ustaw flagę HasPayload --------------------------------------
        model.GenerationConfig.HasPayload =
            model.DefaultPayloadType is not null ||
            model.TriggerPayloadTypes.Count > 0;
    }

    private void AnalyzeAndSetCallbackSignature(
        IMethodSymbol methodSymbol,
        string callbackType,
        Action<CallbackSignatureInfo> setSigAction)
    {
        // Initialize analyzers if not already done
        if (_callbackAnalyzer == null)
        {
            var typeHelper = new TypeSystemHelper();
            var asyncAnalyzer = new AsyncSignatureAnalyzer(typeHelper);
            _callbackAnalyzer = new CallbackSignatureAnalyzer(typeHelper, asyncAnalyzer);
        }

        // Analyze exactly the selected overload:
        var signature = _callbackAnalyzer.AnalyzeSpecificMethod(
            methodSymbol,
            callbackType,
            compilation);

        setSigAction(signature);
    }

    // Add this field to your parser class:
    private CallbackSignatureAnalyzer _callbackAnalyzer;

    private void ParseOnExceptionAttribute(
        INamedTypeSymbol classSymbol,
        StateMachineModel model,
        INamedTypeSymbol stateTypeSymbol,
        INamedTypeSymbol triggerTypeSymbol,
        ref bool criticalErrorOccurred,
        ref bool? isMachineAsyncMode,
        Action<string>? report = null)
    {
        bool isAsyncOce = classSymbol.Name.Contains("AsyncOceOnEntryMachine");
        if (isAsyncOce)
            report?.Invoke("[DEBUG AOE] ParseOnExceptionAttribute started");
            
        // Find [OnException] attribute on class
        var onExceptionAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OnExceptionAttributeFullName);

        if (onExceptionAttr == null)
        {
            if (isAsyncOce)
                report?.Invoke("[DEBUG AOE] No OnException attribute found");
            return;
        }
        
        if (isAsyncOce)
            report?.Invoke("[DEBUG AOE] OnException attribute found");

        Location attrLocation = onExceptionAttr.ApplicationSyntaxReference?
            .GetSyntax(context.CancellationToken)
            .GetLocation() ?? classSymbol.Locations.FirstOrDefault() ?? Location.None;

        // Validate single argument
        if (onExceptionAttr.ConstructorArguments.Length != 1 || 
            onExceptionAttr.ConstructorArguments[0].Value is not string methodName)
        {
            criticalErrorOccurred = true;
            return;
        }

        // Find method overloads
        var overloads = classSymbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => !m.IsStatic && m.DeclaredAccessibility != Accessibility.Public)
            .ToList();

        if (!overloads.Any())
        {
            // Method not found - report FSM003
            var notFoundCtx = new MethodSignatureValidationContext(methodName, "OnException", "", false)
            {
                MethodFound = false
            };
            ProcessRuleResults(_invalidMethodSignatureRule.Validate(notFoundCtx), attrLocation, ref criticalErrorOccurred);
            return;
        }

        // Construct closed ExceptionContext type
        var exceptionContextOpen = compilation.GetTypeByMetadataName(ExceptionContextFullNameOpen);
        if (exceptionContextOpen == null)
        {
            if (isAsyncOce)
                report?.Invoke($"[DEBUG AOE] ERROR: Could not find ExceptionContext type: {ExceptionContextFullNameOpen}");
            criticalErrorOccurred = true;
            return;
        }
        
        if (isAsyncOce)
            report?.Invoke("[DEBUG AOE] Found ExceptionContext type");

        var exceptionContextClosed = exceptionContextOpen.Construct(stateTypeSymbol, triggerTypeSymbol);
        var exceptionDirectiveType = compilation.GetTypeByMetadataName(ExceptionDirectiveFullName);
        var cancellationTokenType = compilation.GetTypeByMetadataName(CancellationTokenFullName);

        if (exceptionDirectiveType == null || cancellationTokenType == null)
        {
            criticalErrorOccurred = true;
            return;
        }

        // Find best overload
        IMethodSymbol? selectedMethod = null;
        
        if (isAsyncOce)
        {
            report?.Invoke($"[DEBUG AOE] Looking for overload. Found {overloads.Count} overloads");
            foreach (var m in overloads)
            {
                var paramStr = string.Join(", ", m.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
                report?.Invoke($"[DEBUG AOE] Overload: {m.ReturnType.ToDisplayString()} {m.Name}({paramStr})");
            }
            report?.Invoke($"[DEBUG AOE] Expected ExceptionContext type: {exceptionContextClosed.ToDisplayString()}");
        }
        
        // Priority 1: (ExceptionContext<TState,TTrigger>, CancellationToken)
        selectedMethod = overloads.FirstOrDefault(m =>
            m.Parameters.Length == 2 &&
            SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, exceptionContextClosed) &&
            SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, cancellationTokenType));

        // Priority 2: (ExceptionContext<TState,TTrigger>)
        if (selectedMethod == null)
        {
            selectedMethod = overloads.FirstOrDefault(m =>
                m.Parameters.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, exceptionContextClosed));
        }
        
        if (isAsyncOce)
            report?.Invoke($"[DEBUG AOE] Selected method: {selectedMethod?.Name ?? "NULL"}");

        if (selectedMethod == null)
        {
            // Invalid signature - report error with proper expected signature
            var expectedReturn = "ExceptionDirective or ValueTask<ExceptionDirective>";
            var expectedSig = $"{expectedReturn} {methodName}(ExceptionContext<{stateTypeSymbol.Name}, {triggerTypeSymbol.Name}> context) or {methodName}(ExceptionContext<{stateTypeSymbol.Name}, {triggerTypeSymbol.Name}> context, CancellationToken ct)";
            
            // Create custom error message since we need specific parameter expectations
            var message = $"Method '{methodName}' used as OnException has an invalid signature. Expected: '{expectedSig}'.";
            EmitLegacy(context, "FSM003", "Invalid method signature",
                message, "FastFSM", RuleSeverity.Error, attrLocation);
            
            criticalErrorOccurred = true;
            return;
        }

        // Validate return type
        bool isAsync = false;
        bool validReturnType = false;

        if (isAsyncOce)
        {
            report?.Invoke($"[DEBUG AOE] Validating return type: {selectedMethod.ReturnType.ToDisplayString()}");
            if (selectedMethod.ReturnType is INamedTypeSymbol nrt)
            {
                report?.Invoke($"[DEBUG AOE] Is generic: {nrt.IsGenericType}");
                if (nrt.IsGenericType)
                {
                    report?.Invoke($"[DEBUG AOE] ConstructedFrom: {nrt.ConstructedFrom.ToDisplayString()}");
                    report?.Invoke($"[DEBUG AOE] Type args count: {nrt.TypeArguments.Length}");
                    if (nrt.TypeArguments.Length > 0)
                        report?.Invoke($"[DEBUG AOE] First type arg: {nrt.TypeArguments[0].ToDisplayString()}");
                }
            }
        }

        if (SymbolEqualityComparer.Default.Equals(selectedMethod.ReturnType, exceptionDirectiveType))
        {
            validReturnType = true;
            isAsync = false;
        }
        else if (selectedMethod.ReturnType is INamedTypeSymbol namedReturn &&
                 namedReturn.IsGenericType &&
                 namedReturn.ConstructedFrom.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>" &&
                 namedReturn.TypeArguments.Length == 1 &&
                 SymbolEqualityComparer.Default.Equals(namedReturn.TypeArguments[0], exceptionDirectiveType))
        {
            validReturnType = true;
            isAsync = true;
        }

        if (!validReturnType)
        {
            // Invalid return type
            var expectedReturn = "ExceptionDirective or ValueTask<ExceptionDirective>";
            var actualReturn = selectedMethod.ReturnType.ToDisplayString();
            
            var message = $"Method '{methodName}' used as OnException has an invalid return type. Expected: '{expectedReturn}', but found: '{actualReturn}'.";
            EmitLegacy(context, "FSM003", "Invalid method signature",
                message, "FastFSM", RuleSeverity.Error, attrLocation);
                
            criticalErrorOccurred = true;
            return;
        }

        // Check parameter count
        if (selectedMethod.Parameters.Length > 2)
        {
            var message = $"Method '{methodName}' used as OnException has too many parameters. Expected: 0-2 parameters, but found: {selectedMethod.Parameters.Length}.";
            EmitLegacy(context, "FSM003", "Invalid method signature",
                message, "FastFSM", RuleSeverity.Error, attrLocation);
                
            criticalErrorOccurred = true;
            return;
        }

        // FSM011: Check mixed mode
        if (isAsyncOce)
            report?.Invoke($"[DEBUG AOE] Mixed mode check - isMachineAsyncMode: {isMachineAsyncMode}, handler isAsync: {isAsync}");
            
        if (isMachineAsyncMode == false && isAsync)
        {
            if (isAsyncOce)
                report?.Invoke("[DEBUG AOE] ERROR: Mixed mode detected - async handler in sync machine");
            var mixedModeCtx = new MixedModeValidationContext(methodName, "asynchronous", "synchronous");
            ProcessRuleResults(_mixedModeRule.Validate(mixedModeCtx), attrLocation, ref criticalErrorOccurred);
            return;
        }

        // Success - create model
        model.ExceptionHandler = new ExceptionHandlerModel
        {
            MethodName = methodName,
            IsAsync = isAsync,
            AcceptsCancellationToken = selectedMethod.Parameters.Length == 2,
            ExceptionContextClosedType = _typeHelper.BuildFullTypeName(exceptionContextClosed)
        };
    }

    private void BuildHierarchy(StateMachineModel model, ref bool criticalErrorOccurred, Action<string>? report)
    {
        report?.Invoke("Building HSM hierarchy from state attributes");

        // Check if any HSM features are used
        bool hasHsmFeatures = false;
        foreach (var state in model.States.Values)
        {
            if (state.ParentState != null || state.History != Generator.Model.HistoryMode.None || state.IsInitial)
            {
                hasHsmFeatures = true;
                break;
            }
        }

        // Auto-enable hierarchy if HSM features are detected
        if (hasHsmFeatures)
        {
            if (!model.HierarchyEnabled)
            {
                model.HierarchyEnabled = true;
                report?.Invoke("HierarchyEnabled auto-set to true due to HSM attribute usage");
            }
            else
            {
                report?.Invoke("HierarchyEnabled already true from attribute");
            }
        }

        // If hierarchy is not enabled, skip building
        if (!model.HierarchyEnabled)
        {
            report?.Invoke("Hierarchy not enabled, skipping hierarchy building");
            return;
        }

        // Build parent-child relationships
        foreach (var state in model.States.Values)
        {
            // report?.Invoke($"[HSM] State {state.Name}: Parent={state.ParentState}, IsInitial={state.IsInitial}, History={state.History}");
            model.ParentOf[state.Name] = state.ParentState;
            
            if (!model.ChildrenOf.ContainsKey(state.Name))
            {
                model.ChildrenOf[state.Name] = new List<string>();
            }

            if (state.ParentState != null)
            {
                // Validate parent exists
                if (!model.States.ContainsKey(state.ParentState))
                {
                    var orphanCtx = new OrphanSubstateContext(
                        substateName: state.Name,
                        parentStateName: state.ParentState,
                        parentExists: false);
                    var orphanResults = _orphanSubstateRule.Validate(orphanCtx);
                    ProcessRuleResults(orphanResults, Location.None, ref criticalErrorOccurred);
                    continue;
                }

                // Add to parent's children list
                if (!model.ChildrenOf.ContainsKey(state.ParentState))
                {
                    model.ChildrenOf[state.ParentState] = new List<string>();
                }
                model.ChildrenOf[state.ParentState].Add(state.Name);
            }
        }

        // Check for circular dependencies
        // report?.Invoke($"[HSM] Checking for circular dependencies...");
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        foreach (var state in model.States.Keys)
        {
            var cyclePath = new List<string>();
            if (HasCircularDependency(state, model.ParentOf, visited, recursionStack, report, cyclePath))
            {
                var circularCtx = new CircularHierarchyContext(state, cyclePath);
                var circularResults = _circularHierarchyRule.Validate(circularCtx);
                ProcessRuleResults(circularResults, Location.None, ref criticalErrorOccurred);
            }
        }

        // Calculate depth for each state
        foreach (var state in model.States.Keys)
        {
            model.Depth[state] = CalculateDepth(state, model.ParentOf);
        }

        // Process initial substates and history
        foreach (var state in model.States.Values)
        {
            // Process history mode
            if (state.History != Generator.Model.HistoryMode.None)
            {
                if (!state.IsComposite)
                {
                    var historyCtx = new InvalidHistoryConfigurationContext(
                        stateName: state.Name,
                        hasHistory: true,
                        historyMode: state.History.ToString(),
                        isComposite: false);
                    var historyResults = _invalidHistoryConfigRule.Validate(historyCtx);
                    ProcessRuleResults(historyResults, Location.None, ref criticalErrorOccurred);
                    // FSM104 is now a warning, don't set criticalErrorOccurred
                }
                else
                {
                    model.HistoryOf[state.Name] = state.History;
                }
            }

            // Process initial substates
            if (state.IsInitial && state.ParentState != null)
            {
                if (model.InitialChildOf.ContainsKey(state.ParentState))
                {
                    // Multiple initial substates for the same parent
                    var multipleInitialCtx = new MultipleInitialSubstatesContext(
                        parentStateName: state.ParentState,
                        initialSubstates: new List<string> { model.InitialChildOf[state.ParentState], state.Name });
                    var multipleInitialResults = _multipleInitialSubstatesRule.Validate(multipleInitialCtx);
                    ProcessRuleResults(multipleInitialResults, Location.None, ref criticalErrorOccurred);
                }
                else
                {
                    model.InitialChildOf[state.ParentState] = state.Name;
                }
            }
        }

        // Validate composite states have initial substates (if no history)
        foreach (var state in model.States.Values)
        {
            if (state.IsComposite && 
                !model.InitialChildOf.ContainsKey(state.Name) && 
                state.History == Generator.Model.HistoryMode.None)
            {
                var hierarchyCtx = new InvalidHierarchyConfigurationContext(
                    compositeStateName: state.Name,
                    isComposite: true,
                    hasInitialSubstate: false,
                    hasHistory: false);
                var hierarchyResults = _invalidHierarchyConfigRule.Validate(hierarchyCtx);
                ProcessRuleResults(hierarchyResults, Location.None, ref criticalErrorOccurred);
                // Not critical - can use first child as default
            }
        }

        report?.Invoke($"Hierarchy built: {model.ParentOf.Count} parent relationships, {model.ChildrenOf.Count} composite states");
    }

    private bool HasCircularDependency(string state, Dictionary<string, string?> parentOf, 
        HashSet<string> visited, HashSet<string> recursionStack, Action<string>? report, List<string> cyclePath)
    {
        // Jeśli stan jest już w stosie rekursji, mamy cykl
        if (recursionStack.Contains(state))
        {
            // Build the cycle path
            cyclePath.Clear();
            bool foundStart = false;
            foreach (var s in recursionStack)
            {
                if (s == state)
                    foundStart = true;
                if (foundStart)
                    cyclePath.Add(s);
            }
            cyclePath.Add(state); // Add the state again to show the cycle
            return true;
        }
            
        // Jeśli już odwiedzony i nie było cyklu, skip
        if (visited.Contains(state))
            return false;

        visited.Add(state);
        recursionStack.Add(state);

        // Sprawdź rodzica
        if (parentOf.TryGetValue(state, out var parent) && parent != null)
        {
            if (HasCircularDependency(parent, parentOf, visited, recursionStack, report, cyclePath))
                return true;
        }

        // Usuń ze stosu po przetworzeniu
        recursionStack.Remove(state);
        return false;
    }

    private int CalculateDepth(string state, Dictionary<string, string?> parentOf)
    {
        int depth = 0;
        var current = state;
        
        while (parentOf.TryGetValue(current, out var parent) && parent != null)
        {
            depth++;
            current = parent;
            
            // Safety check against infinite loops (should be caught by circular dependency check)
            if (depth > 100)
            {
                return -1;
            }
        }
        
        return depth;
    }

}
