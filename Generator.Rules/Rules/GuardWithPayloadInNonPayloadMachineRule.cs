using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM010 – Guard expects payload, ale maszyna nie obsługuje payloadów.
/// </summary>
public class GuardWithPayloadInNonPayloadMachineRule
    : IValidationRule<GuardWithPayloadContext>
{
    public IEnumerable<ValidationResult> Validate(GuardWithPayloadContext context)
    {
        if (context.GuardExpectsPayload && !context.MachineHasPayload)
        {
            yield return ValidationResult.Fail(
                RuleIdentifiers.GuardWithPayloadInNonPayloadMachine,
                $"Guard method '{context.GuardMethodName}' expects a payload parameter, " +
                "but the state machine is configured without payload support. " +
                "Either remove the parameter from the guard method or configure the state machine " +
                "with payload support using [PayloadType] attribute.",
                RuleSeverity.Error);
        }
    }
}