using System.Collections.Generic;
using System.Linq;
using FastFsm.HsmPrototype.Helpers;
using Generator.Model;

namespace FastFsm.HsmPrototype.Core;

public class HierarchicalCodeGenerator
{
    private readonly StateMachineModel _model;
    private readonly IndentedStringBuilder _sb = new();
    
    public HierarchicalCodeGenerator(StateMachineModel model)
    {
        _model = model;
    }
    
    public string Generate()
    {
        _sb.AppendLine("// Generated Hierarchical State Machine");
        _sb.AppendLine($"namespace {_model.Namespace};");
        _sb.AppendLine();
        
        _sb.AppendLine($"public partial class {_model.ClassName}");
        _sb.OpenBrace();
        
        GenerateFields();
        _sb.AppendLine();
        GenerateFireMethod();
        _sb.AppendLine();
        GenerateHistoryHelperMethods();
        
        _sb.CloseBrace();
        
        return _sb.ToString();
    }
    
    private void GenerateFields()
    {
        _sb.AppendLine($"private {_model.StateType} _currentState;");
        _sb.AppendLine();
        
        // Generate history fields
        GenerateHistoryFields();
        _sb.AppendLine();
        
        // Generate delegates for callbacks
        foreach (var state in _model.States.Values)
        {
            if (!string.IsNullOrEmpty(state.OnEntryMethod))
            {
                _sb.AppendLine($"private Action? {state.OnEntryMethod};");
            }
            if (!string.IsNullOrEmpty(state.OnExitMethod))
            {
                _sb.AppendLine($"private Action? {state.OnExitMethod};");
            }
        }
    }
    
    private void GenerateHistoryFields()
    {
        foreach (var kvp in _model.HistoryOf)
        {
            var stateName = kvp.Key;
            var historyMode = kvp.Value;
            
            if (historyMode == HistoryMode.Shallow)
            {
                _sb.AppendLine($"// Shallow history for {stateName}");
                _sb.AppendLine($"private {_model.StateType}? _history_{stateName};");
            }
            else if (historyMode == HistoryMode.Deep)
            {
                _sb.AppendLine($"// Deep history for {stateName}");
                _sb.AppendLine($"private {_model.StateType}? _historyDeep_{stateName};");
            }
        }
    }
    
    private void GenerateFireMethod()
    {
        _sb.AppendLine($"public void Fire({_model.TriggerType} trigger)");
        _sb.OpenBrace();
        
        _sb.AppendLine("switch (_currentState)");
        _sb.OpenBrace();
        
        // Group transitions by FromState
        var transitionsByState = _model.Transitions
            .GroupBy(t => t.FromState)
            .OrderBy(g => g.Key);
        
        foreach (var group in transitionsByState)
        {
            _sb.AppendLine($"case {_model.StateType}.{group.Key}:");
            _sb.Indent();
            
            // Sort transitions by priority (descending) then by order of definition
            var sortedTransitions = group
                .Select((t, index) => new { Transition = t, Index = index })
                .OrderByDescending(x => x.Transition.Priority)
                .ThenBy(x => x.Index)
                .Select(x => x.Transition);
            
            foreach (var transition in sortedTransitions)
            {
                GenerateCaseForTransition(transition);
            }
            
            // Add fallthrough to parent if this state has a parent
            if (_model.ParentOf.ContainsKey(group.Key) && _model.ParentOf[group.Key] != null)
            {
                var parent = _model.ParentOf[group.Key];
                _sb.AppendLine($"// Fallthrough to parent");
                _sb.AppendLine($"goto case {_model.StateType}.{parent};");
            }
            else
            {
                _sb.AppendLine("break;");
            }
            
            _sb.Outdent();
        }
        
        _sb.CloseBrace(); // switch
        _sb.CloseBrace(); // method
    }
    
    private void GenerateCaseForTransition(TransitionModel transition)
    {
        // Generate if statement with guard
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            _sb.AppendLine($"if (trigger == {_model.TriggerType}.{transition.Trigger} && {transition.GuardMethod}())");
        }
        else
        {
            _sb.AppendLine($"if (trigger == {_model.TriggerType}.{transition.Trigger})");
        }
        
        _sb.OpenBrace();
        
        // Add priority comment for debugging
        _sb.AppendLine($"// Priority: {transition.Priority}, Internal: {transition.IsInternal}");
        
        if (transition.IsInternal)
        {
            GenerateInternalTransition(transition);
        }
        else
        {
            GenerateTransitionCode(transition);
        }
        
        _sb.AppendLine("return;");
        _sb.CloseBrace();
    }
    
    private void GenerateInternalTransition(TransitionModel transition)
    {
        _sb.AppendLine($"// Internal transition in {transition.FromState}");
        
        // Execute action if present
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            _sb.AppendLine($"{transition.ActionMethod}();");
        }
        
        // No state change, no exit/entry
        _sb.AppendLine("// State remains: " + transition.FromState);
    }
    
    private void GenerateTransitionCode(TransitionModel transition)
    {
        var fromState = transition.FromState;
        var toStateName = transition.ToState!;
        
        // Check if target has history
        if (_model.HistoryOf.ContainsKey(toStateName))
        {
            GenerateHistoryTransition(transition);
        }
        else
        {
            // Determine actual target state (handle composite states)
            var actualTargetState = DetermineActualTargetState(toStateName);
            
            // Find LCA between source and actual target
            var lca = FindLowestCommonAncestor(fromState, actualTargetState);
            
            _sb.AppendLine($"// Transition: {fromState} -> {actualTargetState} (LCA: {lca ?? "none"})");
            
            // Generate exit sequence up to (but not including) LCA
            GenerateExitSequenceUpTo(fromState, lca);
            
            // Execute transition action if present
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                _sb.AppendLine($"// Transition action");
                _sb.AppendLine($"{transition.ActionMethod}();");
            }
            
            // Generate entry sequence from (but not including) LCA to target
            GenerateEntrySequenceFrom(lca, toStateName, actualTargetState);
            
            // Set the current state
            _sb.AppendLine($"_currentState = {_model.StateType}.{actualTargetState};");
        }
    }
    
    private void GenerateHistoryTransition(TransitionModel transition)
    {
        var fromState = transition.FromState;
        var toStateName = transition.ToState!;
        var historyMode = _model.HistoryOf[toStateName];
        
        _sb.AppendLine($"// Transition to {toStateName} with {historyMode} history");
        
        // Find LCA between source and target for proper exit sequence
        var lca = FindLowestCommonAncestor(fromState, toStateName);
        
        // Exit from source up to LCA
        GenerateExitSequenceUpTo(fromState, lca);
        
        // Execute transition action if present
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            _sb.AppendLine($"// Transition action");
            _sb.AppendLine($"{transition.ActionMethod}();");
        }
        
        // Check for saved history
        string historyField = historyMode == HistoryMode.Shallow 
            ? $"_history_{toStateName}" 
            : $"_historyDeep_{toStateName}";
            
        _sb.AppendLine($"if ({historyField}.HasValue)");
        _sb.OpenBrace();
        
        if (historyMode == HistoryMode.Shallow)
        {
            _sb.AppendLine($"// Restore shallow history");
            _sb.AppendLine($"var targetState = {historyField}.Value;");
            
            // Generate entry sequence from LCA to target parent
            GenerateEntrySequenceFrom(lca, toStateName, toStateName);
            
            // Then dispatch to the historical child
            _sb.AppendLine($"// Enter historical child");
            _sb.AppendLine($"switch (targetState)");
            _sb.OpenBrace();
            
            if (_model.ChildrenOf.ContainsKey(toStateName))
            {
                foreach (var child in _model.ChildrenOf[toStateName])
                {
                    _sb.AppendLine($"case {_model.StateType}.{child}:");
                    _sb.Indent();
                    GenerateEntryForState(child);
                    _sb.AppendLine($"_currentState = {_model.StateType}.{child};");
                    _sb.AppendLine("break;");
                    _sb.Outdent();
                }
            }
            
            _sb.CloseBrace(); // switch
        }
        else // Deep history
        {
            _sb.AppendLine($"// Restore deep history");
            _sb.AppendLine($"var targetState = {historyField}.Value;");
            
            // Need to generate entry sequence for all states from LCA to target
            _sb.AppendLine($"// Enter all states from LCA to historical state");
            _sb.AppendLine($"switch (targetState)");
            _sb.OpenBrace();
            
            // Generate cases for all possible deep states
            var allDeepStates = GetAllDescendants(toStateName);
            foreach (var deepState in allDeepStates)
            {
                _sb.AppendLine($"case {_model.StateType}.{deepState}:");
                _sb.Indent();
                
                // Generate entry sequence from LCA to deepState
                GenerateEntrySequenceFrom(lca, toStateName, deepState);
                
                _sb.AppendLine($"_currentState = {_model.StateType}.{deepState};");
                _sb.AppendLine("break;");
                _sb.Outdent();
            }
            
            _sb.CloseBrace(); // switch
        }
        
        _sb.CloseBrace(); // if has history
        _sb.AppendLine("else");
        _sb.OpenBrace();
        
        // No history - use initial state
        var initialChild = _model.InitialChildOf.ContainsKey(toStateName) 
            ? _model.InitialChildOf[toStateName] 
            : toStateName;
            
        _sb.AppendLine($"// No history, enter default");
        
        // Generate normal entry sequence
        var actualTarget = DetermineActualTargetState(toStateName);
        GenerateEntrySequenceFrom(null, toStateName, actualTarget);
        _sb.AppendLine($"_currentState = {_model.StateType}.{actualTarget};");
        
        _sb.CloseBrace(); // else
    }
    
    private void GenerateEntryForState(string state)
    {
        if (_model.States.ContainsKey(state))
        {
            var stateModel = _model.States[state];
            if (!string.IsNullOrEmpty(stateModel.OnEntryMethod))
            {
                _sb.AppendLine($"{stateModel.OnEntryMethod}?.Invoke();");
            }
        }
    }
    
    private List<string> GetAllDescendants(string state)
    {
        var descendants = new List<string>();
        
        if (_model.ChildrenOf.ContainsKey(state))
        {
            foreach (var child in _model.ChildrenOf[state])
            {
                descendants.Add(child);
                descendants.AddRange(GetAllDescendants(child));
            }
        }
        
        return descendants;
    }
    
    private List<string> BuildPathFromTo(string from, string to)
    {
        var path = new List<string>();
        
        // Build path from 'to' up to (but not including) 'from'
        var current = to;
        var reversePath = new List<string>();
        
        while (current != null && current != from)
        {
            reversePath.Add(current);
            current = _model.ParentOf.ContainsKey(current) ? _model.ParentOf[current] : null;
        }
        
        // Add 'from' at the beginning
        path.Add(from);
        
        // Add the rest in reverse order
        reversePath.Reverse();
        path.AddRange(reversePath);
        
        return path;
    }
    
    private string DetermineActualTargetState(string targetState)
    {
        // If target is composite, return its initial child (unless history overrides)
        if (_model.ChildrenOf.ContainsKey(targetState) && 
            _model.InitialChildOf.ContainsKey(targetState))
        {
            return _model.InitialChildOf[targetState];
        }
        
        // Otherwise return the target itself
        return targetState;
    }
    
    private void GenerateExitSequenceUpTo(string fromState, string? stopBefore)
    {
        var exitPath = new List<string>();
        var current = fromState;
        
        // Build path from current to (but not including) stopBefore
        while (current != null && current != stopBefore)
        {
            exitPath.Add(current);
            current = _model.ParentOf.ContainsKey(current) ? _model.ParentOf[current] : null;
        }
        
        // Execute exits from child to parent
        foreach (var state in exitPath)
        {
            var stateModel = _model.States[state];
            
            // Save history if this state has history
            if (_model.HistoryOf.ContainsKey(state))
            {
                var historyMode = _model.HistoryOf[state];
                if (historyMode == HistoryMode.Shallow)
                {
                    // Save the current direct child
                    var currentChild = GetDirectChildOf(state, fromState);
                    if (currentChild != null)
                    {
                        _sb.AppendLine($"// Save shallow history for {state}");
                        _sb.AppendLine($"_history_{state} = {_model.StateType}.{currentChild};");
                    }
                }
                else if (historyMode == HistoryMode.Deep)
                {
                    _sb.AppendLine($"// Save deep history for {state}");
                    _sb.AppendLine($"_historyDeep_{state} = _currentState;");
                }
            }
            
            if (!string.IsNullOrEmpty(stateModel.OnExitMethod))
            {
                _sb.AppendLine($"// Exit {state}");
                _sb.AppendLine($"{stateModel.OnExitMethod}?.Invoke();");
            }
        }
    }
    
    private string? GetDirectChildOf(string parent, string descendant)
    {
        // Find the direct child of parent that contains descendant
        var current = descendant;
        var previous = current;
        
        while (current != null && current != parent)
        {
            previous = current;
            current = _model.ParentOf.ContainsKey(current) ? _model.ParentOf[current] : null;
            
            if (current == parent)
            {
                return previous;
            }
        }
        
        return null;
    }
    
    private void GenerateEntrySequenceFrom(string? startAfter, string targetState, string actualTargetState)
    {
        var entryPath = new List<string>();
        
        // Build full path to actual target
        var fullPath = new List<string>();
        var current = actualTargetState;
        
        while (current != null)
        {
            fullPath.Insert(0, current); // Insert at beginning to reverse order
            current = _model.ParentOf.ContainsKey(current) ? _model.ParentOf[current] : null;
        }
        
        // If target is composite and different from actual, ensure it's in path
        if (targetState != actualTargetState && _model.ChildrenOf.ContainsKey(targetState))
        {
            if (!fullPath.Contains(targetState))
            {
                // Find where to insert it
                var actualParent = _model.ParentOf.ContainsKey(actualTargetState) 
                    ? _model.ParentOf[actualTargetState] : null;
                if (actualParent == targetState)
                {
                    var index = fullPath.IndexOf(actualTargetState);
                    fullPath.Insert(index, targetState);
                }
            }
        }
        
        // Skip everything up to and including startAfter
        bool foundStart = startAfter == null;
        foreach (var state in fullPath)
        {
            if (foundStart)
            {
                entryPath.Add(state);
            }
            if (state == startAfter)
            {
                foundStart = true;
            }
        }
        
        // Execute entries from parent to child
        foreach (var state in entryPath)
        {
            var stateModel = _model.States[state];
            if (!string.IsNullOrEmpty(stateModel.OnEntryMethod))
            {
                _sb.AppendLine($"// Enter {state}");
                _sb.AppendLine($"{stateModel.OnEntryMethod}?.Invoke();");
            }
        }
    }
    
    private bool AreInSameHierarchy(string state1, string state2)
    {
        // Check if states share a common parent
        var parent1 = _model.ParentOf.ContainsKey(state1) ? _model.ParentOf[state1] : null;
        var parent2 = _model.ParentOf.ContainsKey(state2) ? _model.ParentOf[state2] : null;
        
        return parent1 != null && parent1 == parent2;
    }
    
    private List<string> GetAncestors(string state)
    {
        var ancestors = new List<string>();
        var current = state;
        
        while (current != null)
        {
            ancestors.Add(current);
            current = _model.ParentOf.ContainsKey(current) ? _model.ParentOf[current] : null;
        }
        
        return ancestors;
    }
    
    private string? FindLowestCommonAncestor(string state1, string state2)
    {
        var ancestors1 = GetAncestors(state1);
        var ancestors2 = GetAncestors(state2);
        
        // Find first common ancestor
        foreach (var ancestor in ancestors1)
        {
            if (ancestors2.Contains(ancestor))
            {
                return ancestor;
            }
        }
        
        return null; // No common ancestor (different hierarchies)
    }
    
    private void GenerateHistoryHelperMethods()
    {
        if (!_model.HistoryOf.Any())
            return;
            
        // Generate RestoreFromHistory method
        _sb.AppendLine("private void RestoreFromHistory(string targetState)");
        _sb.OpenBrace();
        _sb.AppendLine("// This method handles runtime dispatch to historical states");
        
        foreach (var kvp in _model.HistoryOf)
        {
            var stateName = kvp.Key;
            var historyMode = kvp.Value;
            
            _sb.AppendLine($"if (targetState == \"{stateName}\")");
            _sb.OpenBrace();
            
            if (historyMode == HistoryMode.Shallow)
            {
                _sb.AppendLine($"if (_history_{stateName}.HasValue)");
                _sb.OpenBrace();
                _sb.AppendLine($"var historicalState = _history_{stateName}.Value;");
                _sb.AppendLine($"// Restore to shallow history");
                _sb.AppendLine($"switch (historicalState)");
                _sb.OpenBrace();
                
                // Generate cases for each direct child
                if (_model.ChildrenOf.ContainsKey(stateName))
                {
                    foreach (var child in _model.ChildrenOf[stateName])
                    {
                        _sb.AppendLine($"case {_model.StateType}.{child}:");
                        _sb.Indent();
                        _sb.AppendLine($"_currentState = {_model.StateType}.{child};");
                        _sb.AppendLine("return;");
                        _sb.Outdent();
                    }
                }
                
                _sb.CloseBrace(); // switch
                _sb.CloseBrace(); // if has value
                
                // Default to initial child
                if (_model.InitialChildOf.ContainsKey(stateName))
                {
                    _sb.AppendLine($"// No history, use initial");
                    _sb.AppendLine($"_currentState = {_model.StateType}.{_model.InitialChildOf[stateName]};");
                }
            }
            else if (historyMode == HistoryMode.Deep)
            {
                _sb.AppendLine($"if (_historyDeep_{stateName}.HasValue)");
                _sb.OpenBrace();
                _sb.AppendLine($"// Restore to deep history");
                _sb.AppendLine($"_currentState = _historyDeep_{stateName}.Value;");
                _sb.AppendLine("return;");
                _sb.CloseBrace();
                
                // Default to initial child
                if (_model.InitialChildOf.ContainsKey(stateName))
                {
                    _sb.AppendLine($"// No history, use initial");
                    _sb.AppendLine($"_currentState = {_model.StateType}.{_model.InitialChildOf[stateName]};");
                }
            }
            
            _sb.CloseBrace(); // if targetState ==
        }
        
        _sb.CloseBrace(); // method
    }
}