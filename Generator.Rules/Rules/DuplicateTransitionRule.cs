

using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;


namespace Generator.Rules.Rules;

// Zmieniono typ kontekstu na DuplicateTransitionContext
public class DuplicateTransitionRule : IValidationRule<DuplicateTransitionContext>
{
    // Zmieniono typ zwracany na IEnumerable<ValidationResult>
    public IEnumerable<ValidationResult> Validate(DuplicateTransitionContext context)
    {
        // Logika .Add() na HashSet<TransitionDefinition> będzie działać zgodnie z oczekiwaniami
        // dzięki implementacji Equals/GetHashCode w TransitionDefinition (porównującej FromState i Trigger).
        if (!context.ProcessedTransitions.Add(context.CurrentTransition))
        {
            // MessageFormat dla FSM001: "Duplicate transition from state '{0}' on trigger '{1}'. Only the first one will be used by the generator."
            string message = string.Format(
                DefinedRules.DuplicateTransition.MessageFormat,
                context.CurrentTransition.FromState, // {0}
                context.CurrentTransition.Trigger    // {1}
            );
            // Zwracamy kolekcję z jednym wynikiem błędu
            yield return ValidationResult.Fail(
                RuleIdentifiers.DuplicateTransition,
                message,
                DefinedRules.DuplicateTransition.DefaultSeverity // Używamy domyślnej ważności z RuleDefinition
            );
        }
        else
        {
            // Jeśli nie ma duplikatu, zwracamy kolekcję z jednym wynikiem sukcesu.
            // Alternatywnie można by użyć yield break; jeśli parser byłby przygotowany
            // na obsługę pustej kolekcji jako "brak problemów".
            // Dla spójności z tym, że każda reguła "coś" zwraca, Success() jest tutaj OK.
            yield return ValidationResult.Success();
        }
    }
}