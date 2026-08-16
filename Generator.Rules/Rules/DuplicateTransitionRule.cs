

using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;


namespace Generator.Rules.Rules;

// Changed context type to DuplicateTransitionContext
public class DuplicateTransitionRule : IValidationRule<DuplicateTransitionContext>
{
    // Changed return type to IEnumerable<ValidationResult>
    public IEnumerable<ValidationResult> Validate(DuplicateTransitionContext context)
    {
        // Logic .Add() on HashSet<TransitionDefinition> will work as expected
        // thanks to Equals/GetHashCode implementation in TransitionDefinition (comparing FromState and Trigger).
        if (!context.ProcessedTransitions.Add(context.CurrentTransition))
        {
            // MessageFormat dla FSM001: "Duplicate transition from state '{0}' on trigger '{1}'. Only the first one will be used by the generator."
            string message = string.Format(
                DefinedRules.DuplicateTransition.MessageFormat,
                context.CurrentTransition.FromState, // {0}
                context.CurrentTransition.Trigger    // {1}
            );
            // Return collection with single error result
            yield return ValidationResult.Fail(
                RuleIdentifiers.DuplicateTransition,
                message,
                DefinedRules.DuplicateTransition.DefaultSeverity // Use default severity from RuleDefinition
            );
        }
        else
        {
            // If no duplicate, return collection with single success result.
            // Alternatively could use yield break; if parser was prepared
            // for handling empty collection as "no problems".
            // For consistency that every rule "returns something", Success() is OK here.
            yield return ValidationResult.Success();
        }
    }
}