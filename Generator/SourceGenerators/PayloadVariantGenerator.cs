using Generator.Helpers;
using Generator.Model;
using System.Linq;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Generuje kod dla wariantów z payloadem (sync i async).
/// </summary>
internal class PayloadVariantGenerator(StateMachineModel model) : StateMachineCodeGenerator(model)
{
    /// <summary>
    /// Creates a safe identifier from the provided base name by sanitizing special characters.
    /// </summary>
    private static string MakeSafeIdentifier(string baseName)
    {
        var sb = new System.Text.StringBuilder(baseName.Length + 8);
        bool first = true;
        foreach (var ch in baseName)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_'
                || (!first && ch >= '0' && ch <= '9'))
                sb.Append(ch);
            else
                sb.Append('_');
            first = false;
        }
        if (sb.Length == 0 || (sb[0] >= '0' && sb[0] <= '9'))
            sb.Insert(0, '_');
        return sb.ToString();
    }

    protected override void WriteNamespaceAndClass()
    {
        var stateTypeForUsage = GetTypeNameForUsage(Model.StateType);
        var triggerTypeForUsage = GetTypeNameForUsage(Model.TriggerType);
        var userNamespace = Model.Namespace;
        var className = Model.ClassName;

        void WriteClassContent()
        {
            // Generowanie interfejsu
            var interfaceName = GetInterfaceName(stateTypeForUsage, triggerTypeForUsage);
            Sb.AppendLine($"public interface I{className} : {interfaceName} {{ }}");

            Sb.AppendLine();

            // Generowanie klasy z odpowiednią klasą bazową
            var baseClass = GetBaseClassName(stateTypeForUsage, triggerTypeForUsage);

            using (Sb.Block($"public partial class {className} : {baseClass}, I{className}"))
            {
                // Pola dla async
                if (IsAsyncMachine)
                {
                    Sb.AppendLine("private readonly string _instanceId = Guid.NewGuid().ToString();");
                    Sb.AppendLine();
                }

                WriteLoggerField(className);

                if (IsMultiPayloadVariant())
                {
                    WritePayloadMap(triggerTypeForUsage);
                }

                // Write HSM arrays and runtime helpers
                WriteHierarchyArrays(stateTypeForUsage);
                WriteHierarchyRuntimeFieldsAndHelpers(stateTypeForUsage);

                WriteConstructor(stateTypeForUsage, className);
                
                // Generate Start override for HSM
                WriteStartMethod();
                
                // Generate OnInitialEntry/OnInitialEntryAsync override
                if (ShouldGenerateInitialOnEntry())
                {
                    if (IsAsyncMachine)
                        WriteOnInitialEntryAsyncMethod(stateTypeForUsage);
                    else
                        WriteOnInitialEntryMethod(stateTypeForUsage);
                }
                
                WriteTryFireMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteFireMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteCanFireMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
                WriteGetPermittedTriggersWithResolver(stateTypeForUsage, triggerTypeForUsage);
                WriteStructuralApiMethods(stateTypeForUsage, triggerTypeForUsage);
                WriteHierarchyMethods(stateTypeForUsage, triggerTypeForUsage);
            }
        }

        if (!string.IsNullOrEmpty(userNamespace))
        {
            using (Sb.Block($"namespace {userNamespace}"))
            {
                WriteContainingTypes();
                WriteClassContent();
                CloseContainingTypes();
            }
        }
        else
        {
            WriteContainingTypes();
            WriteClassContent();
            CloseContainingTypes();
        }
        void WriteContainingTypes()
        {
            foreach (var container in Model.ContainerClasses)
            {
                Sb.AppendLine($"public partial class {container}");
                Sb.AppendLine("{");
            }
        }

        void CloseContainingTypes()
        {
            for (int i = 0; i < Model.ContainerClasses.Count; i++)
            {
                Sb.AppendLine("}");
            }
        }
    }

    protected virtual void WriteConstructor(string stateTypeForUsage, string className)
    {
        var paramList = BuildConstructorParameters(stateTypeForUsage, GetLoggerConstructorParameter(className));

        // Dla async maszyn przekaż continueOnCapturedContext
        var baseCall = IsAsyncMachine
            ? $"base(initialState, continueOnCapturedContext: {Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()})"
            : "base(initialState)";

        using (Sb.Block($"public {className}({string.Join(", ", paramList)}) : {baseCall}"))
        {
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("// Initialize history tracking array with -1 (no history)");
                Sb.AppendLine("_lastActiveChild = new int[s_initialChild.Length];");
                Sb.AppendLine("for (int i = 0; i < _lastActiveChild.Length; i++) _lastActiveChild[i] = -1;");
            }
            
            WriteLoggerAssignment();

            // Initial OnEntry dispatch moved to OnInitialEntry/OnInitialEntryAsync method
            // HSM initial state resolution is done in Start() method, not constructor
        }
        Sb.AppendLine();
    }

    protected void WriteOnInitialEntryAsyncMethod(string stateTypeForUsage)
    {
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();

        if (statesWithParameterlessOnEntry.Any())
        {
            using (Sb.Block("protected override async ValueTask OnInitialEntryAsync(System.Threading.CancellationToken cancellationToken = default)"))
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
                            foreach (var stateEntry in statesWithParameterlessOnEntry)
                            {
                                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                                using (Sb.Indent())
                                {
                                    AsyncGenerationHelper.EmitMethodInvocation(
                                        Sb,
                                        stateEntry.OnEntryMethod,
                                        stateEntry.OnEntryIsAsync,
                                        callerIsAsync: true,
                                        Model.ContinueOnCapturedContext
                                    );
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
                        foreach (var stateEntry in statesWithParameterlessOnEntry)
                        {
                            Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                            using (Sb.Indent())
                            {
                                AsyncGenerationHelper.EmitMethodInvocation(
                                    Sb,
                                    stateEntry.OnEntryMethod,
                                    stateEntry.OnEntryIsAsync,
                                    callerIsAsync: true,
                                    Model.ContinueOnCapturedContext
                                );
                                Sb.AppendLine("break;");
                            }
                        }
                    }
                }
            }
            Sb.AppendLine();
        }
    }

    protected override void WriteTransitionLogic(
       TransitionModel transition,
       string stateTypeForUsage,
       string triggerTypeForUsage)
    {
        // Pobierz definicje stanów z mapy (jawne przypisanie, żeby uniknąć CS0165)
        Model.States.TryGetValue(transition.FromState, out var fromStateDef);

        Model.States.TryGetValue(transition.ToState, out var toStateDef);

        bool fromHasExit = !transition.IsInternal
                            && fromStateDef != null
                            && !string.IsNullOrEmpty(fromStateDef.OnExitMethod);

        bool toHasEntry = !transition.IsInternal
                            && toStateDef != null
                            && !string.IsNullOrEmpty(toStateDef.OnEntryMethod);

        // Hook przed przejściem
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteGuardCheck(transition, stateTypeForUsage, triggerTypeForUsage);
        }

        // OnExit (keep exception handling - it prevents transition if OnExit fails)
        if (fromHasExit)
        {
            CallbackGenerationHelper.EmitOnExitCall(
                Sb,
                fromStateDef!,
                transition.ExpectedPayloadType,
                Model.DefaultPayloadType,
                PayloadVar,
                IsAsyncMachine,
                wrapInTryCatch: true,
                Model.ContinueOnCapturedContext,
                IsSinglePayloadVariant(),
                IsMultiPayloadVariant(),
                cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                treatCancellationAsFailure: IsAsyncMachine
            );

            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef!.OnExitMethod}\", \"{transition.FromState}\");");
        }

        // State change (before OnEntry)
        if (!transition.IsInternal)
        {
            // Record history before changing state (for HSM)
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("RecordHistoryForCurrentPath();");
            }
            
            // For HSM, handle composite states properly
            if (Model.HierarchyEnabled)
            {
                WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
            }
            else
            {
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
        }

        // OnEntry (with optional exception policy)
        if (toHasEntry)
        {
            EmitOnEntryWithExceptionPolicyPayload(
                toStateDef!,
                transition.ExpectedPayloadType,
                Model.DefaultPayloadType!,
                transition.FromState,
                transition.ToState,
                transition.Trigger,
                IsSinglePayloadVariant(),
                IsMultiPayloadVariant()
            );
        }

        // Action (with optional exception policy)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            EmitActionWithExceptionPolicyPayload(transition, transition.FromState, transition.ToState);
        }

        // Log successful transition only after OnEntry succeeds
        if (!transition.IsInternal)
        {
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        Sb.AppendLine($"{SuccessVar} = true;");
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);
        Sb.AppendLine($"goto {EndOfTryFireLabel};");
    }
    
    private void WriteTransitionLogicDirect(
       TransitionModel transition,
       string stateTypeForUsage,
       string triggerTypeForUsage)
    {
        // Get state definitions
        Model.States.TryGetValue(transition.FromState, out var fromStateDef);
        Model.States.TryGetValue(transition.ToState, out var toStateDef);

        bool fromHasExit = !transition.IsInternal
                            && fromStateDef != null
                            && !string.IsNullOrEmpty(fromStateDef.OnExitMethod);

        bool toHasEntry = !transition.IsInternal
                            && toStateDef != null
                            && !string.IsNullOrEmpty(toStateDef.OnEntryMethod);

        // Determine if we need payload pattern matching
        bool needsPayload = (transition.GuardExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
                          || (transition.ActionExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
                          || (fromHasExit && fromStateDef!.OnExitExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
                          || (toHasEntry && toStateDef!.OnEntryExpectsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType));

        // Hook before transition (must be called for all transitions)
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);
        
        // Generate unique payload variable name if needed
        string payloadVarName = "";
        if (needsPayload && !string.IsNullOrEmpty(transition.ExpectedPayloadType))
        {
            payloadVarName = MakeSafeIdentifier($"p_{transition.FromState}_{transition.Trigger}_{transition.ToState}");
            var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType);
            
            // Open payload pattern matching block
            Sb.AppendLine($"if ({PayloadVar} is {payloadType} {payloadVarName})");
            Sb.AppendLine("{");
            using (Sb.Indent())
            {

        // Guard with direct return
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            
            Sb.AppendLine("bool guardOk = true;");
            if (transition.GuardExpectsPayload && !string.IsNullOrEmpty(payloadVarName))
            {
                Sb.AppendLine($"try {{ guardOk = {transition.GuardMethod}({payloadVarName}); }} catch {{ return false; }}");
            }
            else
            {
                Sb.AppendLine($"try {{ guardOk = {transition.GuardMethod}(); }} catch {{ return false; }}");
            }

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

        // OnExit with direct return on failure
        if (fromHasExit)
        {
            if (fromStateDef!.OnExitExpectsPayload && !string.IsNullOrEmpty(payloadVarName))
            {
                if (fromStateDef.OnExitIsAsync && !IsAsyncMachine)
                {
                    Sb.AppendLine($"try {{ {fromStateDef.OnExitMethod}({payloadVarName}, System.Threading.CancellationToken.None); }} catch {{ return false; }}");
                }
                else
                {
                    Sb.AppendLine($"try {{ {fromStateDef.OnExitMethod}({payloadVarName}); }} catch {{ return false; }}");
                }
            }
            else
            {
                if (fromStateDef.OnExitIsAsync && !IsAsyncMachine)
                {
                    Sb.AppendLine($"try {{ {fromStateDef.OnExitMethod}(System.Threading.CancellationToken.None); }} catch {{ return false; }}");
                }
                else
                {
                    Sb.AppendLine($"try {{ {fromStateDef.OnExitMethod}(); }} catch {{ return false; }}");
                }
            }
            
            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
        }

        // State change
        if (!transition.IsInternal)
        {
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("RecordHistoryForCurrentPath();");
                WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
            }
            else
            {
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
        }

        // OnEntry with direct return on failure
        if (toHasEntry)
        {
            if (toStateDef!.OnEntryExpectsPayload && !string.IsNullOrEmpty(payloadVarName))
            {
                if (toStateDef.OnEntryIsAsync && !IsAsyncMachine)
                {
                    Sb.AppendLine($"try {{ {toStateDef.OnEntryMethod}({payloadVarName}, System.Threading.CancellationToken.None); }} catch {{ return false; }}");
                }
                else
                {
                    Sb.AppendLine($"try {{ {toStateDef.OnEntryMethod}({payloadVarName}); }} catch {{ return false; }}");
                }
            }
            else
            {
                if (toStateDef.OnEntryIsAsync && !IsAsyncMachine)
                {
                    Sb.AppendLine($"try {{ {toStateDef.OnEntryMethod}(System.Threading.CancellationToken.None); }} catch {{ return false; }}");
                }
                else
                {
                    Sb.AppendLine($"try {{ {toStateDef.OnEntryMethod}(); }} catch {{ return false; }}");
                }
            }
            
            WriteLogStatement("Debug",
                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
        }

        // Action with direct return on failure
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            if (transition.ActionExpectsPayload && !string.IsNullOrEmpty(payloadVarName))
            {
                if (transition.ActionIsAsync && !IsAsyncMachine)
                {
                    Sb.AppendLine($"try {{ {transition.ActionMethod}({payloadVarName}, System.Threading.CancellationToken.None); }} catch {{ return false; }}");
                }
                else
                {
                    Sb.AppendLine($"try {{ {transition.ActionMethod}({payloadVarName}); }} catch {{ return false; }}");
                }
            }
            else
            {
                if (transition.ActionIsAsync && !IsAsyncMachine)
                {
                    Sb.AppendLine($"try {{ {transition.ActionMethod}(System.Threading.CancellationToken.None); }} catch {{ return false; }}");
                }
                else
                {
                    Sb.AppendLine($"try {{ {transition.ActionMethod}(); }} catch {{ return false; }}");
                }
            }
            
            WriteLogStatement("Debug",
                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        if (!transition.IsInternal)
        {
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);
                Sb.AppendLine("return true;");
            } // This closes the using(Sb.Indent()) from payload block
            Sb.AppendLine("}");
            Sb.AppendLine("else");
            using (Sb.Block(""))
            {
                Sb.AppendLine("return false;");
            }
        }
        else
        {
            // For transitions without payload requirements, emit simple logic
            // State change
            if (!transition.IsInternal)
            {
                if (Model.HierarchyEnabled)
                {
                    Sb.AppendLine("RecordHistoryForCurrentPath();");
                    WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
                }
                else
                {
                    Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
                }
                
                WriteLogStatement("Information",
                    $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            }
            
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);
            Sb.AppendLine("return true;");
        }
    }

    protected override void WriteOnInitialEntryMethod(string stateTypeForUsage)
    {
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();
        if (statesWithParameterlessOnEntry.Any())
        {
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
                            foreach (var stateEntry in statesWithParameterlessOnEntry)
                            {
                                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                                using (Sb.Indent())
                                {
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
                        foreach (var stateEntry in statesWithParameterlessOnEntry)
                        {
                            Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                            using (Sb.Indent())
                            {
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
    }
    protected void WritePayloadMap(string triggerTypeForUsage)
    {
        using (Sb.Block($"private static readonly Dictionary<{triggerTypeForUsage}, Type> {PayloadMapField} = new()"))
        {
            foreach (var kvp in Model.TriggerPayloadTypes)
            {
                var triggerName = kvp.Key;
                var payloadTypeName = kvp.Value;
                var typeForTypeof = TypeHelper.FormatForTypeof(payloadTypeName);
                Sb.AppendLine($"{{ {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(triggerName)}, typeof({typeForTypeof}) }},");
            }
        }
        Sb.AppendLine(";");
        Sb.AppendLine();
    }

    protected virtual void WriteTryFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            // 1. Internal async method with logic
            WriteTryFireInternalAsync(stateTypeForUsage, triggerTypeForUsage);

            // 2. Public typed async overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    // Skip if it's 'object' to avoid duplicate
                    if (payloadType != "object")
                    {
                        WriteMethodAttribute();
                        using (Sb.Block($"public async ValueTask<bool> TryFireAsync({triggerTypeForUsage} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)"))
                        {
                            Sb.AppendLine("EnsureStarted();");
                            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                            Sb.AppendLine($"return await TryFireInternalAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                WriteMethodAttribute();
                using (Sb.Block($"public async ValueTask<bool> TryFireAsync<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                    Sb.AppendLine($"return await TryFireInternalAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                }
                Sb.AppendLine();
            }

            // 3. Sync methods that throw for async machines (required by interface)
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
                    {
                        WriteMethodAttribute();
                        using (Sb.Block($"public bool TryFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                        {
                            Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                WriteMethodAttribute();
                using (Sb.Block($"public bool TryFire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }

            // Override with object? - użyj override bo nadpisujemy metodę virtual z klasy bazowej
            WriteMethodAttribute();
            using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
            {
                Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
            }
            Sb.AppendLine();
            
            // Note: Removed the object? overload as it conflicts with base class method
        }
        else
        {
            // Sync-only implementation
            WriteTryFireInternal(stateTypeForUsage, triggerTypeForUsage);

            // Public typed overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
                    {
                        WriteMethodAttribute();
                        using (Sb.Block($"public bool TryFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                        {
                            Sb.AppendLine("EnsureStarted();");
                            Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                WriteMethodAttribute();
                using (Sb.Block($"public bool TryFire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
                }
                Sb.AppendLine();
            }

            // Override with object?
            WriteMethodAttribute();
            using (Sb.Block($"public override bool TryFire({triggerTypeForUsage} trigger, object? {PayloadVar} = null)"))
            {
                Sb.AppendLine("EnsureStarted();");
                Sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
            }
            Sb.AppendLine();
        }
    }

    private void WriteTryFireInternal(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool TryFireInternal({triggerTypeForUsage} trigger, object? {PayloadVar})"))
        {
            if (!Model.Transitions.Any())
            {
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            Sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
            Sb.AppendLine();

            // Payload validation for multi-payload variant
            if (IsMultiPayloadVariant())
            {
                Sb.AppendLine("// Payload-type validation for multi-payload variant");
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"({PayloadVar} == null || !expectedType.IsInstanceOfType({PayloadVar})))"))
                {
                    WriteLogStatement("Warning",
                        $"PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", {PayloadVar}?.GetType().Name ?? \"null\");");
                    Sb.AppendLine("return false; // wrong payload type");
                }
                Sb.AppendLine();
            }

            WriteTryFireStructure(
                stateTypeForUsage,
                triggerTypeForUsage,
                WriteTransitionLogicDirect);
        }
        Sb.AppendLine();
    }

    private void WriteTryFireInternalAsync(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"protected override async ValueTask<bool> TryFireInternalAsync({triggerTypeForUsage} trigger, object? {PayloadVar}, CancellationToken cancellationToken)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();
            
            if (!Model.Transitions.Any())
            {
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            Sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
            Sb.AppendLine();

            // Payload validation for multi-payload variant
            if (IsMultiPayloadVariant())
            {
                Sb.AppendLine("// Payload-type validation for multi-payload variant");
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"({PayloadVar} == null || !expectedType.IsInstanceOfType({PayloadVar})))"))
                {
                    WriteLogStatement("Warning",
                        $"PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", {PayloadVar}?.GetType().Name ?? \"null\");");
                    Sb.AppendLine("return false; // wrong payload type");
                }
                Sb.AppendLine();
            }

            WriteTryFireStructure(
                stateTypeForUsage,
                triggerTypeForUsage,
                WriteTransitionLogicDirect);
        }
        Sb.AppendLine();
    }

    protected override void WriteGuardCheck(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        GuardGenerationHelper.EmitGuardCheck(
            Sb,
            transition,
            GuardResultVar,
            PayloadVar,
            IsAsyncMachine,
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            handleResultAfterTry: true,
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: IsAsyncMachine
        );

        // Hook: After guard evaluated
        WriteAfterGuardEvaluatedHook(transition, GuardResultVar, stateTypeForUsage, triggerTypeForUsage);

        using (Sb.Block($"if (!{GuardResultVar})"))
        {
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");

            Sb.AppendLine($"{SuccessVar} = false;");
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
        }
    }

    // Odchudzone metody - cała logika jest teraz w helperze
    protected override void WriteActionCall(TransitionModel transition)
    {
        CallbackGenerationHelper.EmitActionCall(
            Sb,
            transition,
            PayloadVar,
            IsAsyncMachine,
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: IsAsyncMachine
        );
    }

    protected override void WriteOnEntryCall(StateModel state, string? expectedPayloadType)
    {
        CallbackGenerationHelper.EmitOnEntryCall(
            Sb,
            state,
            expectedPayloadType,
            Model.DefaultPayloadType,
            PayloadVar,
            IsAsyncMachine,
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            IsSinglePayloadVariant(),
            IsMultiPayloadVariant(),
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: IsAsyncMachine
        );
    }

    protected override void WriteOnExitCall(StateModel fromState, string? expectedPayloadType)
    {
        CallbackGenerationHelper.EmitOnExitCall(
            Sb,
            fromState,
            expectedPayloadType,
            Model.DefaultPayloadType,
            PayloadVar,
            IsAsyncMachine,
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            IsSinglePayloadVariant(),
            IsMultiPayloadVariant(),
            cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
            treatCancellationAsFailure: IsAsyncMachine
        );
    }

    protected void WriteFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            // Async Fire methods
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                using (Sb.Block($"public async Task FireAsync({triggerTypeForUsage} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                    using (Sb.Block($"if (!await TryFireAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()})"))
                    {
                        Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
                Sb.AppendLine();
            }
            else if (IsMultiPayloadVariant())
            {
                using (Sb.Block($"public async Task FireAsync<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                    using (Sb.Block($"if (!await TryFireAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()})"))
                    {
                        Sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
                Sb.AppendLine();
            }

            // Sync Fire methods that throw for async machines
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                using (Sb.Block($"public void Fire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
            else if (IsMultiPayloadVariant())
            {
                using (Sb.Block($"public void Fire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
            
            // Note: Removed the object? overloads as they conflict with base class methods
        }
        else
        {
            // Sync Fire methods
            if (IsSinglePayloadVariant())
            {
                var payloadType = GetTypeNameForUsage(GetSinglePayloadType()!);
                using (Sb.Block($"public void Fire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    using (Sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                    {
                        Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
                Sb.AppendLine();
            }
            else if (IsMultiPayloadVariant())
            {
                using (Sb.Block($"public void Fire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    using (Sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                    {
                        Sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
                Sb.AppendLine();
            }
        }
    }

    protected void WriteCanFireMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            // Async CanFire methods
            WriteAsyncCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
            WriteAsyncCanFireWithPayload(stateTypeForUsage, triggerTypeForUsage);

            // Additional async overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
                    {
                        Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired " +
                                        "with the given payload (runtime evaluation incl. guards)");
                        WriteMethodAttribute();
                        using (Sb.Block($"public async ValueTask<bool> CanFireAsync({triggerTypeForUsage} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)"))
                        {
                            Sb.AppendLine("EnsureStarted();");
                            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                            Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired " +
                                "with the given payload (runtime evaluation incl. guards)");
                WriteMethodAttribute();
                using (Sb.Block($"public async ValueTask<bool> CanFireAsync<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                    using (Sb.Block(
                        $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                        $"!expectedType.IsInstanceOfType({PayloadVar}))"))
                    {
                        Sb.AppendLine("return false;");
                    }
                    Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken){GetConfigureAwait()};");
                }
                Sb.AppendLine();
            }

            // Note: Removed the object? overload as it conflicts with base class method

            // Sync CanFire methods required by interface - NIE używaj override
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    WriteMethodAttribute();
                    using (Sb.Block($"public bool CanFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                    {
                        Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                    }
                    Sb.AppendLine();
                }
            }
            
            // Note: Removed the object? overload as it conflicts with base class method
        }
        else
        {
            // Sync CanFire methods
            base.WriteCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
            WriteCanFireWithPayload(stateTypeForUsage, triggerTypeForUsage);

            // Additional sync overloads
            if (IsSinglePayloadVariant())
            {
                var single = GetSinglePayloadType();
                if (single is not null)
                {
                    var payloadType = GetTypeNameForUsage(single);
                    if (payloadType != "object")
                    {
                        Sb.WriteSummary("Checks if the specified trigger can be fired " +
                                        "with the given payload (runtime evaluation incl. guards)");
                        WriteMethodAttribute();
                        using (Sb.Block($"public bool CanFire({triggerTypeForUsage} trigger, {payloadType} {PayloadVar})"))
                        {
                            Sb.AppendLine("EnsureStarted();");
                            Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
                        }
                        Sb.AppendLine();
                    }
                }
            }
            else if (IsMultiPayloadVariant())
            {
                Sb.WriteSummary("Checks if the specified trigger can be fired " +
                                "with the given payload (runtime evaluation incl. guards)");
                WriteMethodAttribute();
                using (Sb.Block($"public bool CanFire<TPayload>({triggerTypeForUsage} trigger, TPayload {PayloadVar})"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    using (Sb.Block(
                        $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                        $"!expectedType.IsInstanceOfType({PayloadVar}))"))
                    {
                        Sb.AppendLine("return false;");
                    }
                    Sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
                }
                Sb.AppendLine();
            }

            // Note: Removed the object? overload as it conflicts with base class method
        }
    }

    private void WriteAsyncCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        Sb.AppendLine("/// <returns>True if the trigger can be fired, false otherwise</returns>");

        using (Sb.Block($"protected override async ValueTask<bool> CanFireInternalAsync({triggerTypeForUsage} trigger, CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();
            
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
                                        // Użyj helpera dla wszystkich przypadków
                                        GuardGenerationHelper.EmitGuardCheck(
                                            Sb,
                                            transition,
                                            "guardResult",
                                            "null", // CanFire bez payloadu
                                            IsAsyncMachine, // dla async guards będzie true
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

    private void WriteAsyncCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private async ValueTask<bool> CanFireWithPayloadAsync({triggerTypeForUsage} trigger, object? {PayloadVar}, CancellationToken cancellationToken)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();
            
            // Payload validation for multi-payload variant
            if (IsMultiPayloadVariant())
            {
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"{PayloadVar} != null && !expectedType.IsInstanceOfType({PayloadVar}))"))
                {
                    Sb.AppendLine("return false;");
                }
            }

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
                                        WriteAsyncGuardCallForCanFire(transition, "guardResult", PayloadVar);
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

    private void WriteAsyncGuardCallForCanFire(TransitionModel transition, string resultVar, string payloadVar)
    {
        GuardGenerationHelper.EmitGuardCheck(
            Sb,
            transition,
            resultVar,
            payloadVar,
            isAsync: true, // Zawsze async w tej metodzie
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            handleResultAfterTry: true,
            cancellationTokenVar: "cancellationToken",
            treatCancellationAsFailure: true
        );
    }
    private void WriteCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private bool CanFireWithPayload({triggerTypeForUsage} trigger, object? {PayloadVar})"))
        {
            // Payload validation for multi-payload variant
            if (IsMultiPayloadVariant())
            {
                using (Sb.Block(
                    $"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                    $"{PayloadVar} != null && !expectedType.IsInstanceOfType({PayloadVar}))"))
                {
                    Sb.AppendLine("return false;");
                }
            }

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
                                                // ZMIANA: Użyj helpera zamiast WriteGuardCall
                                                GuardGenerationHelper.EmitGuardCheck(
                                                    Sb,
                                                    transition,
                                                    "guardResult",
                                                    PayloadVar,
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
                                            // ZMIANA: Użyj helpera zamiast WriteGuardCall
                                            GuardGenerationHelper.EmitGuardCheck(
                                                Sb,
                                                transition,
                                                "guardResult",
                                                PayloadVar,
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

    protected override void WriteGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            WriteAsyncGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
        }
        else
        {
            base.WriteGetPermittedTriggersMethod(stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteAsyncGetPermittedTriggersMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        Sb.AppendLine("/// <returns>List of triggers that can be fired in the current state</returns>");

        using (Sb.Block($"protected override async ValueTask<{ReadOnlyListType}<{triggerTypeForUsage}>> GetPermittedTriggersInternalAsync(CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();
            
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
                        var hasAsyncGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync);

                        if (!hasAsyncGuards && !stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod)))
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
                                    // Użyj helpera dla wszystkich przypadków
                                    GuardGenerationHelper.EmitGuardCheck(
                                        Sb,
                                        transition,
                                        "canFire",
                                        "null", // GetPermittedTriggers nie ma payloadu
                                        IsAsyncMachine, // dla async guards będzie true
                                        wrapInTryCatch: true,
                                        Model.ContinueOnCapturedContext,
                                        handleResultAfterTry: true,
                                        cancellationTokenVar: "cancellationToken",
                                        treatCancellationAsFailure: true
                                    );
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

    protected void WriteGetPermittedTriggersWithResolver(string stateTypeForUsage, string triggerTypeForUsage)
    {
        // Sync version
        if (!IsAsyncMachine)
        {
            Sb.WriteSummary("Gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            Sb.AppendLine("/// <param name=\"payloadResolver\">Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.</param>");
            using (Sb.Block($"public {ReadOnlyListType}<{triggerTypeForUsage}> GetPermittedTriggers(Func<{triggerTypeForUsage}, object?> payloadResolver)"))
            {
                Sb.AppendLine("EnsureStarted();");
                Sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
                Sb.AppendLine();

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
                                            if (transition.GuardExpectsPayload)
                                            {
                                                // Guard needs payload - use resolver
                                                Sb.AppendLine($"var payload_{transition.Trigger} = payloadResolver({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                                // ZMIANA: Użyj helpera z rzeczywistym payloadem
                                                GuardGenerationHelper.EmitGuardCheck(
                                                    Sb,
                                                    transition,
                                                    "canFire",
                                                    $"payload_{transition.Trigger}",
                                                    IsAsyncMachine,
                                                    wrapInTryCatch: true,
                                                    Model.ContinueOnCapturedContext,
                                                    handleResultAfterTry: true
                                                );
                                            }
                                            else
                                            {
                                                // Guard doesn't need payload
                                                // ZMIANA: Użyj helpera bez payloadu
                                                GuardGenerationHelper.EmitGuardCheck(
                                                    Sb,
                                                    transition,
                                                    "canFire",
                                                    "null",
                                                    IsAsyncMachine,
                                                    wrapInTryCatch: true,
                                                    Model.ContinueOnCapturedContext,
                                                    handleResultAfterTry: true
                                                );
                                            }

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
                                Sb.AppendLine($"var permitted = new List<{triggerTypeForUsage}>();");

                                foreach (var transition in stateGroup)
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        if (transition.GuardExpectsPayload)
                                        {
                                            // Guard needs payload - use resolver
                                            Sb.AppendLine($"var payload_{transition.Trigger} = payloadResolver({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                            // ZMIANA: Użyj helpera z rzeczywistym payloadem
                                            GuardGenerationHelper.EmitGuardCheck(
                                                Sb,
                                                transition,
                                                "canFire",
                                                $"payload_{transition.Trigger}",
                                                IsAsyncMachine,
                                                wrapInTryCatch: true,
                                                Model.ContinueOnCapturedContext,
                                                handleResultAfterTry: true
                                            );
                                        }
                                        else
                                        {
                                            // Guard doesn't need payload
                                            // ZMIANA: Użyj helpera bez payloadu
                                            GuardGenerationHelper.EmitGuardCheck(
                                                Sb,
                                                transition,
                                                "canFire",
                                                "null",
                                                IsAsyncMachine,
                                                wrapInTryCatch: true,
                                                Model.ContinueOnCapturedContext,
                                                handleResultAfterTry: true
                                            );
                                        }

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
        }
        else
        {
            // Async version
            WriteAsyncGetPermittedTriggersWithResolver(stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteAsyncGetPermittedTriggersWithResolver(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"payloadResolver\">Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.</param>");
        Sb.AppendLine("/// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        using (Sb.Block($"public async ValueTask<{ReadOnlyListType}<{triggerTypeForUsage}>> GetPermittedTriggersAsync(Func<{triggerTypeForUsage}, object?> payloadResolver, CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("EnsureStarted();");
            Sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();

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
                        Sb.AppendLine($"var permitted = new List<{triggerTypeForUsage}>();");

                        foreach (var transition in stateGroup)
                        {
                            if (!string.IsNullOrEmpty(transition.GuardMethod))
                            {
                                if (transition.GuardExpectsPayload)
                                {
                                    // Guard needs payload - use resolver
                                    Sb.AppendLine($"var payload_{transition.Trigger} = payloadResolver({triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                    WriteAsyncGuardCallForGetPermitted(transition, "canFire", $"payload_{transition.Trigger}");
                                }
                                else
                                {
                                    // Guard doesn't need payload
                                    WriteAsyncGuardCallForGetPermitted(transition, "canFire", "null");
                                }

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

    private void WriteAsyncGuardCallForGetPermitted(TransitionModel transition, string resultVar, string payloadVar)
    {
        // Cała logika jest teraz w helperze
        GuardGenerationHelper.EmitGuardCheck(
            Sb,
            transition,
            resultVar,
            payloadVar,
            isAsync: true, // To jest async metoda
            wrapInTryCatch: true,
            Model.ContinueOnCapturedContext,
            handleResultAfterTry: true,
            cancellationTokenVar: "cancellationToken",
            treatCancellationAsFailure: true
        );
    }

    protected void WriteStructuralApiMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (!Model.EmitStructuralHelpers)
            return;
        // GetExpectedPayloadType
        using (Sb.Block($"public Type? GetExpectedPayloadType({triggerTypeForUsage} trigger)"))
        {
            if (IsSinglePayloadVariant())
            {
                Sb.AppendLine($"return typeof({GetTypeNameForUsage(GetSinglePayloadType()!)});");
            }
            else if (IsMultiPayloadVariant())
            {
                Sb.AppendLine($"{PayloadMapField}.TryGetValue(trigger, out var type);");
                Sb.AppendLine("return type;");
            }
            else
            {
                Sb.AppendLine("return null;");
            }
        }
        Sb.AppendLine();

        // GetPayloadVariant
        using (Sb.Block("public PayloadVariant GetPayloadVariant()"))
        {
            if (IsSinglePayloadVariant())
            {
                Sb.AppendLine("return PayloadVariant.SinglePayload;");
            }
            else if (IsMultiPayloadVariant())
            {
                Sb.AppendLine("return PayloadVariant.MultiPayload;");
            }
            else
            {
                Sb.AppendLine("return PayloadVariant.NoPayload;");
            }
        }
        Sb.AppendLine();
    }
}
