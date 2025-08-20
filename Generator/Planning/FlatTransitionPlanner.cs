using System.Collections.Generic;
using System.Linq;

namespace Generator.Planning;

/// <summary>
/// Planner for flat (non-hierarchical) state machines
/// </summary>
internal class FlatTransitionPlanner : ITransitionPlanner
{
    /// <summary>
    /// Builds a transition plan for flat state machines.
    /// </summary>
    public TransitionPlan BuildPlan(TransitionBuildContext context)
    {
        var steps = new List<PlanStep>();
        var transition = context.Transition;
        var fromStateIndex = context.AllStates.ToList().IndexOf(transition.FromState);
        var toStateIndex = context.AllStates.ToList().IndexOf(transition.ToState);
        
        // Check if it's an internal transition
        if (transition.IsInternal)
        {
            // Internal transition - only execute action, no state change
            if (!string.IsNullOrEmpty(transition.GuardMethod))
            {
                steps.Add(new PlanStep(
                    PlanStepKind.GuardCheck,
                    guardMethod: transition.GuardMethod,
                    stateName: transition.FromState));
            }
            
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                steps.Add(new PlanStep(
                    PlanStepKind.InternalAction,
                    actionMethod: transition.ActionMethod,
                    isAsyncAction: transition.ActionIsAsync,
                    isInternal: true,
                    hasPayload: transition.ActionExpectsPayload,
                    stateName: transition.FromState));
            }
            
            return new TransitionPlan(
                isInternal: true,
                fromStateIndex: fromStateIndex,
                toStateIndex: fromStateIndex, // Same state for internal
                lcaIndex: -1,
                steps: steps);
        }
        
        // Regular transition
        // 1. Guard check (if present)
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            steps.Add(new PlanStep(
                PlanStepKind.GuardCheck,
                guardMethod: transition.GuardMethod,
                stateName: transition.FromState,
                hasPayload: transition.GuardExpectsPayload));
        }
        
        // 2. Exit from current state
        if (context.Model.States.TryGetValue(transition.FromState, out var fromStateDef) && 
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            steps.Add(new PlanStep(
                PlanStepKind.ExitState,
                stateIndex: fromStateIndex,
                stateName: transition.FromState,
                onExitMethod: fromStateDef.OnExitMethod,
                isAsyncAction: fromStateDef.OnExitSignature.IsAsync,
                hasPayload: fromStateDef.OnExitExpectsPayload));
        }
        
        // 3. Execute transition action (if present)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            steps.Add(new PlanStep(
                PlanStepKind.InternalAction,
                actionMethod: transition.ActionMethod,
                isAsyncAction: transition.ActionIsAsync,
                hasPayload: transition.ActionExpectsPayload,
                stateName: transition.FromState));
        }
        
        // 4. Assign new state
        steps.Add(new PlanStep(
            PlanStepKind.AssignState,
            stateIndex: toStateIndex,
            stateName: transition.ToState));
        
        // 5. Enter new state
        if (context.Model.States.TryGetValue(transition.ToState, out var toStateDef) && 
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            steps.Add(new PlanStep(
                PlanStepKind.EntryState,
                stateIndex: toStateIndex,
                stateName: transition.ToState,
                onEntryMethod: toStateDef.OnEntryMethod,
                isAsyncAction: toStateDef.OnEntrySignature.IsAsync,
                hasPayload: toStateDef.OnEntryExpectsPayload));
        }
        
        return new TransitionPlan(
            isInternal: false,
            fromStateIndex: fromStateIndex,
            toStateIndex: toStateIndex,
            lcaIndex: -1,
            steps: steps);
    }
}