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
/// Unified state machine generator that handles all variants through feature flags
/// instead of inheritance hierarchy.
/// Phase 2: Implementing Core/Basic logic directly
/// </summary>
public class UnifiedStateMachineGenerator : StateMachineCodeGenerator
{
    // Feature detection flags
    private bool HasPayload => Model.GenerationConfig.HasPayload;
    private bool HasExtensions => Model.GenerationConfig.HasExtensions;
    private bool ExtensionsOn => HasExtensions || IsExtensionsVariant();
    private bool HasOnEntryExit => Model.GenerationConfig.HasOnEntryExit;
    private bool IsHierarchical => Model.HierarchyEnabled;
    private bool HasMultiPayload => Model.TriggerPayloadTypes?.Any() == true;

    // Track if smCtx variable has been created in current transition context
    private bool _smCtxCreated = false;

    // Extensions feature writer (used when HasExtensions)
    private readonly ExtensionsFeatureWriter _ext = new();

    public UnifiedStateMachineGenerator(StateMachineModel model) : base(model)
{
        // Unified generator handles all variants directly (Pure/Basic/WithPayload/WithExtensions/Full)
}

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
        void WriteInner()
    {
            // Write interface and the main class
            WriteInterface(className, stateTypeForUsage, triggerTypeForUsage);
            WriteClass(className, stateTypeForUsage, triggerTypeForUsage);
        }

        if (Model.ContainerClasses.Count == 0)
    {
            WriteInner();
            return;
        }

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

        WriteNested(0);
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
            // Write class content
            WriteFields(className);
            WriteConstructor(stateType, className);
            WriteStartMethods();
            WriteInitialEntryMethods(stateType);
            WriteTryFireMethods(stateType, triggerType);
            WriteFireMethods(stateType, triggerType);
            WriteCanFireMethods(stateType, triggerType);
            WriteGetPermittedTriggersMethods(stateType, triggerType);
            if (ExtensionsOn)
        {
                _ext.WriteManagementMethods(Sb);
            }
            WriteStructuralApiMethods(stateType, triggerType);
            WriteHierarchyMethods(stateType, triggerType);

            // Emit per-transition guard helpers for sync machines
            if (!IsAsyncMachine)
            {
                WriteGuardHelperMethods(stateType, triggerType);
            }
        }
    }

    // Generates private helper methods EvaluateGuard__<FROM>__<TRIGGER>(object? payload)
    // and Guard__<FROM>__<TRIGGER>(object? payload) for sync machines.
    private void WriteGuardHelperMethods(string stateTypeForUsage, string triggerTypeForUsage)
    {
        var transitionsWithGuards = Model.Transitions.Where(t => !string.IsNullOrEmpty(t.GuardMethod)).ToList();
        if (transitionsWithGuards.Count == 0) return;

        Sb.AppendLine();
        Sb.AppendLine("// Guard evaluation helpers (sync)");
        foreach (var tr in transitionsWithGuards)
        {
            var from = TypeHelper.EscapeIdentifier(tr.FromState);
            var trig = TypeHelper.EscapeIdentifier(tr.Trigger);
            var guardWrapper = $"Guard__{from}__{trig}";
            var evalName = $"EvaluateGuard__{from}__{trig}";

            // Core guard invocation without try/catch
            Sb.AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
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
            Sb.AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            using (Sb.Block($"private bool {evalName}(object? payload)"))
            {
                Sb.AppendLine("#if FASTFSM_SAFE_GUARDS");
                using (Sb.Block("try"))
                {
                    Sb.AppendLine($"return {guardWrapper}(payload);");
                }
                Sb.AppendLine("catch (System.OperationCanceledException) { return false; }");
                Sb.AppendLine("catch (System.Exception) { return false; }");
                Sb.AppendLine("#else");
                Sb.AppendLine($"return {guardWrapper}(payload);");
                Sb.AppendLine("#endif");
            }

            Sb.AppendLine();
        }
    }

    private void WriteFields(string className)
    {
        // Instance ID for async machines
        if (IsAsyncMachine)
    {
            Sb.AppendLine("private readonly string _instanceId = Guid.NewGuid().ToString();");
            Sb.AppendLine();
        }

        // Logger field
        WriteLoggerField(className);

        // Extensions fields
        if (ExtensionsOn)
    {
            _ext.WriteFields(Sb);
        }

        // Multi-payload: emit trigger→payload type map for validation
        if (HasPayload && HasMultiPayload)
    {
            WritePayloadMap(GetTypeNameForUsage(Model.TriggerType));
        }

        // HSM arrays
        if (IsHierarchical)
    {
            GenerateActionIdEnum(); // Generate ActionId enum for zero-allocation dispatch
            GenerateAsyncActionIdEnum(); // Generate AsyncActionId enum for async actions
            WriteHierarchyArrays(GetTypeNameForUsage(Model.StateType));
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
            var stateEnumName = TypeHelper.EscapeIdentifier(stateNameRaw);
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
                if (unguarded.Count == 0)
                {
                    Sb.AppendLine($"private static readonly {triggerTypeForUsage}[] s_perm__{stateFieldSuffix} = System.Array.Empty<{triggerTypeForUsage}>();");
                }
                else
                {
                    Sb.AppendLine($"private static readonly {triggerTypeForUsage}[] s_perm__{stateFieldSuffix} = new {triggerTypeForUsage}[] {{ {string.Join(", ", unguarded)} }};");
                }
                Sb.AppendLine();
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
                        if (((mask >> i) & 1) != 0)
                        {
                            var tr = guarded[i];
                            entries.Add($"{triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(tr.Trigger)}");
                        }
                    }
                    entries = entries.Distinct().ToList();
                    if (entries.Count == 0)
                    {
                        rows.Add($"System.Array.Empty<{triggerTypeForUsage}>()");
                    }
                    else
                    {
                        rows.Add($"new {triggerTypeForUsage}[] {{ {string.Join(", ", entries)} }}");
                    }
                }
                Sb.AppendLine($"private static readonly {triggerTypeForUsage}[][] s_perm__{stateFieldSuffix} = new {triggerTypeForUsage}[][] {{ {string.Join(", ", rows)} }};");
                Sb.AppendLine();
            }
        }
    }

    private static string MakeSafeMemberSuffix(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "_";
        // Remove leading '@' if present (verbatim identifier)
        if (raw.Length > 0 && raw[0] == '@') raw = raw.Substring(1);
        // Replace invalid identifier chars with '_'
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            else sb.Append('_');
        }
        var s = sb.ToString();
        // Prefix if starts with digit
        if (s.Length == 0 || char.IsDigit(s[0])) s = "_" + s;
        // If keyword, add trailing underscore to avoid collisions
        switch (s)
        {
            case "class": case "return": case "void": case "int": case "interface": case "namespace":
            case "new": case "throw": case "break": case "continue": case "goto":
                s = s + "_"; break;
        }
        return s;
    }

    private void WriteConstructor(string stateTypeForUsage, string className)
{
        var extras = new List<string>();
        if (ExtensionsOn)
    {
            extras.Add("IEnumerable<IStateMachineExtension>? extensions = null");
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
                Sb.AppendLine("// Initialize history tracking array with -1 (no history)");
                Sb.AppendLine("_lastActiveChild = new int[s_initialChild.Length];");
                Sb.AppendLine("for (int i = 0; i < _lastActiveChild.Length; i++) _lastActiveChild[i] = -1;");
            }
            WriteLoggerAssignment();
            if (ExtensionsOn)
        {
                _ext.WriteConstructorBody(Sb, ShouldGenerateLogging);
            }
        }
        Sb.AppendLine();
}

    private void WriteStartMethods()
{
        if (IsHierarchical || HasOnEntryExit)
    {
            WriteStartMethod();
        }
}

    private void WriteInitialEntryMethods(string stateType)
{
        if (ShouldGenerateInitialOnEntry())
    {
            if (IsAsyncMachine)
        {
                WriteOnInitialEntryAsyncMethod(stateType);
            }
            else
        {
                WriteOnInitialEntryMethod(stateType);
            }
        }
}

    private void WriteOnInitialEntryAsyncMethod(string stateTypeForUsage)
{
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();

        if (!statesWithParameterlessOnEntry.Any())
    {
            return; // Nothing to emit for async initial entry
        }

        using (Sb.Block("protected override async ValueTask OnInitialEntryAsync(System.Threading.CancellationToken cancellationToken = default)"))
    {
        if (Model.HierarchyEnabled)
    {
            Sb.AppendLine("// Build entry chain from root to current leaf");
            Sb.AppendLine($"var entryChain = new List<{stateTypeForUsage}>();");
            Sb.AppendLine($"int currentIdx = (int){CurrentStateField};");
            Sb.AppendLine();
            using (Sb.Block("while (currentIdx >= 0)"))
        {
                Sb.AppendLine($"entryChain.Add(({stateTypeForUsage})currentIdx);");
                Sb.AppendLine("if ((uint)currentIdx >= (uint)s_parent.Length) break;");
                Sb.AppendLine("currentIdx = s_parent[currentIdx];");
            }
            Sb.AppendLine();
            Sb.AppendLine("entryChain.Reverse();");
            Sb.AppendLine();
            using (Sb.Block("foreach (var state in entryChain)"))
            using (Sb.Block("switch (state)"))
        {
            foreach (var stateEntry in statesWithParameterlessOnEntry)
        {
                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                using (Sb.Block(""))
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
                    Sb.AppendLine("break;");
                }
            }
                Sb.AppendLine("default: break;");
            }
        }
        else
    {
            using (Sb.Block($"switch ({CurrentStateField})"))
        {
            foreach (var stateEntry in statesWithParameterlessOnEntry)
        {
                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                using (Sb.Block(""))
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
                    Sb.AppendLine("break;");
                }
            }
                Sb.AppendLine("default: break;");
            }
        }
        Sb.AppendLine();
}
}


    protected  void WriteOnInitialEntryMethod(string stateTypeForUsage)
{
        var statesWithParameterlessOnEntry = Model.States.Values
            .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
            .ToList();
        if (!statesWithParameterlessOnEntry.Any())
    {
            return;
        }

        using (Sb.Block("protected override void OnInitialEntry()"))
    {
        if (Model.HierarchyEnabled)
    {
            Sb.AppendLine("var entryChain = new List<" + stateTypeForUsage + ">();");
            Sb.AppendLine($"int currentIdx = (int){CurrentStateField};");
            using (Sb.Block("while (currentIdx >= 0)"))
        {
                Sb.AppendLine($"entryChain.Add(({stateTypeForUsage})currentIdx);");
                Sb.AppendLine("if ((uint)currentIdx >= (uint)s_parent.Length) break;");
                Sb.AppendLine("currentIdx = s_parent[currentIdx];");
            }
            Sb.AppendLine("entryChain.Reverse();");
            using (Sb.Block("foreach (var state in entryChain)"))
            using (Sb.Block("switch (state)"))
        {
            foreach (var stateEntry in statesWithParameterlessOnEntry)
        {
                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                using (Sb.Indent())
            {
                    Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                    Sb.AppendLine("try { ");
                    Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                    Sb.AppendLine("} catch (System.OperationCanceledException) { } catch (System.Exception) { }");
                    Sb.AppendLine("#else");
                    Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                    Sb.AppendLine("#endif");
                    Sb.AppendLine("break;");
                }
            }
                Sb.AppendLine("default: break;");
            }
        }
        else
    {
            using (Sb.Block($"switch ({CurrentStateField})"))
        {
            foreach (var stateEntry in statesWithParameterlessOnEntry)
        {
                Sb.AppendLine($"case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateEntry.Name)}:");
                using (Sb.Indent())
            {
                    Sb.AppendLine("#if FASTFSM_SAFE_ACTIONS");
                    Sb.AppendLine("try { ");
                    Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                    Sb.AppendLine("} catch (System.OperationCanceledException) { } catch (System.Exception) { }");
                    Sb.AppendLine("#else");
                    Sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                    Sb.AppendLine("#endif");
                    Sb.AppendLine("break;");
                }
            }
                Sb.AppendLine("default: break;");
            }
        }
        }
        Sb.AppendLine();
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
                    if (payloadType != "object")
                {
                        WriteMethodAttribute();
                        using (Sb.Block($"public async ValueTask<bool> TryFireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)"))
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
                    if (payloadType != "object")
                {
                        WriteMethodAttribute();
                        using (Sb.Block($"public bool TryFire({triggerType} trigger, {payloadType} payload)"))
                    {
                            Sb.AppendLine("EnsureStarted();");
                            Sb.AppendLine("return TryFireInternal(trigger, payload);");
                        }
                        Sb.AppendLine();
                    }
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
                    Sb.AppendLine($"if (!await TryFireAsync(trigger, payload, cancellationToken){GetConfigureAwait()})");
                    using (Sb.Block(""))
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
                Sb.AppendLine($"    public void Fire<TPayload>({triggerType} trigger, TPayload payload)");
                Sb.AppendLine("    {");
                Sb.AppendLine("        EnsureStarted();");
                Sb.AppendLine("        if (!TryFire(trigger, payload))");
                Sb.AppendLine("        {");
                Sb.AppendLine("            throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                Sb.AppendLine("        }");
                Sb.AppendLine("    }");
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

    private void WriteTryFireMethodAsync(string stateType, string triggerType)
{
        WriteMethodAttribute();
        using (Sb.Block($"protected override async ValueTask<bool> TryFireInternalAsync({triggerType} trigger, object? payload, CancellationToken cancellationToken = default)"))
    {
        Sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
        Sb.AppendLine();

        if (!Model.Transitions.Any())
    {
            Sb.AppendLine($"return false; {NoTransitionsComment}");
            return;
        }

        // For multi-payload: validate payload type upfront (no runtime branching later)
        if (HasPayload && HasMultiPayload)
    {
            Sb.AppendLine("        // Payload-type validation for multi-payload variant");
            Sb.AppendLine($"        if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && (payload == null || !expectedType.IsInstanceOfType(payload)))");
            Sb.AppendLine("        {");
            if (ShouldGenerateLogging)
        {
                WriteLogStatement("Warning", $"PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", payload?.GetType().Name ?? \"null\");");
            }
            Sb.AppendLine("            return false; // wrong payload type");
            Sb.AppendLine("        }");
            Sb.AppendLine();
        }

        Sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
        Sb.AppendLine($"bool {SuccessVar} = false;");
        Sb.AppendLine();

        // Use payload-aware or core transition logic based on feature flags
        if (HasPayload)
    {
            WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
        {
                _smCtxCreated = false;  // Reset flag for each transition
                WriteTransitionLogicPayloadAsync(transition, stateType, triggerType);
            });
        }
        else
    {
            WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
        {
                _smCtxCreated = false;  // Reset flag for each transition
                WriteTransitionLogic(transition, stateType, triggerType);
            });
        }

        Sb.AppendLine($"{EndOfTryFireLabel}:;");
        Sb.AppendLine();

        // Log failure if needed
        if (ShouldGenerateLogging)
    {
            Sb.AppendLine($"if (!{SuccessVar})");
            using (Sb.Block(""))
        {
            WriteLogStatement("Warning", $"TransitionFailed(_logger, _instanceId, {OriginalStateVar}.ToString(), trigger.ToString());");
            // Extensions failure hook
            if (ExtensionsOn)
        {
                Sb.AppendLine($"var failCtx = new StateMachineContext<{stateType}, {triggerType}>(");
                Sb.AppendLine("    Guid.NewGuid().ToString(),");
                Sb.AppendLine($"    {OriginalStateVar},");
                Sb.AppendLine("    trigger,");
                Sb.AppendLine($"    {OriginalStateVar},");
                Sb.AppendLine("    payload);");
                Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, failCtx, false);");
                }
            }
            }

        Sb.AppendLine($"return {SuccessVar};");
        }
        Sb.AppendLine();
}

    private void WriteTryFireMethodSync(string stateType, string triggerType)
{
        WriteMethodAttribute();
        using (Sb.Block($"protected override bool TryFireInternal({triggerType} trigger, object? payload)"))
    {

        if (!Model.Transitions.Any())
    {
            Sb.AppendLine($"return false; {NoTransitionsComment}");
            return;
        }

        // For sync: choose writer depending on features
        if (HasPayload && HasMultiPayload)
    {
            Sb.AppendLine("        // Payload-type validation for multi-payload variant");
            Sb.AppendLine($"        if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && (payload == null || !expectedType.IsInstanceOfType(payload)))");
            Sb.AppendLine("        {");
            if (ShouldGenerateLogging)
        {
                WriteLogStatement("Warning", $"PayloadValidationFailed(_logger, _instanceId, trigger.ToString(), expectedType?.Name ?? \"unknown\", payload?.GetType().Name ?? \"null\");");
            }
            Sb.AppendLine("            return false; // wrong payload type");
            Sb.AppendLine("        }");
            Sb.AppendLine();
        }

        var writer = HasPayload
            ? (Action<TransitionModel, string, string>)WriteTransitionLogicPayloadSyncDirect
            : (Action<TransitionModel, string, string>)WriteTransitionLogicSyncCore;

        WriteTryFireStructureDispatcher(stateType, triggerType, (transition, stateType, triggerType) =>
    {
            _smCtxCreated = false;  // Reset flag for each transition
            writer(transition, stateType, triggerType);
        });

        }
        Sb.AppendLine();

        // Generate public wrapper for sync
        WriteMethodAttribute();
        using (Sb.Block($"public override bool TryFire({triggerType} trigger, object? payload = null)"))
    {
        Sb.AppendLine("EnsureStarted();");
        if (ExtensionsOn)
    {
            // For ExtensionsOn, transitions already handle all extension calls including failure cases
            // The catch block in WriteTransitionLogicSyncWithExtensions already calls RunAfterTransition with false
            // So no additional failure hook is needed here - just pass through
            Sb.AppendLine("return TryFireInternal(trigger, payload);");
        }
        else
    {
            // For non-extension variants, simple pass-through
            Sb.AppendLine("return TryFireInternal(trigger, payload);");
        }
        Sb.AppendLine();
}
}

    // Sync core logic using base non-payload implementation (includes hooks)
    private void WriteTransitionLogicSyncCore(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
{
        // For WithExtensions variant, we need special exception handling
        if (ExtensionsOn)
    {
            Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
            Sb.AppendLine($"// DEBUG: Using WriteTransitionLogicSyncWithExtensions for {Model.ClassName}");
            Sb.AppendLine("#endif");
            WriteTransitionLogicSyncWithExtensions(transition, stateTypeForUsage, triggerTypeForUsage);
        }
        else
    {
            Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
            Sb.AppendLine($"// DEBUG: Using base WriteTransitionLogicForFlatNonPayload for {Model.ClassName}");
            Sb.AppendLine("#endif");
            base.WriteTransitionLogicForFlatNonPayload(transition, stateTypeForUsage, triggerTypeForUsage);
        }
}

    // Special sync transition logic for WithExtensions variant
    // Wraps entire transition in try-catch to handle exceptions properly
    private void WriteTransitionLogicSyncWithExtensions(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
{
        var hasOnEntryExit = ShouldGenerateOnEntryExit();

        // Create context for hooks
        Sb.AppendLine($"var smCtx = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
        Sb.AppendLine("    Guid.NewGuid().ToString(),");
        Sb.AppendLine($"    {CurrentStateField},");
        Sb.AppendLine("    trigger,");
        Sb.AppendLine($"    {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
        Sb.AppendLine("    payload);");
        Sb.AppendLine();

        // Hook: Before transition
        Sb.AppendLine("_extensionRunner.RunBeforeTransition(_extensions, smCtx);");
        Sb.AppendLine();

        // Comment about exception handling
        Sb.AppendLine("#if DEBUG || FASTFSM_DEBUG_GENERATED_COMMENTS");
        Sb.AppendLine($"// FSM_DEBUG: No handler for {Model.ClassName}, action={transition.ActionMethod}");
        Sb.AppendLine("#endif");

        // All transition logic in try-catch
        Sb.AppendLine("try {");

        // Guard check (if present)
        if (!string.IsNullOrEmpty(transition.GuardMethod))
    {
            // Notify extensions about guard evaluation
            Sb.AppendLine($"    _extensionRunner.RunGuardEvaluation(_extensions, smCtx, \"{transition.GuardMethod}\");");
            Sb.AppendLine($"    var guardResult = {transition.GuardMethod}();");
            Sb.AppendLine($"    _extensionRunner.RunGuardEvaluated(_extensions, smCtx, \"{transition.GuardMethod}\", guardResult);");
            Sb.AppendLine("    if (!guardResult)");
            Sb.AppendLine("    {");
            Sb.AppendLine("        _extensionRunner.RunAfterTransition(_extensions, smCtx, false);");
            Sb.AppendLine("        return false;");
            Sb.AppendLine("    }");
        }

        // UML-friendly order: OnExit → Action → State change → OnEntry
        // All in try block; exceptions surface as failed transition

        // OnExit (if applicable)
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
    {
            Sb.AppendLine($"    {fromStateDef.OnExitMethod}();");
        }

        // Action (if present) - BEFORE OnEntry (Extensions order)
        if (!string.IsNullOrEmpty(transition.ActionMethod))
    {
            Sb.AppendLine($"    {transition.ActionMethod}();");
        }

        // State change BEFORE OnEntry (so OnEntry runs in target state)
        if (!transition.IsInternal)
    {
            Sb.AppendLine($"    {CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
        }

        // OnEntry (if applicable) - AFTER state change
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
            !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
    {
            Sb.AppendLine($"    {toStateDef.OnEntryMethod}();");
        }

        Sb.AppendLine("}");
        Sb.AppendLine("catch {");
        Sb.AppendLine("    _extensionRunner.RunAfterTransition(_extensions, smCtx, false);");
        Sb.AppendLine("    return false;");
        Sb.AppendLine("}");

        // Success
        Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, smCtx, true);");
        Sb.AppendLine("return true;");
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
            base.WriteTryFireStructure(stateType, triggerType, writeTransitionLogic);
        }
}

    private void WriteTryFireStructureWithExtensions(string stateType, string triggerType, Action<TransitionModel, string, string> writeTransitionLogic)
{
        // Custom implementation that notifies extensions even when no transition is found
        if (!Model.Transitions.Any())
    {
            // No transitions at all - notify extensions and return false
            Sb.AppendLine("// No transitions defined - notify extensions");
            Sb.AppendLine($"var failCtx = new StateMachineContext<{stateType}, {triggerType}>(");
            Sb.AppendLine("    Guid.NewGuid().ToString(),");
            Sb.AppendLine($"    {CurrentStateField},");
            Sb.AppendLine("    trigger,");
            Sb.AppendLine($"    {CurrentStateField},");
            Sb.AppendLine("    payload);");
            Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, failCtx, false);");
            Sb.AppendLine("return false;");
            return;
        }

        // Generate our own structure (don't call base which would duplicate)
        var sortedTransitions = Model.Transitions
            .Select((t, index) => new { Transition = t, Index = index })
            .OrderByDescending(x => x.Transition.Priority)
            .ThenBy(x => x.Index)
            .Select(x => x.Transition);

        var grouped = sortedTransitions.GroupBy(t => t.FromState);

        Sb.AppendLine($"switch ({CurrentStateField}) {{");
        foreach (var state in grouped)
    {
            Sb.AppendLine($"    case {stateType}.{TypeHelper.EscapeIdentifier(state.Key)}: {{");

            var triggerGroups = state.GroupBy(t => t.Trigger);
            Sb.AppendLine("        switch (trigger) {");

            foreach (var triggerGroup in triggerGroups)
        {
                Sb.AppendLine($"            case {triggerType}.{TypeHelper.EscapeIdentifier(triggerGroup.Key)}: {{");

                foreach (var tr in triggerGroup)
            {
                    Sb.AppendLine($"                // Transition: {tr.FromState} -> {tr.ToState} (Priority: {tr.Priority})");
                    writeTransitionLogic(tr, stateType, triggerType);
                    break; // Only first matching transition
                }

                Sb.AppendLine("            }");
            }

            Sb.AppendLine("            default: break;");
            Sb.AppendLine("        }");
            Sb.AppendLine("        break;");
            Sb.AppendLine("    }");
        }
        Sb.AppendLine("    default: break;");
        Sb.AppendLine("}");
        Sb.AppendLine();

        // No transition found - notify extensions
        Sb.AppendLine("// No matching transition - notify extensions");
        Sb.AppendLine($"var noTransitionCtx = new StateMachineContext<{stateType}, {triggerType}>(");
        Sb.AppendLine("    Guid.NewGuid().ToString(),");
        Sb.AppendLine($"    {CurrentStateField},");
        Sb.AppendLine("    trigger,");
        Sb.AppendLine($"    {CurrentStateField},");
        Sb.AppendLine("    payload);");
        Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, noTransitionCtx, false);");
        Sb.AppendLine("return false;");
}


    private void WriteCanFireMethods(string stateType, string triggerType)
{
        // Base CanFire without payload
        WriteCanFireMethod(stateType, triggerType);

        // Payload-aware CanFire overloads
        if (HasPayload)
    {
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
                        Sb.AppendLine($"    public async ValueTask<bool> CanFireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)");
                        Sb.AppendLine("    {");
                        Sb.AppendLine("        EnsureStarted();");
                        Sb.AppendLine($"        return await CanFireWithPayloadAsync(trigger, payload, cancellationToken){GetConfigureAwait()};");
                        Sb.AppendLine("    }");
                        Sb.AppendLine();
                    }
                }
                else
            {
                    WriteMethodAttribute();
                    Sb.AppendLine($"    public async ValueTask<bool> CanFireAsync<TPayload>({triggerType} trigger, TPayload payload, CancellationToken cancellationToken = default)");
                    Sb.AppendLine("    {");
                    Sb.AppendLine("        EnsureStarted();");
                    Sb.AppendLine($"        return await CanFireWithPayloadAsync(trigger, payload, cancellationToken){GetConfigureAwait()};");
                    Sb.AppendLine("    }");
                    Sb.AppendLine();
                }

                // Sync wrappers for async machines: throw on sync CanFire(payload) to preserve API parity
                if (!HasMultiPayload)
            {
                    var single = Model.DefaultPayloadType;
                    if (!string.IsNullOrEmpty(single))
                {
                        var payloadType = GetTypeNameForUsage(single!);
                        WriteMethodAttribute();
                        Sb.AppendLine($"    public bool CanFire({triggerType} trigger, {payloadType} payload)");
                        Sb.AppendLine("    {");
                        Sb.AppendLine("        throw new SyncCallOnAsyncMachineException();");
                        Sb.AppendLine("    }");
                        Sb.AppendLine();
                    }
                }
                else
            {
                    WriteMethodAttribute();
                    Sb.AppendLine($"    public bool CanFire<TPayload>({triggerType} trigger, TPayload payload)");
                    Sb.AppendLine("    {");
                    Sb.AppendLine("        throw new SyncCallOnAsyncMachineException();");
                    Sb.AppendLine("    }");
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
                    if (!string.IsNullOrEmpty(single))
                {
                        var payloadType = GetTypeNameForUsage(single!);
                        WriteMethodAttribute();
                        Sb.AppendLine($"    public bool CanFire({triggerType} trigger, {payloadType} payload)");
                        Sb.AppendLine("    {");
                        Sb.AppendLine("        EnsureStarted();");
                        Sb.AppendLine($"        return CanFireWithPayload(trigger, payload);");
                        Sb.AppendLine("    }");
                        Sb.AppendLine();
                    }
                }
                else
            {
                    WriteMethodAttribute();
                    Sb.AppendLine($"    public bool CanFire<TPayload>({triggerType} trigger, TPayload payload)");
                    Sb.AppendLine("    {");
                    Sb.AppendLine("        EnsureStarted();");
                    Sb.AppendLine($"        return CanFireWithPayload(trigger, payload);");
                    Sb.AppendLine("    }");
                    Sb.AppendLine();
                }
            }
        }
}

    protected override void WriteCanFireMethod(string stateTypeForUsage, string triggerTypeForUsage)
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
        Sb.AppendLine("    /// <param name=\"trigger\">The trigger to check</param>");
        Sb.AppendLine("    /// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        Sb.AppendLine("    /// <returns>True if the trigger can be fired, false otherwise</returns>");

        Sb.AppendLine($"    protected override async ValueTask<bool> CanFireInternalAsync({triggerTypeForUsage} trigger, CancellationToken cancellationToken = default)");
        Sb.AppendLine("    {");
        Sb.AppendLine("        cancellationToken.ThrowIfCancellationRequested();");
        Sb.AppendLine();

        if (Model.HierarchyEnabled)
    {
            Sb.AppendLine($"        int check = (int){CurrentStateField};");
            Sb.AppendLine("        while (check >= 0)");
            Sb.AppendLine("        {");
            Sb.AppendLine($"            var state = ({stateTypeForUsage})check;");
            Sb.AppendLine("            switch (state)");
            Sb.AppendLine("            {");
            var grouped = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);
            foreach (var group in grouped)
        {
                Sb.AppendLine($"                case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(group.Key)}:");
                Sb.AppendLine("                {");
                Sb.AppendLine("                    switch (trigger)");
                Sb.AppendLine("                    {");
                foreach (var transition in group)
            {
                    Sb.AppendLine($"                        case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                    Sb.AppendLine("                        {");
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
                            WriteGuardCall(transition, "guardResult", "null", throwOnException: false);
                        }
                        Sb.AppendLine("                            return guardResult;");
                    }
                    else
                {
                        Sb.AppendLine("                            return true;");
                    }
                    Sb.AppendLine("                        }");
                }
                Sb.AppendLine("                        default: break;");
                Sb.AppendLine("                    }");
                Sb.AppendLine("                    break;");
                Sb.AppendLine("                }");
            }
            Sb.AppendLine("                default: break;");
            Sb.AppendLine("            }");
            Sb.AppendLine("            check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
            Sb.AppendLine("        }");
            Sb.AppendLine("        return false;");
        }
        else
    {
            Sb.AppendLine($"        switch ({CurrentStateField})");
            Sb.AppendLine("        {");
            var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);
            foreach (var stateName in allHandledFromStates)
        {
                Sb.AppendLine($"            case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                Sb.AppendLine("            {");
                Sb.AppendLine("                switch (trigger)");
                Sb.AppendLine("                {");
                var transitionsFromThisState = Model.Transitions.Where(t => t.FromState == stateName);
                foreach (var transition in transitionsFromThisState)
            {
                    Sb.AppendLine($"                    case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                    Sb.AppendLine("                    {");
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
                            WriteGuardCall(transition, "guardResult", "null", throwOnException: false);
                        }
                        Sb.AppendLine("                        return guardResult;");
                    }
                    else
                {
                        Sb.AppendLine("                        return true;");
                    }
                    Sb.AppendLine("                    }");
                }
                Sb.AppendLine("                    default: return false;");
                Sb.AppendLine("                }");
                Sb.AppendLine("            }");
            }
            Sb.AppendLine("            default: return false;");
            Sb.AppendLine("        }");
        }
        Sb.AppendLine("    }");
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
                                                var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                                var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                                Sb.AppendLine($"var guardResult = EvaluateGuard__{from}__{trig}(null);");
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
                                            var from = TypeHelper.EscapeIdentifier(transition.FromState);
                                            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
                                            Sb.AppendLine($"var guardResult = EvaluateGuard__{from}__{trig}(null);");
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
        Sb.AppendLine("    /// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
        Sb.AppendLine("    /// <returns>List of triggers that can be fired in the current state</returns>");

        Sb.AppendLine($"    protected override async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersInternalAsync(CancellationToken cancellationToken = default)");
        Sb.AppendLine("    {");
        Sb.AppendLine("        cancellationToken.ThrowIfCancellationRequested();");
        Sb.AppendLine();
        if (Model.HierarchyEnabled)
    {
            Sb.AppendLine($"        var permitted = new List<{triggerType}>();");
            Sb.AppendLine($"        int check = (int){CurrentStateField};");
            Sb.AppendLine("        while (check >= 0)");
            Sb.AppendLine("        {");
            Sb.AppendLine($"            var state = ({stateType})check;");
            Sb.AppendLine("            switch (state)");
            Sb.AppendLine("            {");
            var transitionsByFromState = Model.Transitions
                .GroupBy(t => t.FromState)
                .OrderBy(g => g.Key);
            foreach (var stateGroup in transitionsByFromState)
        {
                var stateName = stateGroup.Key;
                Sb.AppendLine($"                case {stateType}.{TypeHelper.EscapeIdentifier(stateName)}:");
                Sb.AppendLine("                {");
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
                            Sb.AppendLine("                    if (canFire)");
                            Sb.AppendLine("                    {");
                            Sb.AppendLine($"                        permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                            Sb.AppendLine("                    }");
                        }
                        else
                    {
                            Sb.AppendLine($"                    var canFire = {transition.GuardMethod}();");
                            Sb.AppendLine("                    if (canFire)");
                            Sb.AppendLine("                    {");
                            Sb.AppendLine($"                        permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                            Sb.AppendLine("                    }");
                        }
                    }
                    else
                {
                        Sb.AppendLine($"                    permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                    }
                }
                Sb.AppendLine("                    break;");
                Sb.AppendLine("                }");
            }
            Sb.AppendLine("                default: break;");
            Sb.AppendLine("            }");
            Sb.AppendLine("            check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
            Sb.AppendLine("        }");
            Sb.AppendLine($"        return permitted.Count == 0 ? {ArrayEmptyMethod}<{triggerType}>() : permitted.ToArray();");
        }
        else
    {
            Sb.AppendLine($"        switch ({CurrentStateField})");
            Sb.AppendLine("        {");

            var transitionsByFromState = Model.Transitions
                .GroupBy(t => t.FromState)
                .OrderBy(g => g.Key);

            foreach (var stateGroup in transitionsByFromState)
        {
                var stateName = stateGroup.Key;
                Sb.AppendLine($"            case {stateType}.{TypeHelper.EscapeIdentifier(stateName)}:");
                Sb.AppendLine("            {");

                // Check if any transition has a guard
                var hasAsyncGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync);

                if (!hasAsyncGuards && stateGroup.All(t => string.IsNullOrEmpty(t.GuardMethod)))
            {
                    // No guards - return static array
                    var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                    if (triggers.Any())
                {
                        var triggerList = string.Join(", ", triggers.Select(t => $"{triggerType}.{TypeHelper.EscapeIdentifier(t)}"));
                        Sb.AppendLine($"                return new {triggerType}[] {{ {triggerList} }};");
                    }
                    else
                {
                        Sb.AppendLine($"                return {ArrayEmptyMethod}<{triggerType}>();");
                    }
                }
                else
            {
                    // Has guards - build list dynamically
                    Sb.AppendLine($"                var permitted = new List<{triggerType}>();");

                    foreach (var transition in stateGroup)
                {
                        Sb.AppendLine("                {");

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

                                Sb.AppendLine("                    if (canFire)");
                                Sb.AppendLine("                    {");
                                Sb.AppendLine($"                        permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                Sb.AppendLine("                    }");
                            }
                            else
                        {
                                Sb.AppendLine($"                    var canFire = {transition.GuardMethod}();");
                                Sb.AppendLine("                    if (canFire)");
                                Sb.AppendLine("                    {");
                                Sb.AppendLine($"                        permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                Sb.AppendLine("                    }");
                            }
                        }
                        else
                    {
                            Sb.AppendLine($"                    permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                        }

                        Sb.AppendLine("                }");
                    }

                    Sb.AppendLine("                return permitted;");
                }

                Sb.AppendLine("            }");
            }

            Sb.AppendLine("            default:");
            Sb.AppendLine($"                return {ArrayEmptyMethod}<{triggerType}>();");
            Sb.AppendLine("        }");
        }
        Sb.AppendLine("    }");
        Sb.AppendLine();
}

    protected override bool ShouldGenerateInitialOnEntry()
{
        // Variants were removed; gate solely on callbacks presence
        return Model.GenerationConfig.HasOnEntryExit;
}

    protected override bool ShouldGenerateOnEntryExit()
{
        // Variants were removed; gate solely on callbacks presence
        return Model.GenerationConfig.HasOnEntryExit;
}

    // Use Core-like transition logic to ensure proper token passing and hooks
    protected  void WriteTransitionLogic(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
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
                Sb.AppendLine("try");
                Sb.AppendLine("{");
                WriteOnExitCall(fromStateDef, null);
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
                Sb.AppendLine("}");
                Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
                Sb.AppendLine("{");
                Sb.AppendLine($"{SuccessVar} = false;");
                WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
                Sb.AppendLine($"goto {EndOfTryFireLabel};");
                Sb.AppendLine("}");
            }
            else
        {
                WriteOnExitCall(fromStateDef, null);
                WriteLogStatement("Debug",
                    $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef.OnExitMethod}\", \"{transition.FromState}\");");
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
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        Sb.AppendLine($"goto {EndOfTryFireLabel};");
}

    // Extension hooks (emitted only when HasExtensions)
    protected override void WriteBeforeTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
        if (!ExtensionsOn) return;
        Sb.AppendLine($"var {HookVarContext} = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
        using (Sb.Indent())
    {
            Sb.AppendLine("Guid.NewGuid().ToString(),");
            Sb.AppendLine($"{CurrentStateField},");
            Sb.AppendLine("trigger,");
            Sb.AppendLine($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
            Sb.AppendLine($"{PayloadVar});");
        }
        Sb.AppendLine();
        _smCtxCreated = true;  // Mark that smCtx has been created
        Sb.AppendLine($"_extensionRunner.RunBeforeTransition(_extensions, {HookVarContext});");
        Sb.AppendLine();
}

    protected override void WriteGuardEvaluationHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
        if (!ExtensionsOn) return;

        // Ensure smCtx variable exists - create it if not already created by WriteBeforeTransitionHook
        // This can happen in payload variants where guard hooks are emitted directly
        if (!_smCtxCreated)
    {
            Sb.AppendLine($"var {HookVarContext} = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
            using (Sb.Indent())
        {
                Sb.AppendLine("Guid.NewGuid().ToString(),");
                Sb.AppendLine($"{CurrentStateField},");
                Sb.AppendLine("trigger,");
                Sb.AppendLine($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
                Sb.AppendLine($"{PayloadVar});");
            }
            Sb.AppendLine();
            _smCtxCreated = true;
        }

        Sb.AppendLine($"_extensionRunner.RunGuardEvaluation(_extensions, {HookVarContext}, \"{transition.GuardMethod}\");");
        Sb.AppendLine();
}

    protected override void WriteAfterGuardEvaluatedHook(
        TransitionModel transition,
        string guardResultVar,
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
        if (!ExtensionsOn) return;
        Sb.AppendLine($"_extensionRunner.RunGuardEvaluated(_extensions, {HookVarContext}, \"{transition.GuardMethod}\", {guardResultVar});");
        Sb.AppendLine();
}

    protected override void WriteAfterTransitionHook(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage,
        bool success)
{
        if (!ExtensionsOn) return;
        Sb.AppendLine($"_extensionRunner.RunAfterTransition(_extensions, {HookVarContext}, {success.ToString().ToLowerInvariant()});");
}

    protected  void WriteTransitionFailureHook(
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
        if (!ExtensionsOn) return;
        // Note: This method is called from two different contexts:
        // 1. From async TryFire where SuccessVar exists
        // 2. From sync TryFire wrapper where we use 'result' 
        // The wrapper handles the condition, so we just emit the body
        Sb.AppendLine($"var failCtx = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
        using (Sb.Indent())
    {
            Sb.AppendLine("Guid.NewGuid().ToString(),");
            Sb.AppendLine($"{OriginalStateVar},");
            Sb.AppendLine("trigger,");
            Sb.AppendLine($"{OriginalStateVar},");
            Sb.AppendLine($"{PayloadVar});");
        }
        Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, failCtx, false);");
}

    // Payload-aware async transition logic (uses success var + END_TRY_FIRE)
    private void WriteTransitionLogicPayloadAsync(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
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

            Sb.AppendLine($"if (!{GuardResultVar})");
            Sb.AppendLine("{");
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
            Sb.AppendLine($"{SuccessVar} = false;");
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
            Sb.AppendLine("}");
        }

        // OnExit
        if (!transition.IsInternal && hasOnEntryExit &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
    {
            Sb.AppendLine("try");
            Sb.AppendLine("{");
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
            WriteLogStatement("Debug",
                $"OnExitExecuted(_logger, _instanceId, \"{fromStateDef!.OnExitMethod}\", \"{transition.FromState}\");");
            Sb.AppendLine("}");
            Sb.AppendLine("catch (Exception ex) when (ex is not System.OperationCanceledException)");
            Sb.AppendLine("{");
            Sb.AppendLine($"{SuccessVar} = false;");
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
            Sb.AppendLine("}");
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
            WriteLogStatement("Information",
                $"TransitionSucceeded(_logger, _instanceId, \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
        }

        // Success
        Sb.AppendLine($"{SuccessVar} = true;");

        // Hook: After successful transition
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

        Sb.AppendLine($"goto {EndOfTryFireLabel};");
}

    // Sync direct-return transition logic with payload
    private void WriteTransitionLogicPayloadSyncDirect(
        TransitionModel transition,
        string stateTypeForUsage,
        string triggerTypeForUsage)
{
        // Hook: Before transition (must be before guard check)
        WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

        // Guard with direct return
        if (!string.IsNullOrEmpty(transition.GuardMethod))
    {
            WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
            var from = TypeHelper.EscapeIdentifier(transition.FromState);
            var trig = TypeHelper.EscapeIdentifier(transition.Trigger);
            Sb.AppendLine($"                        var guardResult = EvaluateGuard__{from}__{trig}({PayloadVar});");
            // Ensure extensions get the evaluated notification
            WriteAfterGuardEvaluatedHook(transition, "guardResult", stateTypeForUsage, triggerTypeForUsage);
            Sb.AppendLine("                        if (!guardResult)");
            Sb.AppendLine("                        {");
            // Hook: After failed transition (guard failed)
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
            Sb.AppendLine("                            return false;");
            Sb.AppendLine("                        }");
        }

        // OnExit with try/catch → false on failure
        if (!transition.IsInternal && ShouldGenerateOnEntryExit() &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
    {
            Sb.AppendLine("                        try");
            Sb.AppendLine("                        {");
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
            Sb.AppendLine("                        }");
            Sb.AppendLine("                        catch");
            Sb.AppendLine("                        {");
            // Hook: After failed transition (OnExit exception)
            WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
            Sb.AppendLine("                            return false;");
            Sb.AppendLine("                        }");
        }

        // State change BEFORE action
        if (!transition.IsInternal)
    {
            Sb.AppendLine($"                        {CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
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

        Sb.AppendLine("                        return true;");
}

    private void WriteAsyncCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
{
        WriteMethodAttribute();
        Sb.AppendLine($"    private async ValueTask<bool> CanFireWithPayloadAsync({triggerTypeForUsage} trigger, object? payload, CancellationToken cancellationToken)");
        Sb.AppendLine("    {");
        Sb.AppendLine("        cancellationToken.ThrowIfCancellationRequested();");
        Sb.AppendLine();

        if (HasPayload && HasMultiPayload)
    {
            Sb.AppendLine($"        if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && payload != null && !expectedType.IsInstanceOfType(payload)) return false;");
            Sb.AppendLine();
        }

        Sb.AppendLine($"        switch ({CurrentStateField})");
        Sb.AppendLine("        {");
        var allHandledFromStates = Model.Transitions.Select(t => t.FromState).Distinct().OrderBy(s => s);
        foreach (var stateName in allHandledFromStates)
    {
            Sb.AppendLine($"            case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
            Sb.AppendLine("            {");
            Sb.AppendLine("                switch (trigger)");
            Sb.AppendLine("                {");
            var transitionsFromThisState = Model.Transitions.Where(t => t.FromState == stateName);
            foreach (var transition in transitionsFromThisState)
        {
                Sb.AppendLine($"                    case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                Sb.AppendLine("                    {");
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
                    Sb.AppendLine("                        return guardResult;");
                }
                else
            {
                    Sb.AppendLine("                        return true;");
                }
                Sb.AppendLine("                    }");
            }
            Sb.AppendLine("                    default: return false;");
            Sb.AppendLine("                }");
            Sb.AppendLine("            }");
        }
        Sb.AppendLine("            default: return false;");
        Sb.AppendLine("        }");
        Sb.AppendLine("    }");
        Sb.AppendLine();
}

    private void WriteCanFireWithPayload(string stateTypeForUsage, string triggerTypeForUsage)
{
        WriteMethodAttribute();
        Sb.AppendLine($"    private bool CanFireWithPayload({triggerTypeForUsage} trigger, object? payload)");
        Sb.AppendLine("    {");
        if (HasPayload && HasMultiPayload)
    {
            Sb.AppendLine($"        if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && payload != null && !expectedType.IsInstanceOfType(payload)) return false;");
            Sb.AppendLine();
        }

        if (Model.HierarchyEnabled)
    {
            Sb.AppendLine($"        int currentIndex = (int){CurrentStateField};");
            Sb.AppendLine("        int check = currentIndex;");
            Sb.AppendLine("        while (check >= 0)");
            Sb.AppendLine("        {");
            Sb.AppendLine($"            var state = ({stateTypeForUsage})check;");
            Sb.AppendLine("            switch (state)");
            Sb.AppendLine("            {");
            var grouped = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);
            foreach (var group in grouped)
        {
                Sb.AppendLine($"                case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(group.Key)}:");
                Sb.AppendLine("                {");
                Sb.AppendLine("                    switch (trigger)");
                Sb.AppendLine("                    {");
                foreach (var transition in group)
            {
                    Sb.AppendLine($"                        case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                    Sb.AppendLine("                        {");
                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                {
                        WriteGuardCall(transition, "canFire", PayloadVar, throwOnException: false);
                        Sb.AppendLine("                            return canFire;");
                    }
                    else
                {
                        Sb.AppendLine("                            return true;");
                    }
                    Sb.AppendLine("                        }");
                }
                Sb.AppendLine("                        default: return false;");
                Sb.AppendLine("                    }");
                Sb.AppendLine("                }");
            }
            Sb.AppendLine("                default: break;");
            Sb.AppendLine("            }");
            Sb.AppendLine("            check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
            Sb.AppendLine("        }");
            Sb.AppendLine("        return false;");
        }
        else
    {
            Sb.AppendLine($"        switch ({CurrentStateField})");
            Sb.AppendLine("        {");
            var transitionsByFromState = Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);
            foreach (var stateGroup in transitionsByFromState)
        {
                var stateName = stateGroup.Key;
                Sb.AppendLine($"            case {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(stateName)}:");
                Sb.AppendLine("            {");
                Sb.AppendLine("                switch (trigger)");
                Sb.AppendLine("                {");
                foreach (var transition in stateGroup)
            {
                    Sb.AppendLine($"                    case {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.Trigger)}:");
                    Sb.AppendLine("                    {");
                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                {
                        WriteGuardCall(transition, "canFire", PayloadVar, throwOnException: false);
                        Sb.AppendLine("                        return canFire;");
                    }
                    else
                {
                        Sb.AppendLine("                        return true;");
                    }
                    Sb.AppendLine("                    }");
                }
                Sb.AppendLine("                    default: return false;");
                Sb.AppendLine("                }");
                Sb.AppendLine("            }");
            }
            Sb.AppendLine("            default: return false;");
            Sb.AppendLine("        }");
        }

        Sb.AppendLine("    }");
        Sb.AppendLine();
}

    private void WritePayloadMap(string triggerTypeForUsage)
{
        Sb.AppendLine($"        private static readonly Dictionary<{triggerTypeForUsage}, Type> {PayloadMapField} = new()");
        Sb.AppendLine("        {");
        foreach (var kvp in Model.TriggerPayloadTypes)
    {
            var triggerName = kvp.Key;
            var payloadTypeName = kvp.Value;
            var typeForTypeof = TypeHelper.FormatForTypeof(payloadTypeName);
            Sb.AppendLine($"            {{ {triggerTypeForUsage}.{TypeHelper.EscapeIdentifier(triggerName)}, typeof({typeForTypeof}) }},");
        }
        Sb.AppendLine("        };");
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
                treatCancellationAsFailure: Model.GenerationConfig.TreatCancellationAsFailure
            );

            // Notify extensions that the guard was evaluated (even in async-aware path)
            WriteAfterGuardEvaluatedHook(transition, GuardResultVar, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));

            Sb.AppendLine($"if (!{GuardResultVar})");
            Sb.AppendLine("{");
            WriteLogStatement("Warning",
                $"GuardFailed(_logger, _instanceId, \"{transition.GuardMethod}\", \"{transition.FromState}\", \"{transition.ToState}\", \"{transition.Trigger}\");");
            WriteLogStatement("Warning",
                $"TransitionFailed(_logger, _instanceId, \"{transition.FromState}\", \"{transition.Trigger}\");");
            Sb.AppendLine($"{SuccessVar} = false;");
            WriteAfterTransitionHook(transition, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType), success: false);
            Sb.AppendLine($"goto {EndOfTryFireLabel};");
            Sb.AppendLine("}");
        }
        else
    {
            // Fallback to base sync-style guard check
            WriteGuardCheck(transition, GetTypeNameForUsage(Model.StateType), GetTypeNameForUsage(Model.TriggerType));
        }
}

    private void WriteGetPermittedTriggersWithResolver(string stateType, string triggerType)
{
        // Sync version
        if (!IsAsyncMachine)
    {
            Sb.WriteSummary("Gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            Sb.AppendLine("    /// <param name=\"payloadResolver\">Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.</param>");
            Sb.AppendLine($"    public {ReadOnlyListType}<{triggerType}> GetPermittedTriggers(Func<{triggerType}, object?> payloadResolver)");
            Sb.AppendLine("    {");
            Sb.AppendLine("        EnsureStarted();");
            Sb.AppendLine("        if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
            Sb.AppendLine();

            if (IsHierarchical)
        {
                // HSM: Walk up the parent chain
                Sb.AppendLine($"        var permitted = new HashSet<{triggerType}>();");
                Sb.AppendLine($"        int currentIndex = (int){CurrentStateField};");
                Sb.AppendLine("        int check = currentIndex;");
                Sb.AppendLine();
                Sb.AppendLine("        while (check >= 0)");
                Sb.AppendLine("        {");
                Sb.AppendLine($"            var enumState = ({stateType})check;");
                Sb.AppendLine("            switch (enumState)");
                Sb.AppendLine("            {");

                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
            {
                    var stateName = stateGroup.Key;
                    Sb.AppendLine($"                case {stateType}.{TypeHelper.EscapeIdentifier(stateName)}:");
                    Sb.AppendLine("                {");

                    foreach (var transition in stateGroup)
                {
                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                    {
                            if (transition.GuardExpectsPayload)
                        {
                                // Guard needs payload - use resolver
                                Sb.AppendLine($"                    var payload_{transition.Trigger} = payloadResolver({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                                GuardGenerationHelper.EmitGuardCheck(
                                    Sb,
                                    transition,
                                    "canFire",
                                    $"payload_{transition.Trigger}",
                                    isAsync: false,
                                    wrapInTryCatch: true,
                                    Model.ContinueOnCapturedContext,
                                    handleResultAfterTry: true
                                );
                            }
                            else
                        {
                                // Guard doesn't need payload
                                GuardGenerationHelper.EmitGuardCheck(
                                    Sb,
                                    transition,
                                    "canFire",
                                    "null",
                                    isAsync: false,
                                    wrapInTryCatch: true,
                                    Model.ContinueOnCapturedContext,
                                    handleResultAfterTry: true
                                );
                            }

                            Sb.AppendLine("                    if (canFire)");
                            Sb.AppendLine("                    {");
                            Sb.AppendLine($"                        permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                            Sb.AppendLine("                    }");
                        }
                        else
                    {
                            Sb.AppendLine($"                    permitted.Add({triggerType}.{TypeHelper.EscapeIdentifier(transition.Trigger)});");
                        }
                    }
                    Sb.AppendLine("                    break;");
                    Sb.AppendLine("                }");
                }
                Sb.AppendLine("                default: break;");
                Sb.AppendLine("            }");
                Sb.AppendLine("            check = (uint)check < (uint)s_parent.Length ? s_parent[check] : -1;");
                Sb.AppendLine("        }");
                Sb.AppendLine();
                Sb.AppendLine("        return permitted.Count == 0 ? ");
                Sb.AppendLine($"            {ArrayEmptyMethod}<{triggerType}>() :");
                Sb.AppendLine("            permitted.ToArray();");
            }
            else
        {
                // Flat FSM
                Sb.AppendLine($"        switch ({CurrentStateField})");
                Sb.AppendLine("        {");

                var transitionsByFromState = Model.Transitions
                    .GroupBy(t => t.FromState)
                    .OrderBy(g => g.Key);

                foreach (var stateGroup in transitionsByFromState)
            {
                    var stateName = stateGroup.Key;
                    Sb.AppendLine($"            case {stateType}.{TypeHelper.EscapeIdentifier(stateName)}:");
                    Sb.AppendLine("            {");

                    var guarded = stateGroup.Where(t => !string.IsNullOrEmpty(t.GuardMethod)).ToList();
                    var stateFieldSuffix = MakeSafeMemberSuffix(stateName);
                    if (guarded.Count == 0)
                {
                        Sb.AppendLine($"                return s_perm__{stateFieldSuffix};");
                    }
                    else
                {
                        Sb.AppendLine("                int mask = 0;");
                        for (int i = 0; i < guarded.Count; i++)
                        {
                            var tr = guarded[i];
                            var from = TypeHelper.EscapeIdentifier(tr.FromState);
                            var trig = TypeHelper.EscapeIdentifier(tr.Trigger);
                            Sb.AppendLine($"                var p_{i} = payloadResolver({triggerType}.{trig});");
                            Sb.AppendLine($"                if (EvaluateGuard__{from}__{trig}(p_{i})) mask |= {1 << i};");
                        }
                        Sb.AppendLine($"                return s_perm__{stateFieldSuffix}[mask];");
                    }

                    Sb.AppendLine("            }");
                }

                Sb.AppendLine("            default:");
                Sb.AppendLine($"                return {ArrayEmptyMethod}<{triggerType}>();");
                Sb.AppendLine("        }");
            }

            Sb.AppendLine("    }");
            Sb.AppendLine();
        }
        else
    {
            // Async version with resolver
            Sb.WriteSummary("Asynchronously gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            Sb.AppendLine("    /// <param name=\"payloadResolver\">Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.</param>");
            Sb.AppendLine("    /// <param name=\"cancellationToken\">A token to observe for cancellation requests</param>");
            Sb.AppendLine($"    public async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersAsync(Func<{triggerType}, object?> payloadResolver, CancellationToken cancellationToken = default)");
            Sb.AppendLine("    {");
            Sb.AppendLine("        EnsureStarted();");
            Sb.AppendLine("        if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
            Sb.AppendLine("        cancellationToken.ThrowIfCancellationRequested();");
            Sb.AppendLine($"        var permitted = new List<{triggerType}>();");
            Sb.AppendLine();

            // Evaluate all distinct triggers defined in the model using CanFireWithPayloadAsync
            var distinctTriggers = Model.Transitions.Select(t => t.Trigger).Distinct().OrderBy(t => t).ToList();
            foreach (var trig in distinctTriggers)
        {
                Sb.AppendLine("        {");
                Sb.AppendLine($"            var __trig = {triggerType}.{TypeHelper.EscapeIdentifier(trig)};");
                Sb.AppendLine("            var __payload = payloadResolver(__trig);");
                Sb.AppendLine($"            if (await CanFireWithPayloadAsync(__trig, __payload, cancellationToken){GetConfigureAwait()})");
                Sb.AppendLine("            {");
                Sb.AppendLine("                permitted.Add(__trig);");
                Sb.AppendLine("            }");
                Sb.AppendLine("        }");
            }

            Sb.AppendLine($"        return permitted.Count == 0 ? {ArrayEmptyMethod}<{triggerType}>() : permitted.ToArray();");
            Sb.AppendLine("    }");
            Sb.AppendLine();
        }
}

    // Sync direct-return transition logic (no success var, no labels)
    private void WriteTransitionLogicSyncDirect(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
{
        // Guard
        if (!string.IsNullOrEmpty(transition.GuardMethod))
    {
            Sb.AppendLine($"                        if (!{transition.GuardMethod}())");
            Sb.AppendLine("                            return false;");
        }

        // OnExit (no exceptions policy in sync path; keep simple)
        if (!transition.IsInternal && ShouldGenerateOnEntryExit() &&
            Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
            !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
    {
            Sb.AppendLine($"                        {fromStateDef.OnExitMethod}();");
        }

        // State change BEFORE action (policy relies on stateAlreadyChanged)
        if (!transition.IsInternal)
    {
            Sb.AppendLine($"                        {CurrentStateField} = {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)};");
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

        Sb.AppendLine("                        return true;");
}
}
