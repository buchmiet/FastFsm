

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
            // Jeśli nie ma zdefiniowanych stanów, nie ma czego sprawdzać.
            // Można zwrócić sukces, bo nie znaleziono "nieosiągalnych" stanów.
            yield return ValidationResult.Success();
            yield break; // lub po prostu yield break; jeśli Success() ma być tylko dla "aktywnego" sukcesuFSM005 
        }

        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        // Determine initial state for traversal
        string? effectiveInitialState = null; // Może być null
        if (!string.IsNullOrEmpty(context.InitialState) && context.AllDefinedStateNames.Contains(context.InitialState))
        {
            effectiveInitialState = context.InitialState;
        }
        else if (context.AllDefinedStateNames.Any())
        {
            // Domyślnie pierwszy stan z listy, jeśli nie podano jawnie stanu początkowego lub jest on nieprawidłowy
            effectiveInitialState = context.AllDefinedStateNames.First();
        }

        if (effectiveInitialState != null)
        {
            queue.Enqueue(effectiveInitialState);
            reachableStates.Add(effectiveInitialState);
        }
        else
        {
            // Jeśli nie można ustalić stanu początkowego (np. brak zdefiniowanych stanów),
            // wszystkie stany (jeśli jakiekolwiek były oczekiwane) można by uznać za nieosiągalne,
            // ale praktyczniej jest nic nie sprawdzać lub zgłosić problem z konfiguracją.
            // Na razie, jeśli nie ma stanu startowego, a są stany, to wszystkie są nieosiągalne z perspektywy braku startu.
            // Jednak obecna logika poniżej obsłuży to poprawnie - żaden stan nie zostanie dodany do reachableStates.
            // Jeśli lista AllDefinedStateNames nie jest pusta, poniższa pętla zgłosi je jako nieosiągalne.
            // Można też rozważyć yield return ValidationResult.Success(); jeśli to jest stan "nie dotyczy".
            // Dla uproszczenia - jeśli nie ma stanu startowego, dalsza logika poprawnie zidentyfikuje wszystkie jako nieosiągalne.
        }

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            foreach (var transition in context.AllTransitions.Where(t => t.FromState == currentState))
            {
                // Używamy transition.ToState, które jest string?
                // Interesują nas tylko przejścia, które mają zdefiniowany stan docelowy.
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
                    DefinedRules.UnreachableState.DefaultSeverity // Użycie domyślnej ważności
                );
            }
        }

        if (!foundUnreachable && (context.AllDefinedStateNames != null && context.AllDefinedStateNames.Any()))
        {
            // Jeśli przeszliśmy przez wszystkie stany i żaden nie był nieosiągalny
            // (a były jakieś stany do sprawdzenia), zwracamy sukces.
            yield return ValidationResult.Success();
        }
        else if (context.AllDefinedStateNames == null || !context.AllDefinedStateNames.Any())
        {
            // Jeśli nie było stanów do sprawdzenia na początku, już zwróciliśmy sukces.
            // Można by to scalić, ale dla jasności zostawiam.
        }
    }
}