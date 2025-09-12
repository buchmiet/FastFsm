using System;
using System.Collections.Generic;

namespace FastFsm.Async.Tests.TestHelpers
{
    public static class InitialStateResolver
    {
        private static readonly Dictionary<string, string[]> Prefs = new(StringComparer.Ordinal)
        {
            ["InitialChild"] = new[] { "Outside", "Parent", "Parent_A" },
            ["ShallowHistory"] = new[] { "Outside", "Menu", "Menu_Main" },
            ["DeepHistory"] = new[] { "Out", "Work", "Work_S1" },
            ["Internal"] = new[] { "Parent", "Child" },
            ["Priority"] = new[] { "Parent", "Child", "A" },
            ["ChildOverrides"] = new[] { "Parent", "Child" },
            ["SourceOrderTie"] = new[] { "A", "B" },
            ["Inheritance"] = new[] { "Outside", "Parent", "Parent_A" }
        };

        public static string Resolve(string machine, Type stateEnumType, string? initial)
        {
            if (!string.IsNullOrWhiteSpace(initial)) return initial!;
            if (Prefs.TryGetValue(machine, out var list))
            {
                foreach (var s in list)
                {
                    if (Enum.IsDefined(stateEnumType, s)) return s;
                }
            }
            // fallback to first value
            var names = Enum.GetNames(stateEnumType);
            return names.Length > 0 ? names[0] : throw new ArgumentException($"No states for {stateEnumType.Name}");
        }
    }
}

