using System.Collections.Generic;
using System.Linq;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM103 - Validates that a composite state doesn't have multiple initial substates.
/// </summary>
public class MultipleInitialSubstatesRule : IValidationRule<MultipleInitialSubstatesContext>
{
    public IEnumerable<ValidationResult> Validate(MultipleInitialSubstatesContext context)
    {
        if (context.InitialSubstates != null && context.InitialSubstates.Count > 1)
        {
            string message = string.Format(
                DefinedRules.MultipleInitialSubstates.MessageFormat,
                context.ParentStateName,
                context.InitialSubstates[0],
                context.InitialSubstates[1]);

            yield return ValidationResult.Fail(
                RuleIdentifiers.MultipleInitialSubstates,
                message,
                DefinedRules.MultipleInitialSubstates.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}