using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM104 - Validates that history mode is only used on composite states.
/// </summary>
public class InvalidHistoryConfigurationRule : IValidationRule<InvalidHistoryConfigurationContext>
{
    public IEnumerable<ValidationResult> Validate(InvalidHistoryConfigurationContext context)
    {
        if (context.HasHistory && !context.IsComposite)
        {
            string message = string.Format(
                DefinedRules.InvalidHistoryConfiguration.MessageFormat,
                context.StateName,
                context.HistoryMode);

            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidHistoryConfiguration,
                message,
                DefinedRules.InvalidHistoryConfiguration.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}