using System.Linq;
using Generator.Model;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Generuje kod dla wariantów z payloadem.
/// </summary>
internal class PayloadVariantGenerator(StateMachineModel model) : StateMachineCodeGenerator(model)
{
    protected override void WriteNamespaceAndClass()
    {
        var stateTypeForUsage = GetTypeNameForUsage(Model.StateType);
        var triggerTypeForUsage = GetTypeNameForUsage(Model.TriggerType);
        var userNamespace = Model.Namespace;
        var className = Model.ClassName;

        void WriteClassContent()
        {
            // Generowanie interfejsu
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                Sb.AppendLine($"public interface I{className} : IStateMachineWithPayload<{stateTypeForUsage}, {triggerTypeForUsage}, {payloadType}> {{ }}");
            }
            else
            {
                Sb.AppendLine($"public interface I{className} : IStateMachineWithMultiPayload<{stateTypeForUsage}, {triggerTypeForUsage}> {{ }}");
            }

            Sb.AppendLine();

            // Generowanie klasy
            using (Sb.Block($"public partial class {className} : StateMachineBase<{stateTypeForUsage}, {triggerTypeForUsage}>, I{className}"))
            {
                WriteLoggerField(className);

                if (IsMultiPayloadVariant())
                {
                    WritePayloadMap(triggerTypeForUsage);
                }

                WriteConstructor(stateTypeForUsage, className);
                WriteTryFireMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteFireMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteCanFireMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteGetPermittedTriggersWithResolver(stateTypeForUsage, triggerTypeForUsage);
            }
        }

        if (!string.IsNullOrEmpty(userNamespace))
        {
            using (Sb.Block($"namespace {userNamespace}"))
            {
                WriteClassContent();
            }
        }
        else
        {
            WriteClassContent();
        }
    }

    protected virtual void WriteConstructor(string stateTypeForUsage, string className)
    {
        var paramList = BuildConstructorParameters(stateTypeForUsage, GetLoggerConstructorParameter(className));

        using (Sb.Block($"public {className}({string.Join(", ", paramList)}) : base(initialState)"))
        {
            WriteLoggerAssignment();

            if (ShouldGenerateInitialOnEntry())
            {
                Sb.AppendLine();
                WriteInitialOnEntryDispatch(stateTypeForUsage);
            }
        }
        Sb.AppendLine();
    }

    protected override void WriteInitialOnEntryDispatch(string stateTypeForUsage)
    {
        Sb.AppendLine(InitialOnEntryComment);
        using (Sb.Block("switch (initialState)"))
        {
            foreach (var stateEntry in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload))
            {
                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                    Sb.AppendLine("break;");
                }
            }
        }
    }

    protected void WritePayloadMap(string triggerTypeForUsage)
    {
        using (Sb.Block($"private static readonly Dictionary<{triggerTypeForUsage}, Type> {PayloadMapField} = new()"))
        {
            foreach (var kvp in Model.TriggerPayloadTypes)
            {
                var triggerName = kvp.Key;
                var payloadTypeName = kvp.Value;
                var typeForTypeof = TypeHelper.FormatForTypeof(payloadTypeName);
                Sb.AppendLine($"{{ {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(triggerName)}, typeof({typeForTypeof}) }},");
            }
        }
        Sb.AppendLine(";");
        Sb.AppendLine();
    }

    protected virtual void WriteTryFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // 1. Internal method with logic
        WriteTryFireInternal(stateTypeForUsage, triggerTypeForUsage);

        // 2. Public typed overloads
        if (IsSinglePayloadVariant())
        {
            var single = GetSinglePayloadType();
            if (single is not null)
            {
                var payloadType = GetTypeNameForUsage(single);
                // Skip if it's 'object' to avoid duplicate
                if (payloadType != "object")
                {
                    WriteMethodAttribute();
                    using (Sb.Block($"public bool TryFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                    {
                        Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
                    }
                    Sb.AppendLine();
                }
            }
        }
        else if (IsMultiPayloadVariant())
        {
            WriteMethodAttribute();
            using (Sb.Block($"public bool TryFire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
            {
                Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
            }
            Sb.AppendLine();
        }

        // 3. Override with object?
        WriteMethodAttribute();
        using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
        {
            Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
        }
        Sb.AppendLine();
    }

    private void WriteTryFireInternal(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private bool TryFireInternal({triggerTypeForUsage} trigger, object? {PayloadVar})"))
        {
            if (!Model.Transitions.Any())
            {
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            Sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
            Sb.AppendLine($"bool {SuccessVar} = false;");
            Sb.AppendLine();

            // Payload validation for multi-payload variant
            if (IsMultiPayloadVariant())
            {
                Sb.AppendLine("// Payload-type validation for multi-payload variant");
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"({PayloadVar} == null || !expectedType.IsInstanceOfType({PayloadVar})))"))
                {
                    WriteLogStatement("Warning",
                        $"PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", {PayloadVar}?.GetType().Name ?? \"null\");");
                    Sb.AppendLine("return false; // wrong payload type");
                }
                Sb.AppendLine();
            }

            WriteTryFireStructure(
                stateTypeForUsage,
                triggerTypeForUsage,
                WriteTransitionLogic);

            Sb.AppendLine($"{EndOfTryFireLabel}:;");
            Sb.AppendLine($"return {SuccessVar};");
        }
        Sb.AppendLine();
    }

    protected override void WriteGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Owijamy całą logikę guard w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            if (transition is { GuardExpectsPayload: true, GuardHasParameterlessOverload: true })
            {
                var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                Sb.AppendLine($"bool {GuardResultVar};");
                using (Sb.Block($"if ({PayloadVar} is {payloadType} typedGuardPayload)"))
                {
                    Sb.AppendLine($"{GuardResultVar} = {transition.GuardMethod}(typedGuardPayload);");
                }
                Sb.AppendLine("else");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"{GuardResultVar} = {transition.GuardMethod}();");
                }
            }
            else if (transition.GuardExpectsPayload)
            {
                var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                Sb.AppendLine($"bool {GuardResultVar} = {PayloadVar} is {payloadType} typedGuardPayload && {transition.GuardMethod}(typedGuardPayload);");
            }
            else if (transition.GuardHasParameterlessOverload)
            {
                Sb.AddProperty($"bool {GuardResultVar}", $"{transition.GuardMethod}()");
            }
            else
            {
                Sb.AddProperty($"bool {GuardResultVar}", "true");
            }

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

            // Hook: After failed transition
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);

            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }
    protected override void WriteActionCall(TransitionModel transition)
    {
        if (string.IsNullOrEmpty(transition.ActionMethod)) return;

        // Owijamy całe wywołanie akcji w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            // Istniejąca logika z payloadami
            if (transition is { ActionExpectsPayload: true, ActionHasParameterlessOverload: true })
            {
                var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                using (Sb.Block($"if ({PayloadVar} is {payloadType} typedActionPayload)"))
                {
                    Sb.AppendLine($"{transition.ActionMethod}(typedActionPayload);");
                }
                Sb.AppendLine("else");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"{transition.ActionMethod}();");
                }
            }
            else if (transition.ActionExpectsPayload)
            {
                var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                using (Sb.Block($"if ({PayloadVar} is {payloadType} typedActionPayload)"))
                {
                    Sb.AppendLine($"{transition.ActionMethod}(typedActionPayload);");
                }
            }
            else if (transition.ActionHasParameterlessOverload)
            {
                Sb.AppendLine($"{transition.ActionMethod}();");
            }
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
    protected override void WriteOnEntryCall(StateModel state, string? expectedPayloadType)
    {
        if (string.IsNullOrEmpty(state.OnEntryMethod)) return;

        var effectiveType = expectedPayloadType ?? Model.DefaultPayloadType;

        // Owijamy całe wywołanie w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            if (!state.OnEntryExpectsPayload || effectiveType == null)
            {
                Sb.AppendLine($"{state.OnEntryMethod}();");
            }
            else
            {
                // Single payload variant
                if (IsSinglePayloadVariant())
                {
                    var payloadType = GetTypeNameForUsage(effectiveType);

                    if (state.OnEntryHasParameterlessOverload)
                    {
                        using (Sb.Block($"if ({PayloadVar} is {payloadType} typedPayload)"))
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
                        using (Sb.Block($"if ({PayloadVar} is {payloadType} typedPayload)"))
                        {
                            Sb.AppendLine($"{state.OnEntryMethod}(typedPayload);");
                        }
                    }
                }
                // Multi payload variant
                else if (IsMultiPayloadVariant())
                {
                    if (expectedPayloadType != null)
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
                    else
                    {
                        if (state.OnEntryHasParameterlessOverload)
                        {
                            Sb.AppendLine($"{state.OnEntryMethod}();");
                        }
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
    protected override void WriteOnExitCall(StateModel fromState, string? expectedPayloadType)
    {
        if (string.IsNullOrEmpty(fromState.OnExitMethod)) return;

        var effectiveType = expectedPayloadType ?? Model.DefaultPayloadType;

        // Owijamy całe wywołanie w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            if (!fromState.OnExitExpectsPayload || effectiveType == null)
            {
                Sb.AppendLine($"{fromState.OnExitMethod}();");
            }
            else
            {
                if (IsSinglePayloadVariant())
                {
                    var payloadType = GetTypeNameForUsage(effectiveType);

                    if (fromState.OnExitHasParameterlessOverload)
                    {
                        using (Sb.Block($"if ({PayloadVar} is {payloadType} typedPayload)"))
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
                        using (Sb.Block($"if ({PayloadVar} is {payloadType} typedPayload)"))
                        {
                            Sb.AppendLine($"{fromState.OnExitMethod}(typedPayload);");
                        }
                    }
                }
                // Multi-payload
                else
                {
                    if (fromState.OnExitHasParameterlessOverload)
                    {
                        using (Sb.Block($"if ({PayloadVar} != null)"))
                        {
                            Sb.AppendLine($"{fromState.OnExitMethod}({PayloadVar});");
                        }
                        Sb.AppendLine("else");
                        using (Sb.Indent())
                        {
                            Sb.AppendLine($"{fromState.OnExitMethod}();");
                        }
                    }
                    else
                    {
                        using (Sb.Block($"if ({PayloadVar} != null)"))
                        {
                            Sb.AppendLine($"{fromState.OnExitMethod}({PayloadVar});");
                        }
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

    protected void WriteFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsSinglePayloadVariant())
        {
            var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
            using (Sb.Block($"public void Fire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
            {
                using (Sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                {
                    Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{{{CurrentStateField}}}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                }
            }
            Sb.AppendLine();
        }
        else if (IsMultiPayloadVariant())
        {
            using (Sb.Block($"public void Fire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
            {
                using (Sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                {
                    Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{{{CurrentStateField}}}}' on trigger '{{trigger}}' with payload of type '{{typeof(TPayload).Name}}'\");");
                }
            }
            Sb.AppendLine();
        }
    }

    protected void WriteCanFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        //------------------------------------------------------------
        // 1.  OBOWIĄZKOWY   override  z  bazowej klasy
        //------------------------------------------------------------
        base.WriteCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
        //  (base.WriteCanFireMethod generuje:
        //   public override bool CanFire({triggerType} trigger) { … })

        //------------------------------------------------------------
        // 2.  DODATKOWE przeciążenia z payloadem  (bez override!)
        //------------------------------------------------------------

        // wspólna prywatna logika:
        WriteCanFireWithPayload(stateTypeForUsage, triggerTypeForUsage);

        // --- single‑payload ---------------------------------------
        if (IsSinglePayloadVariant())
        {
            var single = GetSinglePayloadType();
            if (single is not null)
            {
                var payloadType = GetTypeNameForUsage(single);
                if (payloadType != "object")          // uniknij duplikatu
                {
                    Sb.WriteSummary("Checks if the specified trigger can be fired " +
                                    "with the given payload (runtime evaluation incl. guards)");
                    WriteMethodAttribute();
                    using (Sb.Block($"public bool CanFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                    {
                        Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
                    }
                    Sb.AppendLine();
                }
            }
        }
        // --- multi‑payload ----------------------------------------
        else if (IsMultiPayloadVariant())
        {
            Sb.WriteSummary("Checks if the specified trigger can be fired " +
                            "with the given payload (runtime evaluation incl. guards)");
            WriteMethodAttribute();
            using (Sb.Block($"public bool CanFire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
            {
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"!expectedType.IsInstanceOfType({PayloadVar}))"))
                {
                    Sb.AppendLine("return false;");    // payload nie‑pasuje → false
                }
                Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
            }
            Sb.AppendLine();
        }

        // --- uniwersalne przeciążenie z object? --------------------
        Sb.WriteSummary("Checks if the specified trigger can be fired " +
                        "with an optional payload (runtime evaluation incl. guards)");
        WriteMethodAttribute();
        using (Sb.Block($"public bool CanFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
        {
            Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
        }
        Sb.AppendLine();
    }

    private void WriteCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private bool CanFireWithPayload({triggerTypeForUsage} trigger, object? {PayloadVar})"))
        {
            // Payload validation for multi-payload variant
            if (IsMultiPayloadVariant())
            {
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"{PayloadVar} != null && !expectedType.IsInstanceOfType({PayloadVar}))"))
                {
                    Sb.AppendLine("return false;");
                }
            }

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
                                        WriteGuardCall(transition, "guardResult", PayloadVar, throwOnException: false);
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
    protected void WriteGetPermittedTriggersWithResolver(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // Call base method first
       // base.WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);

        // Add overload with payload resolver
        Sb.WriteSummary("Gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"payloadResolver\">Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.</param>");
        using (Sb.Block($"public {ReadOnlyListType}<{triggerTypeForUsage}> GetPermittedTriggers(Func<{triggerTypeForUsage}, object?> payloadResolver)"))
        {
            Sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
            Sb.AppendLine();

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
                        Sb.AppendLine($"var permitted = new List<{triggerTypeForUsage}>();");

                        foreach (var transition in stateGroup)
                        {
                            if (!string.IsNullOrEmpty(transition.GuardMethod))
                            {
                                if (transition.GuardExpectsPayload)
                                {
                                    // Guard needs payload - use resolver
                                    Sb.AppendLine($"var payload_{transition.Trigger} = payloadResolver({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                    WriteGuardCall(transition, "canFire", $"payload_{transition.Trigger}", throwOnException: false);
                                }
                                else
                                {
                                    // Guard doesn't need payload
                                    WriteGuardCall(transition, "canFire", "null", throwOnException: false);
                                }

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
}