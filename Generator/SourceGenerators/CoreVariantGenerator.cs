using System.Linq;
using Generator.Model;
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
            Sb.AppendLine($"public interface I{className} : IStateMachine<{stateTypeForUsage}, {triggerTypeForUsage}> {{ }}");
            Sb.AppendLine();

            using (Sb.Block($"public partial class {className} : StateMachineBase<{stateTypeForUsage}, {triggerTypeForUsage}>, I{className}"))
            {
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
    /// Generuje konstruktor maszyny stanów.
    /// </summary>
    private void WriteConstructor(string stateTypeForUsage, string className)
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

    private void WriteTryFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
        {
            if (!Model.Transitions.Any())
            {
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            Sb.AddProperty($"{stateTypeForUsage} {OriginalStateVar}", CurrentStateField);
            Sb.AddProperty($"bool {SuccessVar}", "false");
            Sb.AppendLine();

            WriteTryFireStructure(
                stateTypeForUsage,
                triggerTypeForUsage,
                WriteTransitionLogic);

            Sb.AppendLine($"{EndOfTryFireLabel}:;");
            Sb.AppendLine();

            using (Sb.Block($"if (!{SuccessVar})"))
            {
                WriteLogStatement("Warning", $"TransitionFailed(_logger, _instanceId, {OriginalStateVar}.ToString(), trigger.ToString());");
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
}