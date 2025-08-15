
using Generator.Helpers;
using Generator.Model;
using System.Linq;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Generuje kod dla wariantu 'WithExtensions'.
/// </summary>
internal sealed class ExtensionsVariantGenerator(StateMachineModel model) : StateMachineCodeGenerator(model)
{
    private readonly ExtensionsFeatureWriter _ext = new();

    protected override void WriteNamespaceAndClass()
    {
        var stateTypeForUsage = GetTypeNameForUsage(Model.StateType);
        var triggerTypeForUsage = GetTypeNameForUsage(Model.TriggerType);
        var userNamespace = Model.Namespace;
        var className = Model.ClassName;

        void WriteClassContent()
        {
            var extensibleInterface = IsAsyncMachine 
                ? $"IExtensibleStateMachineAsync<{stateTypeForUsage}, {triggerTypeForUsage}>"
                : $"IExtensibleStateMachineSync<{stateTypeForUsage}, {triggerTypeForUsage}>";
            Sb.AppendLine($"public interface I{className} : {extensibleInterface} {{ }}");
            Sb.AppendLine();

            var baseClass = GetBaseClassName(stateTypeForUsage, triggerTypeForUsage);
            using (Sb.Block($"public partial class {className} : {baseClass}, I{className}"))
            {
                _ext.WriteFields(Sb);
                WriteLoggerField(className);
                
                // Write HSM arrays and runtime helpers
                WriteHierarchyArrays(stateTypeForUsage);
                WriteHierarchyRuntimeFieldsAndHelpers(stateTypeForUsage);

                WriteConstructor(stateTypeForUsage, className);
                
                // Generate Start override for HSM
                WriteStartMethod();
                
                // Generate OnInitialEntry override if needed
                if (ShouldGenerateInitialOnEntry())
                {
                    WriteOnInitialEntryMethod(stateTypeForUsage);
                }
                
                WriteTryFireMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);

                _ext.WriteManagementMethods(Sb);
                WriteStructuralApiMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteHierarchyMethods(stateTypeForUsage, triggerTypeForUsage);
            }
        }

        if (!string.IsNullOrEmpty(userNamespace))
        {
            using (Sb.Block($"namespace {userNamespace}"))
            {
                WriteContainingTypes();
                WriteClassContent();
                CloseContainingTypes();
            }
        }
        else
        {
            WriteContainingTypes();
            WriteClassContent();
            CloseContainingTypes();
        }
        void WriteContainingTypes()
        {
            foreach (var container in Model.ContainerClasses)
            {
                Sb.AppendLine($"public partial class {container}");
                Sb.AppendLine("{");
            }
        }

        void CloseContainingTypes()
        {
            for (int i = 0; i < Model.ContainerClasses.Count; i++)
            {
                Sb.AppendLine("}");
            }
        }
    }

    private void WriteConstructor(string stateTypeForUsage, string className)
    {
        var paramList = BuildConstructorParameters(
            stateTypeForUsage,
            "IEnumerable<IStateMachineExtension>? extensions = null",
            GetLoggerConstructorParameter(className)
        );

        using (Sb.Block($"public {className}({string.Join(", ", paramList)}) : base(initialState)"))
        {
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("// Initialize history tracking array with -1 (no history)");
                Sb.AppendLine("_lastActiveChild = new int[s_initialChild.Length];");
                Sb.AppendLine("for (int i = 0; i < _lastActiveChild.Length; i++) _lastActiveChild[i] = -1;");
            }
            
            WriteLoggerAssignment();
            _ext.WriteConstructorBody(Sb, ShouldGenerateLogging);

            // Initial OnEntry dispatch moved to OnInitialEntry method
        }
        Sb.AppendLine();
    }

    private void WriteTryFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // Generate unified TryFireInternal method
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool TryFireInternal({triggerTypeForUsage} trigger, object? payload)"))
        {
            if (!Model.Transitions.Any())
            {
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }
            
            Sb.AddProperty($"var {OriginalStateVar}", CurrentStateField);
            Sb.AppendLine();

            WriteTryFireStructure(
                stateTypeForUsage,
                triggerTypeForUsage,
                WriteTransitionLogic);

            // Return is handled inside WriteTryFireStructure
        }
        Sb.AppendLine();
        
        // Generate public wrapper
        WriteMethodAttribute();
        using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? payload = null)"))
        {
            Sb.AppendLine("EnsureStarted();");
            Sb.AppendLine("return TryFireInternal(trigger, payload);");
        }
        Sb.AppendLine();
    }

    protected override void WriteTransitionLogic(
    TransitionModel transition,
    string stateTypeForUsage,
    string triggerTypeForUsage)
    {
        // Czy generator ma wygenerować OnEntry/OnExit
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // ────────────── HOOK: BeforeTransition ──────────────
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // ────────────── Guard evaluation (jeśli jest) ───────
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheckForExtensions(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // ────────────────────────────────────────────────────
        // GŁÓWNA LOGIKA PRZEJŚCIA umieszczona w try/catch,
        // aby w razie wyjątku wywołać RunAfterTransition(..., false).
        // ────────────────────────────────────────────────────
        using (Sb.Block("try"))                        // ← otwiera klamrę try { … }
        {
            // ---------- OnExit ----------
            if (!transition.IsInternal &&
                hasOnEntryExit &&
                Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
                !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
            {
                // Use CallbackGenerationHelper for consistent OnExit handling
                CallbackGenerationHelper.EmitOnExitCall(
                    Sb,
                    fromStateDef,
                    transition.ExpectedPayloadType,
                    null, // no default payload type
                    PayloadVar,
                    IsAsyncMachine,
                    wrapInTryCatch: false, // Already in try block
                    Model.ContinueOnCapturedContext,
                    isSinglePayload: false,
                    isMultiPayload: false,
                    cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                    treatCancellationAsFailure: false
                );
                WriteLogStatement(
                    "Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
            }

            // ---------- Action ----------
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                // Use CallbackGenerationHelper for consistent Action handling
                CallbackGenerationHelper.EmitActionCall(
                    Sb,
                    transition,
                    PayloadVar,
                    IsAsyncMachine,
                    wrapInTryCatch: false, // Already in try block
                    Model.ContinueOnCapturedContext,
                    cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                    treatCancellationAsFailure: false
                );
                
                // Note: For Actions that expect payload but don't get the right type,
                // the helper will handle the conditional logic internally
                
                WriteLogStatement(
                    "Debug",
                    $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", " +
                    $"\"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            }

            // ---------- OnEntry ----------
            if (!transition.IsInternal &&
                hasOnEntryExit &&
                Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
                !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
            {
                // Use CallbackGenerationHelper for consistent OnEntry handling
                CallbackGenerationHelper.EmitOnEntryCall(
                    Sb,
                    toStateDef,
                    transition.ExpectedPayloadType,
                    null, // no default payload type
                    PayloadVar,
                    IsAsyncMachine,
                    wrapInTryCatch: false, // Already in try block
                    Model.ContinueOnCapturedContext,
                    isSinglePayload: false,
                    isMultiPayload: false,
                    cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                    treatCancellationAsFailure: false
                );
                WriteLogStatement(
                    "Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
            }

            // ---------- zmiana stanu ----------
            if (!transition.IsInternal)
            {
                // Record history before changing state (for HSM)
                if (Model.HierarchyEnabled)
                {
                    Sb.AppendLine("RecordHistoryForCurrentPath();");
                }
                
                // For HSM, handle composite states properly
                if (Model.HierarchyEnabled)
                {
                    WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
                }
                else
                {
                    Sb.AppendLine(
                        $"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
                }
                WriteLogStatement(
                    "Information",
                    $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", " +
                    $"\"{transition.ToState}\", \"{transition.Trigger}\");");
            }

            // ---------- sukces ----------
            // HOOK: AfterTransition – sukces
            WriteAfterTransitionHook(
                transition, stateTypeForUsage, triggerTypeForUsage, success: true);

            // Direct return success
            Sb.AppendLine("return true;");
        }                                          // ← koniec bloku try

        using (Sb.Block("catch (Exception)"))                        // ← blok catch { … }
        {

            // HOOK: AfterTransition – porażka (KLUCZOWA POPRAWKA)
            WriteAfterTransitionHook(
                transition, stateTypeForUsage, triggerTypeForUsage, success: false);

            // Direct return failure
            Sb.AppendLine("return false;");
        }
    }
    
    private void WriteGuardCheckForExtensions(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Use GuardGenerationHelper for consistent guard handling
        GuardGenerationHelper.EmitGuardCheck(
            Sb,
            transition,
            GuardResultVar,
            payloadVar: "null", // Extensions variant doesn't use payload in guards
            IsAsyncMachine,
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            handleResultAfterTry: true,
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: false
        );
        
        // Hook: After guard evaluated
        WriteAfterGuardEvaluatedHook(transition, GuardResultVar, stateTypeForUsage, triggerTypeForUsage);
        
        // Check guard result
        using (Sb.Block($"if (!{GuardResultVar})"))
        {
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
            
            // Hook: After failed transition
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
            
            // Direct return failure
            Sb.AppendLine("return false;");
        }
    }


    #region Extension Hooks Implementation

    protected override void WriteBeforeTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        Sb.AppendLine($"var {HookVarContext} = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
        using (Sb.Indent())
        {
            Sb.AppendLine("Guid.NewGuid().ToString(),");
            Sb.AppendLine($"{CurrentStateField},");
            Sb.AppendLine("trigger,");
            Sb.AppendLine($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
            Sb.AppendLine($"{PayloadVar});");
        }
        Sb.AppendLine();
        Sb.AppendLine($"_extensionRunner.RunBeforeTransition(_extensions, {HookVarContext});");
        Sb.AppendLine();
    }

    protected override void WriteGuardEvaluationHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        Sb.AppendLine($"_extensionRunner.RunGuardEvaluation(_extensions, {HookVarContext}, \"{transition.GuardMethod}\");");
        Sb.AppendLine();
    }

    protected override void WriteAfterGuardEvaluatedHook(
        TransitionModel transition,
        string guardResultVar,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        Sb.AppendLine($"_extensionRunner.RunGuardEvaluated(_extensions, {HookVarContext}, \"{transition.GuardMethod}\", {guardResultVar});");
        Sb.AppendLine();
    }

    protected override void WriteAfterTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage,
        bool success)
    {
        Sb.AppendLine($"_extensionRunner.RunAfterTransition(_extensions, {HookVarContext}, {success.ToString().ToLower()});");
    }

    protected override void WriteTransitionFailureHook(
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        // In the new direct-return pattern, this hook is not needed
        // Failures are handled inline with WriteAfterTransitionHook(..., success: false)
    }

    #endregion

    protected override bool ShouldGenerateInitialOnEntry() => Model.GenerationConfig.HasOnEntryExit;
    protected override bool ShouldGenerateOnEntryExit() => Model.GenerationConfig.HasOnEntryExit;
}
