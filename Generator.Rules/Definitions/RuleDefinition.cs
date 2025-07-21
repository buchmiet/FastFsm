

using System.Collections.Generic;

namespace Generator.Rules.Definitions;

public class RuleDefinition(
    string id,
    string title,
    string messageFormat,
    string category,
    RuleSeverity defaultSeverity,
    string description,
    bool isEnabledByDefault = true)
{
    public string Id { get; } = id;
    public string Title { get; } = title;
    public string MessageFormat { get; } = messageFormat; // Może być używany bezpośrednio lub jako klucz do zasobów
    public string Category { get; } = category;
    public RuleSeverity DefaultSeverity { get; } = defaultSeverity;
    public string Description { get; } = description;
    public bool IsEnabledByDefault { get; } = isEnabledByDefault;
}

public static class DefinedRules
{
    public static readonly RuleDefinition DuplicateTransition = new(
        id: RuleIdentifiers.DuplicateTransition, // "FSM001"
        title: "Duplicate transition detected",
        messageFormat: "Duplicate transition from state '{0}' on trigger '{1}'. Only the first one will be used by the generator.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Warning,
        description: "There are multiple transitions defined for the same 'from state' and 'trigger'. The generator will only consider the first one encountered.");

    public static readonly RuleDefinition UnreachableState = new(
        id: RuleIdentifiers.UnreachableState, // "FSM002"
        title: "Unreachable state detected",
        messageFormat: "State '{0}' might be unreachable based on defined transitions.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Warning,
        description: "A state exists in the state enum that may not be reachable from the initial state or any other state via the defined transitions. This is a simplified check.");

    public static readonly RuleDefinition InvalidMethodSignature = new(
        id: RuleIdentifiers.InvalidMethodSignature, // "FSM003"
        title: "Invalid method signature for FSM callback",
        messageFormat: "Method '{0}' used as {1} has an invalid signature. Expected: '{2}'.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Error,
        description: "Guard, Action, OnEntry, or OnExit methods must have a specific signature (e.g., guards return bool, actions are void; both can optionally take object? payload).");

    public static readonly RuleDefinition MissingStateMachineAttribute = new(
        id: RuleIdentifiers.MissingStateMachineAttribute, // "FSM004"
        title: "Potentially missing StateMachine attribute",
        messageFormat: "Class '{0}' uses FSM transition attributes but is missing the [StateMachine(typeof(StateEnum), typeof(TriggerEnum))] attribute, or is not partial.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Warning, 
        description: "If this class is intended to be a FSM, it needs the [StateMachine] attribute and must be declared as partial.");

    public static readonly RuleDefinition InvalidTypesInAttribute = new(
        id: RuleIdentifiers.InvalidTypesInAttribute,          // "FSM005"
        title: "State/Trigger types must be enums",
        messageFormat: "State and Trigger types must be enums. '{0}' or '{1}' is not an enum.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Error,
        description: "The StateType and TriggerType arguments of the StateMachineAttribute must be enum types.");


    public static readonly RuleDefinition InvalidEnumValueInTransition = new(
        id: RuleIdentifiers.InvalidEnumValueInTransition, // "FSM006"
        title: "Invalid enum value in transition",
        messageFormat: "Invalid enum value '{0}' for enum type '{1}'. Use a valid enum member.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Error,
        description: "Enum values in transition attributes must be valid members of the specified enum type.");

    public static readonly RuleDefinition MissingPayloadType = new(
        id: RuleIdentifiers.MissingPayloadType,
        title: "Missing payload type definition",
        messageFormat: "State machine forced to use '{0}' variant but no payload type is defined. Add [PayloadType] attribute or change the generation mode.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Error,
        description: "WithPayload and Full variants require at least one payload type to be defined via [PayloadType] attribute.");

    public static readonly RuleDefinition ConflictingPayloadConfiguration = new(
        id: RuleIdentifiers.ConflictingPayloadConfiguration,
        title: "Conflicting payload configuration",
        messageFormat: "Forced 'WithPayload' variant expects single payload type but found {0} trigger-specific types. Use 'Full' variant or remove Force to allow auto-detection.",
        category: "FSM.Generator",
        defaultSeverity: RuleSeverity.Error,
        description: "WithPayload variant supports only single payload type. For multiple payload types use Full variant.");

    public static readonly RuleDefinition InvalidForcedVariantConfiguration = new(
        id: RuleIdentifiers.InvalidForcedVariantConfiguration,
        title: "Invalid forced variant configuration",
        messageFormat: "Forced '{0}' variant conflicts with {1}. {2}",
        category: "FSM.Generator",
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
        category: "FSM.Generator.Async",
        defaultSeverity: RuleSeverity.Error,
        description: "All state machine callbacks (OnEntry, OnExit, Action, Guard) must be either all synchronous or all asynchronous to ensure consistent behavior.");

    public static readonly RuleDefinition InvalidGuardTaskReturnType = new(
        id: RuleIdentifiers.InvalidGuardTaskReturnType,
        title: "Invalid async guard return type",
        messageFormat: "Asynchronous guards must return 'ValueTask<bool>', not 'Task<bool>'. Method '{0}' has an invalid return type.",
        category: "FSM.Generator.Async",
        defaultSeverity: RuleSeverity.Error,
        description: "Using Task<bool> for guards causes unnecessary memory allocations. Use ValueTask<bool> for optimal performance.");

    public static readonly RuleDefinition InvalidAsyncVoid = new(
        id: RuleIdentifiers.InvalidAsyncVoid,
        title: "Callback returns 'async void'",
        messageFormat: "Callback method '{0}' returns 'async void'. Use 'Task' or 'ValueTask' instead to allow the state machine to correctly await its completion and handle exceptions.",
        category: "FSM.Generator.Async",
        defaultSeverity: RuleSeverity.Warning,
        description: "'async void' methods are fire-and-forget and can lead to unhandled exceptions and race conditions. State machine callbacks should always be awaitable.");
    public static readonly RuleDefinition AsyncCallbackInSyncMachine = new(
        id: RuleIdentifiers.AsyncCallbackInSyncMachine,
        title: "Asynchronous callback in synchronous state machine",
        messageFormat: "Method '{0}' is asynchronous, but the state machine is synchronous. Either make all callbacks asynchronous or change the return type of this method.",
        category: "FSM.Generator.Async",
        defaultSeverity: RuleSeverity.Error,
        description: "A state machine must be consistently synchronous or asynchronous. Mixing callback types can lead to unexpected behavior and deadlocks.");
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
    }.AsReadOnly();
}