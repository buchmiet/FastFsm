using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM014 - Warns about async void callbacks which should be async Task or async ValueTask.
/// </summary>
public class InvalidAsyncVoidRule : IValidationRule<InvalidAsyncVoidContext>
{
    public IEnumerable<ValidationResult> Validate(InvalidAsyncVoidContext context)
    {
        // Check if method is async and returns void
        if (context.IsAsync && context.ReturnType == "void")
        {
            string message = string.Format(
                DefinedRules.InvalidAsyncVoid.MessageFormat,
                context.MethodName);

            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidAsyncVoid,
                message,
                DefinedRules.InvalidAsyncVoid.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}