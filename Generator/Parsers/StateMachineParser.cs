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
    private readonly TypeSystemHelper _typeHelper = new();
    private readonly AsyncSignatureAnalyzer _asyncAnalyzer = new(new TypeSystemHelper());
    private readonly HashSet<TransitionDefinition> _processedTransitionsInCurrentFsm = [];
    private readonly MixedModeRule _mixedModeRule = new();
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
     out StateMachineModel? model,
     Action<string>? report = null)
    {
        model = null;
        _processedTransitionsInCurrentFsm.Clear();
        bool criticalErrorOccurred = false;
        bool? isMachineAsyncMode = null;

        report?.Invoke("=== START TryParse ===");
        report?.Invoke($"Parsing class: {classDeclaration.Identifier.Text}");

        // === SEKCJA 1: Pobieranie semantic model i class symbol ===
        report?.Invoke("Section 1: Getting semantic model and class symbol");
        var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            report?.Invoke("ERROR: Failed to get class symbol");
            return false;
        }
        report?.Invoke($"Class symbol obtained: {classSymbol.Name}");

        // === SEKCJA 2: Tworzenie początkowego modelu ===
        report?.Invoke("Section 2: Creating initial model");
        var currentModel = new StateMachineModel
        {
            Namespace = classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name
        };
        report?.Invoke($"Model created - Namespace: {currentModel.Namespace}, ClassName: {currentModel.ClassName}");

        // === SEKCJA 3: Pobieranie atrybutu StateMachine ===
        report?.Invoke("Section 3: Getting StateMachine attribute");
        var fsmAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);
        report?.Invoke($"StateMachine attribute found: {fsmAttribute != null}");

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
        ParseMemberAttributes(classSymbol, currentModel, stateTypeArg, triggerTypeArg, ref criticalErrorOccurred, ref isMachineAsyncMode);
        report?.Invoke($"ParseMemberAttributes completed. Critical error: {criticalErrorOccurred}, IsAsync: {isMachineAsyncMode}");

        // === SEKCJA 9: Określenie wariantu generacji ===
        report?.Invoke("Section 9: Determining generation variant");
        var variantSelector = new VariantSelector();
        report?.Invoke("Calling DetermineVariant");
        variantSelector.DetermineVariant(currentModel, classSymbol);
        report?.Invoke($"Variant determined: {currentModel.GenerationConfig.Variant}");

        // === SEKCJA 10: Walidacje po ustaleniu wariantu ===
        report?.Invoke("Section 10: Post-variant validations");
        // Tu powinna być logika walidacji dla Force (FSM007, FSM008, FSM009)
        // Nie widzę jej w kodzie, ale zostawiam komentarz

        if (criticalErrorOccurred)
        {
            report?.Invoke("ERROR: Critical error occurred during validations - returning false");
            return false;
        }

        // === SEKCJA 11: Walidacja osiągalności stanów ===
        report?.Invoke("Section 11: Validating state reachability");
        var allStateNames = currentModel.States.Keys.ToList();
        report?.Invoke($"Total states count: {allStateNames.Count}");

        var transitionsForReachability = currentModel.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ToState))
            .Select(t => new TransitionDefinition(t.FromState, t.Trigger, t.ToState!))
            .ToList();
        report?.Invoke($"Transitions count: {transitionsForReachability.Count}");

        string initialStateForReachability = allStateNames.FirstOrDefault() ?? string.Empty;
        report?.Invoke($"Initial state: {initialStateForReachability}");

        var unreachableCtx = new UnreachableStateContext(initialStateForReachability, allStateNames, transitionsForReachability);
        report?.Invoke("Validating unreachable states");
        ProcessRuleResults(_unreachableStateRule.Validate(unreachableCtx), classDeclaration.Identifier.GetLocation(), ref criticalErrorOccurred);
        report?.Invoke($"Critical error after reachability validation: {criticalErrorOccurred}");

        // === SEKCJA 12: Finalizacja ===
        report?.Invoke("Section 12: Finalization");
        if (criticalErrorOccurred)
        {
            report?.Invoke("ERROR: Critical error occurred - returning false");
            return false;
        }

        currentModel.GenerationConfig.IsAsync = isMachineAsyncMode ?? false;
        report?.Invoke($"Final IsAsync: {currentModel.GenerationConfig.IsAsync}");

        model = currentModel;
        report?.Invoke("=== SUCCESS: TryParse completed successfully ===");
        return true;
    }


    private void ParseMemberAttributes(INamedTypeSymbol classSymbol, StateMachineModel model,
        INamedTypeSymbol stateTypeSymbol, INamedTypeSymbol triggerTypeSymbol,
        ref bool criticalErrorOccurred,
        ref bool? isMachineAsyncMode)
    {
        ParsePayloadTypeAttributes(classSymbol, model, ref criticalErrorOccurred);
        ParseTransitionAttributes(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode);
        ParseInternalTransitionAttributes(classSymbol, model, stateTypeSymbol, triggerTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode);
        ParseStateAttributes(classSymbol, model, stateTypeSymbol, ref criticalErrorOccurred, ref isMachineAsyncMode);
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
                    if (namedArg is { Key: GuardCallbackType, Value.Value: string guardMethodName })
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, guardMethodName, GuardCallbackType,
                            attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                            out bool guardIsAsync, out var guardExpectsPayload,
                            model.GenerationConfig.HasPayload, transition.ExpectedPayloadType))
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

                            // Analyze full signature
                            AnalyzeAndSetCallbackSignature(
                                classSymbolContainingMethods,
                                guardMethodName,
                                "Guard",
                                sig => transition.GuardSignature = sig
                            );
                        }
                    }
                    if (namedArg is { Key: ActionCallbackType, Value.Value: string actionMethodName })
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, actionMethodName, ActionCallbackType,
                            attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                            out bool actionIsAsync, out var actionExpectsPayload,
                            model.GenerationConfig.HasPayload, transition.ExpectedPayloadType))
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

                            // Analyze full signature
                            AnalyzeAndSetCallbackSignature(
                                classSymbolContainingMethods,
                                actionMethodName,
                                "Action",
                                sig => transition.ActionSignature = sig
                            );
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

                if (attrData.ConstructorArguments.Length < 3 || !(attrData.ConstructorArguments[2].Value is string actionMethodNameFromCtor))
                {
                    criticalErrorOccurred = true;
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

                var transition = new TransitionModel { FromState = state, Trigger = trigger, ToState = state };

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
                    model.GenerationConfig.HasPayload, transition.ExpectedPayloadType);

                if (actionValid)
                {
                    transition.ActionMethod = actionMethodNameFromCtor;
                    transition.ActionIsAsync = actionIsAsync;
                    transition.ActionExpectsPayload = actionExpectsPayload;

                    var parameterlessAction = classSymbolContainingMethods
                        .GetMembers(actionMethodNameFromCtor).OfType<IMethodSymbol>()
                        .FirstOrDefault(m => m.Parameters.IsEmpty);
                    transition.ActionHasParameterlessOverload = parameterlessAction != null;

                    // Analyze full signature
                    AnalyzeAndSetCallbackSignature(
                        classSymbolContainingMethods,
                        actionMethodNameFromCtor,
                        "Action",
                        sig => transition.ActionSignature = sig
                    );
                }

                bool guardValid = true;
                foreach (var namedArg in attrData.NamedArguments)
                {
                    if (namedArg.Key == GuardCallbackType && namedArg.Value.Value is string guardMethodName)
                    {
                        if (!ValidateCallbackMethodSignature(classSymbolContainingMethods, guardMethodName, GuardCallbackType,
                            attrData, ref criticalErrorOccurred, ref isMachineAsyncMode,
                            out bool guardIsAsync, out var guardExpectsPayload,
                            model.GenerationConfig.HasPayload, transition.ExpectedPayloadType))
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

                            // Analyze full signature
                            AnalyzeAndSetCallbackSignature(
                                classSymbolContainingMethods,
                                guardMethodName,
                                "Guard",
                                sig => transition.GuardSignature = sig
                            );
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

                            // Analyze full signature
                            AnalyzeAndSetCallbackSignature(
                                classSymbolContainingMethods,
                                onEntryMethodName,
                                "OnEntry",
                                sig => stateModel.OnEntrySignature = sig
                            );
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

                            // Analyze full signature
                            AnalyzeAndSetCallbackSignature(
                                classSymbolContainingMethods,
                                onExitMethodName,
                                "OnExit",
                                sig => stateModel.OnExitSignature = sig
                            );
                        }
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
            return false; // safeguard
        }

        if (matching.Parameters.Length == 1)
        {
            expectsPayload = true;
        }

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
        if (signatureInfo.IsInvalidAsyncVoid)
        {
            var result = ValidationResult.Fail(
                RuleIdentifiers.InvalidAsyncVoid,
                string.Format(DefinedRules.InvalidAsyncVoid.MessageFormat, methodName),
                RuleSeverity.Warning);

            if (DiagnosticFactory.TryCreateDiagnostic(result, loc, out var diag))
            {
                context.ReportDiagnostic(diag);
            }
            // Ostrzeżenie – nie przerywamy
        }

        if (callbackType == GuardCallbackType && signatureInfo.IsInvalidGuardTask)
        {
            var result = ValidationResult.Fail(
                RuleIdentifiers.InvalidGuardTaskReturnType,
                string.Format(DefinedRules.InvalidGuardTaskReturnType.MessageFormat, methodName),
                RuleSeverity.Error);

            if (DiagnosticFactory.TryCreateDiagnostic(result, loc, out var diag))
            {
                context.ReportDiagnostic(diag);
            }

            criticalErrorOccurred = true;
            return false; // błąd krytyczny
        }

        // ---------------------------------------------------------------------
        // 6. Ogólna poprawność sygnatury (FSM003)
        // ---------------------------------------------------------------------
        bool isReturnTypeCorrect = (callbackType == GuardCallbackType && signatureInfo.IsBoolEquivalent) ||
                                   (callbackType != GuardCallbackType && signatureInfo.IsVoidEquivalent);

        // Prosta walidacja parametrów – >1 param = błąd
        bool hasTooManyParams = matching.Parameters.Length > 1;

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

    private void AnalyzeAndSetCallbackSignature(
        INamedTypeSymbol classSymbol,
        string callbackName,
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

        var signature = _callbackAnalyzer.AnalyzeCallback(
            classSymbol,
            callbackName,
            callbackType,
            compilation);

        setSigAction(signature);
    }

    // Add this field to your parser class:
    private CallbackSignatureAnalyzer _callbackAnalyzer;

}