using Generator.Rules.Contexts;
using Generator.Rules.Definitions;
using System.Collections.Generic;

namespace Generator.Rules.Rules;

public class InvalidVariantConfigRule : IValidationRule<InvalidVariantConfigContext>
{
    public IEnumerable<ValidationResult> Validate(InvalidVariantConfigContext context)
    {
        if (!context.HasConflict)
        {
            yield return ValidationResult.Success();
            yield break;
        }

        string reason = context.ConflictType switch
        {
            "PayloadTypes" => "Remove PayloadType attributes or use WithPayload/Full variant.",
            "Extensions" => "Set GenerateExtensibleVersion=true or use different variant.",
            "OnEntryExit" => "Remove OnEntry/OnExit methods or use Basic/WithExtensions/Full variant.",
            _ => "Check variant requirements."
        };

        string message = string.Format(
            DefinedRules.InvalidForcedVariantConfiguration.MessageFormat,
            context.VariantName,
            context.ConflictType,
            reason);

        yield return ValidationResult.Fail(
            RuleIdentifiers.InvalidForcedVariantConfiguration,
            message,
            DefinedRules.InvalidForcedVariantConfiguration.DefaultSeverity);
    }
}