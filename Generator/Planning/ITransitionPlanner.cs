namespace Generator.Planning;

/// <summary>
/// Interface for transition planners that generate execution plans for state transitions
/// </summary>
internal interface ITransitionPlanner
{
    /// <summary>
    /// Builds a plan for executing a transition
    /// </summary>
    /// <param name="context">Context containing all information needed to build the plan</param>
    /// <returns>A plan containing all steps to execute the transition</returns>
    TransitionPlan BuildPlan(TransitionBuildContext context);
}