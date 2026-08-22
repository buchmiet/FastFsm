using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Helpers;
using Generator.Model;
using static Generator.Strings;

namespace Generator.SourceGenerators;

/// <summary>
/// Unified state machine generator that handles all variants through feature flags
/// instead of inheritance hierarchy.
/// Phase 2: Implementing Core/Basic logic directly
/// </summary>
internal class UnifiedStateMachineGenerator(StateMachineModel model) : StateMachineCodeGenerator(model)
{
    // Feature detection flags
    private bool HasPayload => Model.GenerationConfig.HasPayload;
    private bool HasExtensions => Model.GenerationConfig.HasExtensions;
    private bool ExtensionsOn => HasExtensions;
    private bool HasOnEntryExit => Model.GenerationConfig.HasOnEntryExit;
    private bool IsHierarchical => Model.HierarchyEnabled;
    private bool HasMultiPayload => Model.TriggerPayloadTypes?.Any() == true;


    private bool _smCtxCreated;
    private int _attemptResultIndex;

    // Extensions feature writer (used when HasExtensions)
    private readonly ExtensionsFeatureWriter _ext = new();


    public override string Generate()
    {
        WriteHeader();
        WriteNamespaceAndClass();
        return Sb.ToString();
    }

    protected override void WriteNamespaceAndClass()
    {
        var stateTypeForUsage = GetTypeNameForUsage(Model.StateType);
        var triggerTypeForUsage = GetTypeNameForUsage(Model.TriggerType);
        var userNamespace = Model.Namespace;
        var className = Model.ClassName;

        if (!string.IsNullOrEmpty(userNamespace))
        {
            using (Sb.Block($"namespace {userNamespace}"))
            {
                WriteContainingTypesAndClass(className, stateTypeForUsage, triggerTypeForUsage);
            }
        }
        else
        {
            WriteContainingTypesAndClass(className, stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteContainingTypesAndClass(string className, string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (Model.ContainerClasses.Count == 0)
        {
            WriteInner();
            return;
        }

        WriteNested(0);
        return;

        void WriteNested(int idx)
        {
            if (idx >= Model.ContainerClasses.Count)
            {
                WriteInner();
                return;
            }
            var container = Model.ContainerClasses[idx];
            using (Sb.Block($"public partial class {container}"))
            {
                WriteNested(idx + 1);
            }
        }

        void WriteInner()
        {
            // Write interface and the main class
            WriteInterface(className, stateTypeForUsage, triggerTypeForUsage);
            WriteClass(className, stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteInterface(string className, string stateType, string triggerType)
    {
        var baseInterface = GetInterfaceName(stateType, triggerType);
        if (ExtensionsOn)
        {
            var extInterface = IsAsyncMachine
                ? $"IExtensibleStateMachineAsync<{stateType}, {triggerType}>"
                : $"IExtensibleStateMachineSync<{stateType}, {triggerType}>";
            Sb.AppendLine($"public interface I{className} : {extInterface} {{ }}");
        }
        else
        {
            Sb.AppendLine($"public interface I{className} : {baseInterface} {{ }}");
        }
        Sb.AppendLine();
    }

    private void WriteClass(string className, string stateType, string triggerType)
    {
        var baseClass = GetBaseClassName(stateType, triggerType);
        using (Sb.Block($"public partial class {className} : {baseClass}, I{className}"))
        {
            Sb.AppendLine("#pragma warning disable CS8618 // User partial may declare properties initialized at runtime");
            // Write class content
            WriteFields(className);
            // Emit optional action-exception hook (no-op if not implemented by user)
            WriteActionExceptionHook();
            WriteConstructor(stateType, className);
            WriteStartMethods();
            WriteInitialEntryMethods(stateType);
            WriteTryFireMethods(stateType, triggerType);
            WriteFireMethods(stateType, triggerType);
            WriteCanFireMethods(stateType, triggerType);
            WriteGetPermittedTriggersMethods(stateType, triggerType);
            if (ExtensionsOn)
            {
                _ext.WriteManagementMethods(Sb, stateType, triggerType);
            }
            WriteStructuralApiMethods(stateType, triggerType);
            WriteHierarchyMethods(stateType, triggerType);

            // Emit per-transition guard helpers for sync machines or HSM (which needs them for TryFireInternal)
            if (!IsAsyncMachine || IsHierarchical)
            {
                WriteGuardHelperMethods(stateType, triggerType);
            }
            Sb.AppendLine("#pragma warning restore CS8618");
        }
    }

    // Emits optional partial hook for action exception reporting
    private void WriteActionExceptionHook()
    {
        Sb.AppendLine(AggressiveInliningString);
        Sb.AppendLine("partial void OnActionException(string context, System.Exception ex);");
        Sb.AppendLine();
    }

    // Generates private helper methods EvaluateGuard__<FROM>__<TRIGGER>(object? payload)
    // and Guard__<FROM>__<TRIGGER>(object? payload) for sync machines and HSM (both sync and async).
    private void WriteGuardHelperMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        var transitionsWithGuards = Model.Transitions.Where(t => !string.IsNullOrEmpty(t.GuardMethod)).ToList();
        if (transitionsWithGuards.Count == 0) return;

        Sb.AppendLine();
        Sb.AppendLine("// Guard evaluation helpers");
        foreach (var tr in transitionsWithGuards)
        {
            var from = TypeHelper.EscapeIdentifier(tr.FromState);
            var trig = TypeHelper.EscapeIdentifier(tr.Trigger);
            var guardWrapper = $"Guard__{from}__{trig}";
            var evalName = $"EvaluateGuard__{from}__{trig}";

            // Core guard invocation without try/catch
            Sb.AppendLine(AggressiveInliningString);
            using (Sb.Block($"private bool {guardWrapper}(object? payload)"))
            {
                // No guard method? Always true (shouldn't happen for this emission path)
                if (string.IsNullOrEmpty(tr.GuardMethod))
                {
                    Sb.AppendLine("return true;");
                }
                else
                {
                    // Use GuardGenerationHelper to emit the call w/o try/catch
                    GuardGenerationHelper.EmitGuardCheck(
                        Sb,
                        tr,
                        resultVar: "__guard",
                        payloadVar: "payload",
                        isAsync: false,
                        wrapInTryCatch: false,
                        continueOnCapturedContext: false,
                        handleResultAfterTry: true,
                        cancellationTokenVar: null,
                        treatCancellationAsFailure: false);
                    Sb.AppendLine("return __guard;");
                }
            }

            // Safe wrapper that handles exceptions if FASTFSM_SAFE_GUARDS is enabled
            Sb.AppendLine(AggressiveInliningString);
            using (Sb.Block($"private bool {evalName}(object? payload)"))
            {
                using (Sb.IfDirective("FASTFSM_SAFE_GUARDS"))
                {
                    using (Sb.Block("try"))
                    {
                        Sb.AppendLine($"return {guardWrapper}(payload);");
                    }
                    Sb.AppendLine("catch (System.OperationCanceledException) { return false; }");
                    Sb.AppendLine("catch (System.Exception) { return false; }");
                    Sb.ElseDirective();
                    Sb.AppendLine($"return {guardWrapper}(payload);");
                }
            }

            Sb.AppendLine();
        }
    }

    private void WriteFields(string className)
    {
        // Instance ID for async machines (or for logging in sync machines)
        if (IsAsyncMachine || ShouldGenerateLogging)
        {
            Sb.AppendLine("private readonly string _instanceId = Guid.NewGuid().ToString();");
            Sb.AppendLine();
        }

        if (ExtensionsOn)
        {
            Sb.AppendLine("private readonly Guid _fsmInstanceId = Guid.NewGuid();");
            Sb.AppendLine("private long _attemptCounter;");
            Sb.AppendLine("public Guid InstanceId => _fsmInstanceId;");
            Sb.AppendLine();
        }

        // Logger field (but not the _instanceId since we handle it above)
        WriteLoggerField(className);

        // Extensions fields
        if (ExtensionsOn)
        {
            _ext.WriteFields(Sb, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));
        }

        // Multi-payload: emit trigger→payload type map for validation
        if (HasPayload && HasMultiPayload)
        {
            WritePayloadMap(GetTypeNameForUsage(Model.TriggerType));
        }

        // State and trigger name arrays for zero-allocation logging
        if (ShouldGenerateLogging)
        {
            WriteStateAndTriggerNameArrays(GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));
        }

        // HSM arrays
        if (IsHierarchical)
        {
            GenerateActionIdEnum(); // Generate ActionId enum for zero-allocation dispatch
            if (HasAsyncActions())
                GenerateAsyncActionIdEnum(); // Generate AsyncActionId enum for async actions
            WriteHierarchyArrays(GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));
            WriteHierarchyRuntimeFieldsAndHelpers(GetTypeNameForUsage(Model.StateType));
        }

        // Emit static permitted-trigger arrays for flat FSM (zero-alloc GetPermittedTriggersInternal)
        if (!IsHierarchical)
        {
            WritePermittedTriggerArrays(GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));
        }
    }

    // Generates static readonly arrays mapping guard masks to permitted trigger arrays per state (flat FSM only)
    private void WritePermittedTriggerArrays(string stateTypeForUsage, string triggerTypeForUsage)
    {
        var transitionsByFromState = Model.Transitions
            .GroupBy(t => t.FromState)
            .OrderBy(g => g.Key);

        foreach (var stateGroup in transitionsByFromState)
        {
            var stateNameRaw = stateGroup.Key;
            var stateFieldSuffix = MakeSafeMemberSuffix(stateNameRaw);
            TypeHelper.EscapeIdentifier(stateNameRaw);
            var unguarded = stateGroup.Where(t => string.IsNullOrEmpty(t.GuardMethod))
                                      .Select(t => $"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(t.Trigger)}")
                                      .Distinct()
                                      .ToList();
            var guarded = stateGroup.Where(t => !string.IsNullOrEmpty(t.GuardMethod)).ToList();

            int m = guarded.Count;
            int tableSize = Math.Max(1, 1 << m);

            if (m == 0)
            {
                // Single static array for states without guards
                Sb.AppendLine(unguarded.Count == 0
                    ? $"private static readonly {triggerTypeForUsage}[] s_perm__{stateFieldSuffix} = System.Array.Empty<{triggerTypeForUsage}>();"
                    : $"private static readonly {triggerTypeForUsage}[] s_perm__{stateFieldSuffix} = new {triggerTypeForUsage}[] {{ {string.Join(", ", unguarded)} }};");
            }
            else
            {
                // Build the jagged array initializer inline
                var rows = new List<string>();
                for (int mask = 0; mask < tableSize; mask++)
                {
                    var entries = new List<string>();
                    entries.AddRange(unguarded);
                    for (int i = 0; i < m; i++)
                    {
                        if (((mask >> i) & 1) == 0) continue;
                        var tr = guarded[i];
                        entries.Add($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(tr.Trigger)}");
                    }
                    entries = entries.Distinct().ToList();
                    rows.Add(entries.Count == 0
                        ? $"System.Array.Empty<{triggerTypeForUsage}>()"
                        : $"new {triggerTypeForUsage}[] {{ {string.Join(", ", entries)} }}");
                }
                Sb.AppendLine($"private static readonly {triggerTypeForUsage}[][] s_perm__{stateFieldSuffix} = new {triggerTypeForUsage}[][] {{ {string.Join(", ", rows)} }};");
            }

            Sb.AppendLine();
        }
    }

    private void WriteConstructor(string stateTypeForUsage, string className)
    {
        var extras = new List<string>();
        if (ExtensionsOn)
        {
            extras.Add($"IEnumerable<IStateMachineExtension<{GetTypeNameForUsage(Model.StateType)}, {GetTypeNameForUsage(Model.TriggerType)}>>? extensions = null");
        }
        var loggerParam = GetLoggerConstructorParameter(className);
        if (!string.IsNullOrWhiteSpace(loggerParam)) extras.Add(loggerParam);
        var paramList = BuildConstructorParameters(stateTypeForUsage, extras.ToArray());

        var baseCall = IsAsyncMachine
            ? $"base(initialState, continueOnCapturedContext: {Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()})"
            : "base(initialState)";

        using (Sb.Block($"public {className}({string.Join(", ", paramList)}) : {baseCall}"))
        {
            if (Model.HierarchyEnabled)
            {
                // History tracking array is now initialized in the base class Start() method
            }
            WriteLoggerAssignment();
            if (ExtensionsOn)
            {
                _ext.WriteConstructorBody(
                    Sb,
                    ShouldGenerateLogging,
                    GetTypeNameForUsage(Model.StateType),
                    GetTypeNameForUsage(Model.TriggerType));
            }
        }
        Sb.AppendLine();
    }

    private void WriteStartMethods()
    {
        // Generate Start/StartAsync if:
        // 1. It's hierarchical (needs DescendToInitialIfComposite)
        // 2. It has logging (to add MachineStarted log)
        // Note: We avoid duplicate StartAsync for async machines that have OnEntryExit
        //       by checking if OnInitialEntry is being overridden separately
        if (IsHierarchical || ShouldGenerateLogging || ExtensionsOn)
        {
            WriteStartMethod();
        }
    }

    private void WriteStartMethod()
    {
        if (IsAsyncMachine)
        {
            WriteStartAsyncMethod();
        }
        else
        {
            WriteStartSyncMethod();
        }
    }

    private void WriteStartSyncMethod()
    {
        using (Sb.Block("public override void Start()"))
        {
            Sb.AppendLine("if (IsStarted) return;");
            Sb.AppendLine();

            if (IsHierarchical)
            {
                Sb.AppendLine("// For HSM: resolve composite initial state to leaf before calling OnInitialEntry");
                Sb.AppendLine("DescendToInitialIfComposite();");
                Sb.AppendLine();
            }

            Sb.AppendLine("base.Start();");

            if (ExtensionsOn)
            {
                Sb.AppendLine();
                Sb.AppendLine("var extensionSet = System.Threading.Volatile.Read(ref _extensionSet);");
                Sb.AppendLine($"_extensionRunner.RunMachineStarted(extensionSet, _fsmInstanceId, {CurrentStateField});");
            }

            // Log machine started
            if (ShouldGenerateLogging)
            {
                Sb.AppendLine();
                var stateAccessor = Model.HierarchyEnabled ? $"NameOf({CurrentStateField})" : $"{CurrentStateField}.ToString()";
                Sb.AppendLine($"{Model.ClassName}Log.MachineStarted(_logger, _instanceId, {stateAccessor});");
            }
        }
        Sb.AppendLine();
    }

    private void WriteStartAsyncMethod()
    {
        using (Sb.Block("public override async ValueTask StartAsync(CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("if (IsStarted) return;");
            Sb.AppendLine();

            if (IsHierarchical)
            {
                Sb.AppendLine("// For HSM: resolve composite initial state to leaf before calling OnInitialEntry");
                Sb.AppendLine("DescendToInitialIfComposite();");
                Sb.AppendLine();
            }

            Sb.AppendLine("await base.StartAsync(cancellationToken).ConfigureAwait(false);");

            if (ExtensionsOn)
            {
                Sb.AppendLine();
                Sb.AppendLine("var extensionSet = System.Threading.Volatile.Read(ref _extensionSet);");
                Sb.AppendLine($"_extensionRunner.RunMachineStarted(extensionSet, _fsmInstanceId, {CurrentStateField});");
            }

            // Log machine started
            if (ShouldGenerateLogging)
            {
                Sb.AppendLine();
                var stateAccessor = Model.HierarchyEnabled ? $"NameOf({CurrentStateField})" : $"{CurrentStateField}.ToString()";
                Sb.AppendLine($"{Model.ClassName}Log.MachineStarted(_logger, _instanceId, {stateAccessor});");
            }
        }
        Sb.AppendLine();
    }

    private void WriteInitialEntryMethods(string stateType)
    {
        if (!ShouldGenerateInitialOnEntry()) return;
        if (IsAsyncMachine)
        {
            WriteOnInitialEntryAsyncMethod(stateType);
        }
        else
        {
            WriteOnInitialEntryMethod(stateType);
        }
    }

    private void WriteOnInitialEntryAsyncMethod(string stateTypeForUsage)
    {
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();

        if (statesWithParameterlessOnEntry.Count == 0)
        {
            return;
        }

        using (Sb.Block("protected override async ValueTask OnInitialEntryAsync(System.Threading.CancellationToken cancellationToken = default)"))
        {
            if (Model.HierarchyEnabled)
            {
                // Build path from root to leaf using ArrayPool (no Span across await)
                Sb.AppendLine("// Count depth for pooled buffer");
                Sb.AppendLine($"int leafIdx = (int){CurrentStateField};");
                Sb.AppendLine("int depth = 0;");
                Sb.AppendLine("for (int i = leafIdx; i >= 0; i = g_parent[i]) depth++;");
                Sb.AppendLine();
                Sb.AppendLine("// Get path from root to leaf using runtime helper with ArrayPool");
                Sb.AppendLine($"var pool = System.Buffers.ArrayPool<{stateTypeForUsage}>.Shared;");
                Sb.AppendLine($"{stateTypeForUsage}[] rented = pool.Rent(depth);");
                using (Sb.Block("try"))
                {
                    Sb.AppendLine("var span = rented.AsSpan(0, depth);");
                    Sb.AppendLine("int written = GetActivePath(span);");
                    Sb.AppendLine();
                    Sb.AppendLine("// Execute OnEntry from root to leaf");
                    using (Sb.Block("for (int i = 0; i < written; i++)"))
                    using (Sb.Switch("rented[i]"))
                    {
                        WriteCases();
                    }
                }
                using (Sb.Block("finally"))
                {
                    Sb.AppendLine("pool.Return(rented, clearArray: false);");
                }
            }
            else
            {
                using (Sb.Switch(CurrentStateField))
                {
                    WriteCases();
                }
            }

            Sb.AppendLine();
        }

        return;

        void WriteCases()
        {
            foreach (var stateEntry in statesWithParameterlessOnEntry)
            {
                using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}"))
                {
#if FASTFSM_SAFE_ACTIONS
                    try
                    {
#endif
                    CallbackGenerationHelper.EmitOnEntryCall(
                        Sb,
                        stateEntry,
                        expectedPayloadType: null,
                        defaultPayloadType: null,
                        payloadVar: "null",
                        isCallerAsync: true,
                        wrapInTryCatch: false,
                        continueOnCapturedContext: Model.ContinueOnCapturedContext,
                        isSinglePayload: false,
                        isMultiPayload: false,
                        cancellationTokenVar: "cancellationToken",
                        treatCancellationAsFailure: false);
#if FASTFSM_SAFE_ACTIONS
                    }
                    catch (System.OperationCanceledException) { }
                    catch (System.Exception) { }
#endif
                    WriteLogStatement(GeneratedLogLevel.Debug,
                        $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
                    Sb.AppendLine("break;");
                }
            }

            Sb.DefaultBreak();
        }
    }

    private void WriteOnInitialEntryMethod(string stateTypeForUsage)
    {
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();
        if (statesWithParameterlessOnEntry.Count == 0)
        {
            return;
        }

        using (Sb.Block("protected override void OnInitialEntry()"))
        {
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("// Count depth for stackalloc");
                Sb.AppendLine($"int leafIdx = (int){CurrentStateField};");
                Sb.AppendLine("int depth = 0;");
                Sb.AppendLine("for (int i = leafIdx; i >= 0; i = g_parent[i]) depth++;");
                Sb.AppendLine();
                Sb.AppendLine("// Get path from root to leaf using runtime helper");
                Sb.AppendLine($"Span<{stateTypeForUsage}> path = depth <= 128 ? stackalloc {stateTypeForUsage}[depth] : new {stateTypeForUsage}[depth];");
                Sb.AppendLine("int written = GetActivePath(path);");
                Sb.AppendLine();
                Sb.AppendLine("// Execute OnEntry from root to leaf");
                using (Sb.Block("for (int i = 0; i < written; i++)"))
                using (Sb.Switch("path[i]"))
                {
                    WriteCases();
                }
            }
            else
            {
                using (Sb.Switch(CurrentStateField))
                {
                    WriteCases();
                }
            }
        }

        Sb.AppendLine();

        void WriteCases()
        {
            foreach (var stateEntry in statesWithParameterlessOnEntry)
            {
                using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}", braces: false))
                {
                    using (Sb.IfDirective("FASTFSM_SAFE_ACTIONS"))
                    {
                        using (Sb.Block("try"))
                        {
                            Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                            WriteLogStatement(GeneratedLogLevel.Debug,
                                $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
                        }
                        using (Sb.Block("catch (System.OperationCanceledException oce)"))
                        {
                            Sb.AppendLine($"OnActionException(\"OnInitialEntry:{stateEntry.OnEntryMethod}\", oce);");
                            Sb.AppendLine("return;");
                        }
                        using (Sb.Block("catch (System.Exception ex)"))
                        {
                            Sb.AppendLine($"OnActionException(\"OnInitialEntry:{stateEntry.OnEntryMethod}\", ex);");
                            Sb.AppendLine("return;");
                        }
                        Sb.ElseDirective();
                        Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                        WriteLogStatement(GeneratedLogLevel.Debug,
                            $"OnEntryExecuted(_logger, _instanceId, \"{stateEntry.OnEntryMethod}\", \"{stateEntry.Name}\");");
                    }
                    Sb.AppendLine("break;");
                }
            }

            Sb.DefaultBreak();
        }
    }

    private void WriteTryFireMethods(string stateType, string triggerType)
    {
        WriteTryFireMethod(stateType, triggerType);

        // Add typed public TryFire wrappers for payload variants
        if (HasPayload)
        {
            if (IsAsyncMachine)
            {
                // Async typed overloads
                if (!HasMultiPayload)
                {
                    var payloadType = GetTypeNameForUsage(Model.DefaultPayloadType!);
                    // Skip if it's 'object' to avoid duplicate
                    if (payloadType == "object") return;
                    WriteMethodAttribute();
                    using (Sb.Block($"public async ValueTask<bool> TryFireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)"))
                    {
                        Sb.AppendLine("EnsureStarted();");
                        Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                        Sb.AppendLine($"return await TryFireInternalAsync(trigger, payload, cancellationToken){GetConfigureAwait()};");
                    }
                    Sb.AppendLine();
                }
                else
                {
                    WriteMethodAttribute();
                    using (Sb.Block($"public async ValueTask<bool> TryFireAsync<TPayload>({triggerType} trigger, TPayload payload, CancellationToken cancellationToken = default)"))
                    {
                        Sb.AppendLine("EnsureStarted();");
                        Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                        Sb.AppendLine($"return await TryFireInternalAsync(trigger, payload, cancellationToken){GetConfigureAwait()};");
                    }
                    Sb.AppendLine();
                }
            }
            else
            {
                // Sync typed overloads
                if (!HasMultiPayload)
                {
                    var payloadType = GetTypeNameForUsage(Model.DefaultPayloadType!);
                    // Skip if it's 'object' to avoid duplicate
                    if (payloadType == "object") return;
                    WriteMethodAttribute();
                    using (Sb.Block($"public bool TryFire({triggerType} trigger, {payloadType} payload)"))
                    {
                        Sb.AppendLine("EnsureStarted();");
                        Sb.AppendLine("return TryFireInternal(trigger, payload);");
                    }
                    Sb.AppendLine();
                }
                else
                {
                    WriteMethodAttribute();
                    using (Sb.Block($"public bool TryFire<TPayload>({triggerType} trigger, TPayload payload)"))
                    {
                        Sb.AppendLine("EnsureStarted();");
                        Sb.AppendLine("return TryFireInternal(trigger, payload);");
                    }
                    Sb.AppendLine();
                }
            }
        }
    }

    private void WriteFireMethods(string stateType, string triggerType)
    {
        if (!HasPayload) return; // Only generate Fire methods for payload variants

        if (IsAsyncMachine)
        {
            // Async Fire methods
            if (!HasMultiPayload)
            {
                var payloadType = GetTypeNameForUsage(Model.DefaultPayloadType!);
                WriteMethodAttribute();
                using (Sb.Block(
                           $"public async Task FireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                    using (Sb.Block($"if (!await TryFireAsync(trigger, payload, cancellationToken){GetConfigureAwait()})"))
                    {
                        Sb.AppendLine(
                            $"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                    Sb.AppendLine();
                }
            }
            else
            {
                WriteMethodAttribute();
                using (Sb.Block($"public async Task FireAsync<TPayload>({triggerType} trigger, TPayload payload, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                    using (Sb.Block($"if (!await TryFireAsync(trigger, payload, cancellationToken){GetConfigureAwait()})"))
                    {
                        Sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
                Sb.AppendLine();
            }
            // Sync Fire methods that throw for async machines
            if (!HasMultiPayload)
            {
                var payloadType = GetTypeNameForUsage(Model.DefaultPayloadType!);
                WriteMethodAttribute();
                using (Sb.Block($"public void Fire({triggerType} trigger, {payloadType} payload)"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
            else
            {
                WriteMethodAttribute();
                using (Sb.Block($"public void Fire<TPayload>({triggerType} trigger, TPayload payload)"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
        }
        else
        {
            // Sync Fire methods
            if (!HasMultiPayload)
            {
                var payloadType = GetTypeNameForUsage(Model.DefaultPayloadType!);
                WriteMethodAttribute();
                using (Sb.Block($"public void Fire({triggerType} trigger, {payloadType} payload)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    using (Sb.Block("if (!TryFire(trigger, payload))"))
                    {
                        Sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
                Sb.AppendLine();
            }
            else
            {
                WriteMethodAttribute();
                using (Sb.Block($"public void Fire<TPayload>({triggerType} trigger, TPayload payload)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    using (Sb.Block("if (!TryFire(trigger, payload))"))
                    {
                        Sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
                Sb.AppendLine();
            }
        }
    }

    private void WriteTryFireMethod(string stateType, string triggerType)
    {
        if (IsAsyncMachine)
        {
            WriteTryFireMethodAsync(stateType, triggerType);
        }
        else
        {
            WriteTryFireMethodSync(stateType, triggerType);
        }
    }

    private void WriteAttemptStart(string stateType, string triggerType)
    {
        if (!ExtensionsOn) return;

        Sb.AppendLine("var extensionSet = System.Threading.Volatile.Read(ref _extensionSet);");
        Sb.AppendLine($"var attempt = default(TransitionAttemptContext<{stateType}, {triggerType}>);");
        using (Sb.Block("if ((extensionSet.Hooks & (ExtensionHooks.Transitions | ExtensionHooks.Guards | ExtensionHooks.States | ExtensionHooks.Callbacks)) != 0)"))
        {
            Sb.AppendLine($"attempt = new TransitionAttemptContext<{stateType}, {triggerType}>(");
            using (Sb.Indent())
            {
                Sb.AppendLine("_fsmInstanceId,");
                Sb.AppendLine("System.Threading.Interlocked.Increment(ref _attemptCounter),");
                Sb.AppendLine($"{CurrentStateField},");
                Sb.AppendLine("trigger,");
                Sb.AppendLine("payload,");
                Sb.AppendLine("System.Diagnostics.Stopwatch.GetTimestamp());");
            }
            Sb.AppendLine("_extensionRunner.RunAttemptStarting(extensionSet, in attempt);");
        }
        Sb.AppendLine();
    }

    private void WritePrepareMatchedTransition(TransitionModel transition, string stateType)
    {
        if (!ExtensionsOn) return;

        var declaredTarget = transition.IsInternal
            ? $"({stateType}?)null"
            : $"{stateType}.{TypeHelper.EscapeIdentifier(transition.ToState)}";
        var kind = transition.IsInternal ? "TransitionKind.Internal" : "TransitionKind.External";
        using (Sb.Block("if ((extensionSet.Hooks & (ExtensionHooks.Transitions | ExtensionHooks.Guards)) != 0)"))
        {
            Sb.AppendLine($"matchedTransition = new TransitionInfo<{stateType}>(");
            using (Sb.Indent())
            {
                Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(transition.FromState)},");
                Sb.AppendLine($"{declaredTarget},");
                Sb.AppendLine($"{kind});");
            }
            using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.Transitions) != 0)"))
            {
                Sb.AppendLine("_extensionRunner.RunTransitionMatched(extensionSet, in attempt, in matchedTransition);");
            }
        }
    }

    private void WriteAttemptCompleted(
        string stateType,
        string outcome,
        string resolvedTarget,
        string matchedTransition,
        string stage = "(global::FastFsm.Exceptions.TransitionStage?)null",
        string exception = "null")
    {
        if (!ExtensionsOn) return;

        var resultVariable = $"attemptResult{_attemptResultIndex++}";
        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.Transitions) != 0)"))
        {
            Sb.AppendLine($"var {resultVariable} = new TransitionResult<{stateType}>(");
            using (Sb.Indent())
            {
                Sb.AppendLine($"TransitionOutcome.{outcome},");
                Sb.AppendLine($"{CurrentStateField},");
                Sb.AppendLine($"{resolvedTarget},");
                Sb.AppendLine($"{matchedTransition},");
                Sb.AppendLine($"{stage},");
                Sb.AppendLine($"{exception});");
            }
            Sb.AppendLine($"_extensionRunner.RunAttemptCompleted(extensionSet, in attempt, in {resultVariable});");
        }
    }

    private void WriteTryFireMethodAsync(string stateType, string triggerType)
    {
        WriteMethodAttribute();
        using (Sb.Block($"protected override async ValueTask<bool> TryFireInternalAsync({triggerType} trigger, object? payload, CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();
            ResetEndTryFireLabel();

            WriteAttemptStart(stateType, triggerType);

            if (!Model.Transitions.Any())
            {
                WriteAttemptCompleted(stateType, "UnhandledTrigger", $"({stateType}?)null", $"(TransitionInfo<{stateType}>?)null");
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            // For multi-payload: validate payload type upfront (no runtime branching later)
            if (HasPayload && HasMultiPayload)
            {
                Sb.AppendLine("// Payload-type validation for multi-payload variant");
                using (Sb.Block($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && (payload == null || !expectedType.IsInstanceOfType(payload)))"))
                {
                    WriteLogStatement(GeneratedLogLevel.Warning, "PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", payload?.GetType().Name ?? \"null\");");
                    WriteAttemptCompleted(stateType, "InvalidPayload", $"({stateType}?)null", $"(TransitionInfo<{stateType}>?)null");
                    Sb.AppendLine("return false; // wrong payload type");
                }
                Sb.AppendLine();
            }

            if (IsHierarchical && !ExtensionsOn)
            {
                // Hierarchical winner-selection returns directly; no success variable or trailing return.
                if (HasPayload)
                {
                    WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
                    {
                        _smCtxCreated = false;
                        WriteTransitionLogicPayloadAsync(transition, stateType, triggerType);
                    });
                }
                else
                {
                    WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
                    {
                        _smCtxCreated = false;
                        WriteTransitionLogic(transition, stateType, triggerType);
                    });
                }
            }
            else
            {
                Sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
                Sb.AppendLine($"bool {SuccessVar} = false;");
                Sb.AppendLine();

                // Use payload-aware or core transition logic based on feature flags
                if (HasPayload)
                {
                    WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
                    {
                        _smCtxCreated = false;
                        WriteTransitionLogicPayloadAsync(transition, stateType, triggerType);
                    });
                }
                else
                {
                    WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
                    {
                        _smCtxCreated = false;
                        WriteTransitionLogic(transition, stateType, triggerType);
                    });
                }

                EmitEndTryFireLabelIfNeeded();
                Sb.AppendLine();

                // Log failure if needed
                if (ShouldGenerateLogging)
                {
                    using (Sb.Block($"if (!{SuccessVar})"))
                    {
                        WriteLogStatement(GeneratedLogLevel.Warning, $"TransitionFailed(_logger, _instanceId, {OriginalStateVar}.ToString(), trigger.ToString());");
                    }
                }

                Sb.AppendLine($"return {SuccessVar};");
            }
        }
        Sb.AppendLine();
    }

    // --- FAST PATH DETECTOR & EMITTER ---------------------------------------------

    private bool IsPureBasicFastPath()
    {
        // Must be: flat (no HSM), sync, no payload, no extensions and no OnEntry/OnExit
        
        if (HasPayload || HasMultiPayload
            || IsHierarchical
            || ExtensionsOn
            || HasOnEntryExit
            || Model.GenerationConfig.IsAsync)
            return false;

        var transitions = Model.Transitions;
        if (transitions == null || transitions.Count == 0) return false;

        // No guards and actions, no internal transitions
        if (transitions.Any(t =>
            !string.IsNullOrEmpty(t.GuardMethod) ||
            !string.IsNullOrEmpty(t.ActionMethod) ||
            t.IsInternal))
            return false;

        // All transitions must have exactly one trigger – the same one
        var distinctTriggers = transitions.Select(t => t.Trigger).Distinct().ToList();
        if (distinctTriggers.Count != 1) return false;

        // Each state must have at most one transition (Basic sequence)
        var multiplePerState = transitions
            .GroupBy(t => t.FromState)
            .Any(g => g.Count() > 1);
        if (multiplePerState) return false;

        // Check that it's really a "chain" (From -> To) without gaps
        // Nie wymuszamy cyklu, ale dopuszczamy go (A->B, B->C, C->A)
        var fromSet = new HashSet<string>(transitions.Select(t => t.FromState));
        return fromSet.Count >= 2; // co najmniej 2 stany, inaczej zysk marginalny
    }

    private string GetSingleTriggerForFastPath(string triggerTypeForUsage)
    {
        var trig = Model.Transitions.Select(t => t.Trigger).Distinct().Single();
        return $"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(trig)}";
    }

    private List<(string fromState, string toState)> GetOrderedStateMapping()
    {
        // Order deterministically by source state ordinal, so switch is stable
        // (Model.States zawiera definicje z OrdinalValue)
        var ord = Model.States; // Dictionary<string, StateDef> (Name -> Def z OrdinalValue)
        var list = Model.Transitions
            .Select(t => (from: t.FromState, to: t.ToState))
            .OrderBy(x => ord.TryGetValue(x.from, out var def) ? def.OrdinalValue : int.MaxValue)
            .ToList();
        return list;
    }

    private void EmitTryFireInternalFastPath(string stateTypeForUsage, string triggerTypeForUsage)
    {
        var triggerLit = GetSingleTriggerForFastPath(triggerTypeForUsage);
        // We also need the trigger name as string for logging
        var triggerName = Model.Transitions.Select(t => t.Trigger).Distinct().Single();
        var mapping = GetOrderedStateMapping();

        // Generujemy: if (trigger != <TRIGGER>) return false; + switch(_currentState){ case FROM: _currentState = TO; return true; ... }
        using (Sb.IfDirective("DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS"))
        {
            Sb.AppendLine($"// FAST-PATH: single-trigger flat basic machine for {Model.ClassName}");
        }

        Sb.AppendLine($"if (trigger != {triggerLit}) return false;");

        using (Sb.Switch(CurrentStateField))
        {
            foreach (var (fromState, toState) in mapping)
            {
                var fromEsc = TypeHelper.EscapeIdentifier(fromState);
                var toEsc = TypeHelper.EscapeIdentifier(toState);
                using (Sb.Case($"{stateTypeForUsage}.{fromEsc}"))
                {
                    WriteLogStatement(GeneratedLogLevel.Debug,
                        $"TransitionStarted(_logger, _instanceId, \"{fromState}\", \"{triggerName}\", \"{toState}\");");
                    Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{toEsc};");
                    WriteLogStatement(GeneratedLogLevel.Information,
                        $"TransitionSucceeded(_logger, _instanceId, \"{fromState}\", \"{toState}\", \"{triggerName}\");");
                    Sb.AppendLine("return true;");
                }
            }
            Sb.DefaultReturn("false");
        }
    }

    // --- HSM FAST PATH DETECTOR & EMITTER ---------------------------------------------

    private bool IsHsmGuardlessEqualPriorityFastPath()
    {
        if (!IsHierarchical
            || Model.GenerationConfig.IsAsync
            || HasPayload
            || HasMultiPayload
            || ExtensionsOn)
            return false;
        // Note: We allow OnEntry/OnExit since fast-path is about winner selection, not execution

        var transitions = Model.Transitions;
        if (transitions == null || transitions.Count == 0) return false;

        // No guards and no explicit priorities (assuming default priority is 0)
        if (transitions.Any(t => !string.IsNullOrEmpty(t.GuardMethod) || t.Priority != 0)) return false;

        // For safety - if any state has >1 transition on same trigger, use general path
        var multi = transitions
            .GroupBy(t => (t.FromState, t.Trigger))
            .Any(g => g.Count() > 1);
        return !multi;
    }

    private void EmitHsmTryFireFastPath(string stateType, string triggerType)
    {
        // Assumption: no guards, equal-priority, max 1 transition per (state, trigger).
        // Emit: walk from current state up parents; first matched case(trigger) → execute plan and return true.

        using (Sb.IfDirective("DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS"))
        {
            Sb.AppendLine("// HSM FAST-PATH: first-match wins (no guards, equal priority)");
        }

        Sb.AppendLine($"int __idx = (int){CurrentStateField};");
        using (Sb.Block("while (__idx >= 0)"))
        {
            using (Sb.Switch($"({stateType})__idx"))
            {
                // Per-state - generate branches for triggers with simple matching
                // Group transitions by FromState
                var byState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => Model.States[g.Key].OrdinalValue);

                foreach (var g in byState)
                {
                    var fromEsc = TypeHelper.EscapeIdentifier(g.Key);
                    using (Sb.Case($"{stateType}.{fromEsc}"))
                    {
                        // For each trigger in this state
                        var byTrigger = g.GroupBy(t => t.Trigger);
                        foreach (var tg in byTrigger)
                        {
                            var trigEsc = TypeHelper.EscapeIdentifier(tg.Key);
                            var t = tg.First(); // guarantee: 1 per (state, trigger)

                            using (Sb.Block($"if (trigger == {triggerType}.{trigEsc})"))
                            {
                                WritePlanStepsForTransitionFastPath(t, stateType, triggerType);
                                Sb.AppendLine("return true;");
                            }
                        }

                        Sb.AppendLine("break;");
                    }
                }

                Sb.DefaultBreak();
            }

            Sb.AppendLine("__idx = g_parent[__idx];");
        }

        Sb.AppendLine("return false;");
    }

    private void WritePlanStepsForTransitionFastPath(TransitionModel transition, string stateType, string triggerType)
    {
        // Simplified version - execute transition steps directly
        // This is a fast-path so we know: no guards, no priorities conflicts

        var hasOnEntryExit = ShouldGenerateOnEntryExit();
        var toEsc = TypeHelper.EscapeIdentifier(transition.ToState);

        // Record history if needed (for HSM)
        if (IsHierarchical && !transition.IsInternal)
        {
            Sb.AppendLine("RecordHistoryForCurrentPath();");
        }

        // OnExit (if applicable)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            WriteOnExitCall(fromStateDef, transition.ExpectedPayloadType);
        }

        // Action (if present)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            WriteActionCall(transition);
        }

        // State assignment
        if (!transition.IsInternal)
        {
            if (IsHierarchical)
            {
                WriteStateChangeWithCompositeHandling(transition.ToState, stateType);
            }
            else
            {
                Sb.AppendLine($"{CurrentStateField} = {stateType}.{toEsc};");
            }
        }

        // OnEntry (if applicable)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            WriteOnEntryCall(toStateDef, transition.ExpectedPayloadType);
        }
    }

    private void WriteTryFireMethodSync(string stateType, string triggerType)
    {
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool TryFireInternal({triggerType} trigger, object? payload)"))
        {

            WriteAttemptStart(stateType, triggerType);

            if (!Model.Transitions.Any())
            {
                WriteAttemptCompleted(stateType, "UnhandledTrigger", $"({stateType}?)null", $"(TransitionInfo<{stateType}>?)null");
                Sb.AppendLine($"return false; {NoTransitionsComment}");
                return;
            }

            // >>> FAST-PATH: simple Basic A->B->C->A variant, one trigger, no payload/guards/actions/onEntry/onExit/extensions/hsm
            if (IsPureBasicFastPath())
            {
                EmitTryFireInternalFastPath(stateType, triggerType);
                Sb.AppendLine(); // spacing
                Sb.AppendLine("// (fast-path) end");
                return; // important: we finish TryFireInternal generation here
            }

            // >>> HSM FAST-PATH: hierarchical without guards and equal priority
            if (IsHsmGuardlessEqualPriorityFastPath())
            {
                EmitHsmTryFireFastPath(stateType, triggerType);
                return; // important: end TryFireInternal generation here
            }

            // --- existing path ---
            // For sync: choose writer depending on features
            if (HasPayload && HasMultiPayload)
            {
                Sb.AppendLine("// Payload-type validation for multi-payload variant");
                using (Sb.Block($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && (payload == null || !expectedType.IsInstanceOfType(payload)))"))
                {
                    WriteLogStatement(GeneratedLogLevel.Warning, "PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", payload?.GetType().Name ?? \"null\");");
                    WriteAttemptCompleted(stateType, "InvalidPayload", $"({stateType}?)null", $"(TransitionInfo<{stateType}>?)null");
                    Sb.AppendLine("return false; // wrong payload type");
                }
                Sb.AppendLine();
            }

            var writer = HasPayload
                ? WriteTransitionLogicPayloadSyncDirect
                : (Action<TransitionModel, string, string>)WriteTransitionLogicSyncCore;

            WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
            {
                _smCtxCreated = false;
                writer(transition, stateType, triggerType);
            });

        }
        Sb.AppendLine();

        // Generate public wrapper for sync
        WriteMethodAttribute();
        using (Sb.Block($"public override bool TryFire({triggerType} trigger, object? payload = null)"))
        {
            Sb.AppendLine("EnsureStarted();");
            Sb.AppendLine("return TryFireInternal(trigger, payload);");
            Sb.AppendLine();
        }
    }

    // Sync core logic using base non-payload implementation (includes hooks)
    private void WriteTransitionLogicSyncCore(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        // For WithExtensions variant, we need special exception handling
        if (ExtensionsOn)
        {
            using (Sb.IfDirective("DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS"))
            {
                Sb.AppendLine($"// DEBUG: Using WriteTransitionLogicWithExtensions for {Model.ClassName}");
            }
            WriteTransitionLogicWithExtensions(
                transition,
                stateTypeForUsage,
                triggerTypeForUsage,
                hasPayload: false,
                useAsyncFlow: false);
        }
        else
        {
            using (Sb.IfDirective("DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS"))
            {
                Sb.AppendLine($"// DEBUG: Using base WriteTransitionLogicForFlatNonPayload for {Model.ClassName}");
            }
            WriteTransitionLogicForFlatNonPayload(transition, stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteTransitionLogicWithExtensions(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage,
        bool hasPayload,
        bool useAsyncFlow)
    {
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        using (Sb.Block(""))
        {
            Sb.AppendLine($"TransitionInfo<{stateTypeForUsage}> matchedTransition = default;");
            WritePrepareMatchedTransition(transition, stateTypeForUsage);
            _smCtxCreated = true;
            Sb.AppendLine($"{stateTypeForUsage}? __resolvedTarget = null;");
            Sb.AppendLine("global::FastFsm.Exceptions.TransitionStage? __transitionStage = null;");

            using (Sb.Block("try"))
            {
                WriteLogStatement(GeneratedLogLevel.Debug,
                    $"TransitionStarted(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\", \"{transition.ToState}\");");

                if (!string.IsNullOrEmpty(transition.GuardMethod))
                {
                    Sb.AppendLine("__transitionStage = global::FastFsm.Exceptions.TransitionStage.Guard;");
                    WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
                    GuardGenerationHelper.EmitGuardCheck(
                        Sb,
                        transition,
                        GuardResultVar,
                        hasPayload ? PayloadVar : "null",
                        IsAsyncMachine,
                        wrapInTryCatch: false,
                        Model.ContinueOnCapturedContext,
                        handleResultAfterTry: true,
                        cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                        treatCancellationAsFailure: false);
                    WriteAfterGuardEvaluatedHook(
                        transition,
                        GuardResultVar,
                        stateTypeForUsage,
                        triggerTypeForUsage);
                    Sb.AppendLine("__transitionStage = null;");

                    using (Sb.Block($"if (!{GuardResultVar})"))
                    {
                        WriteLogStatement(GeneratedLogLevel.Warning,
                            $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                        WriteLogStatement(GeneratedLogLevel.Warning,
                            $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
                        WriteAttemptCompleted(
                            stateTypeForUsage,
                            "GuardRejected",
                            "__resolvedTarget",
                            "matchedTransition");
                        WriteExtensionTransitionExit(useAsyncFlow, success: false);
                    }
                }

                if (!transition.IsInternal)
                {
                    if (Model.HierarchyEnabled)
                    {
                        Sb.AppendLine("int __lifecycleLca = -1;");
                        WriteHierarchicalExtensionStateExits(transition, stateTypeForUsage);
                    }
                    else
                    {
                        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.States) != 0)"))
                        {
                            Sb.AppendLine("_extensionRunner.RunStateExiting(extensionSet, in attempt, attempt.SourceState);");
                        }
                    }
                }

                if (!transition.IsInternal && hasOnEntryExit &&
                    Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
                    !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
                {
                    WriteExtensionCallback(
                        transition,
                        stateTypeForUsage,
                        "global::FastFsm.Exceptions.TransitionStage.OnExit",
                        fromStateDef.OnExitMethod!,
                        stateAlreadyChanged: false,
                        () =>
                        {
                            if (hasPayload)
                            {
                                CallbackGenerationHelper.EmitOnExitCall(
                                    Sb,
                                    fromStateDef,
                                    transition.ExpectedPayloadType,
                                    Model.DefaultPayloadType,
                                    PayloadVar,
                                    IsAsyncMachine,
                                    wrapInTryCatch: false,
                                    Model.ContinueOnCapturedContext,
                                    isSinglePayload: !HasMultiPayload,
                                    isMultiPayload: HasMultiPayload,
                                    cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                                    treatCancellationAsFailure: false);
                            }
                            else
                            {
                                WriteOnExitCall(fromStateDef, null);
                            }

                            WriteLogStatement(GeneratedLogLevel.Debug,
                                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                        });
                }

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

                    Sb.AppendLine($"__resolvedTarget = {CurrentStateField};");

                    if (Model.HierarchyEnabled)
                    {
                        WriteHierarchicalExtensionStateEntries(stateTypeForUsage);
                    }
                    else
                    {
                        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.States) != 0)"))
                        {
                            Sb.AppendLine($"_extensionRunner.RunStateEntered(extensionSet, in attempt, {CurrentStateField});");
                        }
                    }
                }

                if (!transition.IsInternal && hasOnEntryExit &&
                    Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
                    !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
                {
                    WriteExtensionCallback(
                        transition,
                        stateTypeForUsage,
                        "global::FastFsm.Exceptions.TransitionStage.OnEntry",
                        toStateDef.OnEntryMethod!,
                        stateAlreadyChanged: true,
                        () =>
                        {
                            if (hasPayload)
                            {
                                CallbackGenerationHelper.EmitOnEntryCall(
                                    Sb,
                                    toStateDef,
                                    transition.ExpectedPayloadType,
                                    Model.DefaultPayloadType,
                                    PayloadVar,
                                    IsAsyncMachine,
                                    wrapInTryCatch: false,
                                    Model.ContinueOnCapturedContext,
                                    isSinglePayload: !HasMultiPayload,
                                    isMultiPayload: HasMultiPayload,
                                    cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                                    treatCancellationAsFailure: false);
                            }
                            else
                            {
                                WriteOnEntryCall(toStateDef, null);
                            }

                            WriteLogStatement(GeneratedLogLevel.Debug,
                                $"OnEntryExecuted(_logger, _instanceId, \"{toStateDef.OnEntryMethod}\", \"{transition.ToState}\");");
                        });
                }

                if (!string.IsNullOrEmpty(transition.ActionMethod))
                {
                    WriteExtensionCallback(
                        transition,
                        stateTypeForUsage,
                        "global::FastFsm.Exceptions.TransitionStage.Action",
                        transition.ActionMethod!,
                        stateAlreadyChanged: !transition.IsInternal,
                        () =>
                        {
                            if (hasPayload)
                            {
                                CallbackGenerationHelper.EmitActionCall(
                                    Sb,
                                    transition,
                                    PayloadVar,
                                    IsAsyncMachine,
                                    wrapInTryCatch: false,
                                    Model.ContinueOnCapturedContext,
                                    cancellationTokenVar: IsAsyncMachine ? "cancellationToken" : null,
                                    treatCancellationAsFailure: false);
                            }
                            else
                            {
                                WriteActionCall(transition);
                            }

                            WriteLogStatement(GeneratedLogLevel.Debug,
                                $"ActionExecuted(_logger, _instanceId, \"{transition.ActionMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                        });
                }

                WriteLogStatement(GeneratedLogLevel.Information,
                    $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteAttemptCompleted(
                    stateTypeForUsage,
                    "Succeeded",
                    "__resolvedTarget",
                    "matchedTransition");
                WriteExtensionTransitionExit(useAsyncFlow, success: true);
            }

            using (Sb.Block("catch (System.OperationCanceledException ex)"))
            {
                WriteAttemptCompleted(
                    stateTypeForUsage,
                    "Canceled",
                    "__resolvedTarget",
                    "matchedTransition",
                    "__transitionStage",
                    "ex");
                if (useAsyncFlow)
                {
                    using (Sb.Block("if (__transitionStage == global::FastFsm.Exceptions.TransitionStage.Guard)"))
                    {
                        WriteExtensionTransitionExit(useAsyncFlow: true, success: false);
                    }
                }
                Sb.AppendLine("throw;");
            }

            using (Sb.Block("catch (System.Exception ex)"))
            {
                WriteAttemptCompleted(
                    stateTypeForUsage,
                    "Faulted",
                    "__resolvedTarget",
                    "matchedTransition",
                    "__transitionStage",
                    "ex");
                if (useAsyncFlow)
                {
                    using (Sb.Block(
                               "if (__transitionStage == global::FastFsm.Exceptions.TransitionStage.Guard || " +
                               "__transitionStage == global::FastFsm.Exceptions.TransitionStage.OnExit)"))
                    {
                        WriteExtensionTransitionExit(useAsyncFlow: true, success: false);
                    }
                }
                Sb.AppendLine("throw;");
            }
        }
    }

    private void WriteHierarchicalExtensionStateExits(TransitionModel transition, string stateType)
    {
        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.States) != 0)"))
        {
            // Lifecycle exits start at the active leaf, but the LCA is computed from the
            // state that owns the transition. Using the active leaf as the semantic source
            // would skip the owner on ancestor-to-descendant and ancestor self-transitions.
            using (Sb.IfDirective("DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS"))
            {
                Sb.AppendLine("// HSM lifecycle: LCA from handled-at state; external source is always exited/re-entered");
            }
            Sb.AppendLine("int __lifecycleSource = (int)attempt.SourceState;");
            Sb.AppendLine($"int __handledState = (int){stateType}.{TypeHelper.EscapeIdentifier(transition.FromState)};");
            Sb.AppendLine($"int __lifecycleTarget = (int){stateType}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            Sb.AppendLine("__lifecycleLca = FindLowestCommonAncestor(__handledState, __lifecycleTarget);");
            using (Sb.Block("if (__lifecycleLca == __handledState)"))
            {
                Sb.AppendLine("__lifecycleLca = (uint)__handledState < (uint)g_parent.Length ? g_parent[__handledState] : -1;");
            }

            using (Sb.Block("for (int __exiting = __lifecycleSource; __exiting >= 0 && __exiting != __lifecycleLca; __exiting = (uint)__exiting < (uint)g_parent.Length ? g_parent[__exiting] : -1)"))
            {
                Sb.AppendLine($"_extensionRunner.RunStateExiting(extensionSet, in attempt, ({stateType})__exiting);");
            }
        }
    }

    private void WriteHierarchicalExtensionStateEntries(string stateType)
    {
        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.States) != 0)"))
        {
            Sb.AppendLine($"int __lifecycleTargetDepth = g_depth[(int){CurrentStateField}];");
            Sb.AppendLine("int __lifecycleLcaDepth = __lifecycleLca >= 0 ? g_depth[__lifecycleLca] : -1;");
            using (Sb.Block("for (int __enterDepth = __lifecycleLcaDepth + 1; __enterDepth <= __lifecycleTargetDepth; __enterDepth++)"))
            {
                Sb.AppendLine($"int __entering = (int){CurrentStateField};");
                using (Sb.Block("while (g_depth[__entering] > __enterDepth)"))
                {
                    Sb.AppendLine("__entering = g_parent[__entering];");
                }
                Sb.AppendLine($"_extensionRunner.RunStateEntered(extensionSet, in attempt, ({stateType})__entering);");
            }
        }
    }

    private void WriteExtensionCallback(
        TransitionModel transition,
        string stateTypeForUsage,
        string stage,
        string callbackName,
        bool stateAlreadyChanged,
        Action writeCallback)
    {
        Sb.AppendLine($"__transitionStage = {stage};");
        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.Callbacks) != 0)"))
        {
            Sb.AppendLine($"_extensionRunner.RunCallbackExecuting(extensionSet, in attempt, {stage}, \"{callbackName}\");");
        }

        using (Sb.Block("try"))
        {
            writeCallback();
        }
        using (Sb.Block("catch (System.Exception ex) when (ex is not System.OperationCanceledException)"))
        {
            if (Model.ExceptionHandler == null)
            {
                Sb.AppendLine("throw;");
            }
            else
            {
                WriteExceptionHandlerDirective(transition, stage, stateAlreadyChanged);
                using (Sb.Block("if (directive == global::FastFsm.Exceptions.ExceptionDirective.Continue)"))
                {
                    using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.Callbacks) != 0)"))
                    {
                        Sb.AppendLine($"_extensionRunner.RunCallbackFaulted(extensionSet, in attempt, {stage}, \"{callbackName}\", ex);");
                    }
                }
                Sb.AppendLine("else");
                using (Sb.Block(""))
                {
                    Sb.AppendLine("throw;");
                }
            }
        }
        Sb.AppendLine("__transitionStage = null;");
    }

    private void WriteExceptionHandlerDirective(
        TransitionModel transition,
        string stage,
        bool stateAlreadyChanged)
    {
        var handler = Model.ExceptionHandler!;
        var stateType = GetTypeNameForUsage(Model.StateType);
        var triggerType = GetTypeNameForUsage(Model.TriggerType);

        Sb.AppendLine($"var exceptionContext = new global::FastFsm.Exceptions.ExceptionContext<{stateType}, {triggerType}>(");
        using (Sb.Indent())
        {
            Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(transition.FromState)},");
            Sb.AppendLine($"{stateType}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
            Sb.AppendLine($"{triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)},");
            Sb.AppendLine("ex,");
            Sb.AppendLine($"{stage},");
            Sb.AppendLine($"{stateAlreadyChanged.ToString().ToLowerInvariant()});");
        }

        var args = handler.AcceptsCancellationToken
            ? "exceptionContext, cancellationToken"
            : "exceptionContext";
        Sb.AppendLine(handler.IsAsync
            ? $"var directive = await {handler.MethodName}({args}).ConfigureAwait({Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});"
            : $"var directive = {handler.MethodName}({args});");
    }

    private void WriteExtensionTransitionExit(bool useAsyncFlow, bool success)
    {
        if (useAsyncFlow)
        {
            Sb.AppendLine($"{SuccessVar} = {success.ToString().ToLowerInvariant()};");
            EmitGotoEndTryFire();
        }
        else
        {
            Sb.AppendLine($"return {success.ToString().ToLowerInvariant()};");
        }
    }

    private void WriteTryFireStructureDispatcher(string stateType, string triggerType, Action<TransitionModel, string, string> writeTransitionLogic)
    {
        // For Extensions variant, we need special handling for no-transition case
        if (ExtensionsOn)
        {
            // Use custom structure that handles no-transition case
            WriteTryFireStructureWithExtensions(stateType, triggerType, writeTransitionLogic);
        }
        else
        {
            // Reuse the robust base implementation for non-extension variants
            WriteTryFireStructure(stateType, triggerType, writeTransitionLogic);
        }
    }

    private void WriteTryFireStructureWithExtensions(string stateType, string triggerType, Action<TransitionModel, string, string> writeTransitionLogic)
    {
        // Custom implementation that notifies extensions even when no transition is found
        if (!Model.Transitions.Any())
        {
            Sb.AppendLine("// No transitions defined - complete the attempt as unhandled");
            WriteAttemptCompleted(stateType, "UnhandledTrigger", $"({stateType}?)null", $"(TransitionInfo<{stateType}>?)null");
            Sb.AppendLine("return false;");
            return;
        }

        // For HSM: implement ancestor chain traversal similar to base generator
        if (Model.HierarchyEnabled)
        {
            // HSM implementation: walk up the parent chain
            Sb.AppendLine($"int check = (int){CurrentStateField};");
            using (Sb.Block("while (check >= 0)"))
            {
                Sb.AppendLine($"var state = ({stateType})check;");
                using (Sb.Switch("state"))
                {
                    // Group transitions by from state
                    var grouped = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);

                    foreach (var group in grouped)
                    {
                        using (Sb.Case($"{stateType}.{TypeHelper.EscapeIdentifier(group.Key)}"))
                        {
                            using (Sb.Switch("trigger"))
                            {
                                // Group by trigger, with priority ordering
                                var triggerGroups = group
                                    .GroupBy(t => t.Trigger)
                                    .OrderBy(tg => tg.Key);

                                foreach (var triggerGroup in triggerGroups)
                                {
                                    using (Sb.Case($"{triggerType}.{TypeHelper.EscapeIdentifier(triggerGroup.Key)}"))
                                    {
                                        // Get the highest priority transition
                                        var bestTransition = triggerGroup
                                            .OrderByDescending(t => t.Priority)
                                            .First();

                                        if (bestTransition.IsInternal && ShouldGenerateLogging)
                                        {
                                            Sb.AppendLine($"// Internal transition on ancestor: {bestTransition.FromState}");
                                            using (Sb.Block("if (_logger?.IsEnabled(LogLevel.Debug) == true)"))
                                            {
                                                Sb.AppendLine($"InternalTransitionOnAncestor(_logger, _instanceId, (({stateType})check).ToString(), {CurrentStateField}.ToString(), trigger.ToString());");
                                            }
                                        }

                                        Sb.AppendLine($"// Transition: {bestTransition.FromState} -> {bestTransition.ToState} (Priority: {bestTransition.Priority})");
                                        writeTransitionLogic(bestTransition, stateType, triggerType);
                                    }
                                }

                                Sb.DefaultBreak();
                            }
                            Sb.AppendLine("break;");
                        }
                    }

                    Sb.DefaultBreak();
                }
                Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
            }
            Sb.AppendLine();
        }
        else
        {
            // Flat FSM implementation: original code
            var sortedTransitions = Model.Transitions
                .Select((t, index) => new { Transition = t, Index = index })
                .OrderByDescending(x => x.Transition.Priority)
                .ThenBy(x => x.Index)
                .Select(x => x.Transition);

            var grouped = sortedTransitions.GroupBy(t => t.FromState);

            using (Sb.Switch(CurrentStateField))
            {
                foreach (var state in grouped)
                {
                    using (Sb.Case($"{stateType}.{TypeHelper.EscapeIdentifier(state.Key)}"))
                    {
                        var triggerGroups = state.GroupBy(t => t.Trigger);
                        using (Sb.Switch("trigger"))
                        {
                            foreach (var triggerGroup in triggerGroups)
                            {
                                using (Sb.Case($"{triggerType}.{TypeHelper.EscapeIdentifier(triggerGroup.Key)}"))
                                {
                                    foreach (var tr in triggerGroup)
                                    {
                                        Sb.AppendLine($"// Transition: {tr.FromState} -> {tr.ToState} (Priority: {tr.Priority})");
                                        writeTransitionLogic(tr, stateType, triggerType);
                                        break; // Only first matching transition
                                    }
                                }
                            }

                            Sb.DefaultBreak();
                        }
                        Sb.AppendLine("break;");
                    }
                }
                Sb.DefaultBreak();
            }
            Sb.AppendLine();
        }

        Sb.AppendLine("// No matching transition - complete the attempt as unhandled");
        WriteAttemptCompleted(stateType, "UnhandledTrigger", $"({stateType}?)null", $"(TransitionInfo<{stateType}>?)null");
        Sb.AppendLine("return false;");
    }


    private void WriteCanFireMethods(string stateType, string triggerType)
    {
        // Base CanFire without payload
        WriteCanFireMethod(stateType, triggerType);

        // Payload-aware CanFire overloads
        if (!HasPayload) return;
        if (IsAsyncMachine)
        {
            WriteAsyncCanFireWithPayload(stateType, triggerType);
            // Public typed overloads
            if (!HasMultiPayload)
            {
                var single = Model.DefaultPayloadType;
                if (!string.IsNullOrEmpty(single))
                {
                    var payloadType = GetTypeNameForUsage(single!);
                    WriteMethodAttribute();
                    using (Sb.Block($"public async ValueTask<bool> CanFireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)"))
                    {
                        Sb.AppendLine("EnsureStarted();");
                        Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, payload, cancellationToken){GetConfigureAwait()};");
                    }
                    Sb.AppendLine();
                }
            }
            else
            {
                WriteMethodAttribute();
                using (Sb.Block($"public async ValueTask<bool> CanFireAsync<TPayload>({triggerType} trigger, TPayload payload, CancellationToken cancellationToken = default)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, payload, cancellationToken){GetConfigureAwait()};");
                }
                Sb.AppendLine();
            }

            // Sync wrappers for async machines: throw on sync CanFire(payload) to preserve API parity
            if (!HasMultiPayload)
            {
                var single = Model.DefaultPayloadType;
                if (string.IsNullOrEmpty(single)) return;
                var payloadType = GetTypeNameForUsage(single!);
                WriteMethodAttribute();
                using (Sb.Block($"public bool CanFire({triggerType} trigger, {payloadType} payload)"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
            else
            {
                WriteMethodAttribute();
                using (Sb.Block($"public bool CanFire<TPayload>({triggerType} trigger, TPayload payload)"))
                {
                    Sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                Sb.AppendLine();
            }
        }
        else
        {
            WriteCanFireWithPayload(stateType, triggerType);
            // Public typed overloads
            if (!HasMultiPayload)
            {
                var single = Model.DefaultPayloadType;
                if (string.IsNullOrEmpty(single)) return;
                var payloadType = GetTypeNameForUsage(single!);
                WriteMethodAttribute();
                using (Sb.Block($"public bool CanFire({triggerType} trigger, {payloadType} payload)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("return CanFireWithPayload(trigger, payload);");
                }
                Sb.AppendLine();
            }
            else
            {
                WriteMethodAttribute();
                using (Sb.Block($"public bool CanFire<TPayload>({triggerType} trigger, TPayload payload)"))
                {
                    Sb.AppendLine("EnsureStarted();");
                    Sb.AppendLine("return CanFireWithPayload(trigger, payload);");
                }
                Sb.AppendLine();
            }
        }
    }

    protected virtual void WriteCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        if (IsAsyncMachine)
        {
            WriteAsyncCanFireMethod(stateTypeForUsage, triggerTypeForUsage);
        }
        else
        {
            WriteCanFireMethodSyncCore(stateTypeForUsage, triggerTypeForUsage);
        }
    }

    private void WriteAsyncCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Asynchronously checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
        Sb.WriteParam("trigger", "The trigger to check");
        Sb.WriteParam("cancellationToken", "A token to observe for cancellation requests");
        Sb.WriteReturns("True if the trigger can be fired, false otherwise");

        using (Sb.Block($"protected override async ValueTask<bool> CanFireInternalAsync({triggerTypeForUsage} trigger, CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();

            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine($"int check = (int){CurrentStateField};");
                using (Sb.Block("while (check >= 0)"))
                {
                    Sb.AppendLine($"var state = ({stateTypeForUsage})check;");
                    using (Sb.Switch("state"))
                    {
                        var grouped = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);
                        foreach (var group in grouped)
                        {
                            using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(group.Key)}", braces: false))
                            {
                                using (Sb.Switch("trigger"))
                                {
                                    foreach (var transition in group)
                                    {
                                        using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                        {
                                            if (!string.IsNullOrEmpty(transition.GuardMethod))
                                            {
                                                if (transition.GuardIsAsync)
                                                {
                                                    GuardGenerationHelper.EmitGuardCheck(
                                                        Sb,
                                                        transition,
                                                        "guardResult",
                                                        "null",
                                                        IsAsyncMachine,
                                                        wrapInTryCatch: true,
                                                        Model.ContinueOnCapturedContext,
                                                        handleResultAfterTry: true,
                                                        cancellationTokenVar: "cancellationToken",
                                                        treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                                    );
                                                }
                                                else
                                                {
                                                    WriteGuardCall(transition, "guardResult");
                                                }
                                                Sb.AppendLine("return guardResult;");
                                            }
                                            else
                                            {
                                                Sb.AppendLine("return true;");
                                            }
                                        }
                                    }
                                    Sb.DefaultBreak();
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.DefaultBreak();
                    }
                    Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                }
                Sb.AppendLine("return false;");
            }
            else
            {
                using (Sb.Switch(CurrentStateField))
                {
                    var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);
                    foreach (var stateName in allHandledFromStates)
                    {
                        using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}", braces: false))
                        {
                            using (Sb.Switch("trigger"))
                            {
                                var transitionsFromThisState = Model.Transitions.Where(t => t.FromState == stateName);
                                foreach (var transition in transitionsFromThisState)
                                {
                                    using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            if (transition.GuardIsAsync)
                                            {
                                                GuardGenerationHelper.EmitGuardCheck(
                                                    Sb,
                                                    transition,
                                                    "guardResult",
                                                    "null",
                                                    IsAsyncMachine,
                                                    wrapInTryCatch: true,
                                                    Model.ContinueOnCapturedContext,
                                                    handleResultAfterTry: true,
                                                    cancellationTokenVar: "cancellationToken",
                                                    treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                                );
                                            }
                                            else
                                            {
                                                WriteGuardCall(transition, "guardResult");
                                            }
                                            Sb.AppendLine("return guardResult;");
                                        }
                                        else
                                        {
                                            Sb.AppendLine("return true;");
                                        }
                                    }
                                }
                                Sb.DefaultReturn("false");
                            }
                        }
                    }
                    Sb.DefaultReturn("false");
                }
            }
        }
        Sb.AppendLine();
    }

    private void WriteCanFireMethodSyncCore(string stateTypeForUsage, string triggerTypeForUsage)
    {
        Sb.WriteSummary("Checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
        Sb.AppendLine("/// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine("/// <returns>True if the trigger can be fired, false otherwise</returns>");
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
                    using (Sb.Switch("enumState"))
                    {
                        var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);

                        foreach (var stateName in allHandledFromStates)
                        {
                            using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}", braces: false))
                            {
                                using (Sb.Switch("trigger"))
                                {
                                    var transitionsFromThisState = Model.Transitions
                                        .Where(t => t.FromState == stateName);

                                    foreach (var transition in transitionsFromThisState)
                                    {
                                        using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                        {
                                            if (!string.IsNullOrEmpty(transition.GuardMethod))
                                            {
                                                var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                                var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                                Sb.AppendLine($"return EvaluateGuard__{from}__{trig}(null);");
                                            }
                                            else
                                            {
                                                Sb.AppendLine("return true;");
                                            }
                                        }
                                    }
                                    Sb.DefaultBreak();
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.DefaultBreak();
                    }
                    Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                }
                Sb.AppendLine("return false;");
            }
            else
            {
                // Flat FSM: Original implementation
                using (Sb.Switch(CurrentStateField))
                {
                    var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);

                    foreach (var stateName in allHandledFromStates)
                    {
                        using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}", braces: false))
                        {
                            using (Sb.Switch("trigger"))
                            {
                                var transitionsFromThisState = Model.Transitions
                                    .Where(t => t.FromState == stateName);

                                foreach (var transition in transitionsFromThisState)
                                {
                                    using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                            Sb.AppendLine($"return EvaluateGuard__{from}__{trig}(null);");
                                        }
                                        else
                                        {
                                            Sb.AppendLine("return true;");
                                        }
                                    }
                                }
                                Sb.DefaultReturn("false");
                            }
                        }
                    }
                    Sb.DefaultReturn("false");
                }
            }
        }
        Sb.AppendLine();
    }

    private void WriteGetPermittedTriggersMethods(string stateType, string triggerType)
    {
        if (IsAsyncMachine)
        {
            WriteAsyncGetPermittedTriggersMethod(stateType, triggerType);
        }
        else
        {
            WriteGetPermittedTriggersMethod(stateType, triggerType);
        }

        // Add resolver-based GetPermittedTriggers for payload variants
        if (HasPayload)
        {
            WriteGetPermittedTriggersWithResolver(stateType, triggerType);
        }
    }

    private void WriteAsyncGetPermittedTriggersMethod(string stateType, string triggerType)
    {
        Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
        Sb.WriteParam("cancellationToken", "A token to observe for cancellation requests");
        Sb.WriteReturns("List of triggers that can be fired in the current state");

        using (Sb.Block($"protected override async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersInternalAsync(CancellationToken cancellationToken = default)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine($"var permitted = new List<{triggerType}>();");
                Sb.AppendLine($"int check = (int){CurrentStateField};");
                using (Sb.Block("while (check >= 0)"))
                {
                    Sb.AppendLine($"var state = ({stateType})check;");
                    using (Sb.Switch("state"))
                    {
                        var transitionsByFromState = Model.Transitions
                            .GroupBy(t => t.FromState)
                            .OrderBy(g => g.Key);
                        foreach (var stateGroup in transitionsByFromState)
                        {
                            var stateName = stateGroup.Key;
                            using (Sb.Case($"{stateType}.{TypeHelper.EscapeIdentifier(stateName)}"))
                            {
                                foreach (var transition in stateGroup)
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        if (transition.GuardIsAsync)
                                        {
                                            GuardGenerationHelper.EmitGuardCheck(
                                                Sb,
                                                transition,
                                                "canFire",
                                                "null",
                                                IsAsyncMachine,
                                                wrapInTryCatch: true,
                                                Model.ContinueOnCapturedContext,
                                                handleResultAfterTry: true,
                                                cancellationTokenVar: "cancellationToken",
                                                treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                            );
                                            using (Sb.Block("if (canFire)"))
                                            {
                                                Sb.AppendLine($"permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                            }
                                        }
                                        else
                                        {
                                            Sb.AppendLine($"var canFire = {transition.GuardMethod}();");
                                            using (Sb.Block("if (canFire)"))
                                            {
                                                Sb.AppendLine($"permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Sb.AppendLine($"permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                    }
                                }
                                Sb.AppendLine("break;");
                            }
                        }
                        Sb.DefaultBreak();
                    }
                    Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                }
                Sb.AppendLine($"return permitted.Count == 0 ? {ArrayEmptyMethod}<{triggerType}>() : permitted.ToArray();");
            }
            else
            {
                using (Sb.Switch(CurrentStateField))
                {
                    var transitionsByFromState = Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByFromState)
                    {
                        var stateName = stateGroup.Key;
                        using (Sb.Case($"{stateType}.{TypeHelper.EscapeIdentifier(stateName)}"))
                        {
                            // Check if any transition has a guard
                            var hasAsyncGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync);

                            if (!hasAsyncGuards && stateGroup.All(t => string.IsNullOrEmpty(t.GuardMethod)))
                            {
                                // No guards - return static array
                                var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                                if (triggers.Any())
                                {
                                    var triggerList = string.Join(", ", triggers.Select(t => $"{triggerType}.{TypeHelper.EscapeIdentifier(t)}"));
                                    Sb.AppendLine($"return new {triggerType}[] {{ {triggerList} }};");
                                }
                                else
                                {
                                    Sb.AppendLine($"return {ArrayEmptyMethod}<{triggerType}>();");
                                }
                            }
                            else
                            {
                                // Has guards - build list dynamically
                                Sb.AppendLine($"var permitted = new List<{triggerType}>();");

                                foreach (var transition in stateGroup)
                                {
                                    using (Sb.Block(""))
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            if (transition.GuardIsAsync)
                                            {
                                                // Use guard helper for async guards
                                                GuardGenerationHelper.EmitGuardCheck(
                                                    Sb,
                                                    transition,
                                                    "canFire",
                                                    "null",
                                                    IsAsyncMachine,
                                                    wrapInTryCatch: true,
                                                    Model.ContinueOnCapturedContext,
                                                    handleResultAfterTry: true,
                                                    cancellationTokenVar: "cancellationToken",
                                                    treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                                );

                                                using (Sb.Block("if (canFire)"))
                                                {
                                                    Sb.AppendLine($"permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                                }
                                            }
                                            else
                                            {
                                                Sb.AppendLine($"var canFire = {transition.GuardMethod}();");
                                                using (Sb.Block("if (canFire)"))
                                                {
                                                    Sb.AppendLine($"permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Sb.AppendLine($"permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                        }
                                    }
                                }

                                Sb.AppendLine("return permitted;");
                            }
                        }
                    }

                    Sb.AppendLine("default:");
                    using (Sb.Indent())
                    {
                        Sb.AppendLine($"return {ArrayEmptyMethod}<{triggerType}>();");
                    }
                }
            }
        }
        Sb.AppendLine();
    }

    protected virtual bool ShouldGenerateInitialOnEntry() =>
        // Variants were removed; gate solely on callbacks presence
        Model.GenerationConfig.HasOnEntryExit;

    protected override bool ShouldGenerateOnEntryExit() =>
        // Variants were removed; gate solely on callbacks presence
        Model.GenerationConfig.HasOnEntryExit;

    // Use Core-like transition logic to ensure proper token passing and hooks
    protected void WriteTransitionLogic(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        if (ExtensionsOn)
        {
            WriteTransitionLogicWithExtensions(
                transition,
                stateTypeForUsage,
                triggerTypeForUsage,
                hasPayload: false,
                useAsyncFlow: true);
            return;
        }

        // Debug to trace the call path
        Sb.AppendLine($"// DEBUG: WriteTransitionLogic called in UnifiedStateMachineGenerator for {transition.ActionMethod}");

        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard check (async-aware)
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            WriteAsyncAwareGuardCheck(transition);
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            if (IsAsyncMachine)
            {
                using (Sb.Block("try"))
                {
                    WriteOnExitCall(fromStateDef, null);
                    WriteLogStatement(GeneratedLogLevel.Debug,
                        $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                }
                using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
                {
                    // Check if we have an exception handler configured
                    if (Model.ExceptionHandler != null)
                    {
                        WriteExceptionHandlerDirective(
                            transition,
                            "global::FastFsm.Exceptions.TransitionStage.OnExit",
                            stateAlreadyChanged: false);
                        using (Sb.Block("if (directive != global::FastFsm.Exceptions.ExceptionDirective.Continue)"))
                        {
                            Sb.AppendLine($"{SuccessVar} = false;");
                            EmitGotoEndTryFire();
                        }
                    }
                    else
                    {
                        Sb.AppendLine($"{SuccessVar} = false;");
                        EmitGotoEndTryFire();
                    }
                }
            }
            else
            {
                using (Sb.IfDirective("FASTFSM_SAFE_ACTIONS"))
                {
                    using (Sb.Block("try"))
                    {
                        WriteOnExitCall(fromStateDef, null);
                        WriteLogStatement(GeneratedLogLevel.Debug,
                            $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                    }
                    using (Sb.Block("catch (System.OperationCanceledException)"))
                    {
                        Sb.AppendLine("return false;");
                    }
                    using (Sb.Block("catch (System.Exception)"))
                    {
                        Sb.AppendLine("if (Model.ExceptionHandler != null) { /* handled below */ } else { return false; }");
                    }
                    Sb.ElseDirective();
                    WriteOnExitCall(fromStateDef, null);
                    WriteLogStatement(GeneratedLogLevel.Debug,
                        $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                }
            }
        }

        // State change (before OnEntry)
        if (!transition.IsInternal)
        {
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("RecordHistoryForCurrentPath();");
                WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
            }
            else
            {
                WriteLogStatement(GeneratedLogLevel.Debug,
                    $"TransitionStarted(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\", \"{transition.ToState}\");");
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
        }

        // OnEntry (with optional exception policy)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            EmitOnEntryWithExceptionPolicy(toStateDef, null, transition.FromState, transition.ToState, transition.Trigger);
        }

        // Action (with optional exception policy)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            EmitActionWithExceptionPolicy(transition, transition.FromState, transition.ToState);
        }

        // Log successful transition only after OnEntry succeeds
        if (!transition.IsInternal)
        {
            WriteLogStatement(GeneratedLogLevel.Information,
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        EmitGotoEndTryFire();
    }

    // Extension hooks (emitted only when HasExtensions)
    protected override void WriteBeforeTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        if (!ExtensionsOn) return;
        WritePrepareMatchedTransition(transition, stateTypeForUsage);
        Sb.AppendLine();
        _smCtxCreated = true;
    }

    protected override void WriteGuardEvaluationHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        if (!ExtensionsOn) return;

        // Ensure candidate information exists if a specialized path skipped the match hook.
        if (!_smCtxCreated)
        {
            WritePrepareMatchedTransition(transition, stateTypeForUsage);
            _smCtxCreated = true;
        }

        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.Guards) != 0)"))
        {
            Sb.AppendLine($"_extensionRunner.RunGuardEvaluating(extensionSet, in attempt, in matchedTransition, \"{transition.GuardMethod}\");");
        }

        Sb.AppendLine();
    }

    protected override void WriteAfterGuardEvaluatedHook(
        TransitionModel transition,
        string guardResultVar,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        if (!ExtensionsOn) return;
        using (Sb.Block("if ((extensionSet.Hooks & ExtensionHooks.Guards) != 0)"))
        {
            Sb.AppendLine($"_extensionRunner.RunGuardEvaluated(extensionSet, in attempt, in matchedTransition, \"{transition.GuardMethod}\", {guardResultVar});");
        }

        Sb.AppendLine();
    }

    protected override void WriteAfterTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage,
        bool success)
    {
        if (!ExtensionsOn) return;
        WriteAttemptCompleted(
            stateTypeForUsage,
            success ? "Succeeded" : "GuardRejected",
            success && !transition.IsInternal ? CurrentStateField : $"({stateTypeForUsage}?)null",
            "matchedTransition");
    }

    // Payload-aware async transition logic (uses success var + END_TRY_FIRE)
    private void WriteTransitionLogicPayloadAsync(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        if (ExtensionsOn)
        {
            WriteTransitionLogicWithExtensions(
                transition,
                stateTypeForUsage,
                triggerTypeForUsage,
                hasPayload: true,
                useAsyncFlow: true);
            return;
        }

        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Hook: Before transition
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard check (async-aware, with payload)
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            GuardGenerationHelper.EmitGuardCheck(
                Sb,
                transition,
                GuardResultVar,
                payloadVar: PayloadVar,
                isAsync: true,
                wrapInTryCatch: true,
                Model.ContinueOnCapturedContext,
                handleResultAfterTry: true,
                cancellationTokenVar: "cancellationToken",
                treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
            );
            // Ensure extensions are notified after guard is evaluated (UML-friendly order)
            WriteAfterGuardEvaluatedHook(transition, GuardResultVar, stateTypeForUsage, triggerTypeForUsage);

            using (Sb.Block($"if (!{GuardResultVar})"))
            {
                WriteLogStatement(GeneratedLogLevel.Warning,
                    $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteLogStatement(GeneratedLogLevel.Warning,
                    $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
                Sb.AppendLine($"{SuccessVar} = false;");
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                EmitGotoEndTryFire();
            }
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            using (Sb.Block("try"))
            {
                CallbackGenerationHelper.EmitOnExitCall(
                    Sb,
                    fromStateDef!,
                    transition.ExpectedPayloadType,
                    Model.DefaultPayloadType,
                    PayloadVar,
                    isCallerAsync: true,
                    wrapInTryCatch: false,
                    continueOnCapturedContext: Model.ContinueOnCapturedContext,
                    isSinglePayload: !HasMultiPayload,
                    isMultiPayload: HasMultiPayload,
                    cancellationTokenVar: "cancellationToken",
                    treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                );
                WriteLogStatement(GeneratedLogLevel.Debug,
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef!.OnExitMethod}\", \"{transition.FromState}\");");
            }
            using (Sb.Block("catch (Exception ex) when (ex is not System.OperationCanceledException)"))
            {
                if (Model.ExceptionHandler != null)
                {
                    WriteExceptionHandlerDirective(
                        transition,
                        "global::FastFsm.Exceptions.TransitionStage.OnExit",
                        stateAlreadyChanged: false);
                    using (Sb.Block("if (directive != global::FastFsm.Exceptions.ExceptionDirective.Continue)"))
                    {
                        Sb.AppendLine($"{SuccessVar} = false;");
                        EmitGotoEndTryFire();
                    }
                }
                else
                {
                    Sb.AppendLine($"{SuccessVar} = false;");
                    EmitGotoEndTryFire();
                }
            }
        }

        // State change (before OnEntry)
        if (!transition.IsInternal)
        {
            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine("RecordHistoryForCurrentPath();");
                WriteStateChangeWithCompositeHandling(transition.ToState, stateTypeForUsage);
            }
            else
            {
                WriteLogStatement(GeneratedLogLevel.Debug,
                    $"TransitionStarted(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\", \"{transition.ToState}\");");
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
        }

        // OnEntry (with optional exception policy)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            EmitOnEntryWithExceptionPolicyPayload(
                toStateDef!,
                transition.ExpectedPayloadType,
                Model.DefaultPayloadType!,
                transition.FromState,
                transition.ToState,
                transition.Trigger,
                isSinglePayload: !HasMultiPayload,
                isMultiPayload: HasMultiPayload
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
            WriteLogStatement(GeneratedLogLevel.Information,
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        EmitGotoEndTryFire();
    }

    // Sync direct-return transition logic with payload
    private void WriteTransitionLogicPayloadSyncDirect(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
    {
        if (ExtensionsOn)
        {
            WriteTransitionLogicWithExtensions(
                transition,
                stateTypeForUsage,
                triggerTypeForUsage,
                hasPayload: true,
                useAsyncFlow: false);
            return;
        }

        // Hook: Before transition (must be before guard check)
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard with direct return
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            var from = TypeHelper.EscapeIdentifier(transition.FromState);
            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
            Sb.AppendLine($"var guardResult = EvaluateGuard__{from}__{trig}({PayloadVar});");
            // Ensure extensions get the evaluated notification
            WriteAfterGuardEvaluatedHook(transition, "guardResult", stateTypeForUsage, triggerTypeForUsage);
            using (Sb.Block("if (!guardResult)"))
            {
                // Hook: After failed transition (guard failed)
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine("return false;");
            }
        }

        // OnExit with FASTFSM_SAFE_ACTIONS → false on failure
        if (!transition.IsInternal && ShouldGenerateOnEntryExit() &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            using (Sb.IfDirective("FASTFSM_SAFE_ACTIONS"))
            {
                using (Sb.Block("try"))
                {
                    CallbackGenerationHelper.EmitOnExitCall(
                        Sb,
                        fromStateDef!,
                        transition.ExpectedPayloadType,
                        Model.DefaultPayloadType,
                        PayloadVar,
                        isCallerAsync: false,
                        wrapInTryCatch: false,
                        continueOnCapturedContext: Model.ContinueOnCapturedContext,
                        isSinglePayload: !HasMultiPayload,
                        isMultiPayload: HasMultiPayload,
                        cancellationTokenVar: null,
                        treatCancellationAsFailure: false
                    );
                }
                using (Sb.Block("catch (System.OperationCanceledException)"))
                {
                    Sb.AppendLine("return false;");
                }
                using (Sb.Block("catch (System.Exception)"))
                {
                    Sb.AppendLine("return false;");
                }
                Sb.ElseDirective();
                CallbackGenerationHelper.EmitOnExitCall(
                    Sb,
                    fromStateDef!,
                    transition.ExpectedPayloadType,
                    Model.DefaultPayloadType,
                    PayloadVar,
                    isCallerAsync: false,
                    wrapInTryCatch: false,
                    continueOnCapturedContext: Model.ContinueOnCapturedContext,
                    isSinglePayload: !HasMultiPayload,
                    isMultiPayload: HasMultiPayload,
                    cancellationTokenVar: null,
                    treatCancellationAsFailure: false
                );
            }
        }

        // State change BEFORE action
        if (!transition.IsInternal)
        {
            if (IsHierarchical)
            {
                WriteHierarchicalStateChangeWithDiagnostics(transition.ToState, stateTypeForUsage);
            }
            else
            {
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
        }

        // OnEntry with exception policy (sync)
        if (!transition.IsInternal && ShouldGenerateOnEntryExit() &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            EmitOnEntryWithExceptionPolicyPayload(
                toStateDef!,
                transition.ExpectedPayloadType,
                Model.DefaultPayloadType!,
                transition.FromState,
                transition.ToState,
                transition.Trigger,
                isSinglePayload: !HasMultiPayload,
                isMultiPayload: HasMultiPayload
            );
        }

        // Action with exception policy (sync) AFTER state change
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            EmitActionWithExceptionPolicyPayload(transition, transition.FromState, transition.ToState);
        }

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        // Log success (parity with other sync/async paths)
        if (!transition.IsInternal && ShouldGenerateLogging)
        {
            WriteLogStatement(GeneratedLogLevel.Information,
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        Sb.AppendLine("return true;");
    }

    private void WriteAsyncCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private async ValueTask<bool> CanFireWithPayloadAsync({triggerTypeForUsage} trigger, object? payload, CancellationToken cancellationToken)"))
        {
            Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine();

            if (HasPayload && HasMultiPayload)
            {
                Sb.AppendLine($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && payload != null && !expectedType.IsInstanceOfType(payload)) return false;");
                Sb.AppendLine();
            }

            using (Sb.Switch(CurrentStateField))
            {
                var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);
                foreach (var stateName in allHandledFromStates)
                {
                    using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}", braces: false))
                    {
                        using (Sb.Switch("trigger"))
                        {
                            var transitionsFromThisState = Model.Transitions.Where(t => t.FromState == stateName);
                            foreach (var transition in transitionsFromThisState)
                            {
                                using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        GuardGenerationHelper.EmitGuardCheck(
                                            Sb,
                                            transition,
                                            "guardResult",
                                            PayloadVar,
                                            isAsync: true,
                                            wrapInTryCatch: true,
                                            Model.ContinueOnCapturedContext,
                                            handleResultAfterTry: true,
                                            cancellationTokenVar: "cancellationToken",
                                            treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
                                        );
                                        Sb.AppendLine("return guardResult;");
                                    }
                                    else
                                    {
                                        Sb.AppendLine("return true;");
                                    }
                                }
                            }
                            Sb.DefaultReturn("false");
                        }
                    }
                }
                Sb.DefaultReturn("false");
            }
        }
        Sb.AppendLine();
    }

    private void WriteCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
    {
        WriteMethodAttribute();
        using (Sb.Block($"private bool CanFireWithPayload({triggerTypeForUsage} trigger, object? payload)"))
        {
            if (HasPayload && HasMultiPayload)
            {
                Sb.AppendLine($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && payload != null && !expectedType.IsInstanceOfType(payload)) return false;");
                Sb.AppendLine();
            }

            if (Model.HierarchyEnabled)
            {
                Sb.AppendLine($"int currentIndex = (int){CurrentStateField};");
                Sb.AppendLine("int check = currentIndex;");
                using (Sb.Block("while (check >= 0)"))
                {
                    Sb.AppendLine($"var state = ({stateTypeForUsage})check;");
                    using (Sb.Switch("state"))
                    {
                        var grouped = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);
                        foreach (var group in grouped)
                        {
                            using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(group.Key)}", braces: false))
                            {
                                using (Sb.Switch("trigger"))
                                {
                                    foreach (var transition in group)
                                    {
                                        using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                        {
                                            if (!string.IsNullOrEmpty(transition.GuardMethod))
                                            {
                                                var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                                var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                                Sb.AppendLine($"return EvaluateGuard__{from}__{trig}({PayloadVar});");
                                            }
                                            else
                                            {
                                                Sb.AppendLine("return true;");
                                            }
                                        }
                                    }
                                    Sb.DefaultReturn("false");
                                }
                            }
                        }
                        Sb.DefaultBreak();
                    }
                    Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                }
                Sb.AppendLine("return false;");
            }
            else
            {
                using (Sb.Switch(CurrentStateField))
                {
                    var transitionsByFromState = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);
                    foreach (var stateGroup in transitionsByFromState)
                    {
                        var stateName = stateGroup.Key;
                        using (Sb.Case($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}", braces: false))
                        {
                            using (Sb.Switch("trigger"))
                            {
                                foreach (var transition in stateGroup)
                                {
                                    using (Sb.Case($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}", braces: false))
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                            Sb.AppendLine($"return EvaluateGuard__{from}__{trig}({PayloadVar});");
                                        }
                                        else
                                        {
                                            Sb.AppendLine("return true;");
                                        }
                                    }
                                }
                                Sb.DefaultReturn("false");
                            }
                        }
                    }
                    Sb.DefaultReturn("false");
                }
            }
        }
        Sb.AppendLine();
    }

    private void WritePayloadMap(string triggerTypeForUsage)
    {
        Sb.AppendLine($"private static readonly Dictionary<{triggerTypeForUsage}, Type> {PayloadMapField} = new()");
        Sb.AppendLine("{");
        using (Sb.Indent())
        {
            foreach (var kvp in Model.TriggerPayloadTypes)
            {
                var triggerName = kvp.Key;
                var payloadTypeName = kvp.Value;
                var typeForTypeof = TypeHelper.FormatForTypeof(payloadTypeName);
                Sb.AppendLine($"{{ {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(triggerName)}, typeof({typeForTypeof}) }},");
            }
        }
        Sb.AppendLine("};");
        Sb.AppendLine();
    }

    private void WriteAsyncAwareGuardCheck(TransitionModel transition)
    {
        if (string.IsNullOrEmpty(transition.GuardMethod)) return;

        if (transition.GuardIsAsync && IsAsyncMachine)
        {
            GuardGenerationHelper.EmitGuardCheck(
                Sb,
                transition,
                GuardResultVar,
                payloadVar: "null",
                isAsync: true,
                wrapInTryCatch: true,
                Model.ContinueOnCapturedContext,
                handleResultAfterTry: true,
                cancellationTokenVar: "cancellationToken",
                treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure);

            WriteAfterGuardEvaluatedHook(transition, GuardResultVar, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));

            using (Sb.Block($"if (!{GuardResultVar})"))
            {
                WriteLogStatement(GeneratedLogLevel.Warning,
                    $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
                WriteLogStatement(GeneratedLogLevel.Warning,
                    $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
                Sb.AppendLine($"{SuccessVar} = false;");
                WriteAfterTransitionHook(transition, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType), success: false);
                EmitGotoEndTryFire();
            }
        }
        else
        {
            WriteGuardCheck(transition, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));
        }
    }

    private void WriteGetPermittedTriggersWithResolver(string stateType, string triggerType)
    {
        // Sync version
        if (!IsAsyncMachine)
        {
            Sb.WriteSummary("Gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            Sb.WriteParam("payloadResolver", "Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.");
            using (Sb.Block($"public {ReadOnlyListType}<{triggerType}> GetPermittedTriggers(Func<{triggerType}, object?> payloadResolver)"))
            {
                Sb.AppendLine("EnsureStarted();");
                Sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
                Sb.AppendLine();

                if (IsHierarchical)
                {
                    // HSM: Walk up parent chain, OR bitmasks, return cached array (zero-alloc)
                    var uniqueTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();

                    Sb.AppendLine("// Walk up parent chain and OR trigger bits based on guards with payload");
                    Sb.AppendLine("int mask = 0;");
                    Sb.AppendLine($"int currentIndex = (int){CurrentStateField};");
                    Sb.AppendLine("int check = currentIndex;");
                    Sb.AppendLine();
                    using (Sb.Block("while (check >= 0)"))
                    {
                        Sb.AppendLine($"var enumState = ({stateType})check;");
                        using (Sb.Switch("enumState"))
                        {
                            var transitionsByFromState = Model.Transitions
                                .GroupBy(t => t.FromState)
                                .OrderBy(g => g.Key);

                            foreach (var stateGroup in transitionsByFromState)
                            {
                                var stateName = stateGroup.Key;
                                using (Sb.Case($"{stateType}.{TypeHelper.EscapeIdentifier(stateName)}"))
                                {
                                    foreach (var transition in stateGroup)
                                    {
                                        var triggerBit = uniqueTriggers.IndexOf(transition.Trigger);
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);

                                            if (transition.GuardExpectsPayload)
                                            {
                                                // Guard needs payload - use resolver
                                                Sb.AppendLine($"var payload_{trig} = payloadResolver({triggerType}.{trig});");
                                                Sb.AppendLine($"if (EvaluateGuard__{from}__{trig}(payload_{trig})) mask |= (1 << {triggerBit});");
                                            }
                                            else
                                            {
                                                // Guard doesn't need payload
                                                Sb.AppendLine($"if (EvaluateGuard__{from}__{trig}(null)) mask |= (1 << {triggerBit});");
                                            }
                                        }
                                        else
                                        {
                                            Sb.AppendLine($"mask |= (1 << {triggerBit}); // {transition.Trigger} (no guard)");
                                        }
                                    }
                                    Sb.AppendLine("break;");
                                }
                            }
                            Sb.DefaultBreak();
                        }
                        Sb.AppendLine("check = (uint)check < (uint)g_parent.Length ? g_parent[check] : -1;");
                    }
                    Sb.AppendLine();
                    Sb.AppendLine("// Return precomputed array based on mask");
                    Sb.AppendLine("return s_perm__Mask[mask];");
                }
                else
                {
                    // Flat FSM
                    using (Sb.Switch(CurrentStateField))
                    {
                        var transitionsByFromState = Model.Transitions
                            .GroupBy(t => t.FromState)
                            .OrderBy(g => g.Key);

                        foreach (var stateGroup in transitionsByFromState)
                        {
                            var stateName = stateGroup.Key;
                            using (Sb.Case($"{stateType}.{TypeHelper.EscapeIdentifier(stateName)}"))
                            {
                                var guarded = stateGroup.Where(t => !string.IsNullOrEmpty(t.GuardMethod)).ToList();
                                var stateFieldSuffix = MakeSafeMemberSuffix(stateName);
                                if (guarded.Count == 0)
                                {
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
                                        Sb.AppendLine($"var p_{i} = payloadResolver({triggerType}.{trig});");
                                        Sb.AppendLine($"if (EvaluateGuard__{from}__{trig}(p_{i})) mask |= {1 << i};");
                                    }
                                    Sb.AppendLine($"return s_perm__{stateFieldSuffix}[mask];");
                                }
                            }
                        }

                        Sb.AppendLine("default:");
                        using (Sb.Indent())
                        {
                            Sb.AppendLine($"return {ArrayEmptyMethod}<{triggerType}>();");
                        }
                    }
                }
            }
            Sb.AppendLine();
        }
        else
        {
            // Async version with resolver
            Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            Sb.WriteParam("payloadResolver", "Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.");
            Sb.WriteParam("cancellationToken", "A token to observe for cancellation requests");
            using (Sb.Block($"public async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersAsync(Func<{triggerType}, object?> payloadResolver, CancellationToken cancellationToken = default)"))
            {
                Sb.AppendLine("EnsureStarted();");
                Sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
                Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                Sb.AppendLine($"var permitted = new List<{triggerType}>();");
                Sb.AppendLine();

                // Evaluate all distinct triggers defined in the model using CanFireWithPayloadAsync
                var distinctTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
                foreach (var trig in distinctTriggers)
                {
                    using (Sb.Block(""))
                    {
                        Sb.AppendLine($"var __trig = {triggerType}.{TypeHelper.EscapeIdentifier(trig)};");
                        Sb.AppendLine("var __payload = payloadResolver(__trig);");
                        using (Sb.Block($"if (await CanFireWithPayloadAsync(__trig, __payload, cancellationToken){GetConfigureAwait()})"))
                        {
                            Sb.AppendLine("permitted.Add(__trig);");
                        }
                    }
                }

                Sb.AppendLine($"return permitted.Count == 0 ? {ArrayEmptyMethod}<{triggerType}>() : permitted.ToArray();");
            }
            Sb.AppendLine();
        }
    }

    // Sync direct-return transition logic (no success var, no labels)
    private void WriteTransitionLogicSyncDirect(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
    {
        // Guard
        if (!string.IsNullOrEmpty(transition.GuardMethod))
        {
            using (Sb.Block($"if (!{transition.GuardMethod}())"))
            {
                Sb.AppendLine("return false;");
            }
        }

        // OnExit (FASTFSM_SAFE_ACTIONS wrapper in sync path)
        if (!transition.IsInternal && ShouldGenerateOnEntryExit() &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
        {
            using (Sb.IfDirective("FASTFSM_SAFE_ACTIONS"))
            {
                using (Sb.Block("try"))
                {
                    Sb.AppendLine($"{fromStateDef.OnExitMethod}();");
                }
                using (Sb.Block("catch (System.OperationCanceledException)"))
                {
                    Sb.AppendLine("return false;");
                }
                using (Sb.Block("catch (System.Exception)"))
                {
                    Sb.AppendLine("return false;");
                }
                Sb.ElseDirective();
                Sb.AppendLine($"{fromStateDef.OnExitMethod}();");
            }
        }

        // State change BEFORE action (policy relies on stateAlreadyChanged)
        if (!transition.IsInternal)
        {
            if (IsHierarchical)
            {
                WriteHierarchicalStateChangeWithDiagnostics(transition.ToState, stateTypeForUsage);
            }
            else
            {
                Sb.AppendLine($"{CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
            }
        }

        // OnEntry with exception policy (sync)
        if (!transition.IsInternal && ShouldGenerateOnEntryExit() &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
        {
            EmitOnEntryWithExceptionPolicy(toStateDef, null, transition.FromState, transition.ToState, transition.Trigger);
        }

        // Action with exception policy (sync) AFTER state change
        if (!string.IsNullOrEmpty(transition.ActionMethod))
        {
            EmitActionWithExceptionPolicy(transition, transition.FromState, transition.ToState);
        }

        Sb.AppendLine("return true;");
    }

    #region Helper Methods (moved from base class)

    protected new void WriteMethodAttribute() =>
        Sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

    protected new string GetTypeNameForUsage(string fullyQualifiedName) =>
        TypeHelper.FormatTypeForUsage(fullyQualifiedName, useGlobalPrefix: false);

    protected new string GetConfigureAwait() =>
        AsyncGenerationHelper.GetConfigureAwait(IsAsyncMachine, Model.ContinueOnCapturedContext);

    // Helper to mirror MakeSafeMemberSuffix - keeping for compatibility

    #endregion
}
