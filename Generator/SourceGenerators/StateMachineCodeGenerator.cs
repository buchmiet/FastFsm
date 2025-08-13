#nullable enable
using Abstractions.Attributes;
using Generator.Helpers;
using Generator.Infrastructure;

using Generator.Model;
using Generator.Planning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Generator.Log;
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
        // FSM990_HSM_FLAG: Log before writing HSM blocks
        Sb.AppendLine($"// FSM990_HSM_FLAG [4-WriteHSM] {Model.ClassName}: HierarchyEnabled={Model.HierarchyEnabled}");
        
        if (!Model.HierarchyEnabled) return;
        
        Sb.AppendLine("// Hierarchical state machine support arrays");
        
        // Get all states in enum ordinal order (by their numeric value, not alphabetically)
        var allStates = Model.States.Values
            .OrderBy(s => s.OrdinalValue)
            .Select(s => s.Name)
            .ToList();
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
        
        // Add DEBUG-only DumpActivePath helper
        Sb.AppendLine("#if DEBUG");
        EmitXmlDocSummary(Sb, "Returns the active path from the root composite down to the current leaf state, e.g. \"Working / Working_Initializing\". DEBUG-only helper to diagnose hierarchy.");
        using (Sb.Block("public string DumpActivePath()"))
        {
            Sb.AppendLine("const int NO_PARENT = -1;");
            Sb.AppendLine();
            Sb.AppendLine("var sb = new global::System.Text.StringBuilder(64);");
            Sb.AppendLine($"var current = {CurrentStateField}; // {stateTypeForUsage} enum");
            Sb.AppendLine("// Convert enum to index:");
            Sb.AppendLine("int idx = (int)(object)current;");
            Sb.AppendLine();
            Sb.AppendLine("// Seed with leaf");
            Sb.AppendLine("sb.Insert(0, current.ToString());");
            Sb.AppendLine();
            Sb.AppendLine("// Walk up to root");
            using (Sb.Block("while (true)"))
            {
                Sb.AppendLine("int parent = s_parent[idx];");
                Sb.AppendLine("if (parent == NO_PARENT) break;");
                Sb.AppendLine();
                Sb.AppendLine("// Cast parent index back to enum");
                Sb.AppendLine($"current = ({stateTypeForUsage})(object)parent;");
                Sb.AppendLine("sb.Insert(0, \" / \");");
                Sb.AppendLine("sb.Insert(0, current.ToString());");
                Sb.AppendLine("idx = parent;");
            }
            Sb.AppendLine();
            Sb.AppendLine("return sb.ToString();");
        }
        Sb.AppendLine("#endif");
        Sb.AppendLine();
        
        // Add IsInHierarchy helper (available in both Debug and Release)
        EmitXmlDocSummary(Sb, "Returns true if the current state lies in the hierarchy of the given ancestor (i.e., ancestor is the current leaf or any of its parents).");
        Sb.AppendLine("/// <param name=\"ancestor\">The potential ancestor state to check</param>");
        Sb.AppendLine("/// <returns>True if ancestor is the current state or any of its parents, false otherwise</returns>");
        using (Sb.Block($"public bool IsInHierarchy({stateTypeForUsage} ancestor)"))
        {
            Sb.AppendLine("const int NO_PARENT = -1;");
            Sb.AppendLine("int idx = (int)(object)_currentState;");
            Sb.AppendLine("int ancIdx = (int)(object)ancestor;");
            Sb.AppendLine();
            Sb.AppendLine("// Bounds check");
            Sb.AppendLine("if ((uint)idx >= (uint)s_parent.Length) return false;");
            Sb.AppendLine("if ((uint)ancIdx >= (uint)s_parent.Length) return false;");
            Sb.AppendLine();
            Sb.AppendLine("// Check if ancestor is current state");
            Sb.AppendLine("if (idx == ancIdx) return true;");
            Sb.AppendLine();
            Sb.AppendLine("// Walk up parent chain");
            using (Sb.Block("while (true)"))
            {
                Sb.AppendLine("int parent = s_parent[idx];");
                Sb.AppendLine("if (parent == NO_PARENT) return false;");
                Sb.AppendLine("if (parent == ancIdx) return true;");
                Sb.AppendLine("idx = parent;");
            }
        }
        Sb.AppendLine();
    }
    
    /// <summary>
    /// Writes HSM runtime fields and helper methods (instance-level) if HSM is enabled.
    /// </summary>
    protected virtual void WriteHierarchyRuntimeFieldsAndHelpers(string stateTypeForUsage)
    {
        if (!Model.HierarchyEnabled) return;

        // Instance array for SHALLOW/DEEP history bookkeeping (index by composite state)
        // Initialize with -1 to distinguish "never visited" from "visited state 0"
        Sb.AppendLine("        private readonly int[] _lastActiveChild;");
        Sb.AppendLine();

        // RecordHistoryForCurrentPath: walks up the parent chain recording current leaf for history
        Sb.WriteSummary("Records the current leaf state in all ancestor composite states that have history enabled.");
        Sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        using (Sb.Block("private void RecordHistoryForCurrentPath()"))
        {
            Sb.AppendLine("int leaf = (int)_currentState;");
            Sb.AppendLine("int parent = (uint)leaf < (uint)s_parent.Length ? s_parent[leaf] : -1;");
            using (Sb.Block("while (parent >= 0)"))
            {
                Sb.AppendLine("if (s_history[parent] != HistoryMode.None)");
                using (Sb.Indent())
                    Sb.AppendLine("_lastActiveChild[parent] = leaf;");
                Sb.AppendLine("leaf = parent;");
                Sb.AppendLine("parent = s_parent[leaf];");
            }
        }
        Sb.AppendLine();
        
        // UpdateLastActiveChild: called before changing state away from a composite
        Sb.WriteSummary("Records the last active child for the given composite parent.");
        using (Sb.Block("private void UpdateLastActiveChild(int parentIndex)"))
        {
            Sb.AppendLine("if ((uint)parentIndex < (uint)_lastActiveChild.Length)");
            using (Sb.Indent())
                Sb.AppendLine("_lastActiveChild[parentIndex] = (int)_currentState;");
        }
        Sb.AppendLine();

        // GetCompositeEntryTarget: resolve initial/history for a composite index → leaf index
        Sb.WriteSummary("Resolves the actual leaf to enter for a composite state, using Initial/History semantics.");
        using (Sb.Block("private int GetCompositeEntryTarget(int compositeIndex)"))
        {
            Sb.AppendLine("int idx = compositeIndex;");
            Sb.AppendLine("while (true)");
            using (Sb.Block(""))
            {
                Sb.AppendLine("// Check if this is a leaf (no initial child)");
                Sb.AppendLine("if ((uint)idx >= (uint)s_initialChild.Length || s_initialChild[idx] < 0)");
                Sb.AppendLine("    return idx;");
                Sb.AppendLine();
                Sb.AppendLine("// Check for history");
                Sb.AppendLine("var mode = s_history[idx];");
                Sb.AppendLine("int child = -1;");
                Sb.AppendLine();
                Sb.AppendLine("if (mode != HistoryMode.None && _lastActiveChild[idx] >= 0)");
                using (Sb.Block(""))
                {
                    Sb.AppendLine("int remembered = _lastActiveChild[idx];");
                    Sb.AppendLine();
                    Sb.AppendLine("if (mode == HistoryMode.Shallow)");
                    using (Sb.Block(""))
                    {
                        Sb.AppendLine("// Map remembered LEAF up to the IMMEDIATE child of 'idx'");
                        Sb.AppendLine("int immediate = remembered;");
                        Sb.AppendLine("while (immediate >= 0 && s_parent[immediate] != idx)");
                        Sb.AppendLine("    immediate = s_parent[immediate];");
                        Sb.AppendLine();
                        Sb.AppendLine("// Fallback to initial if something went wrong");
                        Sb.AppendLine("child = immediate >= 0 ? immediate : s_initialChild[idx];");
                    }
                    Sb.AppendLine("else // Deep");
                    using (Sb.Block(""))
                    {
                        Sb.AppendLine("child = remembered;");
                    }
                }
                Sb.AppendLine("else");
                using (Sb.Block(""))
                {
                    Sb.AppendLine("child = s_initialChild[idx];");
                }
                Sb.AppendLine();
                Sb.AppendLine("if (child < 0) return idx; // Safety check");
                Sb.AppendLine("idx = child; // Descend");
            }
        }
        Sb.AppendLine();

        // DescendToInitialIfComposite: applied at startup (and can be reused if needed)
        Sb.WriteSummary("If CurrentState is composite, resolves and assigns the leaf according to Initial/History.");
        using (Sb.Block("private void DescendToInitialIfComposite()"))
        {
            Sb.AppendLine("int currentIdx = (int)_currentState;");
            Sb.AppendLine("if ((uint)currentIdx >= (uint)s_initialChild.Length) return;");
            Sb.AppendLine("int initialChild = s_initialChild[currentIdx];");
            Sb.AppendLine("if (initialChild < 0) return; // Already a leaf");
            Sb.AppendLine("int resolved = GetCompositeEntryTarget(currentIdx);");
            Sb.AppendLine("_currentState = (" + stateTypeForUsage + ")resolved;");
        }
        Sb.AppendLine();
    }
    
    #endregion

    #region Common Implementation Methods

    protected virtual void WriteStartMethod()
    {
        // For debugging: Always generate Start() method if there are hierarchy arrays
        // Check if we have hierarchy by looking for parent/child relationships
        bool hasHierarchy = Model.ParentOf.Any() || Model.InitialChildOf.Any() || Model.States.Values.Any(s => s.History != Generator.Model.HistoryMode.None);
        
        if (!Model.HierarchyEnabled && !hasHierarchy) 
        {
            return;
        }
        
        if (IsAsyncMachine)
        {
            Sb.WriteSummary("Starts the state machine asynchronously and ensures proper HSM initialization.");
            Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
            using (Sb.Block("public override async ValueTask StartAsync(CancellationToken cancellationToken = default)"))
            {
                Sb.AppendLine("if (IsStarted) return;");
                Sb.AppendLine();
                Sb.AppendLine("// For HSM: resolve composite initial state to leaf before calling OnInitialEntryAsync");
                Sb.AppendLine("DescendToInitialIfComposite();");
                Sb.AppendLine();
                Sb.AppendLine("await base.StartAsync(cancellationToken).ConfigureAwait(" + Model.ContinueOnCapturedContext.ToString().ToLowerInvariant() + ");");
            }
        }
        else
        {
            Sb.WriteSummary("Starts the state machine and ensures proper HSM initialization.");
            using (Sb.Block("public override void Start()"))
            {
                Sb.AppendLine("if (IsStarted) return;");
                Sb.AppendLine();
                Sb.AppendLine("// For HSM: resolve composite initial state to leaf before calling OnInitialEntry");
                Sb.AppendLine("DescendToInitialIfComposite();");
                Sb.AppendLine();
                Sb.AppendLine("base.Start();");
            }
        }
        Sb.AppendLine();
    }

    protected virtual void WriteOnInitialEntryMethod(string stateTypeForUsage)
    {
        if (!ShouldGenerateInitialOnEntry())
            return;
            
        using (Sb.Block("protected override void OnInitialEntry()"))
        {
            if (Model.HierarchyEnabled)
            {
                // For HSM: Build entry chain from root to current leaf and call each OnEntry
                Sb.AppendLine("// Build entry chain from root to current leaf");
                Sb.AppendLine($"var entryChain = new List<{stateTypeForUsage}>();");
                Sb.AppendLine($"int currentIdx = (int){CurrentStateField};");
                Sb.AppendLine();
                
                // Build chain from leaf to root
                Sb.AppendLine("// Walk from leaf to root");
                using (Sb.Block("while (currentIdx >= 0)"))
                {
                    Sb.AppendLine($"entryChain.Add(({stateTypeForUsage})currentIdx);");
                    Sb.AppendLine("if ((uint)currentIdx >= (uint)s_parent.Length) break;");
                    Sb.AppendLine("currentIdx = s_parent[currentIdx];");
                }
                Sb.AppendLine();
                
                // Reverse to get root-to-leaf order
                Sb.AppendLine("// Reverse to get root-to-leaf order");
                Sb.AppendLine("entryChain.Reverse();");
                Sb.AppendLine();
                
                // Call OnEntry for each state in the chain that has one
                Sb.AppendLine("// Call OnEntry for each state in the chain");
                using (Sb.Block("foreach (var state in entryChain)"))
                {
                    using (Sb.Block("switch (state)"))
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
            }
            else
            {
                // Non-HSM: Original single-state entry
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
        // Sort transitions by priority (descending) then by declaration order
        var sortedTransitions = Model.Transitions
            .Select((t, index) => new { Transition = t, Index = index })
            .OrderByDescending(x => x.Transition.Priority)
            .ThenBy(x => x.Index)
            .Select(x => x.Transition);
            
        var grouped = sortedTransitions.GroupBy(t => t.FromState);

        // switch (CurrentState)
        using (Sb.Block($"switch ({CurrentStateField})"))
        {
            foreach (var state in grouped)
            {
                // case <State>:
                using (Sb.Block($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Key)}:"))
                {
                    Sb.AppendLine("{");
                    using (Sb.Indent())
                    {
                        // Group by trigger for this state
                        var triggerGroups = state.GroupBy(t => t.Trigger);
                        
                        // switch (trigger)
                        using (Sb.Block("switch (trigger)"))
                        {
                            foreach (var triggerGroup in triggerGroups)
                            {
                                // case <Trigger>:
                                using (Sb.Block($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(triggerGroup.Key)}:"))
                                {
                                    Sb.AppendLine("{");
                                    using (Sb.Indent())
                                    {
                                        // Process all transitions for this trigger in priority order
                                        foreach (var tr in triggerGroup)
                                        {
                                            Sb.AppendLine($"// Transition: {tr.FromState} -> {tr.ToState} (Priority: {tr.Priority})");
                                            WriteTransitionLogicDirect(tr, stateTypeForUsage, triggerTypeForUsage);
                                            // Only first matching transition executes due to return
                                            break;
                                        }
                                    }
                                    Sb.AppendLine("}");
                                }
                            }
                            Sb.AppendLine("default: break;");
                        }
                        Sb.AppendLine("break;");
                    }
                    Sb.AppendLine("}");
                }
            }
            Sb.AppendLine("default: break;");
        }
        
        Sb.AppendLine();
        Sb.AppendLine("return false;");
    }
    
    private void WriteTryFireStructureHierarchical(
        string stateTypeForUsage,
        string triggerTypeForUsage,
        Action<TransitionModel, string, string> writeTransitionLogic)
    {
        // Generate inline winner selection without goto or local functions
        Sb.AppendLine("// Hierarchical trigger resolution with inline winner selection");
        Sb.AppendLine("bool found = false;");
        Sb.AppendLine();
        
        // Best candidate tracking variables (allocation-free)
        Sb.AppendLine("// Best candidate tracking");
        Sb.AppendLine("int bestPriority = int.MinValue;");
        Sb.AppendLine("int bestDepthFromCurrent = int.MaxValue;");  
        Sb.AppendLine("int bestDeclOrder = int.MaxValue;");
        Sb.AppendLine("bool bestIsInternal = false;");
        Sb.AppendLine("int bestDestIndex = -1;");
        Sb.AppendLine("System.Action? bestAction = null;");
        Sb.AppendLine();
        
        Sb.AppendLine("int declOrder = 0;");
        Sb.AppendLine("int currentIndex = (int)_currentState;");
        Sb.AppendLine("int check = currentIndex;");
        Sb.AppendLine();
        
        // Build lookup of all transitions by index
        var allTransitions = Model.Transitions.Select((t, i) => new { Transition = t, Index = i }).ToList();
        
        // Loop through the state and its ancestors
        using (Sb.Block("while (check >= 0)"))
        {
            Sb.AppendLine($"var enumState = ({stateTypeForUsage})check;");
            Sb.AppendLine($"int depthFromCurrent = (check == currentIndex) ? 0 : (s_depth[currentIndex] - s_depth[check]);");
            Sb.AppendLine();
            
            // Group transitions by source state
            var grouped = allTransitions.GroupBy(x => x.Transition.FromState);
            
            using (Sb.Block("switch (enumState)"))
            {
                foreach (var state in grouped)
                {
                    using (Sb.Block($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Key)}:"))
                    {
                        // Group by trigger
                        var triggerGroups = state.GroupBy(x => x.Transition.Trigger);
                        
                        using (Sb.Block("switch (trigger)"))
                        {
                            foreach (var triggerGroup in triggerGroups)
                            {
                                using (Sb.Block($"case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(triggerGroup.Key)}:"))
                                {
                                    // Process all matching transitions inline
                                    foreach (var item in triggerGroup)
                                    {
                                        var tr = item.Transition;
                                        Sb.AppendLine($"// Candidate: {tr.FromState} -> {tr.ToState} (Priority: {tr.Priority})");
                                        
                                        // Generate inline candidate evaluation
                                        GenerateInlineCandidateEvaluation(tr, item.Index, stateTypeForUsage, triggerTypeForUsage);
                                    }
                                    Sb.AppendLine("break;");
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
            Sb.AppendLine("// Move to parent state");
            Sb.AppendLine("check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
        }
        
        Sb.AppendLine();
        
        // Apply the best candidate if found
        Sb.AppendLine("// Apply winner");
        using (Sb.Block("if (!found)"))
        {
            Sb.AppendLine("return false;");
        }
        Sb.AppendLine();
        
        using (Sb.Block("if (bestIsInternal)"))
        {
            Sb.AppendLine("// Internal transition: execute action without state change");
            using (Sb.Block("if (bestAction != null)"))
            {
                Sb.AppendLine("try { bestAction(); } catch { return false; }");
            }
            Sb.AppendLine("return true; // state unchanged, no history recording");
        }
        using (Sb.Block("else"))
        {
            Sb.AppendLine("// External transition: record history and resolve composite destination");
            Sb.AppendLine("RecordHistoryForCurrentPath();");
            Sb.AppendLine($"_currentState = ({stateTypeForUsage})bestDestIndex;");
            Sb.AppendLine($"_currentState = ({stateTypeForUsage})GetCompositeEntryTarget((int)_currentState);");
            
            // Execute action if present
            using (Sb.Block("if (bestAction != null)"))
            {
                Sb.AppendLine("try { bestAction(); } catch { /* action failed but transition succeeded */ }");
            }
            
            Sb.AppendLine("return true;");
        }
    }
    
    private void GenerateInlineCandidateEvaluation(
        TransitionModel transition,
        int transitionIndex,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        Sb.AppendLine("{");
        using (Sb.Indent())
        {
            // Guard check if present
            if (!string.IsNullOrEmpty(transition.GuardMethod))
            {
                Sb.AppendLine("bool guardResult = false;");
                
                // Handle payload-based guards
                if (transition.GuardExpectsPayload && Model.GenerationConfig.HasPayload)
                {
                    var payloadType = transition.ExpectedPayloadType ?? Model.DefaultPayloadType;
                    if (!string.IsNullOrEmpty(payloadType))
                    {
                        Sb.AppendLine($"if (payload is {GetTypeNameForUsage(payloadType)} p)");
                        Sb.AppendLine("{");
                        Sb.AppendLine($"    try {{ guardResult = {transition.GuardMethod}(p); }} catch {{ guardResult = false; }}");
                        Sb.AppendLine("}");
                    }
                    else
                    {
                        Sb.AppendLine($"try {{ guardResult = {transition.GuardMethod}(payload); }} catch {{ guardResult = false; }}");
                    }
                }
                else
                {
                    Sb.AppendLine($"try {{ guardResult = {transition.GuardMethod}(); }} catch {{ guardResult = false; }}");
                }
                
                Sb.AppendLine("if (!guardResult) { declOrder++; } // skip this candidate");
                Sb.AppendLine("else");
                Sb.AppendLine("{");
                using (Sb.Indent())
                {
                    GenerateCandidateSelection(transition, transitionIndex, stateTypeForUsage);
                }
                Sb.AppendLine("}");
            }
            else
            {
                // No guard - always evaluate
                GenerateCandidateSelection(transition, transitionIndex, stateTypeForUsage);
            }
        }
        Sb.AppendLine("}");
    }
    
    private void GenerateCandidateSelection(
        TransitionModel transition,
        int transitionIndex,
        string stateTypeForUsage)
    {
        // Compare with current best using priority rules
        Sb.AppendLine($"int priority = {transition.Priority};");
        Sb.AppendLine("bool isBetter = false;");
        
        Sb.AppendLine("if (!found) isBetter = true;");
        Sb.AppendLine("else if (priority > bestPriority) isBetter = true;");
        Sb.AppendLine("else if (priority == bestPriority && depthFromCurrent < bestDepthFromCurrent) isBetter = true;");
        Sb.AppendLine("else if (priority == bestPriority && depthFromCurrent == bestDepthFromCurrent && declOrder < bestDeclOrder) isBetter = true;");
        
        Sb.AppendLine("if (isBetter)");
        Sb.AppendLine("{");
        using (Sb.Indent())
        {
            Sb.AppendLine("found = true;");
            Sb.AppendLine($"bestPriority = priority;");
            Sb.AppendLine("bestDepthFromCurrent = depthFromCurrent;");
            Sb.AppendLine("bestDeclOrder = declOrder;");
            Sb.AppendLine($"bestIsInternal = {(transition.IsInternal ? "true" : "false")};");
            
            if (!transition.IsInternal)
            {
                Sb.AppendLine($"bestDestIndex = (int){stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
            
            // Store action to execute later
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                if (transition.ActionExpectsPayload && Model.GenerationConfig.HasPayload)
                {
                    var payloadType = transition.ExpectedPayloadType ?? Model.DefaultPayloadType;
                    if (!string.IsNullOrEmpty(payloadType))
                    {
                        Sb.AppendLine($"bestAction = () => {{ if (payload is {GetTypeNameForUsage(payloadType)} p) {transition.ActionMethod}(p); }};");
                    }
                    else
                    {
                        Sb.AppendLine($"bestAction = () => {transition.ActionMethod}(payload);");
                    }
                }
                else
                {
                    Sb.AppendLine($"bestAction = () => {transition.ActionMethod}();");
                }
            }
            else
            {
                Sb.AppendLine("bestAction = null;");
            }
        }
        Sb.AppendLine("}");
        Sb.AppendLine("declOrder++;");
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
        // DEPRECATED - use WriteTransitionLogicDirect instead
        WriteTransitionLogicDirect(transition, stateTypeForUsage, triggerTypeForUsage);
    }
    
    private void WriteTransitionLogicDirect(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard check with direct return
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            
            Sb.AppendLine("bool guardOk = true;");
            if (transition.GuardExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType);
                Sb.AppendLine($"if (payload is {payloadType} guardPayload)");
                Sb.AppendLine($"    try {{ guardOk = {transition.GuardMethod}(guardPayload); }} catch {{ return false; }}");
                Sb.AppendLine("else");
                Sb.AppendLine("    return false;");
            }
            else
            {
                Sb.AppendLine($"try {{ guardOk = {transition.GuardMethod}(); }} catch {{ return false; }}");
            }
            
            // Hook: After guard evaluated
            WriteAfterGuardEvaluatedHook(transition, "guardOk", stateTypeForUsage, triggerTypeForUsage);
            
            Sb.AppendLine("if (!guardOk)");
            Sb.AppendLine("{");
            using (Sb.Indent())
            {
                WriteLogStatement("Warning",
                    $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteLogStatement("Warning",
                    $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
            Sb.AppendLine("}");
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            if (fromStateDef.OnExitExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType);
                Sb.AppendLine($"if (payload is {payloadType} exitPayload)");
                Sb.AppendLine($"    try {{ {fromStateDef.OnExitMethod}(exitPayload); }} catch {{ return false; }}");
                Sb.AppendLine("else");
                Sb.AppendLine("    return false;");
            }
            else
            {
                Sb.AppendLine($"try {{ {fromStateDef.OnExitMethod}(); }} catch {{ return false; }}");
            }
            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
        }

        // Action (if present)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            if (transition.ActionExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType);
                Sb.AppendLine($"if (payload is {payloadType} actionPayload)");
                Sb.AppendLine($"    try {{ {transition.ActionMethod}(actionPayload); }} catch {{ return false; }}");
                Sb.AppendLine("else");
                Sb.AppendLine("    return false;");
            }
            else
            {
                Sb.AppendLine($"try {{ {transition.ActionMethod}(); }} catch {{ return false; }}");
            }
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // State change (after action succeeds)
        if (!transition.IsInternal)
        {
            Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
        }

        // OnEntry (after state change)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            if (toStateDef.OnEntryExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType);
                Sb.AppendLine($"if (payload is {payloadType} entryPayload)");
                Sb.AppendLine($"    try {{ {toStateDef.OnEntryMethod}(entryPayload); }} catch {{ return false; }}");
                Sb.AppendLine("else");
                Sb.AppendLine("    return false;");
            }
            else
            {
                Sb.AppendLine($"try {{ {toStateDef.OnEntryMethod}(); }} catch {{ return false; }}");
            }
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
        }

        // Log successful transition
        if (!transition.IsInternal)
        {
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        // Direct return success
        Sb.AppendLine("return true;");
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
        var allStates = Model.States.Values
            .OrderBy(s => s.OrdinalValue)
            .Select(s => s.Name)
            .ToList();
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
    
    protected void WriteStateChangeWithCompositeHandling(string targetState, string stateTypeForUsage)
    {
        // Always use GetCompositeEntryTarget for all external transitions
        // This ensures proper history handling even for leaf destinations
        Sb.AppendLine($"// Set destination and resolve through GetCompositeEntryTarget");
        Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(targetState)};");
        Sb.AppendLine($"{CurrentStateField} = ({stateTypeForUsage})GetCompositeEntryTarget((int){CurrentStateField});");
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
                                            handleResultAfterTry: true
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

    #region Planning and Emission Support
    
    /// <summary>
    /// Gets the appropriate planner based on hierarchy configuration
    /// </summary>
    protected ITransitionPlanner GetPlanner()
    {
        return Model.HierarchyEnabled 
            ? new HierarchicalTransitionPlanner() 
            : new FlatTransitionPlanner();
    }
    
    /// <summary>
    /// Creates a build context for planning transitions
    /// </summary>
    protected TransitionBuildContext CreateBuildContext(TransitionModel transition)
    {
        var allStates = Model.States.Values
            .OrderBy(s => s.OrdinalValue)
            .Select(s => s.Name)
            .ToList();
        var currentStateIndex = allStates.IndexOf(transition.FromState);
        
        // Build hierarchy arrays if needed
        int[] parentIndices = new int[allStates.Count];
        int[] depths = new int[allStates.Count];
        int[] initialChildIndices = new int[allStates.Count];
        Generator.Model.HistoryMode[] historyModes = new Generator.Model.HistoryMode[allStates.Count];
        
        if (Model.HierarchyEnabled)
        {
            for (int i = 0; i < allStates.Count; i++)
            {
                var state = allStates[i];
                
                // Parent index
                if (Model.ParentOf.TryGetValue(state, out var parent) && parent != null)
                {
                    parentIndices[i] = allStates.IndexOf(parent);
                }
                else
                {
                    parentIndices[i] = -1;
                }
                
                // Depth
                if (Model.Depth.TryGetValue(state, out var depth))
                {
                    depths[i] = depth;
                }
                
                // Initial child
                if (Model.InitialChildOf.TryGetValue(state, out var initial) && initial != null)
                {
                    initialChildIndices[i] = allStates.IndexOf(initial);
                }
                else
                {
                    initialChildIndices[i] = -1;
                }
                
                // History mode
                if (Model.HistoryOf.TryGetValue(state, out var history))
                {
                    historyModes[i] = history;
                }
            }
        }
        
        return new TransitionBuildContext(
            Model,
            transition,
            currentStateIndex,
            IsAsyncMachine,
            Model.GenerationConfig.HasPayload,
            allStates,
            parentIndices,
            depths,
            initialChildIndices,
            historyModes);
    }
    
    /// <summary>
    /// Emits code for a transition plan
    /// </summary>
    protected void EmitTransitionPlan(TransitionPlan plan, TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        foreach (var step in plan.Steps)
        {
            switch (step.Kind)
            {
                case PlanStepKind.GuardCheck:
                    EmitGuardCheckStep(step, transition, stateTypeForUsage, triggerTypeForUsage);
                    break;
                    
                case PlanStepKind.ExitState:
                    EmitExitStateStep(step, transition);
                    break;
                    
                case PlanStepKind.InternalAction:
                    EmitActionStep(step, transition);
                    break;
                    
                case PlanStepKind.AssignState:
                    EmitAssignStateStep(step, stateTypeForUsage);
                    break;
                    
                case PlanStepKind.EntryState:
                    EmitEntryStateStep(step, transition);
                    break;
                    
                case PlanStepKind.RecordHistory:
                    EmitRecordHistoryStep(step, stateTypeForUsage);
                    break;
                    
                case PlanStepKind.Log:
                    EmitLogStep(step);
                    break;
            }
        }
    }
    
    private void EmitGuardCheckStep(PlanStep step, TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (!string.IsNullOrEmpty(step.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }
    }
    
    private void EmitExitStateStep(PlanStep step, TransitionModel transition)
    {
        if (!string.IsNullOrEmpty(step.OnExitMethod) && !string.IsNullOrEmpty(step.StateName))
        {
            if (Model.States.TryGetValue(step.StateName, out var stateDef))
            {
                WriteOnExitCall(stateDef, transition.ExpectedPayloadType);
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{step.OnExitMethod}\", \"{step.StateName}\");");
            }
        }
    }
    
    private void EmitActionStep(PlanStep step, TransitionModel transition)
    {
        if (!string.IsNullOrEmpty(step.ActionMethod))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{step.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }
    }
    
    private void EmitAssignStateStep(PlanStep step, string stateTypeForUsage)
    {
        if (!string.IsNullOrEmpty(step.StateName))
        {
            // For HSM, use composite handling to properly resolve history/initial
            if (Model.HierarchyEnabled)
            {
                WriteStateChangeWithCompositeHandling(step.StateName, stateTypeForUsage);
            }
            else
            {
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(step.StateName)};");
            }
        }
    }
    
    private void EmitEntryStateStep(PlanStep step, TransitionModel transition)
    {
        if (!string.IsNullOrEmpty(step.OnEntryMethod) && !string.IsNullOrEmpty(step.StateName))
        {
            if (Model.States.TryGetValue(step.StateName, out var stateDef))
            {
                WriteOnEntryCall(stateDef, transition.ExpectedPayloadType);
                WriteLogStatement("Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{step.OnEntryMethod}\", \"{step.StateName}\");");
            }
        }
    }
    
    private void EmitRecordHistoryStep(PlanStep step, string stateTypeForUsage)
    {
        // Record history before changing state
        if (Model.HierarchyEnabled)
        {
            Sb.AppendLine("RecordHistoryForCurrentPath();");
        }
    }
    
    private void EmitLogStep(PlanStep step)
    {
        if (!string.IsNullOrEmpty(step.LogTemplate))
        {
            WriteLogStatement("Debug", step.LogTemplate);
        }
    }
    
    #endregion

    #region Abstractions to be implemented by concrete generators
    protected abstract void WriteNamespaceAndClass();
    #endregion
    
    #region Helper Methods
    private static void EmitXmlDocSummary(IndentedStringBuilder.IndentedStringBuilder sb, string text)
    {
        var normalized = global::System.Text.RegularExpressions.Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        sb.AppendLine("/// <summary>");
        sb.Append("/// ").AppendLine(normalized);
        sb.AppendLine("/// </summary>");
    }
    #endregion
}