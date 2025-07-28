//  ←  NOWY using

namespace Generator;

/// <summary>
/// Zbiór stałych/literalów używanych przez generator.
/// </summary>
internal static class Strings
{

    // --- Consts for attribute class names ---
    public const string StateMachineAttributeName = "StateMachine.Attributes.StateMachineAttribute";
    public const string TransitionAttributeName = "StateMachine.Attributes.TransitionAttribute";
    public const string InternalTransitionAttributeName = "StateMachine.Attributes.InternalTransitionAttribute";

    // ──────────────────────────────────────────────────────────────
    //  Dependency-Injection (nazwy pól/typów)
    // ──────────────────────────────────────────────────────────────
    public const string ServiceProviderField = "_serviceProvider";
   // public const string ActivatorUtilitiesClass = "Microsoft.Extensions.DependencyInjection.ActivatorUtilities";
   // public const string DINamespace = "Microsoft.Extensions.DependencyInjection";
    public const string StateMachineContractsNamespace = "StateMachine.Contracts";
  //  public const string StateMachineDINamespace = "StateMachine.DependencyInjection";
    public const string FactorySuffix = "Factory";
    public const string ServiceCollectionExtensionsSuffix = "ServiceCollectionExtensions";
    public const string InitialStateProviderInterface = "IInitialStateProvider";
    public const string StateMachineFactoryInterface = "IStateMachineFactory";
    public const string StateMachineWithPayloadFactoryInterface = "IStateMachineWithPayloadFactory";

    // ──────────────────────────────────────────────────────────────
    //  Pełne nazwy atrybutów (Roslyn)
    // ──────────────────────────────────────────────────────────────
    public const string AbstractionsNamespace = "Abstractions.Attributes";

    public const string StateMachineAttributeFullName = $"{AbstractionsNamespace}.StateMachineAttribute";
    public const string TransitionAttributeFullName = $"{AbstractionsNamespace}.TransitionAttribute";
    public const string InternalTransitionAttributeFullName = $"{AbstractionsNamespace}.InternalTransitionAttribute";
    public const string StateAttributeFullName = $"{AbstractionsNamespace}.StateAttribute";
    public const string PayloadTypeAttributeFullName = $"{AbstractionsNamespace}.PayloadTypeAttribute";
    public const string GenerationModeAttributeFullName = $"{AbstractionsNamespace}.GenerationModeAttribute";

    // ──────────────────────────────────────────────────────────────
    //  Callback-types / nazwy parametrów w atrybutach
    // ──────────────────────────────────────────────────────────────
    public const string GuardCallbackType = "Guard";
    public const string ActionCallbackType = "Action";
    public const string ActionCtorCallbackType = "Action (from constructor)";
    public const string OnEntryCallbackType = "OnEntry";
    public const string OnExitCallbackType = "OnExit";
    public const string PayloadTypeArgName = "PayloadType";
    public const string PayloadTypeForTriggerArgName = "PayloadType for trigger ";
    public const string PayloadTypeForTriggerConflictArgName = "PayloadType for trigger '{0}'";
    public const string ConflictsWithAlreadyDefinedType = "conflicts with already defined type";
    public const string NullString = "null";

    // ──────────────────────────────────────────────────────────────
    //  Inlining / wygenerowany kod
    // ──────────────────────────────────────────────────────────────
    public const string MethodImplAttribute = "System.Runtime.CompilerServices.MethodImpl";
    public const string AggressiveInliningAttribute = "System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining";
    public const string ReadOnlyListType = "System.Collections.Generic.IReadOnlyList";
    public const string ArrayEmptyMethod = "System.Array.Empty";

    // ──────────────────────────────────────────────────────────────
    //  Nazwy pól/zmiennych generowanych maszyn
    // ──────────────────────────────────────────────────────────────
    public const string CurrentStateField = "_currentState";
    public const string OriginalStateVar = "originalState";
    public const string SuccessVar = "success";
    public const string GuardResultVar = "guardResult";
    public const string PayloadVar = "payload";
    public const string PayloadMapField = "_payloadMap";

    // ──────────────────────────────────────────────────────────────
    //  Komentarze w kodzie gen.
    // ──────────────────────────────────────────────────────────────
    public const string NoTransitionsComment = "// No transitions defined";
    public const string InitialOnEntryComment = "// Initial OnEntry dispatch";

    // ──────────────────────────────────────────────────────────────
    //  Standardowe przestrzenie nazw (do importu)
    // ──────────────────────────────────────────────────────────────
    public const string NamespaceSystem = "System";
    public const string NamespaceSystemCollectionsGeneric = "System.Collections.Generic";
    public const string NamespaceSystemLinq = "System.Linq";
    public const string NamespaceSystemRuntimeCompilerServices = "System.Runtime.CompilerServices";
    public const string NamespaceStateMachineContracts = "StateMachine.Contracts";
    public const string NamespaceStateMachineRuntime = "StateMachine.Runtime";
    public const string NamespaceStateMachineRuntimeExtensions = "StateMachine.Runtime.Extensions";
    public const string NamespaceMicrosoftExtensionsLogging = "Microsoft.Extensions.Logging";
    public const string NamespaceMicrosoftDependencyInjection = "Microsoft.Extensions.DependencyInjection";

    public const string GlobalNamespace = "global::";
    public const string DefaultObjectTypeName = "object";

}
