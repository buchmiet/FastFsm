using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM102 - Validates that composite states have proper initial substate configuration.
/// </summary>
public class InvalidHierarchyConfigurationRule : IValidationRule<InvalidHierarchyConfigurationContext>
{
    public IEnumerable<ValidationResult> Validate(InvalidHierarchyConfigurationContext context)
    {
        if (context.IsComposite && !context.HasInitialSubstate && !context.HasHistory)
        {
            string message = string.Format(
                DefinedRules.InvalidHierarchyConfiguration.MessageFormat,
                context.CompositeStateName);

            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidHierarchyConfiguration,
                message,
                DefinedRules.InvalidHierarchyConfiguration.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}