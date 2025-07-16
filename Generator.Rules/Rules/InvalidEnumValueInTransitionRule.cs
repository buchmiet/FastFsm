

using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

// Dodajemy using dla IEnumerable

namespace Generator.Rules.Rules;

public class InvalidEnumValueInTransitionRule : IValidationRule<EnumValueValidationContext>
{
    // Zmieniono typ zwracany na IEnumerable<ValidationResult>
    public IEnumerable<ValidationResult> Validate(EnumValueValidationContext context)
    {
        if (!context.IsValueDefinedInEnum)
        {
            string message = string.Format(
                DefinedRules.InvalidEnumValueInTransition.MessageFormat,
                context.ProvidedValueString ?? "null", // {0}
                context.EnumTypeName                  // {1}
            );
            // Używamy yield return i DefaultSeverity
            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidEnumValueInTransition,
                message,
                DefinedRules.InvalidEnumValueInTransition.DefaultSeverity
            );
        }
        else
        {
            // Używamy yield return
            yield return ValidationResult.Success();
        }
    }
}