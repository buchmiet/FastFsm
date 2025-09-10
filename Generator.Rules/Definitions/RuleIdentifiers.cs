

namespace Generator.Rules.Definitions;

public static class RuleIdentifiers
{
    // A. Model & Declarations (0100–0599)
    public const string DuplicateTransition = "FSM0400";                         // was FSM001
    public const string UnreachableState = "FSM0500";                            // was FSM002
    public const string InvalidMethodSignature = "FSM0300";                      // was FSM003
    public const string MissingStateMachineAttribute = "FSM0100";                // was FSM004
    public const string InvalidTypesInAttribute = "FSM0101";                     // was FSM005
    public const string InvalidEnumValueInTransition = "FSM0200";                // was FSM006
    // Removed: FSM007 MissingPayloadType, FSM008 ConflictingPayloadConfiguration (unused)
    public const string GuardWithPayloadInNonPayloadMachine = "FSM0301";          // was FSM010
    public const string MixedSyncAsyncCallbacks = "FSM1100";                     // was FSM011
    public const string InvalidGuardTaskReturnType = "FSM1110";                  // was FSM012
    public const string AsyncCallbackInSyncMachine = "FSM1120";                  // was FSM013
    public const string InvalidAsyncVoid = "FSM0302";                            // was FSM014
    
    // C. HSM-specific diagnostics (2000–2099)
    public const string CircularHierarchy = "FSM2000";                           // was FSM100
    public const string OrphanSubstate = "FSM2010";                              // was FSM101
    public const string InvalidHierarchyConfiguration = "FSM2020";              // was FSM102
    public const string MultipleInitialSubstates = "FSM2030";                   // was FSM103
    public const string InvalidHistoryConfiguration = "FSM2040";                // was FSM104
    // Removed: FSM105 ConflictingTransitionTargets (unused)

    // D. Fluent API-specific diagnostics (3000–3099)
    public const string OpenTransition = "FSM3000";                              // was FSM200
    public const string AutoFinalizedTransition = "FSM3010";                     // was FSM201
    public const string MultiplePayloadsOnTransition = "FSM3020";                // was FSM202
    // Removed: FSM203/204/205/206 (unused Fluent/Async diagnostics)
    public const string InvalidPriorityArgument = "FSM3030";                     // was FSM207

    // Global handler diagnostics
    public const string DuplicateOnExceptionHandler = "FSM3050";                 // was FSM208
    public const string InvalidOnExceptionSignature = "FSM3060";                 // was FSM209
    public const string PriorityWithoutActiveTransition = "FSM3040";             // was FSM210

    // E. Generator infrastructure diagnostics (logging, discovery, config) (9000–9099)
    public const string DebugEntry = "FSM9000";                                   // was FSM989D (merged into Processing candidate)
    public const string ConfigurationSections = "FSM9010";                        // was FSM989
    public const string VariantDecision = "FSM9009";                              // was FSM991
    public const string DeclarationPlan = "FSM9001";                              // was FSM992
    public const string EmptyCodeGenerated = "FSM9002";                           // was FSM993
    public const string EnumOnlyStatesFallback = "FSM9003";                       // was FSM994
    public const string MsBuildAnalyzerProperties = "FSM9004";                    // was FSM995
    public const string AddSourceOk = "FSM9005";                                   // was FSM996
    public const string SkippedCandidate = "FSM9006";                              // was FSM997
    public const string DiscoveryOrTrace = "FSM9007";                              // was FSM998
    public const string StartingParse = "FSM9008";                                 // was FSM998A
    public const string ProcessingCandidate = "FSM9000";                           // was FSM990
    public const string LogHelperPreAdd = "FSM9012";                               // was FSM990_PRE
    public const string LogProps = "FSM9013";                                      // was FSM990_PROP
    public const string HsmFlagTracking = "FSM9011";                               // was FSM990_HSM_FLAG
}
