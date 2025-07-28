

namespace Generator.Rules.Definitions;

public static class RuleIdentifiers
{
    public const string DuplicateTransition = "FSM001";
    public const string UnreachableState = "FSM002";
    public const string InvalidMethodSignature = "FSM003";
    public const string MissingStateMachineAttribute = "FSM004";
    public const string InvalidTypesInAttribute = "FSM005";
    public const string InvalidEnumValueInTransition = "FSM006";
    public const string MissingPayloadType = "FSM007";
    public const string ConflictingPayloadConfiguration = "FSM008";
    public const string InvalidForcedVariantConfiguration = "FSM009";
    public const string GuardWithPayloadInNonPayloadMachine = "FSM010";
    public const string MixedSyncAsyncCallbacks = "FSM011";
    public const string InvalidGuardTaskReturnType = "FSM012";
    public const string AsyncCallbackInSyncMachine = "FSM013"; // Ta reguła jest logocznie tożsama z FSM011, ale może mieć inny komunikat
    public const string InvalidAsyncVoid = "FSM014";

}
