﻿using Generator.Model;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Generates state machines for the 'Full' variant (Payloads + Extensions).
/// Inherits from PayloadVariantGenerator to reuse payload-related logic.
/// </summary>
internal sealed class FullVariantGenerator(StateMachineModel model) : PayloadVariantGenerator(model)
{
    private readonly ExtensionsFeatureWriter _ext = new();

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
            // Interface generation
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                Sb.AppendLine($"public interface I{className} : ");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"IStateMachineWithPayload<{stateTypeForUsage}, {triggerTypeForUsage}, {payloadType}>,");
                    Sb.AppendLine($"IExtensibleStateMachine<{stateTypeForUsage}, {triggerTypeForUsage}>");
                }
            }
            else
            {
                Sb.AppendLine($"public interface I{className} : ");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"IStateMachineWithMultiPayload<{stateTypeForUsage}, {triggerTypeForUsage}>,");
                    Sb.AppendLine($"IExtensibleStateMachine<{stateTypeForUsage}, {triggerTypeForUsage}>");
                }
            }

            Sb.AppendLine("{ }");
            Sb.AppendLine();

            // Class generation
            using (Sb.Block($"public partial class {className} : StateMachineBase<{stateTypeForUsage}, {triggerTypeForUsage}>, I{className}"))
            {
                _ext.WriteFields(Sb);
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

                _ext.WriteManagementMethods(Sb);
                WriteStructuralApiMethods(stateTypeForUsage, triggerTypeForUsage);
            }
        }
    }

    protected override void WriteConstructor(string stateTypeForUsage, string className)
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

            if (ShouldGenerateInitialOnEntry())
            {
                Sb.AppendLine();
                WriteInitialOnEntryDispatch(stateTypeForUsage);
            }
        }
        Sb.AppendLine();
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
}