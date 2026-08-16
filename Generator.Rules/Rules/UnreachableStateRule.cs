

using System.Collections.Generic;
using System.Linq;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;


namespace Generator.Rules.Rules;

// Implementacja IValidationRule<UnreachableStateContext>
public class UnreachableStateRule : IValidationRule<UnreachableStateContext>
{
    // Metoda Validate przyjmuje nowy kontekst i zwraca IEnumerable<ValidationResult>
    public IEnumerable<ValidationResult> Validate(UnreachableStateContext context)
    {
        if (context.AllDefinedStateNames == null || !context.AllDefinedStateNames.Any())
        {
            // If there are no defined states, there's nothing to check.
            // Can return success as no "unreachable" states were found.
            yield return ValidationResult.Success();
            yield break; // or just yield break; if Success() should only be for "active" success FSM005 
        }

        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        // Determine initial state for traversal
        string? effectiveInitialState = null; // Can be null
        if (!string.IsNullOrEmpty(context.InitialState) && context.AllDefinedStateNames.Contains(context.InitialState))
        {
            effectiveInitialState = context.InitialState;
        }
        else if (context.AllDefinedStateNames.Any())
        {
            // Default to first state from list if no initial state was explicitly provided or if it's invalid
            effectiveInitialState = context.AllDefinedStateNames.First();
        }

        if (effectiveInitialState != null)
        {
            queue.Enqueue(effectiveInitialState);
            reachableStates.Add(effectiveInitialState);
        }
        else
        {
            // If initial state cannot be determined (e.g., no defined states),
            // all states (if any were expected) could be considered unreachable,
            // but it's more practical to skip checking or report a configuration issue.
            // For now, if there's no start state but there are states, they're all unreachable from the perspective of no start.
            // However, the logic below will handle this correctly - no state will be added to reachableStates.
            // If AllDefinedStateNames list is not empty, the loop below will report them as unreachable.
            // Could also consider yield return ValidationResult.Success(); if this is a "not applicable" state.
            // For simplicity - if there's no start state, the logic will correctly identify all as unreachable.
        }

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            foreach (var transition in context.AllTransitions.Where(t => t.FromState == currentState))
            {
                // We use transition.ToState, which is string?
                // We're only interested in transitions that have a defined target state.
                if (transition.ToState != null)
                {
                    string toState = transition.ToState;
                    if (context.AllDefinedStateNames.Contains(toState) && reachableStates.Add(toState))
                    {
                        queue.Enqueue(toState);
                    }
                }
            }
        }

        bool foundUnreachable = false;
        foreach (var stateName in context.AllDefinedStateNames)
        {
            if (!reachableStates.Contains(stateName))
            {
                foundUnreachable = true;
                string message = string.Format(
                    DefinedRules.UnreachableState.MessageFormat,
                    stateName // {0}
                );
                yield return ValidationResult.Fail(
                    RuleIdentifiers.UnreachableState,
                    message,
                    DefinedRules.UnreachableState.DefaultSeverity // Using default severity
                );
            }
        }

        if (!foundUnreachable && (context.AllDefinedStateNames != null && context.AllDefinedStateNames.Any()))
        {
            // If we went through all states and none were unreachable
            // (and there were states to check), return success.
            yield return ValidationResult.Success();
        }
        else if (context.AllDefinedStateNames == null || !context.AllDefinedStateNames.Any())
        {
            // If there were no states to check at the beginning, we already returned success.
            // Could merge this, but leaving for clarity.
        }
    }
}