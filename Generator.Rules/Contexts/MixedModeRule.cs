using System.Collections.Generic;
using Generator.Rules.Definitions;
using Generator.Rules.Rules;

namespace Generator.Rules.Contexts;

/// <summary>
/// FSM011 - Validates that a state machine does not mix sync and async callbacks.
/// </summary>
public class MixedModeRule : IValidationRule<MixedModeValidationContext>
{
    public IEnumerable<ValidationResult> Validate(MixedModeValidationContext context)
    {
        // Conflict detection is performed by the parser; this rule formats the diagnostic result.
        if (context.CallbackMode != context.MachineMode)
        {
            string message = string.Format(
                DefinedRules.MixedSyncAsyncCallbacks.MessageFormat,
                context.MethodName,
                context.CallbackMode,
                context.MachineMode
            );

            yield return ValidationResult.Fail(
                RuleIdentifiers.MixedSyncAsyncCallbacks,
                message,
                DefinedRules.MixedSyncAsyncCallbacks.DefaultSeverity
            );
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}
