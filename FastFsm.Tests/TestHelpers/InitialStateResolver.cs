using System;
using System.Collections.Generic;
using System.Linq;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Resolves initial state names for state machines with fallback logic
    /// </summary>
    public static class InitialStateResolver
    {
        // Global preferences - order matters, most likely first
        private static readonly string[] GlobalPrefs = new[]
        {
            "Initial", "A", "Active", "New", "Idle", "Start", 
            "Disconnected", "Parent", "Root", "Outside", "None", 
            "Off", "Waiting", "Ready", "Stopped", "Closed", 
            "Unknown", "Default", "Empty", "Uninitialized", "NotStarted"
        };

        // Machine-specific preferences to avoid mismatches
        private static readonly Dictionary<string, string[]> MachinePrefs = new(StringComparer.Ordinal)
        {
            ["InternalTransition"] = new[] { "Active", "A", "Initial" },
            ["GuardPermitted"] = new[] { "Idle", "Initial", "A" },
            ["PayloadStateMachine"] = new[] { "Initial", "New" },
            ["FullMultiPayload"] = new[] { "Initial", "Configured" },
            ["ExceptionCallback"] = new[] { "A", "Initial" },
            ["CoreBenchmark"] = new[] { "A", "Initial" },
            
            // HSM machines
            ["SimpleParentChild"] = new[] { "Idle", "Initial" },
            ["DeepHistory"] = new[] { "Outside", "Out", "Initial" },
            ["ShallowHistory"] = new[] { "Outside", "Out", "Initial" },
            ["InitialChild"] = new[] { "Outside", "Initial" }
        };

        /// <summary>
        /// Resolves the initial state name, using fallback logic if preferred name is invalid
        /// </summary>
        /// <typeparam name="TState">The state enum type</typeparam>
        /// <param name="machineName">Name of the machine (for machine-specific preferences)</param>
        /// <param name="preferredName">Preferred initial state name (can be null or invalid)</param>
        /// <returns>Valid state name from the enum</returns>
        public static string ResolveOrDefault<TState>(string machineName, string? preferredName)
            where TState : struct, Enum
        {
            var availableStates = new HashSet<string>(Enum.GetNames(typeof(TState)), StringComparer.Ordinal);
            
            // 1) Exact match with preferred name
            if (!string.IsNullOrWhiteSpace(preferredName) && availableStates.Contains(preferredName))
            {
                return preferredName;
            }

            // 2) Machine-specific preferences
            if (MachinePrefs.TryGetValue(machineName, out var prefs))
            {
                foreach (var pref in prefs)
                {
                    if (availableStates.Contains(pref))
                        return pref;
                }
            }

            // 3) Global preferences
            foreach (var pref in GlobalPrefs)
            {
                if (availableStates.Contains(pref))
                    return pref;
            }

            // 4) First enum value as absolute fallback
            var firstState = Enum.GetNames(typeof(TState)).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstState))
                return firstState;

            throw new InvalidOperationException(
                $"Cannot resolve initial state (machine: {machineName}, enum: {typeof(TState).Name}). " +
                $"No valid states found in enum.");
        }

        /// <summary>
        /// Tries to resolve the initial state name without throwing
        /// </summary>
        public static bool TryResolveOrDefault<TState>(
            string machineName, 
            string? preferredName, 
            out string resolvedName) where TState : struct, Enum
        {
            try
            {
                resolvedName = ResolveOrDefault<TState>(machineName, preferredName);
                return true;
            }
            catch
            {
                resolvedName = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Gets all available state names for a given enum type
        /// </summary>
        public static IReadOnlyList<string> GetAvailableStates<TState>() where TState : struct, Enum
        {
            return Enum.GetNames(typeof(TState));
        }
    }
}