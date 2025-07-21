using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Infrastructure;
using Generator.Log;
using Generator.Model;
using static Generator.Strings;

namespace Generator.SourceGenerators;

public abstract class StateMachineCodeGenerator(StateMachineModel model)
{
    protected readonly StateMachineModel Model = model;
    protected IndentedStringBuilder.IndentedStringBuilder Sb = new();
    protected readonly TypeSystemHelper TypeHelper = new();
    protected HashSet<string> AddedUsings = [];
    protected readonly bool IsAsyncMachine = model.GenerationConfig.IsAsync;

    protected bool ShouldGenerateLogging => Model.GenerateLogging;

    // Hook variable names
    protected const string HookVarContext = "smCtx";
    protected const string EndOfTryFireLabel = "END_TRY_FIRE";

    public virtual string Generate()
    {
        WriteHeader();
        WriteNamespaceAndClass();
        return Sb.ToString();
    }

    #region Common Implementation Methods

    protected virtual void WriteInitialOnEntryDispatch(string stateTypeForUsage)
    {
        Sb.AppendLine(InitialOnEntryComment);
        using (Sb.Block("switch (initialState)"))
        {
            foreach (var stateEntry in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
            {
                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                    WriteLogStatement("Debug",
                        $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
                    Sb.AppendLine("break;");
                }
            }
        }
    }

    protected void WriteTryFireStructure(
        string stateTypeForUsage,
        string triggerTypeForUsage,
        Action<TransitionModel, string, string> writeTransitionLogic)
    {
        var grouped = Model.Transitions.GroupBy(t => t.FromState);

        // switch (CurrentState)
        using (Sb.Block($"switch ({CurrentStateField})"))
        {
            foreach (var state in grouped)
            {
                // case <State>:
                using (Sb.Block(
                           $"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Key)}:"))
                {
                    // switch (trigger)
                    using (Sb.Block("switch (trigger)"))
                    {
                        foreach (var tr in state)
                        {
                            // case <Trigger>:
                            using (Sb.Block(
                                       $"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(tr.Trigger)}:"))
                            {
                                // Właściwa logika przejścia
                                writeTransitionLogic(tr, stateTypeForUsage, triggerTypeForUsage);
                            }
                        }

                        Sb.AppendLine("default: break;");
                    }

                    // break;  ── kończy zewnętrzne switch (CurrentState)
                    Sb.AppendLine("break;");
                }
            }

            Sb.AppendLine("default: break;");
        }

        Sb.AppendLine();

        // Hook – przejście nie znalezione
        WriteTransitionFailureHook(stateTypeForUsage, triggerTypeForUsage);
    }



    protected virtual void WriteTransitionLogic(
     TransitionModel transition,
     string stateTypeForUsage,
     string triggerTypeForUsage)
    {
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard check
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            WriteCallbackInvocation(fromStateDef.OnExitMethod, fromStateDef.OnExitIsAsync);
            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
        }

        // Action
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // OnEntry  
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            WriteCallbackInvocation(toStateDef.OnEntryMethod, toStateDef.OnEntryIsAsync);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
        }

        // State change
        if (!transition.IsInternal)
        {
            Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        Sb.AppendLine($"goto {EndOfTryFireLabel};");
    }

    #endregion

    #region Template Method Hooks

    protected virtual void WriteBeforeTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    protected virtual void WriteGuardEvaluationHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    protected virtual void WriteAfterGuardEvaluatedHook(
        TransitionModel transition,
        string guardResultVar,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    protected virtual void WriteAfterTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage,
        bool success)
    { }

    protected virtual void WriteTransitionFailureHook(
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    #endregion

    #region Virtual Methods for Customization

    protected virtual bool ShouldGenerateInitialOnEntry() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual bool ShouldGenerateOnEntryExit() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual void WriteGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Owijamy całą logikę guard w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            Sb.AddProperty($"bool {GuardResultVar}", $"{transition.GuardMethod}()");

            // Hook: After guard evaluated
            WriteAfterGuardEvaluatedHook(transition, GuardResultVar, stateTypeForUsage, triggerTypeForUsage);

            using (Sb.Block($"if (!{GuardResultVar})"))
            {
                WriteLogStatement("Warning",
                    $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteLogStatement("Warning",
                    $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

                Sb.AppendLine($"{SuccessVar} = false;");

                // Hook: After failed transition
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);

                Sb.AppendLine($"goto {EndOfTryFireLabel};");
            }
        }
        Sb.AppendLine("catch (Exception ex)");
        using (Sb.Block(""))
        {
            // Traktujemy wyjątek w guard jako false (guard nie przeszedł)
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

            Sb.AppendLine($"{SuccessVar} = false;");

            // Skok do końca metody
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }
    protected virtual void WriteActionCall(TransitionModel transition)
    {
        if (string.IsNullOrEmpty(transition.ActionMethod)) return;

        // Owijamy wywołanie akcji w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            // Istniejący kod wywołania akcji
            Sb.AppendLine($"{transition.ActionMethod}();");
        }
        Sb.AppendLine("catch (Exception ex)");
        using (Sb.Block(""))
        {
            // Ustawiamy success na false
            Sb.AppendLine($"{SuccessVar} = false;");

            // Logowanie (jeśli włączone)
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

            // Skok do końca metody
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }

    protected virtual void WriteOnEntryCall(StateModel state, string? expectedPayloadType)
    {
        if (string.IsNullOrEmpty(state.OnEntryMethod)) return;

        // Owijamy wywołanie OnEntry w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            // Istniejący kod wywołania OnEntry
            if (expectedPayloadType == null || !state.OnEntryExpectsPayload)
            {
                Sb.AppendLine($"{state.OnEntryMethod}();");
            }
            else
            {
                var payloadTypeForUsage = GetTypeNameForUsage(expectedPayloadType);

                if (state.OnEntryHasParameterlessOverload)
                {
                    using (Sb.Block($"if ({PayloadVar} is {payloadTypeForUsage} typedPayload)"))
                    {
                        Sb.AppendLine($"{state.OnEntryMethod}(typedPayload);");
                    }
                    Sb.AppendLine("else");
                    using (Sb.Indent())
                    {
                        Sb.AppendLine($"{state.OnEntryMethod}();");
                    }
                }
                else
                {
                    using (Sb.Block($"if ({PayloadVar} is {payloadTypeForUsage} typedPayload)"))
                    {
                        Sb.AppendLine($"{state.OnEntryMethod}(typedPayload);");
                    }
                }
            }
        }
        Sb.AppendLine("catch (Exception ex)");
        using (Sb.Block(""))
        {
            // Ustawiamy success na false i skaczemy do końca
            Sb.AppendLine($"{SuccessVar} = false;");
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }


    protected virtual void WriteOnExitCall(StateModel fromState, string? expectedPayloadType)
    {
        if (string.IsNullOrEmpty(fromState.OnExitMethod)) return;

        // Owijamy wywołanie OnExit w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            // Istniejący kod wywołania OnExit
            if (expectedPayloadType == null || !fromState.OnExitExpectsPayload)
            {
                Sb.AppendLine($"{fromState.OnExitMethod}();");
            }
            else
            {
                var payloadTypeForUsage = GetTypeNameForUsage(expectedPayloadType);

                if (fromState.OnExitHasParameterlessOverload)
                {
                    using (Sb.Block($"if ({PayloadVar} is {payloadTypeForUsage} typedPayload)"))
                    {
                        Sb.AppendLine($"{fromState.OnExitMethod}(typedPayload);");
                    }

                    Sb.AppendLine("else");
                    using (Sb.Indent())
                    {
                        Sb.AppendLine($"{fromState.OnExitMethod}();");
                    }
                }
                else
                {
                    using (Sb.Block($"if ({PayloadVar} is {payloadTypeForUsage} typedPayload)"))
                    {
                        Sb.AppendLine($"{fromState.OnExitMethod}(typedPayload);");
                    }
                }
            }
        }

        Sb.AppendLine("catch (Exception ex)");
        using (Sb.Block(""))
        {
            // Ustawiamy success na false i skaczemy do końca
            Sb.AppendLine($"{SuccessVar} = false;");
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }

    #endregion

    #region Helper Methods

    protected void WriteMethodAttribute() =>
        Sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

    protected bool IsPayloadVariant() =>
        Model.Variant is GenerationVariant.WithPayload or GenerationVariant.Full;

    protected bool IsSinglePayloadVariant() =>
        IsPayloadVariant() && !Model.TriggerPayloadTypes.Any();

    protected bool IsMultiPayloadVariant() =>
        IsPayloadVariant() && Model.TriggerPayloadTypes.Any();

    protected bool IsExtensionsVariant() =>
        Model.Variant is GenerationVariant.WithExtensions or GenerationVariant.Full;

    protected string? GetSinglePayloadType()
    {
        if (IsSinglePayloadVariant() && Model.DefaultPayloadType != null)
        {
            return Model.DefaultPayloadType;
        }
        return null;
    }

    protected HashSet<string> GetAllPayloadTypes()
    {
        var types = new HashSet<string>();

        if (Model.DefaultPayloadType != null)
        {
            types.Add(Model.DefaultPayloadType);
        }

        foreach (var payloadType in Model.TriggerPayloadTypes.Values)
        {
            types.Add(payloadType);
        }

        return types;
    }

    protected List<string> BuildConstructorParameters(string stateTypeForUsage, params string[] extras)
    {
        var parameters = new List<string> { $"{stateTypeForUsage} initialState" };
        parameters.AddRange(extras.Where(e => !string.IsNullOrWhiteSpace(e)));
        return parameters;
    }


    #endregion

    #region Header Generation

    protected virtual void WriteHeader()
    {
        Sb.AppendLine("// <auto-generated/>");
        Sb.AppendLine("#nullable enable");

        // Standard usings
        AddUsing(NamespaceSystem);
        AddUsing(NamespaceSystemCollectionsGeneric);
        AddUsing(NamespaceSystemLinq);
        AddUsing(NamespaceSystemRuntimeCompilerServices);
        AddUsing(NamespaceStateMachineContracts);
        AddUsing(NamespaceStateMachineRuntime);

        if (IsExtensionsVariant())
        {
            AddUsing(NamespaceStateMachineRuntimeExtensions);
        }

        if (ShouldGenerateLogging)
        {
            AddUsing(NamespaceMicrosoftExtensionsLogging);
        }
        if (IsAsyncMachine)
        {
            AddUsing("System.Threading");
            AddUsing("System.Threading.Tasks");
            AddUsing("StateMachine.Exceptions");
        }
        // Type-specific namespaces
        var allNamespaces = new HashSet<string>();
        allNamespaces.UnionWith(TypeHelper.GetRequiredNamespaces(Model.StateType));
        allNamespaces.UnionWith(TypeHelper.GetRequiredNamespaces(Model.TriggerType));

        if (IsPayloadVariant())
        {
            foreach (var payload in GetAllPayloadTypes())
            {
                allNamespaces.UnionWith(TypeHelper.GetRequiredNamespaces(payload));
            }
        }

        // Filter out standard namespaces and add remaining
        foreach (var ns in allNamespaces.OrderBy(n => n))
        {
            if (ns is NamespaceSystem or NamespaceSystemCollectionsGeneric or
                NamespaceSystemLinq or NamespaceSystemRuntimeCompilerServices or
                NamespaceStateMachineContracts or NamespaceStateMachineRuntime)
                continue;

            AddUsing(ns);
        }

        // Hook for additional usings
        foreach (var ns in GetAdditionalUsings().OrderBy(n => n))
        {
            AddUsing(ns);
        }

        Sb.AppendLine();
    }

    protected virtual IEnumerable<string> GetAdditionalUsings() =>
        [];

    #endregion

    #region Type Name Handling

    protected string GetTypeNameForUsage(string fullyQualifiedName) =>
        TypeHelper.FormatTypeForUsage(fullyQualifiedName, useGlobalPrefix: false);


    #endregion

    #region Common Methods

    protected virtual void WriteCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine($"/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine($"/// <returns>True if the trigger can be fired, false otherwise</returns>");
        WriteMethodAttribute();
        using (Sb.Block($"public override bool CanFire({triggerTypeForUsage} trigger)"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);

                foreach (var stateName in allHandledFromStates)
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                    using (Sb.Indent())
                    {
                        using (Sb.Block("switch (trigger)"))
                        {
                            var transitionsFromThisState = Model.Transitions
                                .Where(t => t.FromState == stateName);

                            foreach (var transition in transitionsFromThisState)
                            {
                                Sb.AppendLine($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                                using (Sb.Indent())
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        // Generate guard call with exception handling
                                        WriteGuardCall(transition, "guardResult", "null", throwOnException: false);
                                        Sb.AppendLine("return guardResult;");
                                    }
                                    else
                                    {
                                        Sb.AppendLine("return true;");
                                    }
                                }
                            }
                            Sb.AppendLine("default: return false;");
                        }
                    }
                }
                Sb.AppendLine("default: return false;");
            }
        }
        Sb.AppendLine();
    }

    protected virtual void WriteGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine($"/// <returns>List of triggers that can be fired in the current state</returns>");
        using (Sb.Block($"public override {ReadOnlyListType}<{triggerTypeForUsage}> GetPermittedTriggers()"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
                {
                    var stateName = stateGroup.Key;
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                    using (Sb.Block(""))
                    {
                        // Check if any transition has a guard
                        var hasGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod));

                        if (!hasGuards)
                        {
                            // No guards - return static array
                            var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                            if (triggers.Any())
                            {
                                var triggerList = string.Join(", ", triggers.Select(t => $"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(t)}"));
                                Sb.AppendLine($"return new {triggerTypeForUsage}[] {{ {triggerList} }};");
                            }
                            else
                            {
                                Sb.AppendLine($"return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                            }
                        }
                        else
                        {
                            // Has guards - build list dynamically
                            Sb.AppendLine($"var permitted = new List<{triggerTypeForUsage}>();");

                            foreach (var transition in stateGroup)
                            {
                                if (!string.IsNullOrEmpty(transition.GuardMethod))
                                {
                                    WriteGuardCall(transition, "canFire", "null", throwOnException: false);
                                    using (Sb.Block("if (canFire)"))
                                    {
                                        Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                    }
                                }
                                else
                                {
                                    Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                }
                            }

                            Sb.AppendLine("return permitted.Count == 0 ? ");
                            using (Sb.Indent())
                            {
                                Sb.AppendLine($"{ArrayEmptyMethod}<{triggerTypeForUsage}>() :");
                                Sb.AppendLine("permitted.ToArray();");
                            }
                        }
                    }
                }

                var statesWithNoOutgoingTransitions = Model.States.Keys
                    .Except(transitionsByFromState.Select(g => g.Key))
                    .OrderBy(s => s);

                foreach (var stateName in statesWithNoOutgoingTransitions)
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                }

                Sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
            }
        }
        Sb.AppendLine();
    }

    /// <summary>
    /// Writes structural API methods if enabled
    /// </summary>
    protected void WriteStructuralApiMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (!Model.EmitStructuralHelpers)
            return;

        WriteHasTransitionMethod(stateTypeForUsage, triggerTypeForUsage);
        WriteGetDefinedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
    }

    /// <summary>
    /// Writes HasTransition method for structural analysis
    /// </summary>
    protected void WriteHasTransitionMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Checks if a transition is defined in the state machine structure (ignores guards)");
        Sb.AppendLine($"/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine($"/// <returns>True if a transition is defined for the trigger in current state, false otherwise</returns>");
        WriteMethodAttribute();
        using (Sb.Block($"public bool HasTransition({triggerTypeForUsage} trigger)"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
                {
                    var stateName = stateGroup.Key;
                    var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();

                    if (triggers.Any())
                    {
                        Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                        using (Sb.Indent())
                        {
                            using (Sb.Block("switch (trigger)"))
                            {
                                foreach (var trigger in triggers)
                                {
                                    Sb.AppendLine($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(trigger)}: return true;");
                                }
                                Sb.AppendLine("default: return false;");
                            }
                        }
                    }
                }

                Sb.AppendLine("default: return false;");
            }
        }
        Sb.AppendLine();
    }

    /// <summary>
    /// Writes GetDefinedTriggers method for structural analysis
    /// </summary>
    protected void WriteGetDefinedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Gets all triggers defined for the current state in the state machine structure (ignores guards)");
        Sb.AppendLine($"/// <returns>List of all triggers defined for the current state, regardless of guard conditions</returns>");
        using (Sb.Block($"public {ReadOnlyListType}<{triggerTypeForUsage}> GetDefinedTriggers()"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
                {
                    var stateName = stateGroup.Key;
                    var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();

                    Sb.Append($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}: return ");
                    if (triggers.Any())
                    {
                        var triggerList = string.Join(", ", triggers.Select(t => $"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(t)}"));
                        Sb.AppendLine($"new {triggerTypeForUsage}[] {{ {triggerList} }};");
                    }
                    else
                    {
                        Sb.AppendLine($"{ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                    }
                }

                var statesWithNoOutgoingTransitions = Model.States.Keys
                    .Except(transitionsByFromState.Select(g => g.Key))
                    .OrderBy(s => s);

                foreach (var stateName in statesWithNoOutgoingTransitions)
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                }

                Sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
            }
        }
        Sb.AppendLine();
    }


    protected void WriteLoggerField(string className)
    {
        if (!ShouldGenerateLogging) return;
        LoggingClassGenerator.WriteLoggerField(className, ref Sb);
    }

    protected string GetLoggerConstructorParameter(string className) =>

    ShouldGenerateLogging ? LoggingClassGenerator.GetLoggerConstructorParameter(className, ref Sb) : string.Empty;
    protected void WriteLoggerAssignment()
    {
        if (!ShouldGenerateLogging) return;
        LoggingClassGenerator.WriteLoggerAssignment(ref Sb);
    }

    protected void WriteLogStatement(string logLevel, string logMethodCall)
    {
        if (!ShouldGenerateLogging) return;
        LoggingClassGenerator.WriteLogStatement(Model.ClassName, logLevel, logMethodCall, ref Sb);
    }

    protected void AddUsing(string usingStatement)
    {
        if (AddedUsings.Add(usingStatement))
        {
            Sb.AppendLine($"using {usingStatement};");
        }
    }

    #endregion

    protected abstract void WriteNamespaceAndClass();


    /// <summary>
    /// Generates code to call a guard method with proper exception handling
    /// </summary>
    protected void WriteGuardCall(
        TransitionModel transition,
        string resultVar,
        string payloadVar = "null",
        bool throwOnException = false)
    {
        // 1. Brak guarda → zawsze true
        if (string.IsNullOrEmpty(transition.GuardMethod))
        {
            Sb.AppendLine($"bool {resultVar} = true;");
            return;
        }

        // 2. Guard Z PARAMETREM, ale metoda, do której wstawiamy kod,
        //    NIE dysponuje zmienną payloadu („null” przekazane w argumencie)
        if (transition.GuardExpectsPayload && payloadVar == "null")
        {
            Sb.AppendLine($"bool {resultVar} = false; // brak payloadu → guard = false");
            return;
        }

        // 3. Standardowy przypadek – payloadVar to zmienna
        var needsTryCatch = !throwOnException;      // CanFire/GetPermittedTriggers – zbijamy wyjątki
        var payloadExpr = transition.GuardExpectsPayload
            ? $"{payloadVar} is {GetTypeNameForUsage(transition.ExpectedPayloadType!)} typedPayload && " +
              $"{transition.GuardMethod}(typedPayload)"
            : $"{transition.GuardMethod}()";

        if (needsTryCatch)
        {
            Sb.AppendLine($"bool {resultVar};");
            Sb.AppendLine("try");
            using (Sb.Block(""))
            {
                Sb.AppendLine($"{resultVar} = {payloadExpr};");
            }
            Sb.AppendLine("catch");
            using (Sb.Block(""))
            {
                Sb.AppendLine($"{resultVar} = false;");      // guard rzucił – traktuj jako false
            }
        }
        else
        {
            Sb.AppendLine($"bool {resultVar} = {payloadExpr};");
        }
    }
    // Helper do generowania sygnatur metod
    protected string GetMethodReturnType(string syncReturnType)
    {
        if (!IsAsyncMachine) return syncReturnType;

        return syncReturnType switch
        {
            "void" => "Task",
            "bool" => "ValueTask<bool>",
            _ => throw new InvalidOperationException($"Unsupported return type for async: {syncReturnType}")
        };
    }

    // Helper do słowa kluczowego async
    protected string GetAsyncKeyword() => IsAsyncMachine ? "async " : "";

    // Helper do await
    protected string GetAwaitKeyword() => IsAsyncMachine ? "await " : "";

    // Helper do ConfigureAwait
    protected string GetConfigureAwait() => IsAsyncMachine ? ".ConfigureAwait(_continueOnCapturedContext)" : "";

    // Helper do nazwy metody (TryFire vs TryFireInternalAsync)
    protected string GetTryFireMethodName() => IsAsyncMachine ? "TryFireInternalAsync" : "TryFire";

    // Helper do parametrów metody
    protected string GetTryFireParameters(string triggerType)
    {
        return IsAsyncMachine
            ? $"{triggerType} trigger, object? payload, CancellationToken cancellationToken"
            : $"{triggerType} trigger, object? payload = null";
    }

    // Helper do wywołania callback
    protected void WriteCallbackInvocation(string methodName, bool isCallbackAsync, string? payload = null)
    {
        if (isCallbackAsync && IsAsyncMachine)
        {
            Sb.Append($"{GetAwaitKeyword()}{methodName}(");
            if (payload != null) Sb.Append(payload);
            Sb.Append($"){GetConfigureAwait()}");
        }
        else
        {
            Sb.Append($"{methodName}(");
            if (payload != null) Sb.Append(payload);
            Sb.Append(")");
        }
        Sb.AppendLine(";");
    }

    // Helper do klasy bazowej
    protected string GetBaseClassName(string stateType, string triggerType)
    {
        return IsAsyncMachine
            ? $"AsyncStateMachineBase<{stateType}, {triggerType}>"
            : $"StateMachineBase<{stateType}, {triggerType}>";
    }

    // Helper do interfejsu
    protected string GetInterfaceName(string stateType, string triggerType)
    {
        return IsAsyncMachine
            ? $"IAsyncStateMachine<{stateType}, {triggerType}>"
            : $"IStateMachine<{stateType}, {triggerType}>";
    }
    /// <summary>
    /// Zwraca nazwę metody z odpowiednim sufiksem.
    /// </summary>
    protected string GetMethodName(string baseName, bool addAsyncSuffix = true)
    {
        if (!IsAsyncMachine || !addAsyncSuffix) return baseName;

        // Sprawdź czy nazwa już kończy się na "Async"
        if (baseName.EndsWith("Async", StringComparison.Ordinal))
            return baseName;

        return baseName + "Async";
    }


   

    /// <summary>
    /// Zwraca visibility dla metody TryFire.
    /// </summary>
    protected string GetTryFireVisibility()
    {
        return IsAsyncMachine ? "protected override" : "public override";
    }

}