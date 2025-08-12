using System.Collections.Generic;
using System.Linq;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// FSM100 - Validates that state hierarchies do not contain circular dependencies.
/// </summary>
public class CircularHierarchyRule : IValidationRule<CircularHierarchyContext>
{
    public IEnumerable<ValidationResult> Validate(CircularHierarchyContext context)
    {
        if (context.CyclePath != null && context.CyclePath.Any())
        {
            string cyclePath = string.Join(" -> ", context.CyclePath);
            string message = string.Format(
                DefinedRules.CircularHierarchy.MessageFormat,
                context.StateName,
                cyclePath);

            yield return ValidationResult.Fail(
                RuleIdentifiers.CircularHierarchy,
                message,
                DefinedRules.CircularHierarchy.DefaultSeverity);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}