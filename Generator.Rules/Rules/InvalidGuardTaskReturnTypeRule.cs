using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM012 - Validates that async guards return ValueTask&lt;bool&gt; instead of Task&lt;bool&gt;.
/// </summary>
public class InvalidGuardTaskReturnTypeRule : IValidationRule<InvalidGuardTaskReturnTypeContext>
{
    public IEnumerable<ValidationResult> Validate(InvalidGuardTaskReturnTypeContext context)
    {
        // Only check if the method is async and returns Task<bool>
        if (context.IsAsync && context.ActualReturnType == "System.Threading.Tasks.Task<bool>")
        {
            string message = string.Format(
                DefinedRules.InvalidGuardTaskReturnType.MessageFormat,
                context.MethodName);

            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidGuardTaskReturnType,
                message,
                DefinedRules.InvalidGuardTaskReturnType.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}