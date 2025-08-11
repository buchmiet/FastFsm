
using Generator.Model;
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

                WriteConstructor(stateTypeForUsage, className);
                
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
            WriteLoggerAssignment();
            _ext.WriteConstructorBody(Sb, ShouldGenerateLogging);

            // Initial OnEntry dispatch moved to OnInitialEntry method
        }
        Sb.AppendLine();
    }

    private void WriteTryFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool TryFireInternal({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
        {
            Sb.AddProperty($"var {OriginalStateVar}", CurrentStateField);
            Sb.AddProperty($"bool {SuccessVar}", "false");
            Sb.AppendLine();

            WriteTryFireStructure(
                stateTypeForUsage,
                triggerTypeForUsage,
                WriteTransitionLogic);

            Sb.AppendLine($"{EndOfTryFireLabel}:;");
            Sb.AppendLine($"return {SuccessVar};");
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
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // ────────────────────────────────────────────────────
        // GŁÓWNA LOGIKA PRZEJŚCIA umieszczona w try/catch,
        // aby w razie wyjątku wywołać RunAfterTransition(..., false).
        // ────────────────────────────────────────────────────
        Sb.AppendLine("try");
        using (Sb.Block(""))                        // ← otwiera klamrę try { … }
        {
            // ---------- OnExit ----------
            if (!transition.IsInternal &&
                hasOnEntryExit &&
                Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
                !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
            {
                WriteOnExitCall(fromStateDef, transition.ExpectedPayloadType);
                WriteLogStatement(
                    "Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
            }

            // ---------- Action ----------
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                if (string.IsNullOrEmpty(transition.ExpectedPayloadType))
                {
                    Sb.AppendLine($"{transition.ActionMethod}();");
                }
                else
                {
                    Sb.AppendLine(
                        $"{transition.ActionMethod}(({transition.ExpectedPayloadType}){PayloadVar});");
                }

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
                WriteOnEntryCall(toStateDef, transition.ExpectedPayloadType);
                WriteLogStatement(
                    "Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
            }

            // ---------- zmiana stanu ----------
            if (!transition.IsInternal)
            {
                Sb.AppendLine(
                    $"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
                WriteLogStatement(
                    "Information",
                    $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", " +
                    $"\"{transition.ToState}\", \"{transition.Trigger}\");");
            }

            // ---------- sukces ----------
            Sb.AppendLine($"{SuccessVar} = true;");

            // HOOK: AfterTransition – sukces
            WriteAfterTransitionHook(
                transition, stateTypeForUsage, triggerTypeForUsage, success: true);

            // przejdź do etykiety końcowej
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }                                          // ← koniec bloku try

        Sb.AppendLine("catch (Exception)");
        using (Sb.Block(""))                        // ← blok catch { … }
        {
            // oznacz niepowodzenie
            Sb.AppendLine($"{SuccessVar} = false;");

            // HOOK: AfterTransition – porażka (KLUCZOWA POPRAWKA)
            WriteAfterTransitionHook(
                transition, stateTypeForUsage, triggerTypeForUsage, success: false);

            // pomiń dalszą wspólną obsługę (if (!success) …)
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
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
        using (Sb.Block($"if (!{SuccessVar})"))
        {
            Sb.AppendLine($"var failCtx = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
            using (Sb.Indent())
            {
                Sb.AppendLine("Guid.NewGuid().ToString(),");
                Sb.AppendLine($"{OriginalStateVar},");
                Sb.AppendLine("trigger,");
                Sb.AppendLine($"{OriginalStateVar},");
                Sb.AppendLine($"{PayloadVar});");
            }
            Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, failCtx, false);");
        }
    }

    #endregion

    protected override bool ShouldGenerateInitialOnEntry() => Model.GenerationConfig.HasOnEntryExit;
    protected override bool ShouldGenerateOnEntryExit() => Model.GenerationConfig.HasOnEntryExit;
}
