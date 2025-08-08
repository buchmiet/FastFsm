#nullable enable
using Abstractions.Attributes;
using Generator.Helpers;
using Generator.Infrastructure;
using Generator.Log;
using Generator.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Baza dla wszystkich generatorów-wariantów.
/// Posiada kompletny zestaw helperów sync/async oraz hooków.
/// </summary>
public abstract class StateMachineCodeGenerator(StateMachineModel model)
{



    #region Fields / Ctor
    protected readonly StateMachineModel Model = model;
    protected IndentedStringBuilder.IndentedStringBuilder Sb = new();
    protected readonly TypeSystemHelper TypeHelper = new();
    protected readonly bool IsAsyncMachine = model.GenerationConfig.IsAsync;
    protected bool ShouldGenerateLogging => Model.GenerateLogging;
    protected HashSet<string> AddedUsings = [];

    // Hook variable names
    protected const string HookVarContext = "smCtx";
    protected const string EndOfTryFireLabel = "END_TRY_FIRE";
    #endregion

    #region Public entry
    public virtual string Generate()
    {
        WriteHeader();
        WriteNamespaceAndClass();
        return Sb.ToString();
    }
    #endregion

    #region Common implementation snippets

    #region Hierarchical State Machine Support
    
    /// <summary>
    /// Writes static hierarchy arrays if HSM is enabled
    /// </summary>
    protected virtual void WriteHierarchyArrays(string stateTypeForUsage)
    {
        if (!Model.HierarchyEnabled) return;
        
        Sb.AppendLine("// Hierarchical state machine support arrays");
        
        // Get all states in enum order
        var allStates = Model.States.Keys.OrderBy(s => s).ToList();
        var stateCount = allStates.Count;
        
        // Parent array (-1 for root states)
        Sb.Append("        private static readonly int[] s_parent = new int[] { ");
        var parentValues = allStates.Select(state =>
        {
            if (Model.ParentOf.TryGetValue(state, out var parent) && parent != null)
            {
                var parentIndex = allStates.IndexOf(parent);
                return parentIndex.ToString();
            }
            return "-1";
        });
        Sb.Append(string.Join(", ", parentValues));
        Sb.AppendLine(" };");
        
        // Depth array
        Sb.Append("        private static readonly int[] s_depth = new int[] { ");
        var depthValues = allStates.Select(state =>
        {
            if (Model.Depth.TryGetValue(state, out var depth))
            {
                return depth.ToString();
            }
            return "0";
        });
        Sb.Append(string.Join(", ", depthValues));
        Sb.AppendLine(" };");
        
        // Initial child array (-1 for non-composites)
        Sb.Append("        private static readonly int[] s_initialChild = new int[] { ");
        var initialValues = allStates.Select(state =>
        {
            if (Model.InitialChildOf.TryGetValue(state, out var initial) && initial != null)
            {
                var initialIndex = allStates.IndexOf(initial);
                return initialIndex.ToString();
            }
            return "-1";
        });
        Sb.Append(string.Join(", ", initialValues));
        Sb.AppendLine(" };");
        
        // History mode array
        Sb.Append("        private static readonly HistoryMode[] s_history = new HistoryMode[] { ");
        var historyValues = allStates.Select(state =>
        {
            if (Model.HistoryOf.TryGetValue(state, out var history))
            {
                return $"HistoryMode.{history}";
            }
            return "HistoryMode.None";
        });
        Sb.Append(string.Join(", ", historyValues));
        Sb.AppendLine(" };");
        
        Sb.AppendLine();
    }
    
    /// <summary>
    /// Writes HSM-specific methods (IsIn, GetActivePath)
    /// </summary>
    protected virtual void WriteHierarchyMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (!Model.HierarchyEnabled) return;
        
        // Override IsIn method
        Sb.WriteSummary("Checks if the given state is in the active path (HSM support)");
        Sb.AppendLine("/// <param name=\"state\">The state to check</param>");
        Sb.AppendLine("/// <returns>True if the state is in the active path, false otherwise</returns>");
        using (Sb.Block($"public override bool IsIn({stateTypeForUsage} state)"))
        {
            Sb.AppendLine("// For hierarchical machines, walk up the parent chain");
            Sb.AppendLine($"var currentIndex = (int){CurrentStateField};");
            Sb.AppendLine("var targetIndex = (int)state;");
            Sb.AppendLine();
            Sb.AppendLine("// If checking current state");
            Sb.AppendLine("if (currentIndex == targetIndex)");
            using (Sb.Indent())
            {
                Sb.AppendLine("return true;");
            }
            Sb.AppendLine();
            Sb.AppendLine("// Walk up the parent chain from current state");
            Sb.AppendLine("var parentIndex = s_parent[currentIndex];");
            using (Sb.Block("while (parentIndex >= 0)"))
            {
                Sb.AppendLine("if (parentIndex == targetIndex)");
                using (Sb.Indent())
                {
                    Sb.AppendLine("return true;");
                }
                Sb.AppendLine("parentIndex = s_parent[parentIndex];");
            }
            Sb.AppendLine();
            Sb.AppendLine("return false;");
        }
        Sb.AppendLine();
        
        // Override GetActivePath method
        Sb.WriteSummary("Gets the active state path from root to current leaf state (HSM support)");
        Sb.AppendLine("/// <returns>The path from root to current state</returns>");
        using (Sb.Block($"public override IReadOnlyList<{stateTypeForUsage}> GetActivePath()"))
        {
            Sb.AppendLine("// Build the path from leaf to root, then reverse");
            Sb.AppendLine($"var path = new List<{stateTypeForUsage}>();");
            Sb.AppendLine($"var currentIndex = (int){CurrentStateField};");
            Sb.AppendLine();
            Sb.AppendLine("// Add current state and walk up to root");
            using (Sb.Block("while (currentIndex >= 0)"))
            {
                Sb.AppendLine($"path.Add(({stateTypeForUsage})currentIndex);");
                Sb.AppendLine("currentIndex = s_parent[currentIndex];");
            }
            Sb.AppendLine();
            Sb.AppendLine("// Reverse to get root-to-leaf order");
            Sb.AppendLine("path.Reverse();");
            Sb.AppendLine("return path;");
        }
        Sb.AppendLine();
        
        // For async machines, add async version
        if (IsAsyncMachine)
        {
            Sb.WriteSummary("Asynchronously gets the active state path from root to current leaf state (HSM support)");
            Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
            Sb.AppendLine("/// <returns>The path from root to current state</returns>");
            using (Sb.Block($"public override ValueTask<IReadOnlyList<{stateTypeForUsage}>> GetActivePathAsync(CancellationToken cancellationToken = default)"))
            {
                Sb.AppendLine("// For now, just return the synchronous result wrapped in a ValueTask");
                Sb.AppendLine($"return new ValueTask<IReadOnlyList<{stateTypeForUsage}>>(GetActivePath());");
            }
            Sb.AppendLine();
        }
    }
    
    #endregion

    #region Common Implementation Methods

    protected virtual void WriteOnInitialEntryMethod(string stateTypeForUsage)
    {
        if (!ShouldGenerateInitialOnEntry())
            return;
            
        using (Sb.Block("protected override void OnInitialEntry()"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                foreach (var stateEntry in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                    using (Sb.Indent())
                    {
                        // Direct call without WriteCallbackInvocation to avoid try-catch in constructor
                        Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                        WriteLogStatement("Debug",
                            $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
                        Sb.AppendLine("break;");
                    }
                }
            }
        }
        Sb.AppendLine();
    }

    protected void WriteTryFireStructure(
        string stateTypeForUsage,
        string triggerTypeForUsage,
        Action<TransitionModel, string, string> writeTransitionLogic)
    {
        if (Model.HierarchyEnabled)
        {
            WriteTryFireStructureHierarchical(stateTypeForUsage, triggerTypeForUsage, writeTransitionLogic);
        }
        else
        {
            WriteTryFireStructureFlat(stateTypeForUsage, triggerTypeForUsage, writeTransitionLogic);
        }
    }
    
    private void WriteTryFireStructureFlat(
        string stateTypeForUsage,
        string triggerTypeForUsage,
        Action<TransitionModel, string, string> writeTransitionLogic)
    {
        var grouped = Model.Transitions.GroupBy(t => t.FromState);

        // switch (CurrentState)
        using (Sb.Block($"switch ({CurrentStateField})"))
        {
            foreach (var state in grouped)
            {
                // case <State>:
                using (Sb.Block(
                           $"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Key)}:"))
                {
                    // switch (trigger)
                    using (Sb.Block("switch (trigger)"))
                    {
                        foreach (var tr in state)
                        {
                            // case <Trigger>:
                            using (Sb.Block(
                                       $"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(tr.Trigger)}:"))
                            {
                                // Właściwa logika przejścia
                                writeTransitionLogic(tr, stateTypeForUsage, triggerTypeForUsage);
                            }
                        }

                        Sb.AppendLine("default: break;");
                    }

                    // break;  ── kończy zewnętrzne switch (CurrentState)
                    Sb.AppendLine("break;");
                }
            }

            Sb.AppendLine("default: break;");
        }

        Sb.AppendLine();

        // Hook – przejście nie znalezione
        WriteTransitionFailureHook(stateTypeForUsage, triggerTypeForUsage);
    }
    
    private void WriteTryFireStructureHierarchical(
        string stateTypeForUsage,
        string triggerTypeForUsage,
        Action<TransitionModel, string, string> writeTransitionLogic)
    {
        // For hierarchical machines, we need to check transitions from the current state
        // and all its ancestors, choosing the closest match
        
        Sb.AppendLine("// Hierarchical trigger resolution");
        Sb.AppendLine($"var currentStateIndex = (int){CurrentStateField};");
        Sb.AppendLine("var stateToCheck = currentStateIndex;");
        Sb.AppendLine();
        
        // Loop through the state and its ancestors
        using (Sb.Block("while (stateToCheck >= 0)"))
        {
            Sb.AppendLine($"var stateToCheckEnum = ({stateTypeForUsage})stateToCheck;");
            
            // Group transitions by source state
            var grouped = Model.Transitions.GroupBy(t => t.FromState);
            
            using (Sb.Block("switch (stateToCheckEnum)"))
            {
                foreach (var state in grouped)
                {
                    using (Sb.Block($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Key)}:"))
                    {
                        using (Sb.Block("switch (trigger)"))
                        {
                            foreach (var tr in state)
                            {
                                using (Sb.Block($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(tr.Trigger)}:"))
                                {
                                    // For internal transitions defined on ancestors, 
                                    // we need special handling to avoid state change
                                    if (tr.IsInternal)
                                    {
                                        Sb.AppendLine("// Internal transition on ancestor");
                                        Sb.AppendLine($"if (stateToCheck != currentStateIndex)");
                                        using (Sb.Block(""))
                                        {
                                            Sb.AppendLine("// Execute internal transition without changing state");
                                            WriteInternalTransitionOnAncestor(tr, stateTypeForUsage, triggerTypeForUsage);
                                        }
                                        Sb.AppendLine("else");
                                        using (Sb.Block(""))
                                        {
                                            writeTransitionLogic(tr, stateTypeForUsage, triggerTypeForUsage);
                                        }
                                    }
                                    else
                                    {
                                        writeTransitionLogic(tr, stateTypeForUsage, triggerTypeForUsage);
                                    }
                                    Sb.AppendLine("return;  // Transition handled");
                                }
                            }
                            Sb.AppendLine("default: break;");
                        }
                        Sb.AppendLine("break;");
                    }
                }
                Sb.AppendLine("default: break;");
            }
            
            // Move to parent state
            Sb.AppendLine();
            Sb.AppendLine("// Check parent state");
            Sb.AppendLine("stateToCheck = s_parent[stateToCheck];");
        }
        
        Sb.AppendLine();
        
        // Hook – przejście nie znalezione
        WriteTransitionFailureHook(stateTypeForUsage, triggerTypeForUsage);
    }
    
    private void WriteInternalTransitionOnAncestor(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        // Internal transition on ancestor - execute guard and action but no state change
        
        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);
        
        // Guard check
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }
        
        // No OnExit, no state change, no OnEntry for internal transitions on ancestors
        
        // Action (no exception catching - let it propagate)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }
        
        Sb.AppendLine($"{SuccessVar} = true;");
        
        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);
        
        WriteLogStatement("Information",
            $"InternalTransitionOnAncestor(_logger, _instanceId, \"{transition.FromState}\", CurrentState.ToString(), \"{transition.Trigger}\");");
    }

    protected virtual void WriteTransitionLogic(
     TransitionModel transition,
     string stateTypeForUsage,
     string triggerTypeForUsage)
    {
        if (Model.HierarchyEnabled && !transition.IsInternal)
        {
            WriteTransitionLogicHierarchical(transition, stateTypeForUsage, triggerTypeForUsage);
        }
        else
        {
            WriteTransitionLogicFlat(transition, stateTypeForUsage, triggerTypeForUsage);
        }
    }
    
    private void WriteTransitionLogicFlat(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard check
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            WriteOnExitCall(fromStateDef, transition.ExpectedPayloadType);
            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
        }

        // State change (before OnEntry)
        if (!transition.IsInternal)
        {
            Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
        }

        // OnEntry (no exception catching - let it propagate)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            WriteOnEntryCall(toStateDef, transition.ExpectedPayloadType);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
        }

        // Action (after OnEntry, no exception catching - let it propagate)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }
        
        // Log successful transition only after OnEntry succeeds
        if (!transition.IsInternal)
        {
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        Sb.AppendLine($"goto {EndOfTryFireLabel};");
    }
    
    private void WriteTransitionLogicHierarchical(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        var hasOnEntryExit = ShouldGenerateOnEntryExit();
        
        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);
        
        // Guard check
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }
        
        // For hierarchical transitions, we need to:
        // 1. Find the LCA (Lowest Common Ancestor) of source and target
        // 2. Exit from current leaf up to (but not including) LCA
        // 3. Enter from (but not including) LCA down to target leaf
        
        Sb.AppendLine("// Hierarchical transition sequence");
        
        // Get state indices
        var allStates = Model.States.Keys.OrderBy(s => s).ToList();
        var fromIndex = allStates.IndexOf(transition.FromState);
        var toIndex = allStates.IndexOf(transition.ToState);
        
        // Compute LCA at generation time if possible
        var lcaIndex = ComputeLCA(fromIndex, toIndex, allStates);
        var lcaState = lcaIndex >= 0 ? allStates[lcaIndex] : "None";
        
        // Calculate exit and entry counts for logging
        var exitCount = 0;
        var entryCount = 0;
        
        // Count exit states
        var tempIndex = fromIndex;
        while (tempIndex >= 0 && tempIndex != lcaIndex)
        {
            exitCount++;
            var state = allStates[tempIndex];
            if (Model.ParentOf.TryGetValue(state, out var parent) && parent != null)
            {
                tempIndex = allStates.IndexOf(parent);
            }
            else
            {
                break;
            }
        }
        
        // Count entry states
        tempIndex = toIndex;
        while (tempIndex >= 0 && tempIndex != lcaIndex)
        {
            entryCount++;
            var state = allStates[tempIndex];
            if (Model.ParentOf.TryGetValue(state, out var parent) && parent != null)
            {
                tempIndex = allStates.IndexOf(parent);
            }
            else
            {
                break;
            }
        }
        
        // Log hierarchical transition details
        WriteLogStatement("Debug",
            $"HierarchicalTransition(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{lcaState}\", {exitCount}, {entryCount});");
        
        if (hasOnEntryExit)
        {
            // Exit sequence - from current state up to (but not including) LCA
            WriteExitSequence(transition.FromState, lcaIndex, allStates, stateTypeForUsage, transition.ExpectedPayloadType);
        }
        
        // Update history tracking before state change
        if (Model.HierarchyEnabled)
        {
            Sb.AppendLine("// Update history tracking");
            Sb.AppendLine($"UpdateLastActiveChild((int){CurrentStateField});");
        }
        
        // State change
        // For composite states, we need to find the actual leaf target
        WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
        
        if (hasOnEntryExit)
        {
            // Entry sequence - from (but not including) LCA down to target
            WriteEntrySequence(lcaIndex, transition.ToState, allStates, stateTypeForUsage, transition.ExpectedPayloadType);
        }
        
        // Action (after OnEntry sequence)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }
        
        // Log successful transition only after OnEntry succeeds
        if (!transition.IsInternal)
        {
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        Sb.AppendLine($"goto {EndOfTryFireLabel};");
    }
    
    private int ComputeLCA(int fromIndex, int toIndex, List<string> allStates)
    {
        // Build ancestor chains for both states
        var fromAncestors = new List<int>();
        var current = fromIndex;
        while (current >= 0)
        {
            fromAncestors.Add(current);
            var state = allStates[current];
            if (Model.ParentOf.TryGetValue(state, out var parent) && parent != null)
            {
                current = allStates.IndexOf(parent);
            }
            else
            {
                current = -1;
            }
        }
        
        var toAncestors = new List<int>();
        current = toIndex;
        while (current >= 0)
        {
            toAncestors.Add(current);
            var state = allStates[current];
            if (Model.ParentOf.TryGetValue(state, out var parent) && parent != null)
            {
                current = allStates.IndexOf(parent);
            }
            else
            {
                current = -1;
            }
        }
        
        // Find LCA - the first common ancestor
        foreach (var ancestor in fromAncestors)
        {
            if (toAncestors.Contains(ancestor))
            {
                return ancestor;
            }
        }
        
        return -1; // No common ancestor (shouldn't happen in valid hierarchy)
    }
    
    private void WriteExitSequence(string fromState, int lcaIndex, List<string> allStates, string stateTypeForUsage, string? expectedPayloadType)
    {
        // Build exit path from current state up to (but not including) LCA
        var exitPath = new List<string>();
        var current = fromState;
        var currentIndex = allStates.IndexOf(current);
        
        while (currentIndex >= 0 && currentIndex != lcaIndex)
        {
            exitPath.Add(current);
            if (Model.ParentOf.TryGetValue(current, out var parent) && parent != null)
            {
                current = parent;
                currentIndex = allStates.IndexOf(current);
            }
            else
            {
                break;
            }
        }
        
        // Execute OnExit callbacks in order (from leaf to ancestor)
        foreach (var state in exitPath)
        {
            if (Model.States.TryGetValue(state, out var stateDef) && !string.IsNullOrEmpty(stateDef.OnExitMethod))
            {
                Sb.AppendLine($"// Exit {state}");
                WriteOnExitCall(stateDef, expectedPayloadType);
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{stateDef.OnExitMethod}\", \"{state}\");");
            }
        }
    }
    
    private void WriteEntrySequence(int lcaIndex, string toState, List<string> allStates, string stateTypeForUsage, string? expectedPayloadType)
    {
        // Build entry path from (but not including) LCA down to target
        var entryPath = new List<string>();
        var current = toState;
        var currentIndex = allStates.IndexOf(current);
        
        // Build path from target up to LCA
        while (currentIndex >= 0 && currentIndex != lcaIndex)
        {
            entryPath.Add(current);
            if (Model.ParentOf.TryGetValue(current, out var parent) && parent != null)
            {
                current = parent;
                currentIndex = allStates.IndexOf(current);
            }
            else
            {
                break;
            }
        }
        
        // Reverse to get top-down order (ancestor to leaf)
        entryPath.Reverse();
        
        // Execute OnEntry callbacks in order (from ancestor to leaf)
        foreach (var state in entryPath)
        {
            if (Model.States.TryGetValue(state, out var stateDef) && !string.IsNullOrEmpty(stateDef.OnEntryMethod))
            {
                Sb.AppendLine($"// Enter {state}");
                WriteOnEntryCall(stateDef, expectedPayloadType);
                WriteLogStatement("Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{stateDef.OnEntryMethod}\", \"{state}\");");
            }
        }
    }
    
    private void WriteStateChangeWithCompositeHandling(string targetState, string stateTypeForUsage)
    {
        // Check if target is a composite state
        var allStates = Model.States.Keys.OrderBy(s => s).ToList();
        var targetIndex = allStates.IndexOf(targetState);
        
        // Check if target has children (is composite)
        if (Model.ChildrenOf.TryGetValue(targetState, out var children) && children.Count > 0)
        {
            Sb.AppendLine($"// Target is composite, resolve to leaf using initial/history");
            Sb.AppendLine($"var targetIndex = {targetIndex};");
            Sb.AppendLine($"var resolvedTarget = GetCompositeEntryTarget(targetIndex);");
            Sb.AppendLine($"{CurrentStateField} = ({stateTypeForUsage})resolvedTarget;");
            
            // Log composite state resolution
            if (ShouldGenerateLogging)
            {
                // Determine resolution method based on history mode
                var resolutionMethod = "Initial";
                if (Model.HistoryOf.TryGetValue(targetState, out var historyMode) && historyMode != Generator.Model.HistoryMode.None)
                {
                    resolutionMethod = historyMode.ToString() + "History";
                }
                
                WriteLogStatement("Debug",
                    $"CompositeStateEntry(_logger, _instanceId, \"{targetState}\", (({stateTypeForUsage})resolvedTarget).ToString(), \"{resolutionMethod}\");");
            }
        }
        else
        {
            // Simple state change
            Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(targetState)};");
        }
    }

    #endregion

    #region Template Method Hooks

    protected virtual void WriteBeforeTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    protected virtual void WriteGuardEvaluationHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    protected virtual void WriteAfterGuardEvaluatedHook(
        TransitionModel transition,
        string guardResultVar,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    protected virtual void WriteAfterTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage,
        bool success)
    { }

    protected virtual void WriteTransitionFailureHook(
        string stateTypeForUsage,
        string triggerTypeForUsage)
    { }

    #endregion

    #region Virtual Methods for Customization

    protected virtual bool ShouldGenerateInitialOnEntry() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual bool ShouldGenerateOnEntryExit() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual void WriteGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Owijamy całą logikę guard w try-catch
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            Sb.AddProperty($"bool {GuardResultVar}", $"{transition.GuardMethod}()");

            // Hook: After guard evaluated
            WriteAfterGuardEvaluatedHook(transition, GuardResultVar, stateTypeForUsage, triggerTypeForUsage);

            using (Sb.Block($"if (!{GuardResultVar})"))
            {
                WriteLogStatement("Warning",
                    $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteLogStatement("Warning",
                    $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

                Sb.AppendLine($"{SuccessVar} = false;");

                // Hook: After failed transition
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);

                Sb.AppendLine($"goto {EndOfTryFireLabel};");
            }
        }
        Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
        using (Sb.Block(""))
        {
            // Traktujemy wyjątek w guard jako false (guard nie przeszedł)
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

            Sb.AppendLine($"{SuccessVar} = false;");

            // Hook: After failed transition
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);

            // Skok do końca metody
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }

    protected virtual void WriteActionCall(TransitionModel transition)
    {
        if (string.IsNullOrEmpty(transition.ActionMethod)) return;

        // For base implementation, use CallbackGenerationHelper which handles CancellationToken properly
        CallbackGenerationHelper.EmitActionCall(
            Sb,
            transition,
            payloadVar: "null",
            IsAsyncMachine,
            wrapInTryCatch: false,
            Model.ContinueOnCapturedContext,
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: false
        );
    }

    protected virtual void WriteOnEntryCall(StateModel state, string? expectedPayloadType)
    {
        if (string.IsNullOrEmpty(state.OnEntryMethod)) return;

        // For base implementation, use CallbackGenerationHelper which handles CancellationToken properly
        CallbackGenerationHelper.EmitOnEntryCall(
            Sb,
            state,
            expectedPayloadType: null,
            defaultPayloadType: null,
            payloadVar: "null",
            IsAsyncMachine,
            wrapInTryCatch: false,
            Model.ContinueOnCapturedContext,
            isSinglePayload: false,
            isMultiPayload: false,
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: false
        );
    }

    protected virtual void WriteOnExitCall(StateModel fromState, string? expectedPayloadType)
    {
        if (string.IsNullOrEmpty(fromState.OnExitMethod)) return;

        // For base implementation, use CallbackGenerationHelper which handles CancellationToken properly
        CallbackGenerationHelper.EmitOnExitCall(
            Sb,
            fromState,
            expectedPayloadType: null,
            defaultPayloadType: null,
            payloadVar: "null",
            IsAsyncMachine,
            wrapInTryCatch: false,
            Model.ContinueOnCapturedContext,
            isSinglePayload: false,
            isMultiPayload: false,
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: false
        );
    }

    #endregion

    #region Helper Methods

    protected void WriteMethodAttribute() =>
        Sb.AppendLine($"[{Strings.MethodImplAttribute}({AggressiveInliningAttribute})]");


    protected bool IsPayloadVariant() =>
        Model.Variant is GenerationVariant.WithPayload or GenerationVariant.Full;

    protected bool IsSinglePayloadVariant() =>
        IsPayloadVariant() && !Model.TriggerPayloadTypes.Any();

    protected bool IsMultiPayloadVariant() =>
        IsPayloadVariant() && Model.TriggerPayloadTypes.Any();

    protected bool IsExtensionsVariant() =>
        Model.Variant is GenerationVariant.WithExtensions or GenerationVariant.Full;

    protected string? GetSinglePayloadType()
    {
        if (IsSinglePayloadVariant() && Model.DefaultPayloadType != null)
        {
            return Model.DefaultPayloadType;
        }
        return null;
    }

    protected HashSet<string> GetAllPayloadTypes()
    {
        var types = new HashSet<string>();

        if (Model.DefaultPayloadType != null)
        {
            types.Add(Model.DefaultPayloadType);
        }

        foreach (var payloadType in Model.TriggerPayloadTypes.Values)
        {
            types.Add(payloadType);
        }

        return types;
    }

    protected List<string> BuildConstructorParameters(string stateTypeForUsage, params string[] extras)
    {
        var parameters = new List<string> { $"{stateTypeForUsage} initialState" };
        parameters.AddRange(extras.Where(e => !string.IsNullOrWhiteSpace(e)));
        return parameters;
    }

    #endregion

    #region Header Generation

    protected virtual void WriteHeader()
    {
        Sb.AppendLine("// <auto-generated/>");
        Sb.AppendLine("#nullable enable");

        // Standard usings
        AddUsing(NamespaceSystem);
        AddUsing(NamespaceSystemCollectionsGeneric);
        AddUsing(NamespaceSystemLinq);
        AddUsing(NamespaceSystemRuntimeCompilerServices);
        AddUsing(NamespaceStateMachineContracts);
        AddUsing(NamespaceStateMachineRuntime);

        if (IsExtensionsVariant())
        {
            AddUsing(NamespaceStateMachineRuntimeExtensions);
        }

        if (ShouldGenerateLogging)
        {
            AddUsing(NamespaceMicrosoftExtensionsLogging);
        }
        if (IsAsyncMachine)
        {
            AddUsing("System.Threading");
            AddUsing("System.Threading.Tasks");
            AddUsing("StateMachine.Exceptions");
        }
        
        if (Model.ExceptionHandler != null)
        {
            AddUsing(NamespaceStateMachineExceptions);
        }
        // Type-specific namespaces
        var allNamespaces = new HashSet<string>();
        allNamespaces.UnionWith(TypeHelper.GetRequiredNamespaces(Model.StateType));
        allNamespaces.UnionWith(TypeHelper.GetRequiredNamespaces(Model.TriggerType));

        if (IsPayloadVariant())
        {
            foreach (var payload in GetAllPayloadTypes())
            {
                allNamespaces.UnionWith(TypeHelper.GetRequiredNamespaces(payload));
            }
        }

        // Filter out standard namespaces and add remaining
        foreach (var ns in allNamespaces.OrderBy(n => n))
        {
            if (ns is NamespaceSystem or NamespaceSystemCollectionsGeneric or
                NamespaceSystemLinq or NamespaceSystemRuntimeCompilerServices or
                NamespaceStateMachineContracts or NamespaceStateMachineRuntime)
                continue;

            AddUsing(ns);
        }

        // Hook for additional usings
        foreach (var ns in GetAdditionalUsings().OrderBy(n => n))
        {
            AddUsing(ns);
        }

        Sb.AppendLine();
    }

    protected virtual IEnumerable<string> GetAdditionalUsings()
    {
        var usings = new List<string>();
        
        // Add Abstractions.Attributes for HSM (HistoryMode enum)
        if (Model.HierarchyEnabled)
        {
            usings.Add("Abstractions.Attributes");
        }
        
        return usings;
    }

    #endregion

    #region Type Name Handling

    protected string GetTypeNameForUsage(string fullyQualifiedName) =>
        TypeHelper.FormatTypeForUsage(fullyQualifiedName, useGlobalPrefix: false);

    #endregion

    #region Common Methods

    protected virtual void WriteCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine("/// <returns>True if the trigger can be fired, false otherwise</returns>");
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool CanFireInternal({triggerTypeForUsage} trigger)"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);

                foreach (var stateName in allHandledFromStates)
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                    using (Sb.Indent())
                    {
                        using (Sb.Block("switch (trigger)"))
                        {
                            var transitionsFromThisState = Model.Transitions
                                .Where(t => t.FromState == stateName);

                            foreach (var transition in transitionsFromThisState)
                            {
                                Sb.AppendLine($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                                using (Sb.Indent())
                                {
if (!string.IsNullOrEmpty(transition.GuardMethod))
{
    // Generate guard call with exception handling
    GuardGenerationHelper.EmitGuardCheck(
        Sb,
        transition,
        "guardResult",
        "null",
        IsAsyncMachine,
        wrapInTryCatch: true,
        Model.ContinueOnCapturedContext,
        handleResultAfterTry: true  // <- zadeklaruje zmienną przed try
    );
    Sb.AppendLine("return guardResult;");
}
                                    else
                                    {
                                        Sb.AppendLine("return true;");
                                    }
                                }
                            }
                            Sb.AppendLine("default: return false;");
                        }
                    }
                }
                Sb.AppendLine("default: return false;");
            }
        }
        Sb.AppendLine();
    }

    protected virtual void WriteGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <returns>List of triggers that can be fired in the current state</returns>");
        using (Sb.Block($"protected override {ReadOnlyListType}<{triggerTypeForUsage}> GetPermittedTriggersInternal()"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
                {
                    var stateName = stateGroup.Key;
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                    using (Sb.Block(""))
                    {
                        // Check if any transition has a guard
                        var hasGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod));

                        if (!hasGuards)
                        {
                            // No guards - return static array
                            var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                            if (triggers.Any())
                            {
                                var triggerList = string.Join(", ", triggers.Select(t => $"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(t)}"));
                                Sb.AppendLine($"return new {triggerTypeForUsage}[] {{ {triggerList} }};");
                            }
                            else
                            {
                                Sb.AppendLine($"return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                            }
                        }
                        else
                        {
                            // Has guards - build list dynamically
                            Sb.AppendLine($"var permitted = new List<{triggerTypeForUsage}>();");

                            foreach (var transition in stateGroup)
                            {
                                if (!string.IsNullOrEmpty(transition.GuardMethod))
                                {
                                    WriteGuardCall(transition, "canFire", "null", throwOnException: false);
                                    using (Sb.Block("if (canFire)"))
                                    {
                                        Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                    }
                                }
                                else
                                {
                                    Sb.AppendLine($"permitted.Add({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                }
                            }

                            Sb.AppendLine("return permitted.Count == 0 ? ");
                            using (Sb.Indent())
                            {
                                Sb.AppendLine($"{ArrayEmptyMethod}<{triggerTypeForUsage}>() :");
                                Sb.AppendLine("permitted.ToArray();");
                            }
                        }
                    }
                }

                var statesWithNoOutgoingTransitions = Model.States.Keys
                    .Except(transitionsByFromState.Select(g => g.Key))
                    .OrderBy(s => s);

                foreach (var stateName in statesWithNoOutgoingTransitions)
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                }

                Sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
            }
        }
        Sb.AppendLine();
    }

    /// <summary>
    /// Writes structural API methods if enabled
    /// </summary>
    protected void WriteStructuralApiMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (!Model.EmitStructuralHelpers)
            return;

        WriteHasTransitionMethod(stateTypeForUsage, triggerTypeForUsage);
        WriteGetDefinedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
    }

    /// <summary>
    /// Writes HasTransition method for structural analysis
    /// </summary>
    protected void WriteHasTransitionMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Checks if a transition is defined in the state machine structure (ignores guards)");
        Sb.AppendLine("/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine("/// <returns>True if a transition is defined for the trigger in current state, false otherwise</returns>");
        WriteMethodAttribute();
        using (Sb.Block($"public bool HasTransition({triggerTypeForUsage} trigger)"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
                {
                    var stateName = stateGroup.Key;
                    var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();

                    if (triggers.Any())
                    {
                        Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                        using (Sb.Indent())
                        {
                            using (Sb.Block("switch (trigger)"))
                            {
                                foreach (var trigger in triggers)
                                {
                                    Sb.AppendLine($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(trigger)}: return true;");
                                }
                                Sb.AppendLine("default: return false;");
                            }
                        }
                    }
                }

                Sb.AppendLine("default: return false;");
            }
        }
        Sb.AppendLine();
    }

    /// <summary>
    /// Writes GetDefinedTriggers method for structural analysis
    /// </summary>
    protected void WriteGetDefinedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Gets all triggers defined for the current state in the state machine structure (ignores guards)");
        Sb.AppendLine("/// <returns>List of all triggers defined for the current state, regardless of guard conditions</returns>");
        using (Sb.Block($"public {ReadOnlyListType}<{triggerTypeForUsage}> GetDefinedTriggers()"))
        {
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
                {
                    var stateName = stateGroup.Key;
                    var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();

                    Sb.Append($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}: return ");
                    if (triggers.Any())
                    {
                        var triggerList = string.Join(", ", triggers.Select(t => $"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(t)}"));
                        Sb.AppendLine($"new {triggerTypeForUsage}[] {{ {triggerList} }};");
                    }
                    else
                    {
                        Sb.AppendLine($"{ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                    }
                }

                var statesWithNoOutgoingTransitions = Model.States.Keys
                    .Except(transitionsByFromState.Select(g => g.Key))
                    .OrderBy(s => s);

                foreach (var stateName in statesWithNoOutgoingTransitions)
                {
                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
                }

                Sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerTypeForUsage}>();");
            }
        }
        Sb.AppendLine();
    }

    protected void WriteLoggerField(string className)
    {
        if (!ShouldGenerateLogging) return;
        LoggingClassGenerator.WriteLoggerField(className, ref Sb);
    }

    protected string GetLoggerConstructorParameter(string className) =>
        ShouldGenerateLogging ? LoggingClassGenerator.GetLoggerConstructorParameter(className, ref Sb) : string.Empty;

    protected void WriteLoggerAssignment()
    {
        if (!ShouldGenerateLogging) return;
        LoggingClassGenerator.WriteLoggerAssignment(ref Sb);
    }

    protected void WriteLogStatement(string logLevel, string logMethodCall)
    {
        if (!ShouldGenerateLogging) return;
        LoggingClassGenerator.WriteLogStatement(Model.ClassName, logLevel, logMethodCall, ref Sb);
    }

    protected void AddUsing(string usingStatement)
    {
        if (AddedUsings.Add(usingStatement))
        {
            Sb.AppendLine($"using {usingStatement};");
        }
    }

    #endregion

    #endregion

    #region Guard Call Helpers

    /// <summary>
    /// Generates code to call a guard method with proper exception handling
    /// </summary>

    protected void WriteGuardCall(
        TransitionModel transition,
        string resultVar,
        string payloadVar = "null",
        bool throwOnException = false)
    {
        GuardGenerationHelper.EmitGuardCheck(
            Sb,
            transition,
            resultVar,
            payloadVar,
            IsAsyncMachine,
            wrapInTryCatch: !throwOnException,
            Model.ContinueOnCapturedContext,
            handleResultAfterTry: true,
            cancellationTokenVar: GetCtVar(),
            treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
        );
    }

    #endregion

    #region Async helpers
    protected string GetMethodReturnType(string syncReturnType) => AsyncGenerationHelper.GetReturnType(syncReturnType, IsAsyncMachine);

    protected string GetAsyncKeyword() => AsyncGenerationHelper.GetMethodModifiers(IsAsyncMachine);

    protected string GetAwaitKeyword() => AsyncGenerationHelper.GetAwaitKeyword(true, IsAsyncMachine);

    protected string GetConfigureAwait() => AsyncGenerationHelper.GetConfigureAwait(IsAsyncMachine, Model.ContinueOnCapturedContext);

    /// <summary>
    /// Returns the cancellation token variable name or CancellationToken.None for sync machines.
    /// </summary>
    protected string GetCtVar() => IsAsyncMachine
        ? "cancellationToken"
        : "System.Threading.CancellationToken.None";

    protected string GetTryFireMethodName() =>
        AsyncGenerationHelper.GetMethodName("TryFire", IsAsyncMachine, addAsyncSuffix: false) +
        (IsAsyncMachine ? "InternalAsync" : "Internal");

    // Helper do parametrów metody
    protected string GetTryFireParameters(string triggerType)
    {
        return IsAsyncMachine
            ? $"{triggerType} trigger, object? payload, CancellationToken cancellationToken"
            : $"{triggerType} trigger, object? payload = null";
    }

    protected void WriteCallbackInvocation(string methodName, bool isCallbackAsync, string? payload = null)
    {
        var args = payload is not null ? [payload] : Array.Empty<string>();
        AsyncGenerationHelper.EmitMethodInvocation(
            Sb,
            methodName,
            isCallbackAsync,
            IsAsyncMachine,
            Model.ContinueOnCapturedContext,
            args
        );
    }

    protected string GetBaseClassName(string stateType, string triggerType) => AsyncGenerationHelper.GetBaseClassName(stateType, triggerType, IsAsyncMachine);
    protected string GetInterfaceName(string stateType, string triggerType) => AsyncGenerationHelper.GetInterfaceName(stateType, triggerType, IsAsyncMachine);

    /// <summary>
    /// Zwraca nazwę metody z odpowiednim sufiksem.
    /// </summary>
    protected string GetMethodName(string baseName, bool addAsyncSuffix = true) => AsyncGenerationHelper.GetMethodName(baseName, IsAsyncMachine, addAsyncSuffix);

    /// <summary>
    /// Zwraca visibility dla metody TryFire.
    /// </summary>
    protected string GetTryFireVisibility()
    {
        return "protected override";
    }
    #endregion

    #region Exception Handling Helpers

    /// <summary>
    /// Emits OnEntry call with optional exception policy wrapping.
    /// </summary>
    protected void EmitOnEntryWithExceptionPolicy(
        StateModel toStateDef,
        string? expectedPayloadType,
        string fromState,
        string toState,
        string trigger)
    {
        if (Model.ExceptionHandler == null)
        {
            // No exception handler - use existing logic
            WriteOnEntryCall(toStateDef, expectedPayloadType);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
            return;
        }

        // Wrap in try/catch with exception policy
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            WriteOnEntryCall(toStateDef, expectedPayloadType);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
        }
        Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
        using (Sb.Block(""))
        {
            EmitExceptionHandlerCall(fromState, toState, trigger, "TransitionStage.OnEntry", true);
        }
    }

    /// <summary>
    /// Emits Action call with optional exception policy wrapping.
    /// </summary>
    protected void EmitActionWithExceptionPolicy(
        TransitionModel transition,
        string fromState,
        string toState)
    {
        if (Model.ExceptionHandler == null)
        {
            // No exception handler - use existing logic
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            return;
        }

        // Wrap in try/catch with exception policy
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
        }
        Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
        using (Sb.Block(""))
        {
            EmitExceptionHandlerCall(fromState, toState, transition.Trigger, "TransitionStage.Action", true);
        }
    }

    /// <summary>
    /// Emits OnEntry call with optional exception policy wrapping (for payload variant).
    /// </summary>
    protected void EmitOnEntryWithExceptionPolicyPayload(
        StateModel toStateDef,
        string? expectedPayloadType,
        string defaultPayloadType,
        string fromState,
        string toState,
        string trigger,
        bool isSinglePayload,
        bool isMultiPayload)
    {
        if (Model.ExceptionHandler == null)
        {
            // No exception handler - use existing logic
            CallbackGenerationHelper.EmitOnEntryCall(
                Sb,
                toStateDef,
                expectedPayloadType,
                defaultPayloadType,
                PayloadVar,
                IsAsyncMachine,
                wrapInTryCatch: false,
                Model.ContinueOnCapturedContext,
                isSinglePayload,
                isMultiPayload,
                cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                treatCancellationAsFailure: IsAsyncMachine
            );
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
            return;
        }

        // Wrap in try/catch with exception policy
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            CallbackGenerationHelper.EmitOnEntryCall(
                Sb,
                toStateDef,
                expectedPayloadType,
                defaultPayloadType,
                PayloadVar,
                IsAsyncMachine,
                wrapInTryCatch: false,
                Model.ContinueOnCapturedContext,
                isSinglePayload,
                isMultiPayload,
                cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                treatCancellationAsFailure: IsAsyncMachine
            );
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
        }
        Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
        using (Sb.Block(""))
        {
            EmitExceptionHandlerCall(fromState, toState, trigger, "TransitionStage.OnEntry", true);
        }
    }

    /// <summary>
    /// Emits Action call with optional exception policy wrapping (for payload variant).
    /// </summary>
    protected void EmitActionWithExceptionPolicyPayload(
        TransitionModel transition,
        string fromState,
        string toState)
    {
        if (Model.ExceptionHandler == null)
        {
            // No exception handler - use existing logic
            CallbackGenerationHelper.EmitActionCall(
                Sb,
                transition,
                PayloadVar,
                IsAsyncMachine,
                wrapInTryCatch: false,
                Model.ContinueOnCapturedContext,
                cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                treatCancellationAsFailure: IsAsyncMachine
            );
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            return;
        }

        // Wrap in try/catch with exception policy
        Sb.AppendLine("try");
        using (Sb.Block(""))
        {
            CallbackGenerationHelper.EmitActionCall(
                Sb,
                transition,
                PayloadVar,
                IsAsyncMachine,
                wrapInTryCatch: false,
                Model.ContinueOnCapturedContext,
                cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                treatCancellationAsFailure: IsAsyncMachine
            );
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
        }
        Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
        using (Sb.Block(""))
        {
            EmitExceptionHandlerCall(fromState, toState, transition.Trigger, "TransitionStage.Action", true);
        }
    }

    /// <summary>
    /// Emits the call to the exception handler and handles the directive.
    /// </summary>
    private void EmitExceptionHandlerCall(
        string fromState,
        string toState,
        string trigger,
        string stage,
        bool stateAlreadyChanged)
    {
        var handler = Model.ExceptionHandler!;
        var stateType = GetTypeNameForUsage(Model.StateType);
        var triggerType = GetTypeNameForUsage(Model.TriggerType);

        // Create exception context
        Sb.AppendLine($"var exceptionContext = new {handler.ExceptionContextClosedType}(");
        using (Sb.Indent())
        {
            Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(fromState)},");
            Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(toState)},");
            Sb.AppendLine($"{triggerType}.{TypeHelper.EscapeIdentifier(trigger)},");
            Sb.AppendLine("ex,");
            Sb.AppendLine($"{stage},");
            Sb.AppendLine($"{stateAlreadyChanged.ToString().ToLowerInvariant()});");
        }

        // Call handler
        string directiveVar = "directive";
        if (handler.IsAsync)
        {
            var args = handler.AcceptsCancellationToken
                ? "exceptionContext, cancellationToken"
                : "exceptionContext";
            Sb.AppendLine($"var {directiveVar} = await {handler.MethodName}({args}).ConfigureAwait({Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
        }
        else
        {
            var args = handler.AcceptsCancellationToken
                ? "exceptionContext, cancellationToken"
                : "exceptionContext";
            Sb.AppendLine($"var {directiveVar} = {handler.MethodName}({args});");
        }

        // Apply directive
        using (Sb.Block($"if ({directiveVar} != ExceptionDirective.Continue)"))
        {
            Sb.AppendLine("throw;");
        }
        Sb.AppendLine("// Exception swallowed by Continue directive");
    }

    #endregion

    #region Abstractions to be implemented by concrete generators
    protected abstract void WriteNamespaceAndClass();
    #endregion
}