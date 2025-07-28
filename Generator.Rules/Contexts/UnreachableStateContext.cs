using System.Collections.Generic;
using Generator.Rules.Definitions;

namespace Generator.Rules.Contexts;

public class UnreachableStateContext( // Zmieniona nazwa klasy
    string initialState,
    IReadOnlyList<string> allStateNames,
    IReadOnlyList<TransitionDefinition> allTransitions) // Zmieniony typ dla allTransitions
{
    /// <summary>
    /// The designated initial state of the state machine.
    /// If not explicitly defined, a default might be assumed by the rule (e.g., the first state alphabetically).
    /// </summary>
    public string InitialState { get; } = initialState;

    /// <summary>
    /// A list of all unique state names defined for the state machine.
    /// </summary>
    public IReadOnlyList<string> AllDefinedStateNames { get; } = allStateNames;

    /// <summary>
    /// A list of all transitions defined in the state machine.
    /// The rule will use the <see cref="TransitionDefinition.ToState"/> property
    /// to determine reachability.
    /// </summary>
    public IReadOnlyList<TransitionDefinition> AllTransitions { get; } = allTransitions;
}