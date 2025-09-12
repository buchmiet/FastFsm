using System;
using System.Collections.Generic;

namespace FastFsm.Logging.Tests.TestHelpers
{
    public static class InitialStateResolver
    {
        private static readonly Dictionary<string, string[]> Prefs = new(StringComparer.Ordinal)
        {
            ["HsmMachine"] = new[] { "Outside", "Menu", "Parent", "Initial" },
            ["PureStateMachine"] = new[] { "Initial", "Idle" },
            ["BasicStateMachine"] = new[] { "Initial", "Idle" },
            ["PayloadStateMachine"] = new[] { "Initial", "Idle" },
            ["ExtensionsStateMachine"] = new[] { "Initial", "Idle" },
            ["FullStateMachine"] = new[] { "Initial", "Idle" },
            ["MultiPayloadStateMachine"] = new[] { "Initial", "Ready" },
            ["LifecycleMachine"] = new[] { "Initial", "Idle" },
            ["AsyncLifecycleMachine"] = new[] { "Initial", "Idle" },
            ["InternalTransitionMachine"] = new[] { "Initial", "A" },
            ["StructStateMachine"] = new[] { "Initial", "A" },
            ["InitialOnEntryStateMachineActions"] = new[] { "Initial", "A" },
            ["ExampleStateMachine"] = new[] { "Initial", "Idle" },
            ["GuardedStateMachine"] = new[] { "Initial", "Idle" },
            ["ExtensibleMachine"] = new[] { "Initial", "Idle" },
            ["FullMultiPayloadMachine"] = new[] { "Initial", "Ready" }
        };

        public static string Resolve(string machine, Type stateEnumType, string? initial)
        {
            if (!string.IsNullOrWhiteSpace(initial)) return initial!;
            if (Prefs.TryGetValue(machine, out var pref))
            {
                foreach (var s in pref)
                {
                    if (Enum.IsDefined(stateEnumType, s)) return s;
                }
            }
            var names = Enum.GetNames(stateEnumType);
            return names.Length > 0 ? names[0] : throw new ArgumentException($"No states for {stateEnumType.Name}");
        }
    }
}

