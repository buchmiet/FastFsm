using Generator.Helpers;
using Generator.Model;
using System.Linq;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Generuje kod dla wariantu "Core" i "Basic" maszyny stanów.
/// </summary>
internal sealed class CoreVariantGenerator(StateMachineModel model) : StateMachineCodeGenerator(model)
{
    /// <summary>
    /// Główna metoda generująca całą klasę i interfejs.
    /// </summary>
    protected override void WriteNamespaceAndClass()
    {
        var stateTypeForUsage = GetTypeNameForUsage(Model.StateType);
        var triggerTypeForUsage = GetTypeNameForUsage(Model.TriggerType);
        var userNamespace = Model.Namespace;
        var className = Model.ClassName;

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

        return;

        void WriteClassContent()
        {
            // Generuj odpowiedni interfejs
            Sb.AppendLine($"public interface I{className} : {GetInterfaceName(stateTypeForUsage, triggerTypeForUsage)} {{ }}");
            Sb.AppendLine();

            // Generuj klasę z odpowiednią klasą bazową
            using (Sb.Block($"public partial class {className} : {GetBaseClassName(stateTypeForUsage, triggerTypeForUsage)}, I{Model.ClassName}"))
            {
                // Pola dla async
                if (IsAsyncMachine)
                {
                    Sb.AppendLine("private readonly string _instanceId = Guid.NewGuid().ToString();");
                    Sb.AppendLine();
                }

                WriteLoggerField(className);
                WriteConstructor(stateTypeForUsage, className);
                WriteTryFireMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteStructuralApiMethods(stateTypeForUsage, triggerTypeForUsage);
            }
        }
    }
    /// <summary>
    /// Nadpisujemy WriteGetPermittedTriggersMethod aby obsłużyć async.
    /// </summary>
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

                        if (!hasAsyncGuards && stateGroup.All(t => string.IsNullOrEmpty(t.GuardMethod)))
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
                                // ✅ Dodaj blok dla każdej iteracji
                                using (Sb.Block(""))
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        if (transition.GuardIsAsync)
                                        {
                                            // Używamy helpera z właściwymi parametrami
                                            GuardGenerationHelper.EmitGuardCheck(
                                                Sb,
                                                transition,
                                                "canFire",
                                                "null",
                                                IsAsyncMachine,
                                                wrapInTryCatch: true,
                                                Model.ContinueOnCapturedContext,
                                                handleResultAfterTry: true,  // Zmienna będzie zadeklarowana przed try
                                                cancellationTokenVar: GetCtVar(),
                                                treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                            );

                                            using (Sb.Block("if (canFire)"))
                                            {
                                                Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                            }
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
    /// Nadpisujemy WriteCanFireMethod aby obsłużyć async.
    /// </summary>
    protected override void WriteCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            WriteAsyncCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
        }
        else
        {
            base.WriteCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
        }
    }

    /// <summary>
    /// Generuje asynchroniczną wersję CanFire.
    /// </summary>
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
                                using (Sb.Block(""))  // ✅ ZMIANA: Block zamiast Indent - każdy case ma własny scope
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        if (transition.GuardIsAsync)
                                        {
                                            GuardGenerationHelper.EmitGuardCheck(
                                                Sb,
                                                transition,
                                                "guardResult",
                                                "null",
                                                IsAsyncMachine,
                                                wrapInTryCatch: true,
                                                Model.ContinueOnCapturedContext,
                                                handleResultAfterTry: true,
                                                cancellationTokenVar: GetCtVar(),
                                                treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                            );
                                        }
                                        else
                                        {
                                            WriteGuardCall(transition, "guardResult", "null", throwOnException: false);
                                        }
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

    /// <summary>
    /// Generuje konstruktor maszyny stanów.
    /// </summary>
    private void WriteConstructor(string stateTypeForUsage, string className)
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

        AsyncGenerationHelper.EmitFireAndForgetAsyncCall(Sb, sb =>
        {
            using (sb.Block("switch (initialState)"))
            {
                foreach (var stateEntry in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
                {
                    sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                    using (sb.Indent())
                    {
                        // Check if OnEntry has token overload
                        var sig = stateEntry.OnEntrySignature;

                        // If it has token-only or payload+token overload, pass CancellationToken.None
                        if (sig.HasTokenOnly || (sig.HasPayloadAndToken && !sig.HasParameterless))
                        {
                            AsyncGenerationHelper.EmitMethodInvocation(
                                sb,
                                stateEntry.OnEntryMethod,
                                stateEntry.OnEntryIsAsync,
                                callerIsAsync: true,
                                Model.ContinueOnCapturedContext,
                                "System.Threading.CancellationToken.None"
                            );
                        }
                        else
                        {
                            AsyncGenerationHelper.EmitMethodInvocation(
                                sb,
                                stateEntry.OnEntryMethod,
                                stateEntry.OnEntryIsAsync,
                                callerIsAsync: true,
                                Model.ContinueOnCapturedContext
                            );
                        }
                        sb.AppendLine("break;");
                    }
                }
            }
        });
    }

    private void WriteTryFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        var returnType = GetMethodReturnType("bool");
        var methodName = GetTryFireMethodName();
        var parameters = GetTryFireParameters(triggerTypeForUsage);
        var asyncKeyword = GetAsyncKeyword();
        var visibility = GetTryFireVisibility();

        WriteMethodAttribute();
        using (Sb.Block($"{visibility} {asyncKeyword}{returnType} {methodName}({parameters})"))
        {
            if (!Model.Transitions.Any())
            {
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            Sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
            Sb.AppendLine($"bool {SuccessVar} = false;");
            Sb.AppendLine();

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


    protected override bool ShouldGenerateInitialOnEntry()
    {
        var config = Model.GenerationConfig;
        return config.Variant != GenerationVariant.Pure && config.HasOnEntryExit;
    }

    protected override bool ShouldGenerateOnEntryExit()
    {
        var config = Model.GenerationConfig;
        return config.Variant != GenerationVariant.Pure && config.HasOnEntryExit;
    }

    protected override void WriteTransitionLogic(
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
            WriteAsyncAwareGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            if (IsAsyncMachine)
            {
                Sb.AppendLine("try");
                using (Sb.Block(""))
                {
                    WriteCallbackInvocation(fromStateDef.OnExitMethod, fromStateDef.OnExitIsAsync);
                    WriteLogStatement("Debug",
                        $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                }
                Sb.AppendLine("catch (Exception)");
                using (Sb.Block(""))
                {
                    Sb.AppendLine($"{SuccessVar} = false;");
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine($"goto {EndOfTryFireLabel};");
                }
            }
            else
            {
                WriteCallbackInvocation(fromStateDef.OnExitMethod, fromStateDef.OnExitIsAsync);
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
            }
        }

        // Action
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            if (IsAsyncMachine)
            {
                Sb.AppendLine("try");
                using (Sb.Block(""))
                {
                    WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync);
                    WriteLogStatement("Debug",
                        $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                }
                Sb.AppendLine("catch (Exception)");
                using (Sb.Block(""))
                {
                    Sb.AppendLine($"{SuccessVar} = false;");
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine($"goto {EndOfTryFireLabel};");
                }
            }
            else
            {
                WriteCallbackInvocation(transition.ActionMethod, transition.ActionIsAsync);
                WriteLogStatement("Debug",
                    $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            }
        }

        // OnEntry  
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            if (IsAsyncMachine)
            {
                Sb.AppendLine("try");
                using (Sb.Block(""))
                {
                    WriteCallbackInvocation(toStateDef.OnEntryMethod, toStateDef.OnEntryIsAsync);
                    WriteLogStatement("Debug",
                        $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
                }
                Sb.AppendLine("catch (Exception)");
                using (Sb.Block(""))
                {
                    Sb.AppendLine($"{SuccessVar} = false;");
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine($"goto {EndOfTryFireLabel};");
                }
            }
            else
            {
                WriteCallbackInvocation(toStateDef.OnEntryMethod, toStateDef.OnEntryIsAsync);
                WriteLogStatement("Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
            }
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
    private void WriteAsyncAwareGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Używamy helpera z flagą handleResultAfterTry=true
        GuardGenerationHelper.EmitGuardCheck(
            Sb,
            transition,
            GuardResultVar,
            "null", // Core variant nie ma payloadu
            IsAsyncMachine,
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            handleResultAfterTry: true, // <-- WAŻNE: to sprawi że zmienna będzie zadeklarowana przed try
            cancellationTokenVar: GetCtVar(),
            treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
        );

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

}