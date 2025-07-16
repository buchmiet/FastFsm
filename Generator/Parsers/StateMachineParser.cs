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
    private readonly TypeSystemHelper _typeHelper = new();

    private readonly HashSet<TransitionDefinition> _processedTransitionsInCurrentFsm = [];

    private void ProcessRuleResults(IEnumerable<ValidationResult> ruleResults,
        Location defaultLocation,
        ref bool criticalErrorOccurredFlag)
    {
        foreach (var result in ruleResults)
        {
            if (!result.IsValid && result.RuleId != null)
            {
                if (DiagnosticFactory.TryCreateDiagnostic(result, defaultLocation, out Diagnostic diagnosticToReport))
                {
                    context.ReportDiagnostic(diagnosticToReport);
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
    out StateMachineModel? model)
    {
        model = null;
        _processedTransitionsInCurrentFsm.Clear();
        bool criticalErrorOccurred = false;

        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol) return false;

        var currentModel = new StateMachineModel
        {
            Namespace = classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name
        };

        var fsmAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);

        var defaultPayloadArg = fsmAttribute?.NamedArguments
            .FirstOrDefault(na => na.Key ==
                                  nameof(StateMachineAttribute.DefaultPayloadType));

        if (defaultPayloadArg?.Value.Value is INamedTypeSymbol payloadSym)
        {
            currentModel.DefaultPayloadType =
                _typeHelper.BuildFullTypeName(payloadSym);

            // zaznacz, że maszyna faktycznie korzysta z payloadu
            currentModel.GenerationConfig.HasPayload = true;
        }

        bool emitStructuralHelpers = false;

        if (fsmAttribute is not null)
        {
            // Szukamy parametru nazwnego "GenerateStructuralApi"
            var arg = fsmAttribute.NamedArguments
                .FirstOrDefault(kv => kv.Key == "GenerateStructuralApi");

            // Jeżeli argument występuje i jest bool‑em – odczytujemy
            if (arg.Key is not null &&              // Key == null → parametru nie było
                arg.Value.Value is bool flag)
            {
                emitStructuralHelpers = flag;
            }
        }

        currentModel.EmitStructuralHelpers = emitStructuralHelpers;


        Location fsmAttributeLocation = fsmAttribute?.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? classDeclaration.Identifier.GetLocation();
        bool isPartial = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

        var missingAttrCtx = new MissingStateMachineAttributeValidationContext(
            fsmAttribute != null,
            fsmAttribute?.ConstructorArguments.Length ?? 0,
            classSymbol.Name,
            isPartial
        );

        ProcessRuleResults(
            _missingStateMachineAttributeRule.Validate(missingAttrCtx),
            fsmAttributeLocation,
            ref criticalErrorOccurred
        );

        if (fsmAttribute == null || fsmAttribute.ConstructorArguments.Length < 2 || !isPartial)
        {
            return false;
        }

        var stateTypeArg = fsmAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
        var triggerTypeArg = fsmAttribute.ConstructorArguments[1].Value as INamedTypeSymbol;

        var attributeTypeValidationCtx = new AttributeTypeValidationContext(
            stateTypeArg?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            stateTypeArg?.TypeKind == TypeKind.Enum,
            triggerTypeArg?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            triggerTypeArg?.TypeKind == TypeKind.Enum
        );

        ProcessRuleResults(
            _invalidTypesInAttributeRule.Validate(attributeTypeValidationCtx),
            fsmAttributeLocation,
            ref criticalErrorOccurred
        );

        if (stateTypeArg is not { TypeKind: TypeKind.Enum })
        {
            return false;
        }

        if (triggerTypeArg is not { TypeKind: TypeKind.Enum })
        {
            return false;
        }

        currentModel.StateType = _typeHelper.BuildFullTypeName(stateTypeArg);
        currentModel.TriggerType = _typeHelper.BuildFullTypeName(triggerTypeArg);

        foreach (var member in stateTypeArg.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst)
            {
                if (!currentModel.States.ContainsKey(member.Name))
                {
                    currentModel.States[member.Name] = new StateModel { Name = member.Name };
                }
            }
        }

        ParseMemberAttributes(classSymbol, currentModel, stateTypeArg, triggerTypeArg, ref criticalErrorOccurred);

        // ========== Określenie wariantu ==========
        var variantSelector = new VariantSelector();
        variantSelector.DetermineVariant(currentModel, classSymbol);

        // ========== Walidacje Force ==========
        if (currentModel.GenerationConfig.IsForced)
        {
            var variant = currentModel.GenerationConfig.Variant;

            // Potrzebujemy instancji reguł
            var missingPayloadRule = new MissingPayloadTypeRule();
            var conflictingPayloadRule = new ConflictingPayloadRule();
            var invalidVariantRule = new InvalidVariantConfigRule();

            // FSM007 - Missing payload for WithPayload/Full
            if (variant is GenerationVariant.WithPayload or GenerationVariant.Full)
            {
                bool hasAnyPayload = currentModel.DefaultPayloadType != null || currentModel.TriggerPayloadTypes.Any();
                if (!hasAnyPayload)
                {
                    var ctx = new MissingPayloadTypeContext(
                        variant.ToString(),
                        currentModel.DefaultPayloadType != null,
                        currentModel.TriggerPayloadTypes.Any(),
                        true);

                    ProcessRuleResults(
                        missingPayloadRule.Validate(ctx),
                        classDeclaration.Identifier.GetLocation(),
                        ref criticalErrorOccurred);
                }
            }

            // FSM008 - Conflicting payload for WithPayload
            if (variant == GenerationVariant.WithPayload)
            {
                var ctx = new ConflictingPayloadContext(
                    true,
                    currentModel.TriggerPayloadTypes.Count);

                ProcessRuleResults(
                    conflictingPayloadRule.Validate(ctx),
                    classDeclaration.Identifier.GetLocation(),
                    ref criticalErrorOccurred);
            }

            // FSM009 - Various invalid configurations
            // Pure + PayloadTypes
            if (variant == GenerationVariant.Pure &&
                (currentModel.DefaultPayloadType != null || currentModel.TriggerPayloadTypes.Any()))
            {
                var ctx = new InvalidVariantConfigContext(
                    variant.ToString(),
                    "PayloadTypes",
                    true);

                ProcessRuleResults(
                    invalidVariantRule.Validate(ctx),
                    classDeclaration.Identifier.GetLocation(),
                    ref criticalErrorOccurred);
            }

            // Full + no extensions
            if (variant == GenerationVariant.Full && !currentModel.GenerationConfig.HasExtensions)
            {
                // Sprawdź czy GenerateExtensibleVersion jest ustawione
                var generateExtensible = fsmAttribute?.NamedArguments
                    .FirstOrDefault(na => na.Key == "GenerateExtensibleVersion");

                bool isExtensibleEnabled = generateExtensible?.Value.Value as bool? ?? false;

                if (!isExtensibleEnabled)
                {
                    var ctx = new InvalidVariantConfigContext(
                        variant.ToString(),
                        "Extensions",
                        true);

                    ProcessRuleResults(
                        invalidVariantRule.Validate(ctx),
                        classDeclaration.Identifier.GetLocation(),
                        ref criticalErrorOccurred);
                }
            }

            // Basic/WithExtensions + PayloadTypes
            if ((variant == GenerationVariant.Basic || variant == GenerationVariant.WithExtensions) &&
                (currentModel.DefaultPayloadType != null || currentModel.TriggerPayloadTypes.Any()))
            {
                var ctx = new InvalidVariantConfigContext(
                    variant.ToString(),
                    "PayloadTypes",
                    true);

                ProcessRuleResults(
                    invalidVariantRule.Validate(ctx),
                    classDeclaration.Identifier.GetLocation(),
                    ref criticalErrorOccurred);
            }

            // ========== NOWE: Sprawdzenie po walidacjach Force ==========
            if (criticalErrorOccurred)
            {
                return false;  // Nie generuj kodu jeśli Force ma błędy
            }
        }
        // ========== KONIEC walidacji Force ==========

        var allStateNames = currentModel.States.Keys.ToList();
        var transitionsForReachability = currentModel.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ToState))
            .Select(t => new TransitionDefinition(t.FromState, t.Trigger, t.ToState!))
            .ToList();

        string initialStateForReachability = allStateNames.FirstOrDefault() ?? string.Empty;

        var unreachableCtx = new UnreachableStateContext(
            initialStateForReachability,
            allStateNames,
            transitionsForReachability
        );

        ProcessRuleResults(
            _unreachableStateRule.Validate(unreachableCtx),
            classDeclaration.Identifier.GetLocation(),
            ref criticalErrorOccurred
        );

        if (criticalErrorOccurred) return false;

        model = currentModel;
        return true;
    }

    private void ParseMemberAttributes(INamedTypeSymbol classSymbol, StateMachineModel model,
        INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol,
        ref bool criticalErrorOccurred)
    {
        ParsePayloadTypeAttributes(classSymbol, model, ref criticalErrorOccurred);
        ParseTransitionAttributes(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred);
        ParseInternalTransitionAttributes(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred);
        ParseStateAttributes(classSymbol, model, stateTypeSymbol, ref criticalErrorOccurred);
    }

    private void ParseTransitionAttributes(INamedTypeSymbol classSymbolContainingMethods, StateMachineModel model,
                                          INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol localTriggerTypeSymbol,
                                          ref bool criticalErrorOccurred)
    {
        var boolType = compilation.GetSpecialType(System_Boolean);
        var voidType = compilation.GetSpecialType(System_Void);

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
                    criticalErrorOccurred = true;
                    continue;
                }

                var currentTransitionDef = new TransitionDefinition(fromState, trigger, toState);
                var duplicateCheckCtx = new DuplicateTransitionContext(currentTransitionDef, _processedTransitionsInCurrentFsm);

                var validationResults = _duplicateTransitionRule.Validate(duplicateCheckCtx).ToList();
                ProcessRuleResults(validationResults, attrLocation, ref criticalErrorOccurred);

                bool isDuplicate = validationResults.Any(r => !r.IsValid && r.RuleId == RuleIdentifiers.DuplicateTransition);

                var transition = new TransitionModel { FromState = fromState, Trigger = trigger, ToState = toState, IsInternal = false };

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
                    if (namedArg is { Key: GuardCallbackType, Value.Value: string guardMethodName })
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, guardMethodName, GuardCallbackType,
                            attrData, boolType, ref criticalErrorOccurred, out var guardExpectsPayload, model.GenerationConfig.HasPayload, transition.ExpectedPayloadType))
                        {
                            guardValid = false;
                        }
                        if (guardValid)
                        {
                            transition.GuardMethod = guardMethodName;
                            transition.GuardExpectsPayload = guardExpectsPayload;

                            var parameterlessGuard = classSymbolContainingMethods
                                .GetMembers(guardMethodName).OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);
                            transition.GuardHasParameterlessOverload = parameterlessGuard != null;
                        }
                    }
                    if (namedArg is { Key: ActionCallbackType, Value.Value: string actionMethodName })
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, actionMethodName, ActionCallbackType,
                            attrData, voidType, ref criticalErrorOccurred, out var actionExpectsPayload, model.GenerationConfig.HasPayload, transition.ExpectedPayloadType))
                        {
                            actionValid = false;
                        }
                        if (actionValid)
                        {
                            transition.ActionMethod = actionMethodName;
                            transition.ActionExpectsPayload = actionExpectsPayload;

                            var parameterlessAction = classSymbolContainingMethods
                                .GetMembers(actionMethodName).OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);
                            transition.ActionHasParameterlessOverload = parameterlessAction != null;
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
                                                  ref bool criticalErrorOccurred)
    {
        var boolType = compilation.GetSpecialType(System_Boolean);
        var voidType = compilation.GetSpecialType(System_Void);

        foreach (var methodSymbol in classSymbolContainingMethods.GetMembers().OfType<IMethodSymbol>())
        {
            var internalTransitionAttributesData = methodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == InternalTransitionAttributeFullName);

            foreach (var attrData in internalTransitionAttributesData)
            {
                Location attrLocation = attrData.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? methodSymbol.Locations.FirstOrDefault() ?? Location.None;

                if (attrData.ConstructorArguments.Length < 3 || !(attrData.ConstructorArguments[2].Value is string actionMethodNameFromCtor))
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                var state = GetEnumMemberName(attrData.ConstructorArguments[0], stateTypeSymbol, attrData, ref criticalErrorOccurred);
                var trigger = GetEnumMemberName(attrData.ConstructorArguments[1], localTriggerTypeSymbol, attrData, ref criticalErrorOccurred);

                if (state == null || trigger == null)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                var currentTransitionDef = new TransitionDefinition(state, trigger, state);
                var duplicateCheckCtx = new DuplicateTransitionContext(currentTransitionDef, _processedTransitionsInCurrentFsm);

                var validationResults = _duplicateTransitionRule.Validate(duplicateCheckCtx).ToList();
                ProcessRuleResults(validationResults, attrLocation, ref criticalErrorOccurred);

                bool isDuplicate = validationResults.Any(r => !r.IsValid && r.RuleId == RuleIdentifiers.DuplicateTransition);

                var transition = new TransitionModel { FromState = state, Trigger = trigger, ToState = state, IsInternal = true };

                if (model.TriggerPayloadTypes.TryGetValue(trigger, out var triggerPayloadType))
                {
                    transition.ExpectedPayloadType = triggerPayloadType;
                }
                else if (model.DefaultPayloadType != null)
                {
                    transition.ExpectedPayloadType = model.DefaultPayloadType;
                }

                bool actionValid = ValidateCallbackMethodSignature(classSymbolContainingMethods, actionMethodNameFromCtor,
                    ActionCtorCallbackType, attrData, voidType, ref criticalErrorOccurred, out var actionExpectsPayload, model.GenerationConfig.HasPayload, transition.ExpectedPayloadType);

                if (actionValid)
                {
                    transition.ActionMethod = actionMethodNameFromCtor;
                    transition.ActionExpectsPayload = actionExpectsPayload;

                    var parameterlessAction = classSymbolContainingMethods
                        .GetMembers(actionMethodNameFromCtor).OfType<IMethodSymbol>()
                        .FirstOrDefault(m => m.Parameters.IsEmpty);
                    transition.ActionHasParameterlessOverload = parameterlessAction != null;
                }

                bool guardValid = true;
                foreach (var namedArg in attrData.NamedArguments)
                {
                    if (namedArg.Key == GuardCallbackType && namedArg.Value.Value is string guardMethodName)
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, guardMethodName, GuardCallbackType,
                            attrData, boolType, ref criticalErrorOccurred, out var guardExpectsPayload, model.GenerationConfig.HasPayload, transition.ExpectedPayloadType))
                        {
                            guardValid = false;
                        }
                        if (guardValid)
                        {
                            transition.GuardMethod = guardMethodName;
                            transition.GuardExpectsPayload = guardExpectsPayload;

                            var parameterlessGuard = classSymbolContainingMethods
                                .GetMembers(guardMethodName).OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty);
                            transition.GuardHasParameterlessOverload = parameterlessGuard != null;
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

    private void ParseStateAttributes(INamedTypeSymbol classSymbolContainingMethods, StateMachineModel model,
      INamedTypeSymbol stateTypeSymbol, ref bool criticalErrorOccurred)
    {
        var voidType = compilation.GetSpecialType(System_Void);

        foreach (var methodSymbol in classSymbolContainingMethods.GetMembers().OfType<IMethodSymbol>())
        {
            var stateAttributesData = methodSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == StateAttributeFullName);

            foreach (var attrData in stateAttributesData)
            {
                if (attrData.ConstructorArguments.Length < 1)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                var stateName = GetEnumMemberName(attrData.ConstructorArguments[0], stateTypeSymbol, attrData,
                    ref criticalErrorOccurred);
                if (stateName == null)
                {
                    criticalErrorOccurred = true;
                    continue;
                }

                if (!model.States.TryGetValue(stateName, out var stateModel))
                {
                    stateModel = new StateModel { Name = stateName };
                    model.States[stateName] = stateModel;
                }

                foreach (var namedArg in attrData.NamedArguments)
                {
                    if (namedArg is { Key: OnEntryCallbackType, Value.Value: string onEntryMethodName })
                    {
                        // Dla wariantów z payloadem, pozwól na parametry w OnEntry
                        string? expectedPayloadTypeForValidation = model.DefaultPayloadType;

                        // Jeśli maszyna obsługuje payloady ale nie ma domyślnego typu (multi-payload),
                        // użyj specjalnej wartości "*" która oznacza "akceptuj dowolny typ"
                        if (model.GenerationConfig.HasPayload && expectedPayloadTypeForValidation == null)
                        {
                            expectedPayloadTypeForValidation = "*"; // Specjalna wartość dla "dowolny typ"
                        }

                        if (ValidateCallbackMethodSignature(classSymbolContainingMethods, onEntryMethodName, OnEntryCallbackType,
                                attrData, voidType, ref criticalErrorOccurred, out var onEntryExpectsPayload, model.GenerationConfig.HasPayload, expectedPayloadTypeForValidation))
                        {
                            stateModel.OnEntryMethod = onEntryMethodName;
                            stateModel.OnEntryExpectsPayload = onEntryExpectsPayload;

                            var parameterlessOverload = classSymbolContainingMethods
                                .GetMembers(onEntryMethodName)
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty &&
                                    SymbolEqualityComparer.Default.Equals(m.ReturnType, voidType));

                            stateModel.OnEntryHasParameterlessOverload = parameterlessOverload != null;
                        }
                    }

                    if (namedArg is { Key: OnExitCallbackType, Value.Value: string onExitMethodName })
                    {
                        // Dla wariantów z payloadem, pozwól na parametry w OnExit
                        string? expectedPayloadTypeForValidation = model.DefaultPayloadType;

                        // Jeśli maszyna obsługuje payloady ale nie ma domyślnego typu (multi-payload),
                        // użyj specjalnej wartości "*" która oznacza "akceptuj dowolny typ"
                        if (model.GenerationConfig.HasPayload && expectedPayloadTypeForValidation == null)
                        {
                            expectedPayloadTypeForValidation = "*"; // Specjalna wartość dla "dowolny typ"
                        }

                        if (ValidateCallbackMethodSignature(classSymbolContainingMethods, onExitMethodName, OnExitCallbackType,
                                attrData, voidType, ref criticalErrorOccurred, out var onExitExpectsPayload, model.GenerationConfig.HasPayload, expectedPayloadTypeForValidation))
                        {
                            stateModel.OnExitMethod = onExitMethodName;
                            stateModel.OnExitExpectsPayload = onExitExpectsPayload;

                            var parameterlessOverload = classSymbolContainingMethods
                                .GetMembers(onExitMethodName)
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(m => m.Parameters.IsEmpty &&
                                    SymbolEqualityComparer.Default.Equals(m.ReturnType, voidType));

                            stateModel.OnExitHasParameterlessOverload = parameterlessOverload != null;
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    ///  Waliduje sygnaturę metody zwrotnej (callback) opisanej w atrybucie FSM.
    ///  Zwraca <c>true</c>, gdy nie wykryto błędów krytycznych.
    /// </summary>
    private bool ValidateCallbackMethodSignature(
        INamedTypeSymbol classSymbol,
        string methodName,
        string callbackType,
        AttributeData attributeData,
        ITypeSymbol expectedRoslynReturnType,
        ref bool criticalErrorOccurred,
        out bool expectsPayload,
        bool machineHasPayload,
        string? expectedPayloadType = null)
    {
        expectsPayload = false;

        // ---------------------------------------------------------------------
        // 1. Wyszukujemy przeciążenia metody w klasie
        // ---------------------------------------------------------------------
        var overloads = classSymbol.GetMembers(methodName)
                                   .OfType<IMethodSymbol>()
                                   .ToList();

        IMethodSymbol? matching = null;

        // -- a) Najpierw spróbuj dobrać wariant z 1 parametrem, jeśli spodziewany --
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
            // "*" = dowolny pojedynczy parametr
            matching = overloads.FirstOrDefault(m => m.Parameters.Length == 1);
        }

        // -- b) Jeżeli nic nie znaleziono, szukamy bezparametrowego overloadu --
        matching ??= overloads.FirstOrDefault(m => m.Parameters.IsEmpty);

        if (matching is not null && matching.Parameters.Length == 1)
            expectsPayload = true;

        // ---------------------------------------------------------------------
        // 2. Budowa kontekstu do reguły FSM003 (niepoprawne sygnatury)
        // ---------------------------------------------------------------------
        var ctx = new MethodSignatureValidationContext(
            methodName,
            callbackType,
            expectedRoslynReturnType.SpecialType == SpecialType.System_Boolean ? "bool" : "void",
            parametersAllowed: expectedPayloadType is not null)
        {
            MethodFound = overloads.Any(),
            ExpectedParameterType = expectedPayloadType == "*" ? null
                                   : expectedPayloadType is null ? null
                                   : _typeHelper.GetSimpleTypeName(expectedPayloadType)
        };

        if (matching is not null)
        {
            ctx.ActualReturnType = matching.ReturnType.SpecialType == SpecialType.System_Boolean ? "bool" : "void";
            ctx.ActualParameterCount = matching.Parameters.Length;

            if (matching.Parameters.Length == 1 && expectedPayloadType != "*")
            {
                ctx.ActualParameterType = _typeHelper.GetSimpleTypeName(
                    _typeHelper.BuildFullTypeName(matching.Parameters[0].Type as INamedTypeSymbol));
            }
        }
        else if (overloads.Any())
        {
            // Metoda istnieje, ale żaden overload nie pasuje.
            var first = overloads[0];
            ctx.ActualReturnType = first.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            ctx.ActualParameterCount = first.Parameters.Length;

            if (first.Parameters.Length == 1)
            {
                ctx.ActualParameterType = _typeHelper.GetSimpleTypeName(
                    _typeHelper.BuildFullTypeName(first.Parameters[0].Type as INamedTypeSymbol));
            }
        }

        // Jeżeli '*' – nie sprawdzamy dokładnego typu parametru
        if (expectedPayloadType == "*" && ctx.ActualParameterCount == 1)
            ctx.ExpectedParameterType = null;

        // ---------------------------------------------------------------------
        // 3. Lokalizacja diagnostyki (ta sama dla FSM003 i FSM010)
        // ---------------------------------------------------------------------
        Location loc = attributeData.ApplicationSyntaxReference?
                           .GetSyntax(context.CancellationToken)
                           .GetLocation()
                       ?? Location.None;

        // ---------------------------------------------------------------------
        // 4. Walidacja regułą FSM003 (niepoprawne sygnatury metod callback)
        // ---------------------------------------------------------------------
        bool isOk = true;

        foreach (var res in _invalidMethodSignatureRule.Validate(ctx))
        {
            if (res.IsValid) continue;

            if (DiagnosticFactory.TryCreateDiagnostic(res, loc, out var diag))
                context.ReportDiagnostic(diag);

            if (res.Severity == RuleSeverity.Error)
            {
                isOk = false;
                criticalErrorOccurred = true;
            }
        }

        // ---------------------------------------------------------------------
        // 5. Walidacja regułą FSM010 (guard z payloadem w maszynie bez payloadu)
        // ---------------------------------------------------------------------
        if (callbackType == GuardCallbackType &&            // to jest guard
            expectsPayload &&                               // …który oczekuje parametru
            !machineHasPayload)             // …ale FSM nie obsługuje payloadu
        {
            var guardCtx = new GuardWithPayloadContext(
                methodName,
                guardExpectsPayload: true,
                machineHasPayload: false);

            foreach (var res in _guardWithPayloadRule.Validate(guardCtx))
            {
                if (res.IsValid) continue;

                if (DiagnosticFactory.TryCreateDiagnostic(res, loc, out var diag))
                    context.ReportDiagnostic(diag);

                if (res.Severity == RuleSeverity.Error)
                {
                    isOk = false;
                    criticalErrorOccurred = true;
                }
            }
        }

        // ---------------------------------------------------------------------
        // 6. Zwracamy wynik globalny
        // ---------------------------------------------------------------------
        return isOk;
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
                if (DiagnosticFactory.TryCreateDiagnostic(result, errorLocation, out Diagnostic? diagnosticToReport))
                {
                    context.ReportDiagnostic(diagnosticToReport);
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
                    var diag = Diagnostic.Create(
                        DiagnosticFactory.Get(RuleIdentifiers.InvalidTypesInAttribute),
                        attrLocation,
                        PayloadTypeArgName,
                        attr.ConstructorArguments[0].Value?.ToString() ?? NullString);

                    context.ReportDiagnostic(diag);
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
                    var diag = Diagnostic.Create(
                        DiagnosticFactory.Get(RuleIdentifiers.InvalidTypesInAttribute),
                        attrLocation,
                        $"{PayloadTypeForTriggerArgName}{triggerName}",
                        payloadTypeArg.Value?.ToString() ?? NullString);

                    context.ReportDiagnostic(diag);
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
                        var diag = Diagnostic.Create(
                            DiagnosticFactory.Get(RuleIdentifiers.InvalidMethodSignature),
                            attrLocation,
                            string.Format(PayloadTypeForTriggerConflictArgName, triggerName),
                            ConflictsWithAlreadyDefinedType);

                        context.ReportDiagnostic(diag);
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
}