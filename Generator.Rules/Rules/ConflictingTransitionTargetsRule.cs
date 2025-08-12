using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM105 - Informs about implicit transition targets to composite states.
/// </summary>
public class ConflictingTransitionTargetsRule : IValidationRule<ConflictingTransitionTargetsContext>
{
    public IEnumerable<ValidationResult> Validate(ConflictingTransitionTargetsContext context)
    {
        // This is an informational diagnostic - always emit when called
        string message = string.Format(
            DefinedRules.ConflictingTransitionTargets.MessageFormat,
            context.CompositeStateName,
            context.ActualTarget,
            context.Reason);

        yield return ValidationResult.Fail(
            RuleIdentifiers.ConflictingTransitionTargets,
            message,
            DefinedRules.ConflictingTransitionTargets.DefaultSeverity);
    }
}