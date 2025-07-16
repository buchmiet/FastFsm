using System.Collections.Generic;
using Generator.Rules.Definitions;

namespace Generator.Rules.Contexts;

/// <summary>
/// Context for validating a single transition against a set of already processed transitions
/// to detect duplicates based on FromState and Trigger.
/// </summary>
public class DuplicateTransitionContext(
    TransitionDefinition currentTransition,
    HashSet<TransitionDefinition> processedTransitions)
{
    /// <summary>
    /// The current transition being checked for duplication.
    /// </summary>
    public TransitionDefinition CurrentTransition { get; } = currentTransition;

    /// <summary>
    /// A set of transitions that have already been processed.
    /// The <see cref="TransitionDefinition.Equals(object?)"/> and <see cref="TransitionDefinition.GetHashCode()"/>
    /// methods of <see cref="TransitionDefinition"/> (which compare FromState and Trigger)
    /// will be used by the HashSet to determine uniqueness.
    /// </summary>
    public HashSet<TransitionDefinition> ProcessedTransitions { get; } = processedTransitions;
}