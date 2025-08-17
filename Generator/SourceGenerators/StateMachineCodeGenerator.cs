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
        Sb.WriteParam("state", "The state to check");
        Sb.WriteReturns("True if the state is in the active path, false otherwise");
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
        Sb.WriteReturns("The path from root to current state");
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
        
        // Add zero-allocation Span-based version
        Sb.WriteSummary("Gets the active state path into a provided buffer (zero-allocation version for HSM)");
        Sb.WriteParam("destination", "The span to write the path into");
        Sb.WriteReturns("The number of states written to the span, or -1 if the buffer is too small");
        using (Sb.Block($"public int GetActivePath(Span<{stateTypeForUsage}> destination)"))
        {
            Sb.AppendLine("// First, count the depth to ensure we have enough space");
            Sb.AppendLine($"var currentIndex = (int){CurrentStateField};");
            Sb.AppendLine("int depth = 0;");
            Sb.AppendLine("var tempIndex = currentIndex;");
            Sb.AppendLine();
            Sb.AppendLine("// Count the depth");
            using (Sb.Block("while (tempIndex >= 0)"))
            {
                Sb.AppendLine("depth++;");
                Sb.AppendLine("tempIndex = s_parent[tempIndex];");
            }
            Sb.AppendLine();
            Sb.AppendLine("// Check if destination has enough space");
            using (Sb.Block("if (destination.Length < depth)"))
            {
                Sb.AppendLine("return -1; // Buffer too small");
            }
            Sb.AppendLine();
            Sb.AppendLine("// Fill the span from the end (leaf) to start (root)");
            Sb.AppendLine("int writeIndex = depth - 1;");
            Sb.AppendLine("currentIndex = (int)_currentState;");
            using (Sb.Block("while (currentIndex >= 0 && writeIndex >= 0)"))
            {
                Sb.AppendLine($"destination[writeIndex] = ({stateTypeForUsage})currentIndex;");
                Sb.AppendLine("currentIndex = s_parent[currentIndex];");
                Sb.AppendLine("writeIndex--;");
            }
            Sb.AppendLine();
            Sb.AppendLine("return depth;");
        }
        Sb.AppendLine();
        
        // Remove the problematic GetActivePathSpan method - can't return stackalloc outside method scope
        // Users should use GetActivePath(Span<T>) directly with their own stackalloc
        Sb.AppendLine();
        
        // For async machines, add async version
        if (IsAsyncMachine)
        {
            Sb.WriteSummary("Asynchronously gets the active state path from root to current leaf state (HSM support)");
            Sb.WriteParam("cancellationToken", "A token to observe for cancellation requests");
            Sb.WriteReturns("The path from root to current state");
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
        Sb.WriteSummary("Returns true if the current state lies in the hierarchy of the given ancestor (i.e., ancestor is the current leaf or any of its parents).");
        Sb.WriteParam("ancestor", "The potential ancestor state to check");
        Sb.WriteReturns("True if ancestor is the current state or any of its parents, false otherwise");
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
            Sb.AppendLine("int leafLeaf = (int)_currentState; // remember the deepest active leaf");
            Sb.AppendLine("int cursor = leafLeaf;");
            Sb.AppendLine("int parent = (uint)cursor < (uint)s_parent.Length ? s_parent[cursor] : -1;");
            Sb.AppendLine();
            using (Sb.Block("while (parent >= 0)"))
            {
                Sb.AppendLine("if (s_history[parent] != HistoryMode.None)");
                using (Sb.Indent())
                    Sb.AppendLine("_lastActiveChild[parent] = leafLeaf; // Always record the original leaf, not cursor");
                Sb.AppendLine();
                Sb.AppendLine("cursor = parent;");
                Sb.AppendLine("parent = s_parent[cursor];");
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
                using (Sb.Indent())
                {
                    Sb.AppendLine("return idx;");
                }
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
                        using (Sb.Indent())
                        {
                            Sb.AppendLine("immediate = s_parent[immediate];");
                        }
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
            Sb.WriteParam("cancellationToken", "A token to observe for cancellation requests");
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

    //protected virtual void WriteOnInitialEntryMethod(string stateTypeForUsage)
    //{
    //    if (!ShouldGenerateInitialOnEntry())
    //        return;
            
    //    using (Sb.Block("protected override void OnInitialEntry()"))
    //    {
    //        if (Model.HierarchyEnabled)
    //        {
    //            // For HSM: Build entry chain from root to current leaf and call each OnEntry
    //            Sb.AppendLine("// Build entry chain from root to current leaf");
    //            Sb.AppendLine($"var entryChain = new List<{stateTypeForUsage}>();");
    //            Sb.AppendLine($"int currentIdx = (int){CurrentStateField};");
    //            Sb.AppendLine();
                
    //            // Build chain from leaf to root
    //            Sb.AppendLine("// Walk from leaf to root");
    //            using (Sb.Block("while (currentIdx >= 0)"))
    //            {
    //                Sb.AppendLine($"entryChain.Add(({stateTypeForUsage})currentIdx);");
    //                Sb.AppendLine("if ((uint)currentIdx >= (uint)s_parent.Length) break;");
    //                Sb.AppendLine("currentIdx = s_parent[currentIdx];");
    //            }
    //            Sb.AppendLine();
                
    //            // Reverse to get root-to-leaf order
    //            Sb.AppendLine("// Reverse to get root-to-leaf order");
    //            Sb.AppendLine("entryChain.Reverse();");
    //            Sb.AppendLine();
                
    //            // Call OnEntry for each state in the chain that has one
    //            Sb.AppendLine("// Call OnEntry for each state in the chain");
    //            using (Sb.Block("foreach (var state in entryChain)"))
    //            {
    //                using (Sb.Block("switch (state)"))
    //                {
    //                    foreach (var stateEntry in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
    //                    {
    //                        Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
    //                        using (Sb.Indent())
    //                        {
    //                            // Direct call without WriteCallbackInvocation to avoid try-catch in constructor
    //                            Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
    //                            WriteLogStatement("Debug",
    //                                $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
    //                            Sb.AppendLine("break;");
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            // Non-HSM: Original single-state entry
    //            using (Sb.Block($"switch ({CurrentStateField})"))
    //            {
    //                foreach (var stateEntry in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
    //                {
    //                    Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
    //                    using (Sb.Indent())
    //                    {
    //                        // Direct call without WriteCallbackInvocation to avoid try-catch in constructor
    //                        Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
    //                        WriteLogStatement("Debug",
    //                            $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
    //                        Sb.AppendLine("break;");
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    Sb.AppendLine();
    //}

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
                                // Process all transitions for this trigger in priority order
                                foreach (var tr in triggerGroup)
                                {
                                    Sb.AppendLine($"// Transition: {tr.FromState} -> {tr.ToState} (Priority: {tr.Priority})");
                                    writeTransitionLogic(tr, stateTypeForUsage, triggerTypeForUsage);
                                    // Only first matching transition executes due to return
                                    break;
                                }
                            }
                        }
                        Sb.AppendLine("default: break;");
                    }
                    Sb.AppendLine("break;");                
                }
            }
            Sb.AppendLine("default: break;");
        }
        
        Sb.AppendLine();
        Sb.AppendLine("return false;");
    }
    
    /// <summary>
    /// Simplified transition logic for flat non-payload machines using direct returns.
    /// No success variable, no goto labels, minimal braces.
    /// </summary>
    protected void WriteTransitionLogicForFlatNonPayload(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard check (if present)
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            
            // Use GuardGenerationHelper for consistent guard handling
            GuardGenerationHelper.EmitGuardCheck(
                Sb,
                transition,
                "guardOk",
                payloadVar: "null", // WriteTransitionLogicForFlatNonPayload doesn't use payload
                IsAsyncMachine,
                wrapInTryCatch: true,
                Model.ContinueOnCapturedContext,
                handleResultAfterTry: true,
                cancellationTokenVar: null, // Not async variant
                treatCancellationAsFailure: false
            );
            
            // Check guard result
            WriteAfterGuardEvaluatedHook(transition, "guardOk", stateTypeForUsage, triggerTypeForUsage);
            using (Sb.Block("if (!guardOk)"))
            {
                WriteLogStatement("Warning",
                    $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteLogStatement("Warning",
                    $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
        }

        // OnExit (if applicable)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            using (Sb.Block("try"))
            {
                // Use CallbackGenerationHelper for consistent OnExit handling
                CallbackGenerationHelper.EmitOnExitCall(
                    Sb,
                    fromStateDef,
                    transition.ExpectedPayloadType,
                    null, // no default payload type
                    "null", // no payload in FlatNonPayload variant
                    IsAsyncMachine,
                    wrapInTryCatch: false, // We're already in a try block
                    Model.ContinueOnCapturedContext,
                    isSinglePayload: false,
                    isMultiPayload: false,
                    cancellationTokenVar: null, // Not async variant
                    treatCancellationAsFailure: false
                );
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
            }
            using (Sb.Block("catch"))
            {
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
        }

        // Store the previous state for potential rollback (only if we have exception handler and action)
        if (Model.ExceptionHandler != null && !string.IsNullOrEmpty(transition.ActionMethod))
        {
            Sb.AppendLine($"// FSM_DEBUG: Handler found: {Model.ExceptionHandler.MethodName}");
            Sb.AppendLine($"var prevState = {CurrentStateField};");
        }
        else if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            Sb.AppendLine($"// FSM_DEBUG: No handler for {Model.ClassName}, action={transition.ActionMethod}");
        }
        
        // State change
        if (!transition.IsInternal)
        {
            Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
        }

        // OnEntry (if applicable)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            using (Sb.Block("try"))
            {
                // Use CallbackGenerationHelper for consistent OnEntry handling
                CallbackGenerationHelper.EmitOnEntryCall(
                    Sb,
                    toStateDef,
                    transition.ExpectedPayloadType,
                    null, // no default payload type
                    "null", // no payload in FlatNonPayload variant
                    IsAsyncMachine,
                    wrapInTryCatch: false, // We're already in a try block
                    Model.ContinueOnCapturedContext,
                    isSinglePayload: false,
                    isMultiPayload: false,
                    cancellationTokenVar: null, // Not async variant
                    treatCancellationAsFailure: false
                );
                WriteLogStatement("Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
            }
            using (Sb.Block("catch"))
            {
                // On OnEntry failure, we're already in the new state, so don't revert
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
        }

        // Action (if present)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            if (Model.ExceptionHandler == null)
            {
                // No exception handler - use existing try/catch logic
                using (Sb.Block("try"))
                {
                    // Use CallbackGenerationHelper for consistent Action handling
                    CallbackGenerationHelper.EmitActionCall(
                        Sb,
                        transition,
                        "null", // no payload in FlatNonPayload variant
                        IsAsyncMachine,
                        wrapInTryCatch: false, // We're already in a try block
                        Model.ContinueOnCapturedContext,
                        cancellationTokenVar: null, // Not async variant
                        treatCancellationAsFailure: false
                    );
                    WriteLogStatement("Debug",
                        $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                }
                using (Sb.Block("catch"))
                {
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
            }
            else
            {
                // Has exception handler - use directive-based exception handling
                using (Sb.Block("try"))
                {
                    // Use CallbackGenerationHelper for consistent Action handling
                    CallbackGenerationHelper.EmitActionCall(
                        Sb,
                        transition,
                        "null", // no payload in FlatNonPayload variant
                        IsAsyncMachine,
                        wrapInTryCatch: false, // We're already in a try block
                        Model.ContinueOnCapturedContext,
                        cancellationTokenVar: null, // Not async variant
                        treatCancellationAsFailure: false
                    );
                    WriteLogStatement("Debug",
                        $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                }
                using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
                {
                    var handler = Model.ExceptionHandler;
                    var stateType = GetTypeNameForUsage(Model.StateType);
                    var triggerType = GetTypeNameForUsage(Model.TriggerType);
                    
                    // Create exception context
                    Sb.AppendLine($"var exceptionContext = new {handler.ExceptionContextClosedType}(");
                    using (Sb.Indent())
                    {
                        Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(transition.FromState)},");
                        Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
                        Sb.AppendLine($"{triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)},");
                        Sb.AppendLine("ex,");
                        Sb.AppendLine("TransitionStage.Action,");
                        Sb.AppendLine("true);"); // State already changed for actions
                    }
                    
                    // Call handler
                    Sb.AppendLine($"var directive = {handler.MethodName}(exceptionContext);");
                    
                    // Apply directive based on policy
                    using (Sb.Block("if (directive == ExceptionDirective.Propagate)"))
                    {
                        // Keep the new state on Propagate in flat FSM
                        Sb.AppendLine("throw;");
                    }
                    Sb.AppendLine("// Continue: keep new state and continue execution");
                }
            }
        }

        // Log successful transition
        if (!transition.IsInternal)
        {
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        // Success - direct return
        Sb.AppendLine("return true;");
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
        Sb.AppendLine("ActionId bestActionId = ActionId.None;");
        if (Model.GenerationConfig.HasPayload)
        {
            Sb.AppendLine("object? bestPayload = null;");
        }
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
            GenerateActionSwitch("bestActionId", isInternal: true);
            Sb.AppendLine("return true; // state unchanged, no history recording");
        }
        using (Sb.Block("else"))
        {
            Sb.AppendLine("// External transition: record history and resolve composite destination");
            Sb.AppendLine("RecordHistoryForCurrentPath();");
            Sb.AppendLine($"_currentState = ({stateTypeForUsage})bestDestIndex;");
            Sb.AppendLine($"_currentState = ({stateTypeForUsage})GetCompositeEntryTarget((int)_currentState);");
            
            // Execute action if present
            GenerateActionSwitch("bestActionId", isInternal: false);
            
            Sb.AppendLine("return true;");
        }
    }
    
    private void GenerateInlineCandidateEvaluation(
        TransitionModel transition,
        int transitionIndex,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        using (Sb.Block(""))
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
                        using (Sb.Block(""))
                        {
                            Sb.AppendLine($"try {{ guardResult = {transition.GuardMethod}(p); }} catch {{ guardResult = false; }}");
                        }
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
                using (Sb.Block(""))
                {
                    GenerateCandidateSelection(transition, transitionIndex, stateTypeForUsage);
                }
            }
            else
            {
                // No guard - always evaluate
                GenerateCandidateSelection(transition, transitionIndex, stateTypeForUsage);
            }
        }
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
        using (Sb.Block(""))
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
            
            // Store action ID to execute later (no lambda allocation)
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                var actionId = GetActionIdName(transition);
                Sb.AppendLine($"bestActionId = ActionId.{actionId};");
                
                // Store payload if needed
                if (Model.GenerationConfig.HasPayload && transition.ActionExpectsPayload)
                {
                    Sb.AppendLine("bestPayload = payload;");
                }
            }
            else
            {
                Sb.AppendLine("bestActionId = ActionId.None;");
            }
        }
        Sb.AppendLine("declOrder++;");
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


    #endregion

    #region Virtual Methods for Customization

    protected virtual bool ShouldGenerateInitialOnEntry() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual bool ShouldGenerateOnEntryExit() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual void WriteGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Owijamy całą logikę guard w try-catch
        using (Sb.Block("try"))
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
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
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
        Model.GenerationConfig.HasPayload || Model.DefaultPayloadType != null || Model.TriggerPayloadTypes.Any();



    protected bool IsExtensionsVariant() =>
        Model.GenerationConfig.HasExtensions;


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
        Sb.WriteParam("trigger", "The trigger to check");
        Sb.WriteReturns("True if the trigger can be fired, false otherwise");
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool CanFireInternal({triggerTypeForUsage} trigger)"))
        {
            if (Model.HierarchyEnabled)
            {
                // HSM: Walk up the parent chain
                Sb.AppendLine($"int currentIndex = (int){CurrentStateField};");
                Sb.AppendLine("int check = currentIndex;");
                using (Sb.Block("while (check >= 0)"))
                {
                    Sb.AppendLine($"var enumState = ({stateTypeForUsage})check;");
                    using (Sb.Block("switch (enumState)"))
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
                                    Sb.AppendLine("default: break;");
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.AppendLine("default: break;");
                    }
                    Sb.AppendLine("check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
                }
                Sb.AppendLine("return false;");
            }
            else
            {
                // Flat FSM: Original implementation
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
        }
        Sb.AppendLine();
    }

    protected virtual void WriteGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.WriteReturns("List of triggers that can be fired in the current state");
        using (Sb.Block($"protected override {ReadOnlyListType}<{triggerTypeForUsage}> GetPermittedTriggersInternal()"))
        {
            if (Model.HierarchyEnabled)
            {
                // HSM: Walk up the parent chain and collect all possible triggers
                Sb.AppendLine($"var permitted = new HashSet<{triggerTypeForUsage}>();");
                Sb.AppendLine($"int currentIndex = (int){CurrentStateField};");
                Sb.AppendLine("int check = currentIndex;");
                
                using (Sb.Block("while (check >= 0)"))
                {
                    Sb.AppendLine($"var enumState = ({stateTypeForUsage})check;");
                    using (Sb.Block("switch (enumState)"))
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
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.AppendLine("default: break;");
                    }
                    Sb.AppendLine("check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
                }
                
                Sb.AppendLine("return permitted.Count == 0 ? ");
                using (Sb.Indent())
                {
                    Sb.AppendLine($"{ArrayEmptyMethod}<{triggerTypeForUsage}>() :");
                    Sb.AppendLine("permitted.ToArray();");
                }
            }
            else
            {
                // Flat FSM: Original implementation
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
        }
        Sb.AppendLine();
        
        // Add Span-based version for GetPermittedTriggers (only for HSM)
        if (Model.HierarchyEnabled)
        {
            Sb.WriteSummary("Gets the permitted triggers into a provided buffer (zero-allocation version for HSM)");
            Sb.WriteParam("destination", "The span to write the permitted triggers into");
            Sb.WriteReturns("The number of triggers written to the span, or -1 if the buffer is too small");
            using (Sb.Block($"public int GetPermittedTriggers(Span<{triggerTypeForUsage}> destination)"))
            {
                Sb.AppendLine("int writeIndex = 0;");
                Sb.AppendLine($"int currentIndex = (int){CurrentStateField};");
                Sb.AppendLine("int check = currentIndex;");
                Sb.AppendLine();
                
                // Get unique triggers count for the seen array
                var uniqueTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
                Sb.AppendLine($"// Track seen triggers to avoid duplicates ({uniqueTriggers.Count} unique triggers)");
                Sb.AppendLine($"Span<bool> seen = stackalloc bool[{uniqueTriggers.Count}];");
                Sb.AppendLine();
                
                using (Sb.Block("while (check >= 0)"))
                {
                    Sb.AppendLine($"var enumState = ({stateTypeForUsage})check;");
                    using (Sb.Block("switch (enumState)"))
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
                                // Group by trigger to avoid duplicates within same state
                                var triggersInState = stateGroup.GroupBy(t => t.Trigger).OrderBy(g => g.Key);
                                
                                foreach (var triggerGroup in triggersInState)
                                {
                                    var trigger = triggerGroup.Key;
                                    var triggerIndex = uniqueTriggers.IndexOf(trigger);
                                    
                                    using (Sb.Block($"if (!seen[{triggerIndex}])"))
                                    {
                                        // Check if any transition for this trigger has no guard or passing guard
                                        var transitionsForTrigger = triggerGroup.ToList();
                                        var hasUnguarded = transitionsForTrigger.Any(t => string.IsNullOrEmpty(t.GuardMethod));
                                        
                                        if (hasUnguarded)
                                        {
                                            // At least one unguarded transition - trigger is always available
                                            Sb.AppendLine($"if (writeIndex >= destination.Length) return -1;");
                                            Sb.AppendLine($"destination[writeIndex++] = {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(trigger)};");
                                            Sb.AppendLine($"seen[{triggerIndex}] = true;");
                                        }
                                        else
                                        {
                                            // All transitions are guarded - need runtime check
                                            bool first = true;
                                            foreach (var transition in transitionsForTrigger)
                                            {
                                                if (!string.IsNullOrEmpty(transition.GuardMethod))
                                                {
                                                    Sb.AppendLine($"{(first ? "if" : "else if")} ({transition.GuardMethod}())");
                                                    using (Sb.Block(""))
                                                    {
                                                        Sb.AppendLine($"if (writeIndex >= destination.Length) return -1;");
                                                        Sb.AppendLine($"destination[writeIndex++] = {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(trigger)};");
                                                        Sb.AppendLine($"seen[{triggerIndex}] = true;");
                                                    }
                                                    first = false;
                                                }
                                            }
                                        }
                                    }
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.AppendLine("default: break;");
                    }
                    Sb.AppendLine("check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
                }
                Sb.AppendLine();
                Sb.AppendLine("return writeIndex;");
            }
            Sb.AppendLine();
        }
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
        Sb.WriteParam("trigger", "The trigger to check");
        Sb.WriteReturns("True if a transition is defined for the trigger in current state, false otherwise");
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
        Sb.WriteReturns("List of all triggers defined for the current state, regardless of guard conditions");
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


    protected string GetConfigureAwait() => AsyncGenerationHelper.GetConfigureAwait(IsAsyncMachine, Model.ContinueOnCapturedContext);

    /// <summary>
    /// Returns the cancellation token variable name or CancellationToken.None for sync machines.
    /// </summary>
    protected string GetCtVar() => IsAsyncMachine
        ? "cancellationToken"
        : "System.Threading.CancellationToken.None";




    protected string GetBaseClassName(string stateType, string triggerType) => AsyncGenerationHelper.GetBaseClassName(stateType, triggerType, IsAsyncMachine);
    protected string GetInterfaceName(string stateType, string triggerType) => AsyncGenerationHelper.GetInterfaceName(stateType, triggerType, IsAsyncMachine);


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
        using (Sb.Block("try"))
        {
            WriteOnEntryCall(toStateDef, expectedPayloadType);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
        }
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
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
            Sb.AppendLine($"// FSM_DEBUG: No exception handler found for {Model.ClassName}");
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            return;
        }

        // Wrap in try/catch with exception policy
        using (Sb.Block("try"))
        {
            WriteActionCall(transition);
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
        }
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
        {
            EmitExceptionHandlerCallForAction(fromState, toState, transition.Trigger);
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
        using (Sb.Block("try"))
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
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
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
        using (Sb.Block("try"))
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
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
        {
            EmitExceptionHandlerCallForAction(fromState, toState, transition.Trigger);
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

    /// <summary>
    /// Emits the call to the exception handler for Actions with proper directive handling.
    /// </summary>
    private void EmitExceptionHandlerCallForAction(
        string fromState,
        string toState,
        string trigger)
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
            Sb.AppendLine("TransitionStage.Action,");
            Sb.AppendLine("true);"); // State already changed for actions
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

        // Apply directive based on policy
        using (Sb.Block($"if ({directiveVar} == ExceptionDirective.Propagate)"))
        {
            // Keep the new state on Propagate in flat FSM
            Sb.AppendLine("throw;");
        }
        Sb.AppendLine("// Continue: keep new state and continue execution");
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
    
   

    
    #endregion

    #region Abstractions to be implemented by concrete generators
    protected abstract void WriteNamespaceAndClass();
    #endregion
    
    #region Helper Methods
    private static void EmitXmlDocSummary(IndentedStringBuilder.IndentedStringBuilder sb, string text)
    {
        var normalized = global::System.Text.RegularExpressions.Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        sb.WriteSummary(normalized);
    }
    
    /// <summary>
    /// Generates the ActionId enum for zero-allocation action dispatch
    /// </summary>
    protected void GenerateActionIdEnum()
    {
        // Collect all unique action methods
        var actionNames = Model.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ActionMethod))
            .Select(t => GetActionIdName(t))
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        
        // Always generate enum, even if empty (just with None)
        Sb.AppendLine("// Action dispatch enum (zero-allocation)");
        using (Sb.Block("private enum ActionId : byte"))
        {
            Sb.AppendLine("None = 0,");
            foreach (var name in actionNames)
            {
                Sb.AppendLine($"{name},");
            }
        }
        Sb.AppendLine();
    }
    
    /// <summary>
    /// Gets the ActionId enum member name for a transition
    /// </summary>
    protected string GetActionIdName(TransitionModel transition)
    {
        if (string.IsNullOrEmpty(transition.ActionMethod))
            return "None";
        return GetSafeActionIdName(transition.ActionMethod);
    }
    
    /// <summary>
    /// Converts action method name to safe enum identifier
    /// </summary>
    private string GetSafeActionIdName(string methodName)
    {
        // Remove async suffix if present
        if (methodName.EndsWith("Async"))
            methodName = methodName.Substring(0, methodName.Length - 5);
        
        // Ensure valid C# identifier
        return $"Action_{methodName}";
    }
    
    /// <summary>
    /// Generates the action execution switch statement
    /// </summary>
    protected void GenerateActionSwitch(string actionIdVar, bool isInternal)
    {
        // Group transitions by action method
        var groups = Model.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ActionMethod))
            .GroupBy(t => t.ActionMethod)
            .OrderBy(g => g.Key)
            .ToList();
        
        // Always generate switch, even if empty
        using (Sb.Block($"switch ({actionIdVar})"))
        {
            Sb.AppendLine("case ActionId.None: break;");
            
            foreach (var group in groups)
            {
                var methodName = group.Key;
                var actionIdName = GetActionIdName(group.First());
                
                // Check if any transition in this group expects payload or is async
                bool anyPayload = group.Any(t => t.ActionExpectsPayload && Model.GenerationConfig.HasPayload);
                bool anyAsync = group.Any(t => t.ActionIsAsync);
                
                using (Sb.Block($"case ActionId.{actionIdName}:"))
                {
                    using (Sb.Block("try"))
                    {
                        if (IsAsyncMachine)
                        {
                            if (anyPayload)
                            {
                                // Get the most specific payload type from the group
                                var payloadType = group.FirstOrDefault(t => !string.IsNullOrEmpty(t.ExpectedPayloadType))?.ExpectedPayloadType
                                                ?? Model.DefaultPayloadType;
                                
                                if (!string.IsNullOrEmpty(payloadType))
                                {
                                    Sb.AppendLine($"if (bestPayload is {GetTypeNameForUsage(payloadType)} p)");
                                    Sb.AppendLine($"    {(anyAsync ? "await " : "")}{methodName}(p){(anyAsync ? GetConfigureAwait() : "")};");
                                }
                                else
                                {
                                    Sb.AppendLine($"{(anyAsync ? "await " : "")}{methodName}(bestPayload){(anyAsync ? GetConfigureAwait() : "")};");
                                }
                            }
                            else
                            {
                                Sb.AppendLine($"{(anyAsync ? "await " : "")}{methodName}(){(anyAsync ? GetConfigureAwait() : "")};");
                            }
                        }
                        else
                        {
                            // Sync machine
                            if (anyPayload)
                            {
                                var payloadType = group.FirstOrDefault(t => !string.IsNullOrEmpty(t.ExpectedPayloadType))?.ExpectedPayloadType
                                                ?? Model.DefaultPayloadType;
                                
                                if (!string.IsNullOrEmpty(payloadType))
                                {
                                    Sb.AppendLine($"if (bestPayload is {GetTypeNameForUsage(payloadType)} p)");
                                    Sb.AppendLine($"    {methodName}(p);");
                                }
                                else
                                {
                                    Sb.AppendLine($"{methodName}(bestPayload);");
                                }
                            }
                            else
                            {
                                Sb.AppendLine($"{methodName}();");
                            }
                        }
                    }
                    using (Sb.Block("catch"))
                    {
                        if (isInternal)
                        {
                            Sb.AppendLine("return false;");
                        }
                        else
                        {
                            Sb.AppendLine("/* action failed but transition succeeded */");
                        }
                    }
                    Sb.AppendLine("break;");
                }
            }
            
            Sb.AppendLine("default: break;");
        }
    }
    #endregion
}