using System.Linq;
using Generator.Model;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Generuje kod dla wariantów z payloadem (sync i async).
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

            // Generowanie klasy z odpowiednią klasą bazową
            var baseClass = GetBaseClassName(stateTypeForUsage, triggerTypeForUsage);

            using (Sb.Block($"public partial class {className} : {baseClass}, I{className}"))
            {
                // Pola dla async
                if (IsAsyncMachine)
                {
                    Sb.AppendLine("private readonly string _instanceId = Guid.NewGuid().ToString();");
                    Sb.AppendLine();
                }

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
                WriteStructuralApiMethods(stateTypeForUsage, triggerTypeForUsage);
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

        // Dla async maszyn przekaż continueOnCapturedContext
        var baseCall = IsAsyncMachine
            ? $"base(initialState, continueOnCapturedContext: {Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()})"
            : "base(initialState)";

        using (Sb.Block($"public {className}({string.Join(", ", paramList)}) : {baseCall}"))
        {
            WriteLoggerAssignment();

            if (ShouldGenerateInitialOnEntry())
            {
                Sb.AppendLine();
                if (IsAsyncMachine)
                    WriteAsyncInitialOnEntryDispatch(stateTypeForUsage);
                else
                    WriteInitialOnEntryDispatch(stateTypeForUsage);
            }
        }
        Sb.AppendLine();
    }

    private void WriteAsyncInitialOnEntryDispatch(string stateTypeForUsage)
    {
        Sb.AppendLine(InitialOnEntryComment);
        Sb.AppendLine("// Note: Constructor cannot be async, so initial OnEntry is fire-and-forget");

        // Sprawdź czy jakikolwiek stan ma bezparametrową OnEntry
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();

        if (statesWithParameterlessOnEntry.Any())
        {
            Sb.AppendLine("_ = Task.Run(async () =>");
            using (Sb.Block("{"))
            {
                using (Sb.Block("switch (initialState)"))
                {
                    foreach (var stateEntry in statesWithParameterlessOnEntry)
                    {
                        Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                        using (Sb.Indent())
                        {
                            WriteCallbackInvocation(stateEntry.OnEntryMethod, stateEntry.OnEntryIsAsync);
                            Sb.AppendLine("break;");
                        }
                    }
                }
            }
            Sb.AppendLine("});");
        }
    }

    protected override void WriteTransitionLogic(
       TransitionModel transition,
       string stateTypeForUsage,
       string triggerTypeForUsage)
    {
        // Pobierz definicje stanów z mapy (jawne przypisanie, żeby uniknąć CS0165)
        StateModel? fromStateDef;
        Model.States.TryGetValue(transition.FromState, out fromStateDef);

        StateModel? toStateDef;
        Model.States.TryGetValue(transition.ToState, out toStateDef);

        bool fromHasExit = !transition.IsInternal
                           && fromStateDef != null
                           && !string.IsNullOrEmpty(fromStateDef.OnExitMethod);

        bool toHasEntry = !transition.IsInternal
                          && toStateDef != null
                          && !string.IsNullOrEmpty(toStateDef.OnEntryMethod);

        // Hook przed przejściem
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // OnExit
        if (fromHasExit)
        {
            WriteOnExitCall(fromStateDef!, transition.ExpectedPayloadType);
            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef!.OnExitMethod}\", \"{transition.FromState}\");");
        }

        // *** KOLEJNOŚĆ: OnEntry -> Action ***
        if (toHasEntry)
        {
            WriteOnEntryCall(toStateDef!, transition.ExpectedPayloadType);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef!.OnEntryMethod}\", \"{transition.ToState}\");");
        }

        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Zmiana stanu
        if (!transition.IsInternal)
        {
            Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        Sb.AppendLine($"{SuccessVar} = true;");
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);
        Sb.AppendLine($"goto {EndOfTryFireLabel};");
    }



    protected override void WriteInitialOnEntryDispatch(string stateTypeForUsage)
    {
        Sb.AppendLine(InitialOnEntryComment);

        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();

        if (statesWithParameterlessOnEntry.Any())
        {
            using (Sb.Block("switch (initialState)"))
            {
                foreach (var stateEntry in statesWithParameterlessOnEntry)
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
        if (IsAsyncMachine)
        {
            // 1. Internal async method with logic
            WriteTryFireInternalAsync(stateTypeForUsage, triggerTypeForUsage);

            // 2. Public typed async overloads
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
                        using (Sb.Block($"public async ValueTask<bool> TryFireAsync({triggerTypeForUsage} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)"))
                        {
                            Sb.AppendLine($"return await TryFireInternalAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                WriteMethodAttribute();
                using (Sb.Block($"public async ValueTask<bool> TryFireAsync<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine($"return await TryFireInternalAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                }
                Sb.AppendLine();
            }

            // 3. Sync methods that throw for async machines (required by interface)
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
                    {
                        WriteMethodAttribute();
                        using (Sb.Block($"public bool TryFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                        {
                            Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
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
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }

            // Override with object? - użyj override bo nadpisujemy metodę virtual z klasy bazowej
            WriteMethodAttribute();
            using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
            {
                Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
            }
            Sb.AppendLine();
        }
        else
        {
            // Sync-only implementation
            WriteTryFireInternal(stateTypeForUsage, triggerTypeForUsage);

            // Public typed overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
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

            // Override with object?
            WriteMethodAttribute();
            using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
            {
                Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
            }
            Sb.AppendLine();
        }
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

            // Logowanie niepowodzenia
            using (Sb.Block($"if (!{SuccessVar})"))
            {
                WriteLogStatement("Warning",
                    $"TransitionFailed(_logger, _instanceId, {OriginalStateVar}.ToString(), trigger.ToString());");
            }

            Sb.AppendLine($"return {SuccessVar};");
        }
        Sb.AppendLine();
    }

    private void WriteTryFireInternalAsync(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"protected override async ValueTask<bool> TryFireInternalAsync({triggerTypeForUsage} trigger, object? {PayloadVar}, CancellationToken cancellationToken)"))
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
            Sb.AppendLine();

            // Logowanie niepowodzenia
            using (Sb.Block($"if (!{SuccessVar})"))
            {
                WriteLogStatement("Warning",
                    $"TransitionFailed(_logger, _instanceId, {OriginalStateVar}.ToString(), trigger.ToString());");
            }

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
                    if (IsAsyncMachine && transition.GuardIsAsync)
                    {
                        Sb.AppendLine($"{GuardResultVar} = {GetAwaitKeyword()}{transition.GuardMethod}(typedGuardPayload){GetConfigureAwait()};");
                    }
                    else
                    {
                        Sb.AppendLine($"{GuardResultVar} = {transition.GuardMethod}(typedGuardPayload);");
                    }
                }
                Sb.AppendLine("else");
                using (Sb.Indent())
                {
                    if (IsAsyncMachine && transition.GuardIsAsync)
                    {
                        Sb.AppendLine($"{GuardResultVar} = {GetAwaitKeyword()}{transition.GuardMethod}(){GetConfigureAwait()};");
                    }
                    else
                    {
                        Sb.AppendLine($"{GuardResultVar} = {transition.GuardMethod}();");
                    }
                }
            }
            else if (transition.GuardExpectsPayload)
            {
                var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                if (IsAsyncMachine && transition.GuardIsAsync)
                {
                    Sb.AppendLine($"bool {GuardResultVar} = {PayloadVar} is {payloadType} typedGuardPayload && {GetAwaitKeyword()}{transition.GuardMethod}(typedGuardPayload){GetConfigureAwait()};");
                }
                else
                {
                    Sb.AppendLine($"bool {GuardResultVar} = {PayloadVar} is {payloadType} typedGuardPayload && {transition.GuardMethod}(typedGuardPayload);");
                }
            }
            else if (transition.GuardHasParameterlessOverload)
            {
                if (IsAsyncMachine && transition.GuardIsAsync)
                {
                    Sb.AppendLine($"bool {GuardResultVar} = {GetAwaitKeyword()}{transition.GuardMethod}(){GetConfigureAwait()};");
                }
                else
                {
                    Sb.AddProperty($"bool {GuardResultVar}", $"{transition.GuardMethod}()");
                }
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
                    WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync, "typedActionPayload");
                }
                Sb.AppendLine("else");
                using (Sb.Indent())
                {
                    WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync);
                }
            }
            else if (transition.ActionExpectsPayload)
            {
                var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                using (Sb.Block($"if ({PayloadVar} is {payloadType} typedActionPayload)"))
                {
                    WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync, "typedActionPayload");
                }
            }
            else if (transition.ActionHasParameterlessOverload)
            {
                WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync);
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
                WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync);
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
                            WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync, "typedPayload");
                        }
                        Sb.AppendLine("else");
                        using (Sb.Indent())
                        {
                            WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync);
                        }
                    }
                    else
                    {
                        using (Sb.Block($"if ({PayloadVar} is {payloadType} typedPayload)"))
                        {
                            WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync, "typedPayload");
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
                                WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync, "typedPayload");
                            }
                            Sb.AppendLine("else");
                            using (Sb.Indent())
                            {
                                WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync);
                            }
                        }
                        else
                        {
                            using (Sb.Block($"if ({PayloadVar} is {payloadTypeForUsage} typedPayload)"))
                            {
                                WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync, "typedPayload");
                            }
                        }
                    }
                    else
                    {
                        if (state.OnEntryHasParameterlessOverload)
                        {
                            WriteCallbackInvocation(state.OnEntryMethod, state.OnEntryIsAsync);
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
                WriteCallbackInvocation(fromState.OnExitMethod, fromState.OnExitIsAsync);
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
                            WriteCallbackInvocation(fromState.OnExitMethod, fromState.OnExitIsAsync, "typedPayload");
                        }
                        Sb.AppendLine("else");
                        using (Sb.Indent())
                        {
                            WriteCallbackInvocation(fromState.OnExitMethod, fromState.OnExitIsAsync);
                        }
                    }
                    else
                    {
                        using (Sb.Block($"if ({PayloadVar} is {payloadType} typedPayload)"))
                        {
                            WriteCallbackInvocation(fromState.OnExitMethod, fromState.OnExitIsAsync, "typedPayload");
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
                            WriteCallbackInvocation(fromState.OnExitMethod, fromState.OnExitIsAsync);
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
        if (IsAsyncMachine)
        {
            // Async Fire methods
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                using (Sb.Block($"public async Task FireAsync({triggerTypeForUsage} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    using (Sb.Block($"if (!await TryFireAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()})"))
                    {
                        Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
                Sb.AppendLine();
            }
            else if (IsMultiPayloadVariant())
            {
                using (Sb.Block($"public async Task FireAsync<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    using (Sb.Block($"if (!await TryFireAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()})"))
                    {
                        Sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
                Sb.AppendLine();
            }

            // Sync Fire methods that throw for async machines
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                using (Sb.Block($"public void Fire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
            else if (IsMultiPayloadVariant())
            {
                using (Sb.Block($"public void Fire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
        }
        else
        {
            // Sync Fire methods
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                using (Sb.Block($"public void Fire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                {
                    using (Sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                    {
                        Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
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
                        Sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
                Sb.AppendLine();
            }
        }
    }

    protected void WriteCanFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            // Async CanFire methods
            WriteAsyncCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
            WriteAsyncCanFireWithPayload(stateTypeForUsage, triggerTypeForUsage);

            // Additional async overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
                    {
                        Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired " +
                                        "with the given payload (runtime evaluation incl. guards)");
                        WriteMethodAttribute();
                        using (Sb.Block($"public async ValueTask<bool> CanFireAsync({triggerTypeForUsage} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)"))
                        {
                            Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired " +
                                "with the given payload (runtime evaluation incl. guards)");
                WriteMethodAttribute();
                using (Sb.Block($"public async ValueTask<bool> CanFireAsync<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    using (Sb.Block(
                        $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                        $"!expectedType.IsInstanceOfType({PayloadVar}))"))
                    {
                        Sb.AppendLine("return false;");
                    }
                    Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                }
                Sb.AppendLine();
            }

            // Universal async overload with object?
            Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired " +
                            "with an optional payload (runtime evaluation incl. guards)");
            WriteMethodAttribute();
            using (Sb.Block($"public async ValueTask<bool> CanFireAsync({triggerTypeForUsage} trigger, object? {PayloadVar} = null, CancellationToken cancellationToken = default)"))
            {
                Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
            }
            Sb.AppendLine();

            // Sync CanFire methods required by interface - NIE używaj override
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    WriteMethodAttribute();
                    using (Sb.Block($"public bool CanFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                    {
                        Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                    }
                    Sb.AppendLine();
                }
            }
        }
        else
        {
            // Sync CanFire methods
            base.WriteCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
            WriteCanFireWithPayload(stateTypeForUsage, triggerTypeForUsage);

            // Additional sync overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
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
                        Sb.AppendLine("return false;");
                    }
                    Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
                }
                Sb.AppendLine();
            }

            // Universal sync overload with object?
            Sb.WriteSummary("Checks if the specified trigger can be fired " +
                            "with an optional payload (runtime evaluation incl. guards)");
            WriteMethodAttribute();
            using (Sb.Block($"public bool CanFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
            {
                Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
            }
            Sb.AppendLine();
        }
    }

    private void WriteAsyncCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        Sb.AppendLine("/// <returns>True if the trigger can be fired, false otherwise</returns>");

        using (Sb.Block($"public override async ValueTask<bool> CanFireAsync({triggerTypeForUsage} trigger, CancellationToken cancellationToken = default)"))
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
                                        if (transition.GuardIsAsync)
                                        {
                                            Sb.AppendLine("try");
                                            using (Sb.Block(""))
                                            {
                                                Sb.AppendLine(transition.GuardHasParameterlessOverload
                                                    ? $"return await {transition.GuardMethod}(){GetConfigureAwait()};"
                                                    : "return false; // Guard expects payload but none provided");
                                            }
                                            Sb.AppendLine("catch");
                                            using (Sb.Block(""))
                                            {
                                                Sb.AppendLine("return false;");
                                            }
                                        }
                                        else
                                        {
                                            WriteGuardCall(transition, "guardResult", "null", throwOnException: false);
                                            Sb.AppendLine("return guardResult;");
                                        }
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

    private void WriteAsyncCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private async ValueTask<bool> CanFireWithPayloadAsync({triggerTypeForUsage} trigger, object? {PayloadVar}, CancellationToken cancellationToken)"))
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
                                        WriteAsyncGuardCallForCanFire(transition, "guardResult", PayloadVar);
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

    private void WriteAsyncGuardCallForCanFire(TransitionModel transition, string resultVar, string payloadVar)
    {
        Sb.AppendLine("bool guardResult;"); 
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            if (transition.GuardIsAsync)
            {
                if (transition is { GuardExpectsPayload: true, GuardHasParameterlessOverload: true })
                {
                    var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                    using (Sb.Block($"if ({payloadVar} is {payloadType} typedGuardPayload)"))
                    {
                        Sb.AppendLine($"guardResult = await {transition.GuardMethod}(typedGuardPayload){GetConfigureAwait()};");
                    }
                    Sb.AppendLine("else");
                    using (Sb.Indent())
                    {
                        Sb.AppendLine($"guardResult = await {transition.GuardMethod}(){GetConfigureAwait()};");
                    }
                }
                else if (transition.GuardExpectsPayload)
                {
                    var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                    Sb.AppendLine($"guardResult = {payloadVar} is {payloadType} typedGuardPayload && await {transition.GuardMethod}(typedGuardPayload){GetConfigureAwait()};");
                }
                else
                {
                    Sb.AppendLine($"guardResult = await {transition.GuardMethod}(){GetConfigureAwait()};");
                }
            }
            else
            {
                // Sync guard in async method
                if (transition is { GuardExpectsPayload: true, GuardHasParameterlessOverload: true })
                {
                    var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                    using (Sb.Block($"if ({payloadVar} is {payloadType} typedGuardPayload)"))
                    {
                        Sb.AppendLine($"guardResult = {transition.GuardMethod}(typedGuardPayload);");
                    }
                    Sb.AppendLine("else");
                    using (Sb.Indent())
                    {
                        Sb.AppendLine($"guardResult = {transition.GuardMethod}();");
                    }
                }
                else if (transition.GuardExpectsPayload)
                {
                    var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                    Sb.AppendLine($"guardResult = {payloadVar} is {payloadType} typedGuardPayload && {transition.GuardMethod}(typedGuardPayload);");
                }
                else
                {
                    Sb.AppendLine($"guardResult = {transition.GuardMethod}();");
                }
            }
        }
        Sb.AppendLine("catch");
        using (Sb.Block(""))
        {
            Sb.AppendLine("guardResult = false;");
        }
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

    protected override void WriteGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            WriteAsyncGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
        }
        else
        {
            base.WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteAsyncGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        Sb.AppendLine("/// <returns>List of triggers that can be fired in the current state</returns>");

        using (Sb.Block($"public override async ValueTask<{ReadOnlyListType}<{triggerTypeForUsage}>> GetPermittedTriggersAsync(CancellationToken cancellationToken = default)"))
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
                        var hasAsyncGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync);

                        if (!hasAsyncGuards && !stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod)))
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
                                    if (transition.GuardIsAsync)
                                    {
                                        Sb.AppendLine("try");
                                        using (Sb.Block(""))
                                        {
                                            if (transition.GuardHasParameterlessOverload || !transition.GuardExpectsPayload)
                                            {
                                                Sb.AppendLine($"if (await {transition.GuardMethod}(){GetConfigureAwait()})");
                                                using (Sb.Block(""))
                                                {
                                                    Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                                }
                                            }
                                        }
                                        Sb.AppendLine("catch { }");
                                    }
                                    else
                                    {
                                        WriteGuardCall(transition, "canFire", "null", throwOnException: false);
                                        using (Sb.Block("if (canFire)"))
                                        {
                                            Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                        }
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

    protected void WriteGetPermittedTriggersWithResolver(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // Sync version
        if (!IsAsyncMachine)
        {
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
        else
        {
            // Async version
            WriteAsyncGetPermittedTriggersWithResolver(stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteAsyncGetPermittedTriggersWithResolver(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"payloadResolver\">Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.</param>");
        Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        using (Sb.Block($"public async ValueTask<{ReadOnlyListType}<{triggerTypeForUsage}>> GetPermittedTriggersAsync(Func<{triggerTypeForUsage}, object?> payloadResolver, CancellationToken cancellationToken = default)"))
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
                                    WriteAsyncGuardCallForGetPermitted(transition, "canFire", $"payload_{transition.Trigger}");
                                }
                                else
                                {
                                    // Guard doesn't need payload
                                    WriteAsyncGuardCallForGetPermitted(transition, "canFire", "null");
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

    private void WriteAsyncGuardCallForGetPermitted(TransitionModel transition, string resultVar, string payloadVar)
    {
        // Similar to WriteAsyncGuardCallForCanFire but inline
        if (transition.GuardIsAsync)
        {
            Sb.AppendLine("bool canFire;");
            Sb.AppendLine("try");
            using (Sb.Block(""))
            {
                if (transition.GuardExpectsPayload && payloadVar != "null")
                {
                    var payloadType = GetTypeNameForUsage(transition.ExpectedPayloadType!);
                    Sb.AppendLine($"canFire = {payloadVar} is {payloadType} typedPayload && await {transition.GuardMethod}(typedPayload){GetConfigureAwait()};");
                }
                else if (transition.GuardHasParameterlessOverload || !transition.GuardExpectsPayload)
                {
                    Sb.AppendLine($"canFire = await {transition.GuardMethod}(){GetConfigureAwait()};");
                }
                else
                {
                    Sb.AppendLine("canFire = false; // Guard expects payload but none provided");
                }
            }
            Sb.AppendLine("catch { canFire = false; }");
        }
        else
        {
            WriteGuardCall(transition, resultVar, payloadVar, throwOnException: false);
        }
    }
}