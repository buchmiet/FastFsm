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
            description: "All parent states referenced by substates must be defined in the state enum. Check for typos in the parent state name.");

        public static readonly RuleDefinition InvalidHierarchyConfiguration = new(
            id: RuleIdentifiers.InvalidHierarchyConfiguration,  // FSM102
            title: "Composite without initial state",
            messageFormat: "Composite state '{0}' has no initial substate defined. Fix: mark one child with [State(..., Parent = {0}, IsInitial = true)] or .IsInitial(), or set History = HistoryMode.Shallow/Deep on the composite state.",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Warning,
            description: "Composite states must have an initial substate to determine which child state to enter. Either define an initial substate or use history mode to remember the last active child.");

        public static readonly RuleDefinition MultipleInitialSubstates = new(
            id: RuleIdentifiers.MultipleInitialSubstates,  // FSM103
            title: "Multiple initial children",
            messageFormat: "Composite state '{0}' has multiple initial substates: '{1}' and '{2}'. Fix: keep only one child marked IsInitial (attribute [State(..., IsInitial = true)] or fluent .IsInitial()).",
            category: RuleCategories.FSM_Generator_HSM,
            defaultSeverity: RuleSeverity.Error,
            description: "A composite state can only have one initial substate. Remove duplicate IsInitial markers.");

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
            defaultSeverity: RuleSeverity.Warning,
            description: "Each transition should have at most one payload type. Multiple Payload() calls will use the last specified type.");



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

        public static readonly RuleDefinition AmbiguousMethodGroup = new(
            id: RuleIdentifiers.AmbiguousMethodGroup,  // FSM3070
            title: "Ambiguous method group reference",
            messageFormat: "Method group '{0}' is ambiguous with {1} overload candidates. Ensure the method has a unique signature compatible with FSM callbacks.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "When using method groups in Fluent API, the method must have a unique signature that matches one of the supported FSM callback patterns.");

        public static readonly RuleDefinition ImpureDslExpression = new(
            id: RuleIdentifiers.ImpureDslExpression, // FSM3071
            title: "Impure expression in Fluent DSL",
            messageFormat: "Expression '{0}' is not allowed in Fluent DSL position '{1}'. Use method groups or literals only.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "The Fluent DSL is declarative. Only method groups and compile-time literals are permitted when configuring the state machine.");

        public static readonly RuleDefinition PropertyMethodGroupNotAllowed = new(
            id: RuleIdentifiers.PropertyMethodGroupNotAllowed, // FSM3072
            title: "Property used where method expected",
            messageFormat: "Property '{0}' cannot be used in '{1}'. Fluent callbacks must reference methods declared on the state machine class.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Method groups in the Fluent DSL must target methods. Referencing properties or indexers is not supported.");

        public static readonly RuleDefinition ExternalMethodGroup = new(
            id: RuleIdentifiers.ExternalMethodGroup, // FSM3073
            title: "External method group not allowed",
            messageFormat: "Method '{0}' belongs to '{1}'. Only methods declared on the state machine class may be used in '{2}'.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Callbacks referenced from Fluent DSL must be declared on the state machine class. Wrap external dependencies in local methods.");

        public static readonly RuleDefinition DslSignatureMismatch = new(
            id: RuleIdentifiers.DslSignatureMismatch, // FSM3074
            title: "Method signature incompatible with DSL position",
            messageFormat: "Method '{0}' cannot be used as '{1}'. Expected signature: {2}.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Each Fluent DSL position (Guard, Action, OnEntry, OnExit, OnException) accepts only specific callback signatures.");

        public static readonly RuleDefinition LambdaExpressionNotAllowed = new(
            id: RuleIdentifiers.LambdaExpressionNotAllowed, // FSM3075
            title: "Lambda expression not allowed in Fluent DSL",
            messageFormat: "Lambda expressions are not allowed in Fluent DSL position '{0}'. Declare a named method instead.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "The Fluent DSL forbids inline lambdas to keep configuration declarative and analyzable.");

        public static readonly RuleDefinition FieldOrPropertyAccessInDsl = new(
            id: RuleIdentifiers.FieldOrPropertyAccessInDsl, // FSM3076
            title: "Field or property access in Fluent DSL",
            messageFormat: "Value '{0}' is not a compile-time literal. Only constants may be used for '{1}'.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Literal values must be supplied directly when configuring Fluent DSL elements like Priority or State references.");

        public static readonly RuleDefinition MethodInvocationInDsl = new(
            id: RuleIdentifiers.MethodInvocationInDsl, // FSM3077
            title: "Method invocation in Fluent DSL",
            messageFormat: "Invocation '{0}' is not allowed in Fluent DSL position '{1}'. Provide enum literals, constants, or method groups instead.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Fluent DSL inputs must be compile-time determinable. Invoking helper methods inside Configure() is not supported.");

        public static readonly RuleDefinition MultipleConfigureMethods = new(
            id: RuleIdentifiers.MultipleConfigureMethods, // FSM3080
            title: "Multiple Configure methods declared",
            messageFormat: "State machine declares multiple Configure/SetupStates methods. Keep a single configuration entry point.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "FastFSM expects exactly one Configure()/SetupStates() method per state machine class.");

        public static readonly RuleDefinition ConfigureMustBePrivate = new(
            id: RuleIdentifiers.ConfigureMustBePrivate, // FSM3081a
            title: "Configure must be private",
            messageFormat: "Method '{0}' must be declared private to be used as the Fluent configuration entry point.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Fluent configuration methods should be private instance members to avoid unintended runtime use.");

        public static readonly RuleDefinition ConfigureMustBeParameterless = new(
            id: RuleIdentifiers.ConfigureMustBeParameterless, // FSM3081b
            title: "Configure must be parameterless",
            messageFormat: "Method '{0}' cannot declare parameters. Fluent configuration methods do not accept arguments.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Configure()/SetupStates() must be parameterless instance methods.");

        public static readonly RuleDefinition ConfigureCannotBeVirtual = new(
            id: RuleIdentifiers.ConfigureCannotBeVirtual, // FSM3081c
            title: "Configure cannot be virtual or override",
            messageFormat: "Method '{0}' cannot be virtual or override for Fluent configuration.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Fluent configuration methods are analyzed at compile time and must not be virtual or overrides.");

        public static readonly RuleDefinition ConfigureMustBeInstance = new(
            id: RuleIdentifiers.ConfigureMustBeInstance, // FSM3081d
            title: "Configure must be an instance method",
            messageFormat: "Method '{0}' must be an instance method. Static configuration is allowed only as a legacy fallback.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Warning,
            description: "Instance Configure() methods are required for method-group friendly Fluent DSL. Static Configure() is discouraged.");

        public static readonly RuleDefinition ConfigureNotDeclaredOnType = new(
            id: RuleIdentifiers.ConfigureNotDeclaredOnType, // FSM3082
            title: "Configure must be declared on the state machine class",
            messageFormat: "Method '{0}' must be declared on the state machine partial class. Do not inherit Configure() from a base type.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Configure()/SetupStates() must be defined directly on the partial class analyzed by the generator.");

        public static readonly RuleDefinition ConfigureCannotBePartial = new(
            id: RuleIdentifiers.ConfigureCannotBePartial, // FSM3083
            title: "Configure cannot be partial",
            messageFormat: "Method '{0}' cannot be a partial method. Use a regular method body for Fluent configuration.",
            category: RuleCategories.FSM_Generator_Fluent,
            defaultSeverity: RuleSeverity.Error,
            description: "Partial methods cannot be analyzed reliably for Fluent configuration and are therefore disallowed.");

        // Generator infrastructure diagnostics removed (FSM9000–9013)

        // Generator infrastructure diagnostics removed (FSM9000–9013)

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
            AmbiguousMethodGroup,           // FSM3070
            ImpureDslExpression,            // FSM3071
            PropertyMethodGroupNotAllowed,  // FSM3072
            ExternalMethodGroup,            // FSM3073
            DslSignatureMismatch,           // FSM3074
            LambdaExpressionNotAllowed,     // FSM3075
            FieldOrPropertyAccessInDsl,     // FSM3076
            MethodInvocationInDsl,          // FSM3077
            MultipleConfigureMethods,       // FSM3080
            ConfigureMustBePrivate,         // FSM3081a
            ConfigureMustBeParameterless,   // FSM3081b
            ConfigureCannotBeVirtual,       // FSM3081c
            ConfigureMustBeInstance,        // FSM3081d
            ConfigureNotDeclaredOnType,     // FSM3082
            ConfigureCannotBePartial,       // FSM3083

            // Generator infrastructure (removed from catalog)
        }.AsReadOnly();
    }
}
