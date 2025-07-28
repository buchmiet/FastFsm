using Generator.Rules.Contexts;
using Generator.Rules.Definitions;
using System.Collections.Generic;

namespace Generator.Rules.Rules;

public class MissingPayloadTypeRule : IValidationRule<MissingPayloadTypeContext>
{
    public IEnumerable<ValidationResult> Validate(MissingPayloadTypeContext context)
    {
        if (!context.IsForced)
        {
            yield return ValidationResult.Success();
            yield break;
        }

        bool requiresPayload = context.Variant == "WithPayload" || context.Variant == "Full";
        bool hasPayload = context.HasDefaultPayloadType || context.HasTriggerPayloadTypes;

        if (requiresPayload && !hasPayload)
        {
            string message = string.Format(
                DefinedRules.MissingPayloadType.MessageFormat,
                context.Variant);

            yield return ValidationResult.Fail(
                RuleIdentifiers.MissingPayloadType,
                message,
                DefinedRules.MissingPayloadType.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}