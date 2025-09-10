using System.Collections.Generic;
using Generator.Rules.Definitions;

namespace Generator.Rules.Definitions
{
    /// <summary>
    /// Immutable metadata describing a generator rule (ID, message format, severity, category).
    /// </summary>
    public class RuleDefinition(
        string id,
        string title,
        string messageFormat,
        string category,
        RuleSeverity defaultSeverity,
        string description,
        bool isEnabledByDefault = true)
    {
        /// <summary>
        /// Gets the unique identifier of the rule (e.g., "FSM001").
        /// </summary>
        public string Id { get; } = id;

        /// <summary>
        /// Gets a short, descriptive title for the rule.
        /// </summary>
        public string Title { get; } = title;

        /// <summary>
        /// Gets the message format string, which follows string.Format conventions (e.g., "State '{0}' is invalid.").
        /// </summary>
        public string MessageFormat { get; } = messageFormat;

        /// <summary>
        /// Gets the category the rule belongs to (e.g., "FSM.Generator").
        /// </summary>
        public string Category { get; } = category;

        /// <summary>
        /// Gets the default severity of the diagnostic (Error, Warning, or Info).
        /// </summary>
        public RuleSeverity DefaultSeverity { get; } = defaultSeverity;

        /// <summary>
        /// Gets a longer description of the rule, explaining the cause and potential fixes.
        /// </summary>
        public string Description { get; } = description;

        /// <summary>
        /// Gets a value indicating whether the rule is enabled by default.
        /// </summary>
        public bool IsEnabledByDefault { get; } = isEnabledByDefault;
    }

    public static class DefinedRules
    {
        public static readonly RuleDefinition DuplicateTransition = new(
            id: RuleIdentifiers.DuplicateTransition, // "FSM001"
            title: "Duplicate transition detected",
            messageFormat: "Duplicate transition from state '{0}' on trigger '{1}'. Only the first one will be used by the generator.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Warning,
            description: "There are multiple transitions defined for the same 'from state' and 'trigger'. The generator will only consider the first one encountered.");

        public static readonly RuleDefinition UnreachableState = new(
            id: RuleIdentifiers.UnreachableState, // "FSM002"
            title: "Unreachable state detected",
            messageFormat: "State '{0}' might be unreachable based on defined transitions.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Info,
            description: "A state exists in the state enum that may not be reachable from the initial state or any other state via the defined transitions. This is a simplified check.");

        public static readonly RuleDefinition InvalidMethodSignature = new(
            id: RuleIdentifiers.InvalidMethodSignature, // "FSM003"
            title: "Invalid method signature for FSM callback",
            messageFormat: "Method '{0}' used as {1} has an invalid signature. Expected: '{2}'.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "Guard, Action, OnEntry, or OnExit methods must have a specific signature (e.g., guards return bool, actions are void; both can optionally take object? payload).");

        public static readonly RuleDefinition MissingStateMachineAttribute = new(
            id: RuleIdentifiers.MissingStateMachineAttribute, // "FSM004"
            title: "Potentially missing StateMachine attribute",
            messageFormat: "Class '{0}' uses FSM transition attributes but is missing the [StateMachine(typeof(StateEnum), typeof(TriggerEnum))] attribute, or is not partial.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Warning,
            description: "If this class is intended to be a FSM, it needs the [StateMachine] attribute and must be declared as partial.");

        public static readonly RuleDefinition InvalidTypesInAttribute = new(
            id: RuleIdentifiers.InvalidTypesInAttribute,          // "FSM005"
            title: "State/Trigger types must be enums",
            messageFormat: "State and Trigger types must be enums. '{0}' or '{1}' is not an enum.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "The StateType and TriggerType arguments of the StateMachineAttribute must be enum types.");


        public static readonly RuleDefinition InvalidEnumValueInTransition = new(
            id: RuleIdentifiers.InvalidEnumValueInTransition, // "FSM006"
            title: "Invalid enum value in transition",
            messageFormat: "Invalid enum value '{0}' for enum type '{1}'. Use a valid enum member.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "Enum values in transition attributes must be valid members of the specified enum type.");

        // Removed: FSM007 & FSM008 (unused)



        public static readonly RuleDefinition GuardWithPayloadInNonPayloadMachine = new(
            id: RuleIdentifiers.GuardWithPayloadInNonPayloadMachine,
            title: "Guard with payload in non-payload machine",
            messageFormat: "{0}",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            isEnabledByDefault: true,
            description: "Guards that expect payload parameters cannot be used in state machines without payload support.");
        public static readonly RuleDefinition MixedSyncAsyncCallbacks = new(
            id: RuleIdentifiers.MixedSyncAsyncCallbacks,
            title: "Mixed synchronous and asynchronous callbacks",
            messageFormat: "Cannot mix synchronous and asynchronous callbacks in the same state machine. Method '{0}' is {1}, but the machine is already configured as {2}.",
            category: RuleCategories.FSM_Generator_Async,
            defaultSeverity: RuleSeverity.Warning,
            description: "All state machine callbacks (OnEntry, OnExit, Action, Guard) must be either all synchronous or all asynchronous to ensure consistent behavior.");

        public static readonly RuleDefinition InvalidGuardTaskReturnType = new(
            id: RuleIdentifiers.InvalidGuardTaskReturnType,
            title: "Invalid async guard return type",
            messageFormat: "Asynchronous guards must return 'ValueTask<bool>', not 'Task<bool>'. Method '{0}' has an invalid return type.",
            category: RuleCategories.FSM_Generator_Async,
            defaultSeverity: RuleSeverity.Error,
            description: "Using Task<bool> for guards causes unnecessary memory allocations. Use ValueTask<bool> for optimal performance.");

        public static readonly RuleDefinition InvalidAsyncVoid = new(
            id: RuleIdentifiers.InvalidAsyncVoid,
            title: "Callback returns 'async void'",
            messageFormat: "Callback method '{0}' returns 'async void'. Use 'Task' or 'ValueTask' instead to allow the state machine to correctly await its completion and handle exceptions.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "'async void' methods are fire-and-forget and can lead to unhandled exceptions and race conditions. State machine callbacks should always be awaitable.");
        public static readonly RuleDefinition AsyncCallbackInSyncMachine = new(
            id: RuleIdentifiers.AsyncCallbackInSyncMachine,
            title: "Asynchronous callback in synchronous state machine",
            messageFormat: "Method '{0}' is asynchronous, but the state machine is synchronous. Either make all callbacks asynchronous or change the return type of this method.",
            category: RuleCategories.FSM_Generator_Async,
            defaultSeverity: RuleSeverity.Error,
            description: "A state machine must be consistently synchronous or asynchronous. Mixing callback types can lead to unexpected behavior and deadlocks.");

        // HSM-specific rules (FSM100-FSM105)
        public static readonly RuleDefinition CircularHierarchy = new(
            id: RuleIdentifiers.CircularHierarchy,  // FSM100
            title: "Circular hierarchy detected",
            messageFormat: "State '{0}' is part of a circular hierarchy chain: {1}. Fix: Review the Parent relationships and remove the circular dependency.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Error,
            description: "State hierarchies cannot contain circular dependencies. A state cannot be its own ancestor or descendant.");

        public static readonly RuleDefinition OrphanSubstate = new(
            id: RuleIdentifiers.OrphanSubstate,  // FSM101
            title: "Multiple or divergent parent",
            messageFormat: "State '{0}' references parent '{1}' which does not exist. Fix: Either define the parent state with [State({1})], or correct the Parent parameter to reference an existing state.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Error,
            description: "All parent states referenced by substates must be defined in the. Check for typos in the parent state name.");

        public static readonly RuleDefinition InvalidHierarchyConfiguration = new(
            id: RuleIdentifiers.InvalidHierarchyConfiguration,  // FSM102
            title: "Composite without initial state",
            messageFormat: "Composite state '{0}' has no initial substate defined. Fix: Add [InitialSubstate({0}, YourInitialChild)] attribute, or set History = HistoryMode.Shallow/Deep on the composite state.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Warning,
            description: "Composite states must have an initial substate to determine which child state to enter. Either define an initial substate or use history mode to remember the last active child.");

        public static readonly RuleDefinition MultipleInitialSubstates = new(
            id: RuleIdentifiers.MultipleInitialSubstates,  // FSM103
            title: "Multiple initial children",
            messageFormat: "Composite state '{0}' has multiple initial substates: '{1}' and '{2}'. Fix: Keep only one [InitialSubstate({0}, ...)] attribute.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Error,
            description: "A composite state can only have one initial substate. Remove duplicate InitialSubstate attributes.");

        public static readonly RuleDefinition InvalidHistoryConfiguration = new(
            id: RuleIdentifiers.InvalidHistoryConfiguration,  // FSM104
            title: "History on non-composite",
            messageFormat: "State '{0}' has History = {1} but is not a composite state (has no children). Fix: Either remove the History parameter, or add child states with Parent = {0}.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Error,
            description: "Only composite states (states with children) can have history mode. History remembers which child was last active.");

        // Removed: FSM105 (unused)

        // Fluent API rules (FSM200-FSM206)
        public static readonly RuleDefinition OpenTransition = new(
            id: RuleIdentifiers.OpenTransition,  // FSM3000
            title: "Open transition not finalized",
            messageFormat: "Transition from state '{0}' on trigger '{1}' is not finalized with GoTo() or Internal(). Add .GoTo(targetState) or .Internal() to complete the transition.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Every transition must be finalized with either GoTo(targetState) for external transitions or Internal() for internal transitions.");

        public static readonly RuleDefinition AutoFinalizedTransition = new(
            id: RuleIdentifiers.AutoFinalizedTransition,  // FSM3010
            title: "Transition auto-finalized as internal",
            messageFormat: "Transition from state '{0}' on trigger '{1}' was auto-finalized as internal. Add explicit .GoTo() or .Internal() to suppress this warning.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Info,
            description: "When a new On() or State() is encountered without finalizing the previous transition, it is auto-finalized as internal. This may not be the intended behavior.");

        public static readonly RuleDefinition MultiplePayloadsOnTransition = new(
            id: RuleIdentifiers.MultiplePayloadsOnTransition,  // FSM3020
            title: "Multiple payload definitions on transition",
            messageFormat: "Transition from state '{0}' on trigger '{1}' has multiple Payload() calls. The last one ('{2}') will be used.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Each transition should have at most one payload type. Multiple Payload() calls will use the last specified type.");

        // Removed: FSM203/204/205/206 (unused)

        public static readonly RuleDefinition InvalidPriorityArgument = new(
            id: RuleIdentifiers.InvalidPriorityArgument,  // FSM3030
            title: "Invalid priority argument",
            messageFormat: "Priority() requires an integer literal value.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "The Priority() fluent call accepts only an integer literal argument used for transition ordering.");

        // Global handler rules (OnException)
        public static readonly RuleDefinition DuplicateOnExceptionHandler = new(
            id: RuleIdentifiers.DuplicateOnExceptionHandler,  // FSM3050
            title: "Multiple global OnException handlers",
            messageFormat: "Multiple global OnException handlers specified; only one is allowed.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "FastFSM supports exactly one global exception handler per state machine. Remove duplicate OnException() calls.");

        public static readonly RuleDefinition InvalidOnExceptionSignature = new(
            id: RuleIdentifiers.InvalidOnExceptionSignature,  // FSM3060
            title: "Invalid OnException handler signature",
            messageFormat: "Method '{0}' used as {1} has an invalid signature. Expected: {2}.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "OnException handler must return ExceptionDirective or ValueTask<ExceptionDirective> and accept ExceptionContext<TState,TTrigger> as first parameter with optional CancellationToken.");

        public static readonly RuleDefinition PriorityWithoutActiveTransition = new(
            id: RuleIdentifiers.PriorityWithoutActiveTransition,  // FSM3040
            title: "Priority() without active transition",
            messageFormat: "Priority() can only be called while configuring a transition.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Priority() is valid only in the context of an active transition builder (after On()/OnInternal()).");

        // Generator infrastructure diagnostics (logging, discovery, config)
        public static readonly RuleDefinition DebugEntry = new(
            id: RuleIdentifiers.DebugEntry, // FSM9000
            title: "Processing candidate",
            messageFormat: "Processing candidate: {0}",
            category: RuleCategories.FSM_Generator_Discovery,
            defaultSeverity: RuleSeverity.Info,
            description: "Processing candidate trace.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition ConfigurationSections = new(
            id: RuleIdentifiers.ConfigurationSections, // FSM9010
            title: "Configuration sections",
            messageFormat: "{0} - StatesFrom: {1} | TransitionsFrom: {2} (ext={3}) | InternalFrom: {4} (int={5}) | PayloadTypes: {6}",
            category: RuleCategories.FSM_Generator_Parser,
            defaultSeverity: RuleSeverity.Info,
            description: "Summary of configuration sources discovered during parsing (methods contributing [State], [Transition], internal transitions and payload types).",
            isEnabledByDefault: false);

        public static readonly RuleDefinition VariantDecision = new(
            id: RuleIdentifiers.VariantDecision, // FSM9009
            title: "Variant decision",
            messageFormat: "{0} -> {1}; internalOnly={2}; payloadPresent={3}",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Info,
            description: "Generator variant/features summary (payload, extensions, callbacks, internal-only). The second argument typically encodes feature flags.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition DeclarationPlan = new(
            id: RuleIdentifiers.DeclarationPlan, // FSM9001
            title: "Declaration plan",
            messageFormat: "DECLARATION_PLAN for {0}: ns='{1}', nesting='{2}', class='{3}', accessibility='{4}', partial={5}",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Info,
            description: "Planned namespace, nesting and class accessibility for the generated state machine.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition EmptyCodeGenerated = new(
            id: RuleIdentifiers.EmptyCodeGenerated, // FSM9002
            title: "Empty code generated",
            messageFormat: "EMPTY_CODE for {0}; variant={1}; states={2}; transitions int={3}, ext={4}; payloads={5}; enumFallback={6}",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Info,
            description: "Indicates that the generator produced empty or too small output for a candidate; includes basic metrics for diagnosis.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition EnumOnlyStatesFallback = new(
            id: RuleIdentifiers.EnumOnlyStatesFallback, // FSM9003
            title: "Enum-only states fallback",
            messageFormat: "Enum-only states fallback applied for '{0}' — 0 [State] attributes found; using all enum members as states",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Info,
            description: "Fallback path when no [State] attributes are found: all enum members are used as states.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition MsBuildAnalyzerProperties = new(
            id: RuleIdentifiers.MsBuildAnalyzerProperties, // FSM9004
            title: "MSBuild analyzer properties",
            messageFormat: "EmitCompilerGeneratedFiles={0}; CompilerGeneratedFilesOutputPath={1}",
            category: RuleCategories.FSM_Generator_Config,
            defaultSeverity: RuleSeverity.Info,
            description: "Displays analyzer-related MSBuild properties to aid debugging of generated files.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition AddSourceOk = new(
            id: RuleIdentifiers.AddSourceOk, // FSM9005
            title: "AddSource succeeded",
            messageFormat: "AddSource ok: {0} (len={1})",
            category: RuleCategories.FSM_Generator_AddSource,
            defaultSeverity: RuleSeverity.Info,
            description: "Indicates a successful AddSource call with the hint name and content length.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition SkippedCandidate = new(
            id: RuleIdentifiers.SkippedCandidate, // FSM9006
            title: "State machine candidate skipped",
            messageFormat: "Skipped state machine candidate {0}: {1}",
            category: RuleCategories.FSM_Generator_Discovery,
            defaultSeverity: RuleSeverity.Info,
            description: "Provides the reason why a discovered state machine candidate was skipped.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition DiscoveryOrTrace = new(
            id: RuleIdentifiers.DiscoveryOrTrace, // FSM9007
            title: "Generator trace",
            messageFormat: "{0}",
            category: RuleCategories.FSM_Generator_Discovery,
            defaultSeverity: RuleSeverity.Info,
            description: "Generic trace or discovery diagnostic used for parser traces or additional info.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition StartingParse = new(
            id: RuleIdentifiers.StartingParse, // FSM9008
            title: "Starting parse",
            messageFormat: "Starting parse for: {0}",
            category: RuleCategories.FSM_Generator_Parser,
            defaultSeverity: RuleSeverity.Info,
            description: "Marks the start of parsing a specific candidate class.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition ProcessingCandidate = new(
            id: RuleIdentifiers.ProcessingCandidate, // FSM9000
            title: "Processing candidate",
            messageFormat: "Processing candidate: {0}",
            category: RuleCategories.FSM_Generator_Discovery,
            defaultSeverity: RuleSeverity.Info,
            description: "Indicates that a discovered candidate is being processed by the generator.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition LogHelperPreAdd = new(
            id: RuleIdentifiers.LogHelperPreAdd, // FSM9012
            title: "Logging helper pre-AddSource",
            messageFormat: "GenerateLogging={0}; ns='{1}'; class='{2}'; hint='{3}'; first='{4}'",
            category: RuleCategories.FSM_Generator_AddSource,
            defaultSeverity: RuleSeverity.Info,
            description: "Summary emitted before adding the optional logging helper source file.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition LogProps = new(
            id: RuleIdentifiers.LogProps, // FSM9013
            title: "MSBuild logging flags",
            messageFormat: "FsmGenerateLogging={0}; FsmGenerateDI={1}",
            category: RuleCategories.FSM_Generator_Config,
            defaultSeverity: RuleSeverity.Info,
            description: "Shows effective MSBuild flags controlling optional logging and DI generation.",
            isEnabledByDefault: false);

        public static readonly RuleDefinition HsmFlagTracking = new(
            id: RuleIdentifiers.HsmFlagTracking, // FSM9011
            title: "HSM Flag Tracking",
            messageFormat: "{0}",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Info,
            description: "Diagnostic used to surface HSM-related feature flags and checkpoints during generation and in generated code comments.",
            isEnabledByDefault: false);

        public static readonly IReadOnlyList<RuleDefinition> All = new List<RuleDefinition>
        {
            DuplicateTransition,
            UnreachableState,
            InvalidMethodSignature,
            MissingStateMachineAttribute,
            InvalidTypesInAttribute,
            InvalidEnumValueInTransition,
            GuardWithPayloadInNonPayloadMachine,
            MixedSyncAsyncCallbacks,
            InvalidGuardTaskReturnType,
            InvalidAsyncVoid,
            AsyncCallbackInSyncMachine,
            // HSM rules (FSM100-FSM105)
            CircularHierarchy,              // FSM2000
            OrphanSubstate,                 // FSM2010
            InvalidHierarchyConfiguration,  // FSM2020
            MultipleInitialSubstates,       // FSM2030
            InvalidHistoryConfiguration,    // FSM2040
            // FSM105 removed
            // Fluent API rules (FSM200-FSM206)
            OpenTransition,                 // FSM3000
            AutoFinalizedTransition,        // FSM3010
            MultiplePayloadsOnTransition,   // FSM3020
            InvalidPriorityArgument,        // FSM3030
            DuplicateOnExceptionHandler,    // FSM3050
            InvalidOnExceptionSignature,    // FSM3060
            PriorityWithoutActiveTransition,// FSM3040

            // Generator infrastructure
            // DebugEntry consolidated into ProcessingCandidate (same ID)
            ConfigurationSections,         // FSM9010
            VariantDecision,               // FSM9009
            DeclarationPlan,               // FSM9001
            EmptyCodeGenerated,            // FSM9002
            EnumOnlyStatesFallback,        // FSM9003
            MsBuildAnalyzerProperties,     // FSM9004
            AddSourceOk,                   // FSM9005
            SkippedCandidate,              // FSM9006
            DiscoveryOrTrace,              // FSM9007
            StartingParse,                 // FSM9008
            ProcessingCandidate,           // FSM9000
            LogHelperPreAdd,               // FSM9012
            LogProps,                      // FSM9013
            HsmFlagTracking,               // FSM9011
        }.AsReadOnly();
    }
}
