using System;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Describes the capabilities of a state machine API implementation
    /// </summary>
    [Flags]
    public enum ApiCapabilities
    {
        None = 0,
        
        /// <summary>
        /// Machine supports async operations (StartAsync, FireAsync, etc.)
        /// </summary>
        HasAsync = 1 << 0,
        
        /// <summary>
        /// Machine has a default payload type configured
        /// </summary>
        HasDefaultPayload = 1 << 1,
        
        /// <summary>
        /// Machine supports multiple payload types via .Payload<T>()
        /// </summary>
        HasMultiPayloads = 1 << 2,
        
        /// <summary>
        /// Machine supports internal transitions
        /// </summary>
        HasInternalTransitions = 1 << 3,
        
        /// <summary>
        /// Machine is hierarchical (HSM)
        /// </summary>
        IsHierarchical = 1 << 4,
        
        /// <summary>
        /// Machine has async guards or actions
        /// </summary>
        RequiresAsyncPath = 1 << 5
    }
    
    /// <summary>
    /// Extension methods for ApiCapabilities
    /// </summary>
    public static class ApiCapabilitiesExtensions
    {
        public static bool Has(this ApiCapabilities caps, ApiCapabilities flag)
        {
            return (caps & flag) == flag;
        }
        
        public static bool RequiresAsync(this ApiCapabilities caps)
        {
            return caps.Has(ApiCapabilities.RequiresAsyncPath) || caps.Has(ApiCapabilities.HasAsync);
        }
        
        public static bool SupportsPayloads(this ApiCapabilities caps)
        {
            return caps.Has(ApiCapabilities.HasDefaultPayload) || caps.Has(ApiCapabilities.HasMultiPayloads);
        }
    }
}