using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// Validates presence and correctness of the [StateMachine] attribute on FSM classes.
/// </summary>
public class MissingStateMachineAttributeRule : IValidationRule<MissingStateMachineAttributeValidationContext>
{
    private const string MissingAttributeFormat = "Class '{0}' is missing the [StateMachine(typeof(StateEnum), typeof(TriggerEnum))] attribute.";
    private const string MissingArgumentsFormat = "The [StateMachine] attribute on class '{0}' requires at least two arguments (state type and trigger type).";
    private const string NotPartialFormat = "Class '{0}' with [StateMachine] attribute must be declared as partial.";

    public IEnumerable<ValidationResult> Validate(MissingStateMachineAttributeValidationContext context)
    {
        if (!context.HasStateMachineAttribute)
        {
            string message = string.Format(MissingAttributeFormat, context.ClassName);
            yield return ValidationResult.Fail(
                RuleIdentifiers.MissingStateMachineAttribute,
                message,
                DefinedRules.MissingStateMachineAttribute.DefaultSeverity
            );
            yield break;
        }

        bool errorFound = false;

        if (context.FsmAttributeConstructorArgCount < 2)
        {
            errorFound = true;
            string message = string.Format(MissingArgumentsFormat, context.ClassName);
            yield return ValidationResult.Fail(
                RuleIdentifiers.MissingStateMachineAttribute,
                message,
                DefinedRules.MissingStateMachineAttribute.DefaultSeverity
            );
        }

        if (!context.IsClassPartial)
        {
            errorFound = true;
            string message = string.Format(NotPartialFormat, context.ClassName);
            yield return ValidationResult.Fail(
                RuleIdentifiers.MissingStateMachineAttribute,
                message,
                DefinedRules.MissingStateMachineAttribute.DefaultSeverity
            );
        }

        // Additional scenario for FSM004: class has transition attributes but is missing [StateMachine].
        // This is outside the scope of this rule with this context.

        if (!errorFound)
        {
            yield return ValidationResult.Success();
        }
    }
}
