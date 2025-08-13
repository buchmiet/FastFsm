using System.Collections.Generic;
using System.Linq;
using Generator.Model;

namespace Generator.Planning;

/// <summary>
/// Planner for hierarchical state machines with parent-child relationships
/// </summary>
public class HierarchicalTransitionPlanner : ITransitionPlanner
{
    public TransitionPlan BuildPlan(TransitionBuildContext context)
    {
        var steps = new List<PlanStep>();
        var transition = context.Transition;
        var allStatesList = context.AllStates.ToList();
        var fromStateIndex = allStatesList.IndexOf(transition.FromState);
        var toStateIndex = allStatesList.IndexOf(transition.ToState);
        
        // Check if it's an internal transition
        if (transition.IsInternal)
        {
            // Internal transition - only execute action, no state change
            if (!string.IsNullOrEmpty(transition.GuardMethod))
            {
                steps.Add(new PlanStep(
                    PlanStepKind.GuardCheck,
                    guardMethod: transition.GuardMethod,
                    stateName: transition.FromState,
                    hasPayload: transition.GuardExpectsPayload));
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
                toStateIndex: fromStateIndex,
                lcaIndex: -1,
                steps: steps);
        }
        
        // Regular hierarchical transition
        
        // 1. Guard check (if present)
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            steps.Add(new PlanStep(
                PlanStepKind.GuardCheck,
                guardMethod: transition.GuardMethod,
                stateName: transition.FromState,
                hasPayload: transition.GuardExpectsPayload));
        }
        
        // 2. Use the target state as-is (composite or leaf)
        // The runtime will handle composite resolution via GetCompositeEntryTarget
        var targetState = transition.ToState;
        var targetStateIndex = allStatesList.IndexOf(targetState);
        
        // 3. Calculate LCA (Lowest Common Ancestor)
        var lcaIndex = CalculateLCA(context, fromStateIndex, targetStateIndex);
        
        // 3.5. Record history before any exits happen
        steps.Add(new PlanStep(
            PlanStepKind.RecordHistory,
            stateIndex: -1,  // -1 means current state  
            stateName: "current"));
        
        // 4. Build exit chain (from current state up to but not including LCA)
        var exitChain = BuildExitChain(context, fromStateIndex, lcaIndex);
        foreach (var exitStateIndex in exitChain)
        {
            var stateName = context.AllStates[exitStateIndex];
            
            // Add OnExit call
            if (context.Model.States.TryGetValue(stateName, out var stateDef) && 
                !string.IsNullOrEmpty(stateDef.OnExitMethod))
            {
                steps.Add(new PlanStep(
                    PlanStepKind.ExitState,
                    stateIndex: exitStateIndex,
                    stateName: stateName,
                    onExitMethod: stateDef.OnExitMethod,
                    isAsyncAction: stateDef.OnExitSignature.IsAsync,
                    hasPayload: stateDef.OnExitExpectsPayload));
            }
        }
        
        // 5. Execute transition action (if present)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            steps.Add(new PlanStep(
                PlanStepKind.InternalAction,
                actionMethod: transition.ActionMethod,
                isAsyncAction: transition.ActionIsAsync,
                hasPayload: transition.ActionExpectsPayload,
                stateName: transition.FromState));
        }
        
        // 5.5. Record history is now done in exits, not here
        // The RecordHistory during exit will capture the correct state
        
        // 6. Assign new state (composite or leaf - runtime will resolve)
        steps.Add(new PlanStep(
            PlanStepKind.AssignState,
            stateIndex: targetStateIndex,
            stateName: targetState));
        
        // 7. Build entry chain (from LCA down to target)
        var entryChain = BuildEntryChain(context, lcaIndex, targetStateIndex);
        foreach (var entryStateIndex in entryChain)
        {
            var stateName = context.AllStates[entryStateIndex];
            
            // Add OnEntry call
            if (context.Model.States.TryGetValue(stateName, out var stateDef) && 
                !string.IsNullOrEmpty(stateDef.OnEntryMethod))
            {
                steps.Add(new PlanStep(
                    PlanStepKind.EntryState,
                    stateIndex: entryStateIndex,
                    stateName: stateName,
                    onEntryMethod: stateDef.OnEntryMethod,
                    isAsyncAction: stateDef.OnEntrySignature.IsAsync,
                    hasPayload: stateDef.OnEntryExpectsPayload));
            }
        }
        
        return new TransitionPlan(
            isInternal: false,
            fromStateIndex: fromStateIndex,
            toStateIndex: targetStateIndex,
            lcaIndex: lcaIndex,
            steps: steps);
    }
    
    private string ResolveToLeafState(TransitionBuildContext context, string targetState)
    {
        var current = targetState;
        
        // Keep resolving until we reach a leaf state
        while (context.Model.ChildrenOf.TryGetValue(current, out var children) && children.Count > 0)
        {
            // Check for history
            if (context.Model.HistoryOf.TryGetValue(current, out var historyMode) && 
                historyMode != HistoryMode.None)
            {
                // For now, just use initial child (history tracking would be runtime)
                // In full implementation, this would check runtime history state
            }
            
            // Use initial child if available
            if (context.Model.InitialChildOf.TryGetValue(current, out var initialChild) && 
                !string.IsNullOrEmpty(initialChild))
            {
                current = initialChild;
            }
            else
            {
                // Fall back to first child
                current = children.First();
            }
        }
        
        return current;
    }
    
    private int CalculateLCA(TransitionBuildContext context, int fromIndex, int toIndex)
    {
        // Build ancestor chain for 'from' state
        var fromAncestors = new List<int>();
        var current = fromIndex;
        while (current >= 0)
        {
            fromAncestors.Add(current);
            if (context.ParentIndices.Length > current)
            {
                current = context.ParentIndices[current];
            }
            else
            {
                break;
            }
        }
        
        // Walk up from 'to' state until we find common ancestor
        current = toIndex;
        while (current >= 0)
        {
            if (fromAncestors.Contains(current))
            {
                return current;
            }
            
            if (context.ParentIndices.Length > current)
            {
                current = context.ParentIndices[current];
            }
            else
            {
                break;
            }
        }
        
        return -1; // No common ancestor (shouldn't happen in valid hierarchy)
    }
    
    private List<int> BuildExitChain(TransitionBuildContext context, int fromIndex, int lcaIndex)
    {
        var chain = new List<int>();
        var current = fromIndex;
        
        // Walk up from current state to (but not including) LCA
        while (current >= 0 && current != lcaIndex)
        {
            chain.Add(current);
            
            if (context.ParentIndices.Length > current)
            {
                current = context.ParentIndices[current];
            }
            else
            {
                break;
            }
        }
        
        return chain;
    }
    
    private List<int> BuildEntryChain(TransitionBuildContext context, int lcaIndex, int toIndex)
    {
        var chain = new List<int>();
        var current = toIndex;
        
        // Build path from target up to (but not including) LCA
        while (current >= 0 && current != lcaIndex)
        {
            chain.Add(current);
            
            if (context.ParentIndices.Length > current)
            {
                current = context.ParentIndices[current];
            }
            else
            {
                break;
            }
        }
        
        // Reverse to get top-down order (from ancestor to leaf)
        chain.Reverse();
        
        return chain;
    }
    
    private bool ShouldRecordHistory(TransitionBuildContext context, int stateIndex)
    {
        // Check if this state has history mode enabled
        if (context.HistoryModes.Length > stateIndex)
        {
            return context.HistoryModes[stateIndex] != HistoryMode.None;
        }
        
        // Also check if any parent has history that would need this state recorded
        var stateName = context.AllStates[stateIndex];
        if (context.Model.ParentOf.TryGetValue(stateName, out var parent) && !string.IsNullOrEmpty(parent))
        {
            var parentIndex = context.AllStates.ToList().IndexOf(parent);
            if (parentIndex >= 0 && context.HistoryModes.Length > parentIndex)
            {
                return context.HistoryModes[parentIndex] == HistoryMode.Deep;
            }
        }
        
        return false;
    }
}