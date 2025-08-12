using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM013 - Validates that async callbacks are not used in established sync machines.
/// This is stricter than FSM011 - it prevents adding async callbacks to already sync machines.
/// </summary>
public class AsyncCallbackInSyncMachineRule : IValidationRule<AsyncCallbackInSyncMachineContext>
{
    public IEnumerable<ValidationResult> Validate(AsyncCallbackInSyncMachineContext context)
    {
        // Check if machine is established as sync and callback is async
        if (context.IsMachineEstablished && !context.IsMachineAsync && context.IsCallbackAsync)
        {
            string message = string.Format(
                DefinedRules.AsyncCallbackInSyncMachine.MessageFormat,
                context.MethodName);

            yield return ValidationResult.Fail(
                RuleIdentifiers.AsyncCallbackInSyncMachine,
                message,
                DefinedRules.AsyncCallbackInSyncMachine.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}