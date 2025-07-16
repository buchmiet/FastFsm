

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

    // Lista wszystkich zdefiniowanych reguł dla łatwiejszego dostępu
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
        GuardWithPayloadInNonPayloadMachine
    }.AsReadOnly();
}