using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM101 - Validates that all substates have existing parent states.
/// </summary>
public class OrphanSubstateRule : IValidationRule<OrphanSubstateContext>
{
    public IEnumerable<ValidationResult> Validate(OrphanSubstateContext context)
    {
        if (!context.ParentExists)
        {
            string message = string.Format(
                DefinedRules.OrphanSubstate.MessageFormat,
                context.SubstateName,
                context.ParentStateName);

            yield return ValidationResult.Fail(
                RuleIdentifiers.OrphanSubstate,
                message,
                DefinedRules.OrphanSubstate.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}