using System;
using System.Collections.Generic;
using System.Linq;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Central registry of all state machines and their type mappings.
    /// Sources enum types from MachineTypeRegistry to avoid duplication.
    /// </summary>
    public static class MachineRegistry
    {
        public class MachineInfo
        {
            public string Name { get; set; } = string.Empty;
            public Type? FluentStateType { get; set; }
            public Type? LegacyStateType { get; set; }
            public Type? FluentTriggerType { get; set; }
            public Type? LegacyTriggerType { get; set; }
            public Func<StateMachineWrapperFactory.ApiType, string, IStateMachineTestWrapper>? WrapperFactory { get; set; }
            public bool IsComplete => FluentStateType != null && LegacyStateType != null &&
                                      FluentTriggerType != null && LegacyTriggerType != null;
        }

        private static readonly Dictionary<string, MachineInfo> _machines = new(StringComparer.Ordinal);

        static MachineRegistry()
        {
            foreach (var kv in MachineTypeRegistry.Types)
            {
                var name = kv.Key;
                var pair = kv.Value;

                Func<StateMachineWrapperFactory.ApiType, string, IStateMachineTestWrapper> factory =
                    (api, initial) => StateMachineWrapperFactory.Create(name, api, initial);

                Register(
                    name,
                    pair.FluentState, pair.LegacyState,
                    pair.FluentTrigger, pair.LegacyTrigger,
                    factory);
            }
        }

        public static void Register(
            string name,
            Type? fluentStateType, Type? legacyStateType,
            Type? fluentTriggerType, Type? legacyTriggerType,
            Func<StateMachineWrapperFactory.ApiType, string, IStateMachineTestWrapper>? wrapperFactory)
        {
            _machines[name] = new MachineInfo
            {
                Name = name,
                FluentStateType = fluentStateType,
                LegacyStateType = legacyStateType,
                FluentTriggerType = fluentTriggerType,
                LegacyTriggerType = legacyTriggerType,
                WrapperFactory = wrapperFactory
            };
        }

        public static MachineInfo? GetMachineInfo(string name)
        {
            return _machines.TryGetValue(name, out var info) ? info : null;
        }

        public static IEnumerable<MachineInfo> GetAllMachines() => _machines.Values;
        public static IEnumerable<string> GetMachineNames() => _machines.Keys;
        public static IEnumerable<MachineInfo> GetCompleteMachines() => _machines.Values.Where(m => m.IsComplete);
        public static IEnumerable<MachineInfo> GetIncompleteMachines() => _machines.Values.Where(m => !m.IsComplete);
    }
}

