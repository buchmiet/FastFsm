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
            defaultSeverity: RuleSeverity.Warning,
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

        public static readonly RuleDefinition MissingPayloadType = new(
            id: RuleIdentifiers.MissingPayloadType,
            title: "Missing payload type definition",
            messageFormat: "State machine forced to use '{0}' variant but no payload type is defined. Add [PayloadType] attribute or change the generation mode.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "WithPayload and Full variants require at least one payload type to be defined via [PayloadType] attribute.");

        public static readonly RuleDefinition ConflictingPayloadConfiguration = new(
            id: RuleIdentifiers.ConflictingPayloadConfiguration,
            title: "Conflicting payload configuration",
            messageFormat: "Forced 'WithPayload' variant expects single payload type but found {0} trigger-specific types. Use 'Full' variant or remove Force to allow auto-detection.",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "WithPayload variant supports only single payload type. For multiple payload types use Full variant.");

        public static readonly RuleDefinition InvalidForcedVariantConfiguration = new(
            id: RuleIdentifiers.InvalidForcedVariantConfiguration,
            title: "Invalid forced variant configuration",
            messageFormat: "Forced '{0}' variant conflicts with {1}. {2}",
            category: RuleCategories.FSM_Generator,
            defaultSeverity: RuleSeverity.Error,
            description: "Forced variant has conflicting configuration that prevents proper code generation.");


        public static readonly RuleDefinition GuardWithPayloadInNonPayloadMachine = new(
            id: RuleIdentifiers.GuardWithPayloadInNonPayloadMachine,
            title: "Guard with payload in non-payload machine",
            messageFormat: "{0}",
            category: "StateMachine.Design",
            defaultSeverity: RuleSeverity.Error,
            isEnabledByDefault: true,
            description: "Guards that expect payload parameters cannot be used in state machines without payload support.");
        public static readonly RuleDefinition MixedSyncAsyncCallbacks = new(
            id: RuleIdentifiers.MixedSyncAsyncCallbacks,
            title: "Mixed synchronous and asynchronous callbacks",
            messageFormat: "Cannot mix synchronous and asynchronous callbacks in the same state machine. Method '{0}' is {1}, but the machine is already configured as {2}.",
            category: RuleCategories.FSM_Generator_Async,
            defaultSeverity: RuleSeverity.Error,
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
            category: RuleCategories.FSM_Generator_Async,
            defaultSeverity: RuleSeverity.Warning,
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
            defaultSeverity: RuleSeverity.Error,
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
            defaultSeverity: RuleSeverity.Warning,
            description: "Only composite states (states with children) can have history mode. History remembers which child was last active.");

        public static readonly RuleDefinition ConflictingTransitionTargets = new(
            id: RuleIdentifiers.ConflictingTransitionTargets,  // FSM105
            title: "Transition to composite without explicit child",
            messageFormat: "Transition to composite state '{0}' without explicit target substate. The state machine will enter '{1}' (reason: {2}). To be explicit, transition directly to the desired substate.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Info,
            description: "When transitioning to a composite state without specifying a target substate, the entry point is determined by: 1) History mode (if set and previously visited), 2) Initial substate (if defined), or 3) First defined child.");

        public static readonly IReadOnlyList<RuleDefinition> All = new List<RuleDefinition>
        {
            DuplicateTransition,
            UnreachableState,
            InvalidMethodSignature,
            MissingStateMachineAttribute,
            InvalidTypesInAttribute,
            InvalidEnumValueInTransition,
            MissingPayloadType,
            ConflictingPayloadConfiguration,
            InvalidForcedVariantConfiguration,
            GuardWithPayloadInNonPayloadMachine,
            MixedSyncAsyncCallbacks,
            InvalidGuardTaskReturnType,
            InvalidAsyncVoid,
            AsyncCallbackInSyncMachine,
            // HSM rules (FSM100-FSM105)
            CircularHierarchy,              // FSM100
            OrphanSubstate,                 // FSM101
            InvalidHierarchyConfiguration,  // FSM102
            MultipleInitialSubstates,       // FSM103
            InvalidHistoryConfiguration,    // FSM104
            ConflictingTransitionTargets,   // FSM105
        }.AsReadOnly();
    }
}