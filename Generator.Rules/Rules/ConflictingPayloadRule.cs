using Generator.Rules.Contexts;
using Generator.Rules.Definitions;
using System.Collections.Generic;

namespace Generator.Rules.Rules;

public class ConflictingPayloadRule : IValidationRule<ConflictingPayloadContext>
{
    public IEnumerable<ValidationResult> Validate(ConflictingPayloadContext context)
    {
        if (context.IsWithPayloadVariant && context.TriggerSpecificPayloadCount > 0)
        {
            string message = string.Format(
                DefinedRules.ConflictingPayloadConfiguration.MessageFormat,
                context.TriggerSpecificPayloadCount);

            yield return ValidationResult.Fail(
                RuleIdentifiers.ConflictingPayloadConfiguration,
                message,
                DefinedRules.ConflictingPayloadConfiguration.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}