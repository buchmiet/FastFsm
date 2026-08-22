
using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

public class InvalidEnumValueInTransitionRule : IValidationRule<EnumValueValidationContext>
{
    public IEnumerable<ValidationResult> Validate(EnumValueValidationContext context)
    {
        if (!context.IsValueDefinedInEnum)
        {
            string message = string.Format(
                DefinedRules.InvalidEnumValueInTransition.MessageFormat,
                context.ProvidedValueString ?? "null", // {0}
                context.EnumTypeName                  // {1}
            );
            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidEnumValueInTransition,
                message,
                DefinedRules.InvalidEnumValueInTransition.DefaultSeverity
            );
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}
