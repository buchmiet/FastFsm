using System;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Represents the enum types used by a state machine for both Fluent and Legacy APIs
    /// </summary>
    public readonly record struct EnumTypePair(
        Type FluentState, 
        Type LegacyState,
        Type FluentTrigger, 
        Type LegacyTrigger)
    {
        /// <summary>
        /// Gets the appropriate enum type based on API and whether it's a state or trigger
        /// </summary>
        public Type For(Api api, bool isState) =>
            (api, isState) switch
            {
                (Api.Fluent, true) => FluentState,
                (Api.Legacy, true) => LegacyState,
                (Api.Fluent, false) => FluentTrigger,
                (Api.Legacy, false) => LegacyTrigger,
                _ => throw new ArgumentException($"Invalid combination: {api}, isState={isState}")
            };
            
        /// <summary>
        /// Checks if this pair uses the same enums for both APIs
        /// </summary>
        public bool UsesSameEnums => 
            FluentState == LegacyState && FluentTrigger == LegacyTrigger;
            
        /// <summary>
        /// Checks if states are the same type in both APIs
        /// </summary>
        public bool UsesSameStateEnum => FluentState == LegacyState;
        
        /// <summary>
        /// Checks if triggers are the same type in both APIs
        /// </summary>
        public bool UsesSameTriggerEnum => FluentTrigger == LegacyTrigger;
    }

    /// <summary>
    /// API type enumeration
    /// </summary>
    public enum Api 
    { 
        Fluent, 
        Legacy 
    }
}