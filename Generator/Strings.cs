//  New using

namespace Generator;

/// <summary>
/// Collection of constants/literals used by the generator.
/// </summary>
internal static class Strings
{

    // --- Consts for attribute class names ---
    public const string StateMachineAttributeName = "FastFsm.Attributes.StateMachineAttribute";
    public const string TransitionAttributeName = "FastFsm.Attributes.TransitionAttribute";
    public const string InternalTransitionAttributeName = "FastFsm.Attributes.InternalTransitionAttribute";

    // ──────────────────────────────────────────────────────────────
    //  Dependency-Injection (field/type names)
    // ──────────────────────────────────────────────────────────────
    public const string ServiceProviderField = "_serviceProvider";
   // public const string ActivatorUtilitiesClass = "Microsoft.Extensions.DependencyInjection.ActivatorUtilities";
   // public const string DINamespace = "Microsoft.Extensions.DependencyInjection";
    public const string StateMachineContractsNamespace = "FastFsm.Contracts";
  //  public const string StateMachineDINamespace = "FastFsm.DependencyInjection";
    public const string FactorySuffix = "Factory";
    public const string ServiceCollectionExtensionsSuffix = "ServiceCollectionExtensions";
    public const string InitialStateProviderInterface = "IInitialStateProvider";
    public const string StateMachineFactoryInterface = "IStateMachineFactory";
    public const string StateMachineWithPayloadFactoryInterface = "IStateMachineWithPayloadFactory";

    // ──────────────────────────────────────────────────────────────
    //  Full attribute names (Roslyn)
    // ──────────────────────────────────────────────────────────────
    public const string AbstractionsNamespace = "Abstractions.Attributes";

    public const string StateMachineAttributeFullName = $"{AbstractionsNamespace}.StateMachineAttribute";
    public const string TransitionAttributeFullName = $"{AbstractionsNamespace}.TransitionAttribute";
    public const string InternalTransitionAttributeFullName = $"{AbstractionsNamespace}.InternalTransitionAttribute";
    public const string StateAttributeFullName = $"{AbstractionsNamespace}.StateAttribute";
    public const string PayloadTypeAttributeFullName = $"{AbstractionsNamespace}.PayloadTypeAttribute";

    // ──────────────────────────────────────────────────────────────
    //  Callback-types / parameter names in attributes
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
    //  Field/variable names for generated machines
    // ──────────────────────────────────────────────────────────────
    public const string CurrentStateField = "_currentState";
    public const string OriginalStateVar = "originalState";
    public const string SuccessVar = "success";
    public const string GuardResultVar = "guardResult";
    public const string PayloadVar = "payload";
    public const string PayloadMapField = "_payloadMap";

    // ──────────────────────────────────────────────────────────────
    //  Comments in generated code
    // ──────────────────────────────────────────────────────────────
    public const string NoTransitionsComment = "// No transitions defined";
    public const string InitialOnEntryComment = "// Initial OnEntry dispatch";

    // ──────────────────────────────────────────────────────────────
    //  Standard namespaces (for import)
    // ──────────────────────────────────────────────────────────────
    public const string NamespaceSystem = "System";
    public const string NamespaceSystemCollectionsGeneric = "System.Collections.Generic";
    public const string NamespaceSystemLinq = "System.Linq";
    public const string NamespaceSystemRuntimeCompilerServices = "System.Runtime.CompilerServices";
    public const string NamespaceStateMachineContracts = "FastFsm.Contracts";
    public const string NamespaceStateMachineRuntime = "FastFsm.Runtime";
    public const string NamespaceStateMachineRuntimeExtensions = "FastFsm.Runtime.Extensions";
    public const string NamespaceMicrosoftExtensionsLogging = "Microsoft.Extensions.Logging";
    public const string NamespaceMicrosoftDependencyInjection = "Microsoft.Extensions.DependencyInjection";

    public const string GlobalNamespace = "global::";
    public const string DefaultObjectTypeName = "object";

    // ──────────────────────────────────────────────────────────────
    //  Exception handling
    // ──────────────────────────────────────────────────────────────
    public const string OnExceptionAttributeFullName = $"{AbstractionsNamespace}.OnExceptionAttribute";
    public const string ExceptionDirectiveFullName = "FastFsm.Exceptions.ExceptionDirective";
    public const string ExceptionContextFullNameOpen = "FastFsm.Exceptions.ExceptionContext`2";
    public const string CancellationTokenFullName = "System.Threading.CancellationToken";
    public const string ValueTaskOpenFullName = "System.Threading.Tasks.ValueTask`1";
    public const string NamespaceStateMachineExceptions = "FastFsm.Exceptions";

}
