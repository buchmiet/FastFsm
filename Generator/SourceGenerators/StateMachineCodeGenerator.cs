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
/// Base for all generator variants.
/// Contains complete set of sync/async helpers and hooks.
/// </summary>
internal abstract class StateMachineCodeGenerator(StateMachineModel model)
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
    /// Writes state and trigger name arrays for zero-allocation logging
    /// </summary>
    protected virtual void WriteStateAndTriggerNameArrays(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // Generate state names array for zero-allocation logging
        var allStates = Model.States.Values
            .OrderBy(s => s.OrdinalValue)
            .ToList();
        
        Sb.AppendLine("// State and trigger name arrays for zero-allocation logging");
        
        // State names array
        Sb.Append("        private static readonly string[] s_stateNames = new string[] { ");
        var stateNames = allStates.Select(s => $"\"{s.Name}\"");
        Sb.Append(string.Join(", ", stateNames));
        Sb.AppendLine(" };");
        
        // Helper method for state name lookup with bounds checking
        Sb.AppendLine(AggressiveInliningString);
        Sb.AppendLine($"        private static string NameOf({stateTypeForUsage} s) {{");
        Sb.AppendLine($"            int index = (int)s;");
        Sb.AppendLine($"            return (index >= 0 && index < s_stateNames.Length) ? s_stateNames[index] : (index == -1 ? \"<root>\" : s.ToString());");
        Sb.AppendLine($"        }}");
        
        // Trigger names array - get unique triggers from transitions and sort them
        var allTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
        if (allTriggers.Count > 0)
        {
            // For now, we'll use the trigger names from transitions
            // In a more complete implementation, we'd need to get all enum values
            // But for logging purposes, the transitions should cover the used triggers
            Sb.Append("        private static readonly string[] s_triggerNames = new string[] { ");
            var triggerNames = allTriggers.Select(t => $"\"{t}\"");
            Sb.Append(string.Join(", ", triggerNames));
            Sb.AppendLine(" };");
            
            // Create a dictionary for lookup
            Sb.AppendLine("        private static readonly System.Collections.Generic.Dictionary<" + triggerTypeForUsage + ", string> s_triggerNameLookup = new System.Collections.Generic.Dictionary<" + triggerTypeForUsage + ", string>");
            Sb.AppendLine("        {");
            foreach (var trigger in allTriggers)
            {
                Sb.AppendLine($"            {{ {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(trigger)}, \"{trigger}\" }},");
            }
            Sb.AppendLine("        };");
            
            // Helper method for trigger name lookup
            Sb.AppendLine(AggressiveInliningString);
            Sb.AppendLine($"        private static string NameOfTrigger({triggerTypeForUsage} t) => s_triggerNameLookup.TryGetValue(t, out var name) ? name : t.ToString();");
        }
        
        Sb.AppendLine();
    }
    
    /// <summary>
    /// Writes static hierarchy arrays if HSM is enabled
    /// </summary>
    protected virtual void WriteHierarchyArrays(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // FSM990_HSM_FLAG: Log before writing HSM blocks
        Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
        Sb.AppendLine($"// FSM990_HSM_FLAG [4-WriteHSM] {Model.ClassName}: HierarchyEnabled={Model.HierarchyEnabled}");
        Sb.AppendLine("#endif");
        
        if (!Model.HierarchyEnabled) return;
        
        Sb.AppendLine("// Hierarchical state machine support arrays");
        
        // Get all states in enum ordinal order (by their numeric value, not alphabetically)
        var allStates = Model.States.Values
            .OrderBy(s => s.OrdinalValue)
            .Select(s => s.Name)
            .ToList();

        // Parent array (-1 for root states)
        Sb.Append("        private static readonly int[] g_parent = new int[] { ");
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
        Sb.Append("        private static readonly int[] g_depth = new int[] { ");
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
        Sb.Append("        private static readonly int[] g_initialChild = new int[] { ");
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
        var historyModeType = TypeHelper.GetHistoryModeTypeName();
        Sb.Append($"        private static readonly {historyModeType}[] g_history = new {historyModeType}[] {{ ");
        var historyValues = allStates.Select(state =>
        {
            if (Model.HistoryOf.TryGetValue(state, out var history))
            {
                return $"{historyModeType}.{history}";
            }
            return $"{historyModeType}.None";
        });
        Sb.Append(string.Join(", ", historyValues));
        Sb.AppendLine(" };");
        
        // Add override properties for base class
        Sb.AppendLine("        protected override int[] ParentArray => g_parent;");
        Sb.AppendLine("        protected override int[] DepthArray => g_depth;");
        Sb.AppendLine("        protected override int[] InitialChildArray => g_initialChild;");
        Sb.AppendLine($"        protected override {historyModeType}[] HistoryArray => g_history;");
        var hasHistory = Model.HistoryOf.Values.Any(h =>
            !h.Equals(Abstractions.Attributes.HistoryMode.None));
        Sb.AppendLine($"        protected override bool HasHistory => {(hasHistory ? "true" : "false")};");

        // Generate precomputed permission arrays for HSM (zero-alloc)
        GenerateHsmPermittedTriggerArrays(triggerTypeForUsage);
        
        Sb.AppendLine();
    }
    
    /// <summary>
    /// Generates precomputed permission arrays for HSM zero-alloc GetPermittedTriggers
    /// </summary>
    private void GenerateHsmPermittedTriggerArrays(string triggerTypeForUsage)
    {
        var uniqueTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
        
        if (uniqueTriggers.Count == 0)
        {
            Sb.AppendLine($"        private static readonly {triggerTypeForUsage}[][] s_perm__Mask = new {triggerTypeForUsage}[][] {{ System.Array.Empty<{triggerTypeForUsage}>() }};");
            return;
        }
        
        // Generate all possible mask combinations (2^n where n is number of unique triggers)
        int maxMask = 1 << uniqueTriggers.Count;
        
        Sb.AppendLine($"        // Precomputed permission arrays for all possible guard mask combinations");
        Sb.AppendLine($"        private static readonly {triggerTypeForUsage}[][] s_perm__Mask = new {triggerTypeForUsage}[][]");
        Sb.AppendLine("        {");
        
        for (int mask = 0; mask < maxMask; mask++)
        {
            var triggers = new List<string>();
            for (int i = 0; i < uniqueTriggers.Count; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    triggers.Add($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(uniqueTriggers[i])}");
                }
            }
            
            if (triggers.Count == 0)
            {
                Sb.AppendLine($"            System.Array.Empty<{triggerTypeForUsage}>(), // mask={mask}");
            }
            else
            {
                Sb.AppendLine($"            new {triggerTypeForUsage}[] {{ {string.Join(", ", triggers)} }}, // mask={mask}");
            }
        }
        
        Sb.AppendLine("        };");
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
            Sb.AppendLine("var parentIndex = g_parent[currentIndex];");
            using (Sb.Block("while (parentIndex >= 0)"))
            {
                Sb.AppendLine("if (parentIndex == targetIndex)");
                using (Sb.Indent())
                {
                    Sb.AppendLine("return true;");
                }
                Sb.AppendLine("parentIndex = g_parent[parentIndex];");
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
                Sb.AppendLine("currentIndex = g_parent[currentIndex];");
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
                Sb.AppendLine("tempIndex = g_parent[tempIndex];");
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
                Sb.AppendLine("currentIndex = g_parent[currentIndex];");
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
        
        // DumpActivePath and IsInHierarchy are now provided by the base class
        // No need to generate them here
        Sb.AppendLine();
        
        // Emit FindLowestCommonAncestor helper — only for HSM
        Sb.AppendLine(AggressiveInliningString);
        using (Sb.Block("protected int FindLowestCommonAncestor(int srcLeaf, int destLeaf)"))
        {
            Sb.AppendLine("if (srcLeaf == destLeaf) return srcLeaf;");
            Sb.AppendLine("var parent = ParentArray;");
            Sb.AppendLine("var depth  = DepthArray;");
            Sb.AppendLine("int a = srcLeaf, b = destLeaf;");
            Sb.AppendLine("// Bring both to the same depth");
            Sb.AppendLine("while (a >= 0 && b >= 0 && depth[a] > depth[b]) a = parent[a];");
            Sb.AppendLine("while (a >= 0 && b >= 0 && depth[b] > depth[a]) b = parent[b];");
            Sb.AppendLine("// Walk up together until common ancestor");
            Sb.AppendLine("while (a >= 0 && b >= 0 && a != b) { a = parent[a]; b = parent[b]; }");
            Sb.AppendLine("return a >= 0 ? a : -1;");
        }
        Sb.AppendLine();
    }
    
    /// <summary>
    /// Writes HSM runtime fields and helper methods (instance-level) if HSM is enabled.
    /// </summary>
    protected virtual void WriteHierarchyRuntimeFieldsAndHelpers(string stateTypeForUsage)
    {
        if (!Model.HierarchyEnabled) return;
        
        // All runtime fields and methods are now in the base class
        // We don't emit anything here anymore
    }
    
    #endregion

    #region Common Implementation Methods

    
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
        // No matching transition at this point
        if (ShouldGenerateLogging)
        {
            WriteLogStatement("Warning",
                $"UnhandledTrigger(_logger, _instanceId, NameOf({CurrentStateField}), NameOfTrigger(trigger));");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, NameOf({CurrentStateField}), NameOfTrigger(trigger));");
        }
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
            var from = TypeHelper.EscapeIdentifier(transition.FromState);
            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
            Sb.AppendLine($"var guardOk = EvaluateGuard__{from}__{trig}(null);");
            // Check guard result and run hooks
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

        // Log transition started before any state changes
        if (ShouldGenerateLogging && !transition.IsInternal)
        {
            WriteLogStatement("Debug",
                $"TransitionStarted(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\", \"{transition.ToState}\");");
        }

        // OnExit (if applicable)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            if (IsAsyncMachine)
            {
                // Async semantics: always convert OnExit exceptions into failed transition (return false)
                using (Sb.Block("try"))
                {
                    CallbackGenerationHelper.EmitOnExitCall(
                        Sb,
                        fromStateDef,
                        transition.ExpectedPayloadType,
                        null,
                        "null",
                        IsAsyncMachine,
                        wrapInTryCatch: false,
                        Model.ContinueOnCapturedContext,
                        isSinglePayload: false,
                        isMultiPayload: false,
                        cancellationTokenVar: "cancellationToken",
                        treatCancellationAsFailure: true
                    );
                    WriteLogStatement("Debug",
                        $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                }
                using (Sb.Block("catch (System.OperationCanceledException)"))
                {
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
                using (Sb.Block("catch (System.Exception)"))
                {
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
            }
            else
            {
                Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                using (Sb.Block("try"))
                {
                    CallbackGenerationHelper.EmitOnExitCall(
                        Sb,
                        fromStateDef,
                        transition.ExpectedPayloadType,
                        null,
                        "null",
                        IsAsyncMachine,
                        wrapInTryCatch: false,
                        Model.ContinueOnCapturedContext,
                        isSinglePayload: false,
                        isMultiPayload: false,
                        cancellationTokenVar: null,
                        treatCancellationAsFailure: false
                    );
                    WriteLogStatement("Debug",
                        $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                }
                using (Sb.Block("catch (System.OperationCanceledException)"))
                {
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
                using (Sb.Block("catch (System.Exception)"))
                {
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
                Sb.AppendLine("#else");
                CallbackGenerationHelper.EmitOnExitCall(
                    Sb,
                    fromStateDef,
                    transition.ExpectedPayloadType,
                    null,
                    "null",
                    IsAsyncMachine,
                    wrapInTryCatch: false,
                    Model.ContinueOnCapturedContext,
                    isSinglePayload: false,
                    isMultiPayload: false,
                    cancellationTokenVar: null,
                    treatCancellationAsFailure: false
                );
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                Sb.AppendLine("#endif");
            }
        }

        // Store the previous state for potential rollback (only if we have exception handler and action)
        if (Model.ExceptionHandler != null && !string.IsNullOrEmpty(transition.ActionMethod))
        {
            Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
            Sb.AppendLine($"// FSM_DEBUG: Handler found: {Model.ExceptionHandler.MethodName}");
            Sb.AppendLine("#endif");
            Sb.AppendLine($"var prevState = {CurrentStateField};");
        }
        else if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
            Sb.AppendLine($"// FSM_DEBUG: No handler for {Model.ClassName}, action={transition.ActionMethod}");
            Sb.AppendLine("#endif");
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
            Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
            using (Sb.Block("try"))
            {
                CallbackGenerationHelper.EmitOnEntryCall(
                    Sb,
                    toStateDef,
                    transition.ExpectedPayloadType,
                    null,
                    "null",
                    IsAsyncMachine,
                    wrapInTryCatch: false,
                    Model.ContinueOnCapturedContext,
                    isSinglePayload: false,
                    isMultiPayload: false,
                    cancellationTokenVar: null,
                    treatCancellationAsFailure: false
                );
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
            }
            using (Sb.Block("catch (System.OperationCanceledException)"))
            {
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
            using (Sb.Block("catch (System.Exception ex)"))
            {
                // Log callback exception for OnEntry
                WriteLogStatement("Warning",
                    $"CallbackException(_logger, _instanceId, \"OnEntry\", \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\", ex);");
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
            Sb.AppendLine("#else");
            CallbackGenerationHelper.EmitOnEntryCall(
                Sb,
                toStateDef,
                transition.ExpectedPayloadType,
                null,
                "null",
                IsAsyncMachine,
                wrapInTryCatch: false,
                Model.ContinueOnCapturedContext,
                isSinglePayload: false,
                isMultiPayload: false,
                cancellationTokenVar: null,
                treatCancellationAsFailure: false
            );
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
            Sb.AppendLine("#endif");
        }

        // Action (if present)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            if (Model.ExceptionHandler == null)
            {
                Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                using (Sb.Block("try"))
                {
                    // Log async action start if async
                    if (IsAsyncMachine && transition.ActionIsAsync)
                    {
                        WriteLogStatement("Debug",
                            $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\");");
                        
                        // Track start time for elapsed calculation
                        Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
                    }
                    
                    CallbackGenerationHelper.EmitActionCall(
                        Sb,
                        transition,
                        "null",
                        IsAsyncMachine,
                        wrapInTryCatch: false,
                        Model.ContinueOnCapturedContext,
                        cancellationTokenVar: null,
                        treatCancellationAsFailure: false
                    );
                    
                    // Log completion based on whether it's async or not
                    if (IsAsyncMachine && transition.ActionIsAsync)
                    {
                        Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                        WriteLogStatement("Debug",
                            $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\", elapsedMs);");
                    }
                    else
                    {
                        WriteLogStatement("Debug",
                            $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                    }
                }
                using (Sb.Block("catch (System.OperationCanceledException)"))
                {
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
                using (Sb.Block("catch (System.Exception ex)"))
                {
                    // Log async action failure if async
                    if (IsAsyncMachine && transition.ActionIsAsync)
                    {
                        WriteLogStatement("Warning",
                            $"AsyncActionFailed(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\", ex);");
                    }
                    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                    Sb.AppendLine("return false;");
                }
                Sb.AppendLine("#else");
                // Log async action start if async (outside of try block)
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    WriteLogStatement("Debug",
                        $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\");");
                    
                    // Track start time for elapsed calculation
                    Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
                }
                
                CallbackGenerationHelper.EmitActionCall(
                    Sb,
                    transition,
                    "null",
                    IsAsyncMachine,
                    wrapInTryCatch: false,
                    Model.ContinueOnCapturedContext,
                    cancellationTokenVar: null,
                    treatCancellationAsFailure: false
                );
                
                // Log completion based on whether it's async or not
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                    WriteLogStatement("Debug",
                        $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\", elapsedMs);");
                }
                else
                {
                    WriteLogStatement("Debug",
                        $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                }
                Sb.AppendLine("#endif");
            }
            else
            {
                // Has exception handler - use directive-based exception handling
                using (Sb.Block("try"))
                {
                    // Log async action start if async
                    if (IsAsyncMachine && transition.ActionIsAsync)
                    {
                        WriteLogStatement("Debug",
                            $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\");");
                        
                        // Track start time for elapsed calculation
                        Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
                    }
                    
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
                    
                    // Log completion based on whether it's async or not
                    if (IsAsyncMachine && transition.ActionIsAsync)
                    {
                        Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                        WriteLogStatement("Debug",
                            $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\", elapsedMs);");
                    }
                    else
                    {
                        WriteLogStatement("Debug",
                            $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                    }
                }
                using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
                {
                    // Log async action failure if async
                    if (IsAsyncMachine && transition.ActionIsAsync)
                    {
                        WriteLogStatement("Warning",
                            $"AsyncActionFailed(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {transition.FromState} -> {transition.ToState}\", ex);");
                    }
                    
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
        Sb.AppendLine("int bestAncestorIndex = -1;");
        
        if (IsAsyncMachine)
        {
            Sb.AppendLine("ActionId bestActionId = ActionId.None;");
            Sb.AppendLine("AsyncActionId bestAsyncActionId = AsyncActionId.None;");
        }
        else
        {
            Sb.AppendLine("ActionId bestActionId = ActionId.None;");
        }
        
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
            Sb.AppendLine($"int depthFromCurrent = (check == currentIndex) ? 0 : (g_depth[currentIndex] - g_depth[check]);");
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
                                        GenerateInlineCandidateEvaluation(tr, item.Index, stateTypeForUsage);
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
            Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
        }
        
        Sb.AppendLine();
        
        // Apply the best candidate if found
        Sb.AppendLine("// Apply winner");
        using (Sb.Block("if (!found)"))
        {
        // No matching transition - failure
        if (ShouldGenerateLogging)
        {
            WriteLogStatement("Warning",
                $"UnhandledTrigger(_logger, _instanceId, NameOf({CurrentStateField}), NameOfTrigger(trigger));");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, NameOf({CurrentStateField}), NameOfTrigger(trigger));");
        }
        Sb.AppendLine("return false;");
    }
        Sb.AppendLine();
        
        using (Sb.Block("if (bestIsInternal)"))
        {
            Sb.AppendLine("// Internal transition: execute action without state change");
            if (IsAsyncMachine && HasAsyncActions())
            {
                // For async machines with async actions, handle both sync and async actions
                GenerateActionSwitch("bestActionId", isInternal: true);
                GenerateAsyncActionSwitch("bestAsyncActionId", isInternal: true);
            }
            else
            {
                GenerateActionSwitch("bestActionId", isInternal: true);
            }
            if (ShouldGenerateLogging)
            {
                WriteLogStatement("Debug",
                    $"InternalTransitionOnAncestor(_logger, _instanceId, NameOf(({stateTypeForUsage})bestAncestorIndex), NameOf(_currentState), NameOfTrigger(trigger));");
            }
            Sb.AppendLine("return true; // state unchanged, no history recording");
        }
        using (Sb.Block("else"))
        {
            Sb.AppendLine("// External transition: execute exit/enter chains with LCA");
            Sb.AppendLine();
            
            // Calculate LCA
            Sb.AppendLine("// Find LCA (Least Common Ancestor)");
            Sb.AppendLine("int srcLeaf = (int)_currentState;");
            Sb.AppendLine("int destLeaf = bestDestIndex;");
            Sb.AppendLine();
            Sb.AppendLine("// LCA via runtime helper");
            Sb.AppendLine("int lca = FindLowestCommonAncestor(srcLeaf, destLeaf);");
            Sb.AppendLine();
            
            // Record history before state change
            Sb.AppendLine("RecordHistoryForCurrentPath();");
            // Precompute exit count for diagnostics
            Sb.AppendLine("int __exitCount = 0;");
            Sb.AppendLine("for (int s = srcLeaf; s != lca && s >= 0; s = g_parent[s]) { __exitCount++; }");
            Sb.AppendLine();
            
            // Generate exit chain
            if (Model.GenerationConfig.HasOnEntryExit)
            {
                Sb.AppendLine("// EXIT chain: from current leaf up to (but not including) LCA");
                Sb.AppendLine("for (int s = srcLeaf; s != lca && s >= 0; s = g_parent[s]) {");
                Sb.AppendLine($"    var exitState = ({stateTypeForUsage})s;");
                Sb.AppendLine("    switch (exitState) {");
                foreach (var state in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnExitMethod)))
                {
                    Sb.AppendLine($"        case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Name)}:");
                    Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                    Sb.AppendLine($"            try {{ {state.OnExitMethod}(); }}");
                    Sb.AppendLine($"            catch (System.OperationCanceledException oce) {{ OnActionException(\"Exit:{state.Name}\", oce); return false; }}");
                    Sb.AppendLine($"            catch (System.Exception ex) {{ OnActionException(\"Exit:{state.Name}\", ex); return false; }}");
                    Sb.AppendLine("#else");
                    Sb.AppendLine($"            {state.OnExitMethod}();");
                    Sb.AppendLine("#endif");
                    Sb.AppendLine("            break;");
                }
                Sb.AppendLine("        default: break;");
                Sb.AppendLine("    }");
                Sb.AppendLine("}");
                Sb.AppendLine();
            }
            
            // Change state and resolve composite
            Sb.AppendLine("// Assign state and resolve composite target");
            
            // SAVE COMPOSITE BEFORE ASSIGNING _currentState
            Sb.AppendLine("int __targetComposite = bestDestIndex;");
            Sb.AppendLine();
            
            // Check if target is composite (has initial child)
            Sb.AppendLine("// Check if target is composite (has initial child)");
            Sb.AppendLine("bool __isComposite = (uint)__targetComposite < (uint)g_initialChild.Length && g_initialChild[__targetComposite] >= 0;");
            Sb.AppendLine();
            
            using (Sb.Block("if (__isComposite)"))
            {
                Sb.AppendLine("// Resolve entry into composite (Initial vs History)");
                Sb.AppendLine("int __resolvedIndex = GetCompositeEntryTarget(__targetComposite);");
                Sb.AppendLine("var __histMode = HistoryArray[__targetComposite];");
                Sb.AppendLine("string __resolution = (__histMode == Abstractions.Attributes.HistoryMode.None ? \"Initial\" : \"History\");");
                Sb.AppendLine();
                
                if (ShouldGenerateLogging)
                {
                    using (Sb.Block("if (_logger?.IsEnabled(LogLevel.Debug) == true)"))
                    {
                        Sb.AppendLine($"{Model.ClassName}Log.CompositeStateEntry(_logger, _instanceId, NameOf(({stateTypeForUsage})__targetComposite), NameOf(({stateTypeForUsage})__resolvedIndex), __resolution);");
                    }
                    using (Sb.Block("if (_logger?.IsEnabled(LogLevel.Debug) == true && __histMode != Abstractions.Attributes.HistoryMode.None)"))
                    {
                        Sb.AppendLine($"{Model.ClassName}Log.HistoryRestored(_logger, _instanceId, __histMode.ToString(), NameOf(({stateTypeForUsage})__targetComposite), NameOf(({stateTypeForUsage})__resolvedIndex));");
                    }
                }
                
                Sb.AppendLine($"_currentState = ({stateTypeForUsage})__resolvedIndex;");
            }
            using (Sb.Block("else"))
            {
                Sb.AppendLine("// Target is not composite - simple assignment");
                Sb.AppendLine($"_currentState = ({stateTypeForUsage})__targetComposite;");
            }
            Sb.AppendLine();
            
            // NOW count __entryCount (top-down) from resolved leaf
            Sb.AppendLine("// Count entries from resolved state");
            Sb.AppendLine("int __entryCount = 0;");
            Sb.AppendLine("for (int s = (int)_currentState; s >= 0 && s != lca; s = (s < g_parent.Length) ? g_parent[s] : -1) { __entryCount++; }");
            if (ShouldGenerateLogging)
            {
                WriteLogStatement("Debug",
                    $"HierarchicalTransition(_logger, _instanceId, NameOf(({stateTypeForUsage})srcLeaf), NameOf(_currentState), NameOf(({stateTypeForUsage})lca), __exitCount, __entryCount);");
                WriteLogStatement("Trace",
                    $"ActivePath(_logger, _instanceId, DumpActivePath());");
            }
            Sb.AppendLine();
            
            // Generate enter chain
            if (Model.GenerationConfig.HasOnEntryExit)
            {
                Sb.AppendLine("// ENTER chain: from LCA child down to final leaf");
                if (IsAsyncMachine)
                {
                    // Async path: use ArrayPool and await OnEntry if async
                    Sb.AppendLine("// Build entry path using ArrayPool to avoid Span across await");
                    Sb.AppendLine("int entryCount = 0;");
                    Sb.AppendLine("var pool = System.Buffers.ArrayPool<int>.Shared;");
                    Sb.AppendLine("int[] entryPath = pool.Rent(g_depth[(int)_currentState] + 1);");
                    Sb.AppendLine("try {");
                    Sb.AppendLine("    for (int s = (int)_currentState; s >= 0 && s != lca; s = (s < g_parent.Length) ? g_parent[s] : -1) {");
                    Sb.AppendLine("        entryPath[entryCount++] = s;");
                    Sb.AppendLine("    }");
                    Sb.AppendLine("    // Execute entry callbacks in top-down order");
                    Sb.AppendLine("    for (int i = entryCount - 1; i >= 0; i--) {");
                    Sb.AppendLine($"        var entryState = ({stateTypeForUsage})entryPath[i];");
                    Sb.AppendLine("        switch (entryState) {");
                    foreach (var state in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
                    {
                        Sb.AppendLine($"            case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Name)}:");
                        Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                        Sb.AppendLine("                try {");
                        if (state.OnEntryIsAsync)
                        {
                            Sb.AppendLine($"                    var vt = {state.OnEntryMethod}();");
                            Sb.AppendLine("                    if (!vt.IsCompletedSuccessfully)");
                            Sb.AppendLine($"                        await vt{GetConfigureAwait()};");
                        }
                        else
                        {
                            Sb.AppendLine($"                    {state.OnEntryMethod}();");
                        }
                        Sb.AppendLine($"                }} catch (System.OperationCanceledException oce) {{ OnActionException(\"Enter:{state.Name}\", oce); return false; }}");
                        Sb.AppendLine($"                  catch (System.Exception ex) {{ OnActionException(\"Enter:{state.Name}\", ex); return false; }}");
                        Sb.AppendLine("#else");
                        if (state.OnEntryIsAsync)
                        {
                            Sb.AppendLine($"                var vt = {state.OnEntryMethod}();");
                            Sb.AppendLine("                if (!vt.IsCompletedSuccessfully)");
                            Sb.AppendLine($"                    await vt{GetConfigureAwait()};");
                        }
                        else
                        {
                            Sb.AppendLine($"                {state.OnEntryMethod}();");
                        }
                        Sb.AppendLine("#endif");
                        Sb.AppendLine("                break;");
                    }
                    Sb.AppendLine("            default: break;");
                    Sb.AppendLine("        }");
                    Sb.AppendLine("    }");
                    Sb.AppendLine("} finally {");
                    Sb.AppendLine("    pool.Return(entryPath, clearArray: false);");
                    Sb.AppendLine("}");
                }
                else
                {
                    // Sync path: keep zero-alloc stackalloc
                    Sb.AppendLine("// Build entry path (stackalloc for zero-alloc)");
                    Sb.AppendLine("int entryCount = 0;");
                    Sb.AppendLine($"Span<int> entryPath = stackalloc int[g_depth[(int)_currentState] + 1];");
                    Sb.AppendLine("for (int s = (int)_currentState; s >= 0 && s != lca; s = (s < g_parent.Length) ? g_parent[s] : -1) {");
                    Sb.AppendLine("    entryPath[entryCount++] = s;");
                    Sb.AppendLine("}");
                    Sb.AppendLine("// Execute entry callbacks in top-down order");
                    Sb.AppendLine("for (int i = entryCount - 1; i >= 0; i--) {");
                    Sb.AppendLine($"    var entryState = ({stateTypeForUsage})entryPath[i];");
                    Sb.AppendLine("    switch (entryState) {");
                    foreach (var state in Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
                    {
                        Sb.AppendLine($"        case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(state.Name)}:");
                        Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                        Sb.AppendLine($"            try {{ {state.OnEntryMethod}(); }}");
                        Sb.AppendLine($"            catch (System.OperationCanceledException oce) {{ OnActionException(\"Enter:{state.Name}\", oce); return false; }}");
                        Sb.AppendLine($"            catch (System.Exception ex) {{ OnActionException(\"Enter:{state.Name}\", ex); return false; }}");
                        Sb.AppendLine("#else");
                        Sb.AppendLine($"            {state.OnEntryMethod}();");
                        Sb.AppendLine("#endif");
                        Sb.AppendLine("            break;");
                    }
                    Sb.AppendLine("        default: break;");
                    Sb.AppendLine("    }");
                    Sb.AppendLine("}");
                    Sb.AppendLine();
                }
            }
            
            // Execute transition action if present
            if (IsAsyncMachine && HasAsyncActions())
            {
                // For async machines with async actions, handle both sync and async actions
                GenerateActionSwitch("bestActionId", isInternal: false);
                GenerateAsyncActionSwitch("bestAsyncActionId", isInternal: false);
            }
            else
            {
                GenerateActionSwitch("bestActionId", isInternal: false);
            }
            
            Sb.AppendLine("return true;");
        }
    }
    
    private void GenerateInlineCandidateEvaluation(
        TransitionModel transition,
        int transitionIndex,
        string stateTypeForUsage)
    {
        using (Sb.Block(""))
        {
            // Guard check if present
            if (!string.IsNullOrEmpty(transition.GuardMethod))
            {
                // Use EvaluateGuard helper for DRY and consistent SAFE policy
                var from = TypeHelper.EscapeIdentifier(transition.FromState);
                var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                Sb.AppendLine($"var guardResult = EvaluateGuard__{from}__{trig}(payload);");
                
                Sb.AppendLine("if (!guardResult) { declOrder++; } // skip this candidate");
                Sb.AppendLine("else");
                using (Sb.Block(""))
                {
                    GenerateCandidateSelection(transition, stateTypeForUsage);
                }
            }
            else
            {
                // No guard - always evaluate
                GenerateCandidateSelection(transition, stateTypeForUsage);
            }
        }
    }
    
    private void GenerateCandidateSelection(
        TransitionModel transition,
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
            Sb.AppendLine("bestAncestorIndex = check;");
            
            if (!transition.IsInternal)
            {
                Sb.AppendLine($"bestDestIndex = (int){stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
            
            // Store action ID to execute later (no lambda allocation)
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                var actionId = GetActionIdName(transition);
                
                // Check if this is an async action and we're in an async machine
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    Sb.AppendLine($"bestAsyncActionId = AsyncActionId.{actionId};");
                }
                else
                {
                    Sb.AppendLine($"bestActionId = ActionId.{actionId};");
                }
                
                // Store payload if needed
                if (Model.GenerationConfig.HasPayload && transition.ActionExpectsPayload)
                {
                    Sb.AppendLine("bestPayload = payload;");
                }
            }
            else
            {
                if (IsAsyncMachine)
                {
                    Sb.AppendLine("bestAsyncActionId = AsyncActionId.None;");
                }
                else
                {
                    Sb.AppendLine("bestActionId = ActionId.None;");
                }
            }
        }
        Sb.AppendLine("declOrder++;");
    }
    
   

    
    protected void WriteStateChangeWithCompositeHandling(string targetState, string stateTypeForUsage)
    {
        // Always use GetCompositeEntryTarget for all external transitions
        // This ensures proper history handling even for leaf destinations
        Sb.AppendLine($"// Set destination and resolve through GetCompositeEntryTarget");
        
        // SAVE COMPOSITE BEFORE ASSIGNING _currentState
        Sb.AppendLine($"int __targetComposite = (int){stateTypeForUsage}.{TypeHelper.EscapeIdentifier(targetState)};");
        Sb.AppendLine();
        
        // Check if target is composite (has initial child)
        Sb.AppendLine("// Check if target is composite (has initial child)");
        Sb.AppendLine("bool __isComposite = (uint)__targetComposite < (uint)g_initialChild.Length && g_initialChild[__targetComposite] >= 0;");
        Sb.AppendLine();
        
        using (Sb.Block("if (__isComposite)"))
        {
            Sb.AppendLine("// Resolve entry into composite (Initial vs History)");
            Sb.AppendLine("int __resolvedIndex = GetCompositeEntryTarget(__targetComposite);");
            Sb.AppendLine("var __histMode = HistoryArray[__targetComposite];");
            Sb.AppendLine("string __resolution = (__histMode == Abstractions.Attributes.HistoryMode.None ? \"Initial\" : \"History\");");
            Sb.AppendLine();
            
            if (ShouldGenerateLogging)
            {
                using (Sb.Block("if (_logger?.IsEnabled(LogLevel.Debug) == true)"))
                {
                    Sb.AppendLine($"{Model.ClassName}Log.CompositeStateEntry(_logger, _instanceId, (({stateTypeForUsage})__targetComposite).ToString(), (({stateTypeForUsage})__resolvedIndex).ToString(), __resolution);");
                }
                using (Sb.Block("if (_logger?.IsEnabled(LogLevel.Debug) == true && __histMode != Abstractions.Attributes.HistoryMode.None)"))
                {
                    Sb.AppendLine($"{Model.ClassName}Log.HistoryRestored(_logger, _instanceId, __histMode.ToString(), (({stateTypeForUsage})__targetComposite).ToString(), (({stateTypeForUsage})__resolvedIndex).ToString());");
                }
            }
            
            Sb.AppendLine($"{CurrentStateField} = ({stateTypeForUsage})__resolvedIndex;");
        }
        using (Sb.Block("else"))
        {
            Sb.AppendLine("// Target is not composite - simple assignment");
            Sb.AppendLine($"{CurrentStateField} = ({stateTypeForUsage})__targetComposite;");
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


    #endregion

    #region Virtual Methods for Customization

    protected virtual bool ShouldGenerateOnEntryExit() => Model.GenerationConfig.HasOnEntryExit;

    protected virtual void WriteGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        // Wrap entire guard logic in try-catch
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
            // Treat exception in guard as false (guard did not pass)
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

            Sb.AppendLine($"{SuccessVar} = false;");

            // Hook: After failed transition
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);

            // Jump to end of method
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
        Sb.AppendLine($"// Generator Build: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Sb.AppendLine("#nullable enable");

        // Standard usings
        AddUsing(NamespaceSystem);
        AddUsing(NamespaceSystemCollectionsGeneric);
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
            AddUsing("FastFsm.Exceptions");
        }
        
        if (Model.ExceptionHandler != null)
        {
            AddUsing(NamespaceStateMachineExceptions);
        }
        
        // Conditionally add LINQ only when generated code needs it (async/HSM/Extensions)
        if (IsAsyncMachine || Model.HierarchyEnabled || IsExtensionsVariant())
        {
            AddUsing(NamespaceSystemLinq);
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

    protected virtual void WriteGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.WriteReturns("List of triggers that can be fired in the current state");
        using (Sb.Block($"protected override {ReadOnlyListType}<{triggerTypeForUsage}> GetPermittedTriggersInternal()"))
        {
            if (Model.HierarchyEnabled)
            {
                // HSM: Walk up parent chain, OR bitmasks, return cached array (zero-alloc)
                // Get unique triggers for bit mapping
                var uniqueTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
                
                Sb.AppendLine("// Walk up parent chain and OR trigger bits");
                Sb.AppendLine("int mask = 0;");
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
                                    var triggerBit = uniqueTriggers.IndexOf(transition.Trigger);
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                        var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                        Sb.AppendLine($"if (EvaluateGuard__{from}__{trig}(null)) mask |= (1 << {triggerBit});");
                                    }
                                    else
                                    {
                                        Sb.AppendLine($"mask |= (1 << {triggerBit}); // {transition.Trigger} (no guard)");
                                    }
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.AppendLine("default: break;");
                    }
                    Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                }
                
                Sb.AppendLine("// Return precomputed array based on mask");
                Sb.AppendLine("return s_perm__Mask[mask];");
            }
            else
            {
                // Flat FSM: Zero-alloc via precomputed arrays and guard mask
                using (Sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByFromState = Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByFromState)
                    {
                        var stateName = stateGroup.Key;
                        var escapedState = TypeHelper.EscapeIdentifier(stateName);
                        var stateFieldSuffix = UnifiedStateMachineGenerator_MemberSuffixWrapper(stateName);
                        Sb.AppendLine($"case {stateTypeForUsage}.{escapedState}:");
                        using (Sb.Block(""))
                        {
                            var guarded = stateGroup.Where(t => !string.IsNullOrEmpty(t.GuardMethod)).ToList();
                            if (guarded.Count == 0)
                            {
                                // No guards: return 1D cached array
                                Sb.AppendLine($"return s_perm__{stateFieldSuffix};");
                            }
                            else
                            {
                                Sb.AppendLine("int mask = 0;");
                                for (int i = 0; i < guarded.Count; i++)
                                {
                                    var tr = guarded[i];
                                    var from = TypeHelper.EscapeIdentifier(tr.FromState);
                                    var trig = TypeHelper.EscapeIdentifier(tr.Trigger);
                                    Sb.AppendLine($"if (EvaluateGuard__{from}__{trig}(null)) mask |= {1 << i};");
                                }
                                Sb.AppendLine($"return s_perm__{stateFieldSuffix}[mask];");
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
                // Get unique triggers for bit mapping
                var uniqueTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
                
                Sb.AppendLine("// Build mask identically to GetPermittedTriggersInternal");
                Sb.AppendLine("int mask = 0;");
                Sb.AppendLine($"int currentIndex = (int){CurrentStateField};");
                Sb.AppendLine("int check = currentIndex;");
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
                                foreach (var transition in stateGroup)
                                {
                                    var triggerBit = uniqueTriggers.IndexOf(transition.Trigger);
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                        var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                        Sb.AppendLine($"if (EvaluateGuard__{from}__{trig}(null)) mask |= (1 << {triggerBit});");
                                    }
                                    else
                                    {
                                        Sb.AppendLine($"mask |= (1 << {triggerBit}); // {transition.Trigger} (no guard)");
                                    }
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.AppendLine("default: break;");
                    }
                    Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                }
                Sb.AppendLine();
                Sb.AppendLine("// Copy from precomputed array to destination span");
                Sb.AppendLine($"var result = s_perm__Mask[mask];");
                Sb.AppendLine("if (result.Length > destination.Length) return -1;");
                Sb.AppendLine("for (int i = 0; i < result.Length; i++)");
                Sb.AppendLine("{");
                Sb.AppendLine("    destination[i] = result[i];");
                Sb.AppendLine("}");
                Sb.AppendLine("return result.Length;");
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

    // Helper to mirror UnifiedStateMachineGenerator.MakeSafeMemberSuffix without coupling
    protected static string UnifiedStateMachineGenerator_MemberSuffixWrapper(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "_";
        if (raw.Length > 0 && raw[0] == '@') raw = raw.Substring(1);
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            else sb.Append('_');
        }
        var s = sb.ToString();
        if (s.Length == 0 || char.IsDigit(s[0])) s = "_" + s;
        switch (s)
        {
            case "class": case "return": case "void": case "int": case "interface": case "namespace":
            case "new": case "throw": case "break": case "continue": case "goto":
                s = s + "_"; break;
        }
        return s;
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
            // No exception handler - wrap with FASTFSM_SAFE_ACTIONS to optionally swallow exceptions
            Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
            using (Sb.Block("try"))
            {
                WriteOnEntryCall(toStateDef, expectedPayloadType);
                WriteLogStatement("Debug",
                    $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
            }
            using (Sb.Block("catch (System.OperationCanceledException)"))
            {
                Sb.AppendLine("return false;");
            }
            using (Sb.Block("catch (System.Exception)"))
            {
                Sb.AppendLine("return false;");
            }
            Sb.AppendLine("#else");
            WriteOnEntryCall(toStateDef, expectedPayloadType);
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{toState}\");");
            Sb.AppendLine("#endif");
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
        // Debug output to trace the call
        Sb.AppendLine($"// DEBUG: EmitActionWithExceptionPolicy called for {transition.ActionMethod}, IsAsync={IsAsyncMachine}, ActionIsAsync={transition.ActionIsAsync}");
        
        if (Model.ExceptionHandler == null)
        {
            // No exception handler - wrap with FASTFSM_SAFE_ACTIONS to optionally swallow exceptions
            Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
            using (Sb.Block("try"))
            {
                // Log async action start if async
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    WriteLogStatement("Debug",
                        $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\");");
                    Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
                }
                
                WriteActionCall(transition);
                
                // Log completion based on whether it's async or not
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                    WriteLogStatement("Debug",
                        $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", elapsedMs);");
                }
                else
                {
                    WriteLogStatement("Debug",
                        $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
                }
            }
            using (Sb.Block("catch (System.OperationCanceledException)"))
            {
                Sb.AppendLine("return false;");
            }
            using (Sb.Block("catch (System.Exception ex)"))
            {
                // Log async action failure if async
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    WriteLogStatement("Warning",
                        $"AsyncActionFailed(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", ex);");
                }
                Sb.AppendLine("return false;");
            }
            Sb.AppendLine("#else");
            
            // Log async action start if async (outside try block)
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                WriteLogStatement("Debug",
                    $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\");");
                Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
            }
            
            WriteActionCall(transition);
            
            // Log completion based on whether it's async or not
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                WriteLogStatement("Debug",
                    $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", elapsedMs);");
            }
            else
            {
                WriteLogStatement("Debug",
                    $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            }
            Sb.AppendLine("#endif");
            return;
        }

        // Wrap in try/catch with exception policy
        using (Sb.Block("try"))
        {
            // Log async action start if async
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                WriteLogStatement("Debug",
                    $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\");");
                Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
            }
            
            WriteActionCall(transition);
            
            // Log completion based on whether it's async or not
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                WriteLogStatement("Debug",
                    $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", elapsedMs);");
            }
            else
            {
                WriteLogStatement("Debug",
                    $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            }
        }
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
        {
            // Log async action failure if async
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                WriteLogStatement("Warning",
                    $"AsyncActionFailed(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", ex);");
            }
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
            // No exception handler - wrap with FASTFSM_SAFE_ACTIONS to optionally swallow exceptions
            Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
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
            using (Sb.Block("catch (System.OperationCanceledException)"))
            {
                Sb.AppendLine("return false;");
            }
            using (Sb.Block("catch (System.Exception)"))
            {
                Sb.AppendLine("return false;");
            }
            Sb.AppendLine("#else");
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
            Sb.AppendLine("#endif");
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
            // No exception handler - wrap with FASTFSM_SAFE_ACTIONS to optionally swallow exceptions
            Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
            using (Sb.Block("try"))
            {
                // Log async action start if async
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    WriteLogStatement("Debug",
                        $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\");");
                    Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
                }
                
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
                
                // Log completion based on whether it's async or not
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                    WriteLogStatement("Debug",
                        $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", elapsedMs);");
                }
                else
                {
                    WriteLogStatement("Debug",
                        $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
                }
            }
            using (Sb.Block("catch (System.OperationCanceledException)"))
            {
                Sb.AppendLine("return false;");
            }
            using (Sb.Block("catch (System.Exception ex)"))
            {
                // Log async action failure if async
                if (IsAsyncMachine && transition.ActionIsAsync)
                {
                    WriteLogStatement("Warning",
                        $"AsyncActionFailed(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", ex);");
                }
                Sb.AppendLine("return false;");
            }
            Sb.AppendLine("#else");
            
            // Log async action start if async (outside try block)
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                WriteLogStatement("Debug",
                    $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\");");
                Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
            }
            
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
            
            // Log completion based on whether it's async or not
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                WriteLogStatement("Debug",
                    $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", elapsedMs);");
            }
            else
            {
                WriteLogStatement("Debug",
                    $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            }
            Sb.AppendLine("#endif");
            return;
        }

        // Wrap in try/catch with exception policy
        using (Sb.Block("try"))
        {
            // Log async action start if async
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                WriteLogStatement("Debug",
                    $"AsyncActionStarted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\");");
                Sb.AppendLine("var actionStart = System.Diagnostics.Stopwatch.GetTimestamp();");
            }
            
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
            
            // Log completion based on whether it's async or not
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                Sb.AppendLine("var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(actionStart).TotalMilliseconds;");
                WriteLogStatement("Debug",
                    $"AsyncActionCompleted(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", elapsedMs);");
            }
            else
            {
                WriteLogStatement("Debug",
                    $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{fromState}\", \"{toState}\", \"{transition.Trigger}\");");
            }
        }
        using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
        {
            // Log async action failure if async
            if (IsAsyncMachine && transition.ActionIsAsync)
            {
                WriteLogStatement("Warning",
                    $"AsyncActionFailed(_logger, _instanceId, \"{transition.ActionMethod}\", \"transition {fromState} -> {toState}\", ex);");
            }
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

    #endregion

    #region Abstractions to be implemented by concrete generators
    protected abstract void WriteNamespaceAndClass();
    #endregion
    
    #region Helper Methods
    private static void EmitXmlDocSummary(IndentedStringBuilder.IndentedStringBuilder sb, string? text)
    {
        var normalized = global::System.Text.RegularExpressions.Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        sb.WriteSummary(normalized);
    }
    
    /// <summary>
    /// Generates the ActionId enum for zero-allocation action dispatch
    /// </summary>
    protected bool HasAsyncActions()
    {
        return Model.Transitions.Any(t => !string.IsNullOrEmpty(t.ActionMethod) && t.ActionIsAsync);
    }
    
    protected void GenerateActionIdEnum()
    {
        // Collect all unique action methods for sync
        var actionNames = Model.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ActionMethod) && !t.ActionIsAsync)
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
    /// Generates the AsyncActionId enum for zero-allocation async action dispatch
    /// </summary>
    protected void GenerateAsyncActionIdEnum()
    {
        // Collect all unique async action methods
        var asyncActionNames = Model.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ActionMethod) && t.ActionIsAsync)
            .Select(GetActionIdName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        // Always generate enum (with None only if no async actions) to satisfy references in async machines
        Sb.AppendLine("// Async action dispatch enum (zero-allocation)");
        using (Sb.Block("private enum AsyncActionId : ushort"))
        {
            Sb.AppendLine("None = 0,");
            ushort id = 1;
            foreach (var name in asyncActionNames)
            {
                Sb.AppendLine($"{name} = {id},");
                id++;
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
    /// Generates the action execution switch statement for sync actions
    /// </summary>
    protected void GenerateActionSwitch(string actionIdVar, bool isInternal)
    {
        // Group transitions by action method - only sync actions
        var groups = Model.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ActionMethod) && !t.ActionIsAsync)
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
                
                // Check if any transition in this group expects payload
                bool anyPayload = group.Any(t => t.ActionExpectsPayload && Model.GenerationConfig.HasPayload);
                
                using (Sb.Block($"case ActionId.{actionIdName}:"))
                {
                    using (Sb.Block("try"))
                    {
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
                    using (Sb.Block("catch"))
                    {
                        if (isInternal)
                        {
                            Sb.AppendLine("return false;");
                        }
                        else
                        {
                            Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
                            Sb.AppendLine("/* action failed but transition succeeded */");
                            Sb.AppendLine("#endif");
                        }
                    }
                    Sb.AppendLine("break;");
                }
            }
            
            Sb.AppendLine("default: break;");
        }
    }
    
    /// <summary>
    /// Generates the async action execution switch statement with ValueTask fast-path
    /// </summary>
    protected void GenerateAsyncActionSwitch(string actionIdVar, bool isInternal)
    {
        // Group transitions by action method - only async actions
        var groups = Model.Transitions
            .Where(t => !string.IsNullOrEmpty(t.ActionMethod) && t.ActionIsAsync)
            .GroupBy(t => t.ActionMethod)
            .OrderBy(g => g.Key)
            .ToList();
        
        // Always generate switch, even if empty
        using (Sb.Block($"switch ({actionIdVar})"))
        {
            Sb.AppendLine("case AsyncActionId.None: break;");
            
            foreach (var group in groups)
            {
                var methodName = group.Key;
                var actionIdName = GetActionIdName(group.First());
                
                // Check if any transition in this group expects payload
                bool anyPayload = group.Any(t => t.ActionExpectsPayload && Model.GenerationConfig.HasPayload);
                
                using (Sb.Block($"case AsyncActionId.{actionIdName}:"))
                {
                    using (Sb.Block("try"))
                    {
                        if (anyPayload)
                        {
                            var payloadType = group.FirstOrDefault(t => !string.IsNullOrEmpty(t.ExpectedPayloadType))?.ExpectedPayloadType
                                            ?? Model.DefaultPayloadType;
                            
                            if (!string.IsNullOrEmpty(payloadType))
                            {
                                using (Sb.Block($"if (bestPayload is {GetTypeNameForUsage(payloadType)} p)"))
                                {
                                    // ValueTask fast-path
                                    Sb.AppendLine($"var vt = {methodName}(p);");
                                    Sb.AppendLine("if (!vt.IsCompletedSuccessfully)");
                                    Sb.AppendLine($"    await vt{GetConfigureAwait()};");
                                }
                            }
                            else
                            {
                                // ValueTask fast-path
                                Sb.AppendLine($"var vt = {methodName}(bestPayload);");
                                Sb.AppendLine("if (!vt.IsCompletedSuccessfully)");
                                Sb.AppendLine($"    await vt{GetConfigureAwait()};");
                            }
                        }
                        else
                        {
                            // ValueTask fast-path
                            Sb.AppendLine($"var vt = {methodName}();");
                            Sb.AppendLine("if (!vt.IsCompletedSuccessfully)");
                            Sb.AppendLine($"    await vt{GetConfigureAwait()};");
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
                            Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
                            Sb.AppendLine("/* action failed but transition succeeded */");
                            Sb.AppendLine("#endif");
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
