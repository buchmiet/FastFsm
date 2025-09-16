using System;
using Shouldly;

namespace FastFsm.Logging.Tests.TestHelpers
{
    [Flags]
    public enum ApiCapabilities
    {
        None = 0,
        HasAsync = 1 << 0,
        HasDefaultPayload = 1 << 1,
        HasMultiPayloads = 1 << 2,
        HasInternalTransitions = 1 << 3,
        IsHierarchical = 1 << 4,
        RequiresAsyncPath = 1 << 5
    }

    public static class ApiCapabilitiesExtensions
    {
        public static bool Has(this ApiCapabilities caps, ApiCapabilities flag) => (caps & flag) == flag;
        public static bool SupportsPayloads(this ApiCapabilities caps) => caps.Has(ApiCapabilities.HasDefaultPayload) || caps.Has(ApiCapabilities.HasMultiPayloads);
        public static void ShouldHaveFlag(this ApiCapabilities caps, ApiCapabilities flag)
        {
            // Use Shouldly extension semantics on bool
            caps.Has(flag).ShouldBeTrue();
        }
    }
}
