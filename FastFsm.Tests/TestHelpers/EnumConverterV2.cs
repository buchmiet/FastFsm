using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Enhanced enum converter with bidirectional mapping and alias support
    /// </summary>
    public static class EnumConverterV2
    {
        private static readonly ConcurrentDictionary<Type, EnumTypeInfo> _typeCache = new();
        
        /// <summary>
        /// Manual mapping overrides for specific machine types
        /// Key: "MachineName.Direction.SourceValue" -> "TargetValue"
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, string>> Maps = new()
        {
            // Example: Maps["CoreBenchmark"] = new() { ["Fluent.StateA"] = "State_A" };
        };

        private class EnumTypeInfo
        {
            public Dictionary<string, object> ValuesByName { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, List<string>> Aliases { get; } = new(StringComparer.Ordinal);
            public Dictionary<object, string> NamesByValue { get; } = new();
        }

        /// <summary>
        /// Converts a Legacy enum value to Fluent
        /// </summary>
        public static TFluent ToFluent<TFluent>(object legacyValue, string machineName) 
            where TFluent : struct, Enum
        {
            if (legacyValue == null)
                throw new ArgumentNullException(nameof(legacyValue));

            var sourceName = legacyValue.ToString()!;
            var targetName = MapName(machineName, "ToFluent", sourceName, legacyValue.GetType(), typeof(TFluent));
            
            var info = GetOrCreateTypeInfo(typeof(TFluent));
            if (info.ValuesByName.TryGetValue(targetName, out var result))
                return (TFluent)result;

            throw CreateMappingException(machineName, "ToFluent", legacyValue.GetType(), typeof(TFluent), sourceName);
        }

        /// <summary>
        /// Converts a Fluent enum value to Legacy
        /// </summary>
        public static TLegacy ToLegacy<TLegacy>(object fluentValue, string machineName) 
            where TLegacy : struct, Enum
        {
            if (fluentValue == null)
                throw new ArgumentNullException(nameof(fluentValue));

            var sourceName = fluentValue.ToString()!;
            var targetName = MapName(machineName, "ToLegacy", sourceName, fluentValue.GetType(), typeof(TLegacy));
            
            var info = GetOrCreateTypeInfo(typeof(TLegacy));
            if (info.ValuesByName.TryGetValue(targetName, out var result))
                return (TLegacy)result;

            throw CreateMappingException(machineName, "ToLegacy", fluentValue.GetType(), typeof(TLegacy), sourceName);
        }

        /// <summary>
        /// Tries to convert a Legacy enum value to Fluent
        /// </summary>
        public static bool TryToFluent<TFluent>(object legacyValue, string machineName, out TFluent result) 
            where TFluent : struct, Enum
        {
            result = default;
            
            try
            {
                result = ToFluent<TFluent>(legacyValue, machineName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to convert a Fluent enum value to Legacy
        /// </summary>
        public static bool TryToLegacy<TLegacy>(object fluentValue, string machineName, out TLegacy result) 
            where TLegacy : struct, Enum
        {
            result = default;
            
            try
            {
                result = ToLegacy<TLegacy>(fluentValue, machineName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates enum parity between Fluent and Legacy types
        /// </summary>
        public static bool ValidateEnumParity<TFluent, TLegacy>(string machineName, out string report)
            where TFluent : struct, Enum
            where TLegacy : struct, Enum
        {
            var fluentInfo = GetOrCreateTypeInfo(typeof(TFluent));
            var legacyInfo = GetOrCreateTypeInfo(typeof(TLegacy));
            
            var fluentNames = new HashSet<string>(fluentInfo.ValuesByName.Keys, StringComparer.Ordinal);
            var legacyNames = new HashSet<string>(legacyInfo.ValuesByName.Keys, StringComparer.Ordinal);
            
            // Apply manual mappings and aliases
            var mappedFluent = new HashSet<string>(StringComparer.Ordinal);
            var mappedLegacy = new HashSet<string>(StringComparer.Ordinal);
            
            foreach (var fluentName in fluentNames)
            {
                var mappedName = MapName(machineName, "ToLegacy", fluentName, typeof(TFluent), typeof(TLegacy));
                if (legacyNames.Contains(mappedName) || legacyInfo.Aliases.ContainsKey(mappedName))
                {
                    mappedFluent.Add(fluentName);
                    mappedLegacy.Add(mappedName);
                }
            }
            
            foreach (var legacyName in legacyNames)
            {
                var mappedName = MapName(machineName, "ToFluent", legacyName, typeof(TLegacy), typeof(TFluent));
                if (fluentNames.Contains(mappedName) || fluentInfo.Aliases.ContainsKey(mappedName))
                {
                    mappedLegacy.Add(legacyName);
                    mappedFluent.Add(mappedName);
                }
            }
            
            var missingInLegacy = fluentNames.Except(mappedFluent).ToList();
            var missingInFluent = legacyNames.Except(mappedLegacy).ToList();
            
            var reportLines = new List<string>();
            reportLines.Add($"=== Enum Parity Report for {machineName} ===");
            reportLines.Add($"Fluent Type: {typeof(TFluent).Name}");
            reportLines.Add($"Legacy Type: {typeof(TLegacy).Name}");
            reportLines.Add("");
            
            bool hasIssues = false;
            
            if (missingInLegacy.Any())
            {
                hasIssues = true;
                reportLines.Add("❌ Missing in Legacy:");
                foreach (var name in missingInLegacy.OrderBy(x => x))
                {
                    reportLines.Add($"  - {name}");
                    reportLines.Add($"    Hint: Add to Maps[\"{machineName}\"][\"ToLegacy.{name}\"] = \"<LegacyName>\"");
                }
                reportLines.Add("");
            }
            
            if (missingInFluent.Any())
            {
                hasIssues = true;
                reportLines.Add("❌ Missing in Fluent:");
                foreach (var name in missingInFluent.OrderBy(x => x))
                {
                    reportLines.Add($"  - {name}");
                    reportLines.Add($"    Hint: Add to Maps[\"{machineName}\"][\"ToFluent.{name}\"] = \"<FluentName>\"");
                }
                reportLines.Add("");
            }
            
            if (!hasIssues)
            {
                reportLines.Add("✅ Full parity achieved!");
            }
            else
            {
                reportLines.Add("Suggested Aliases:");
                
                // Try to suggest mappings based on similarity
                foreach (var fluentName in missingInLegacy)
                {
                    var similar = FindSimilarName(fluentName, legacyNames);
                    if (similar != null)
                    {
                        reportLines.Add($"  Maps[\"{machineName}\"][\"ToLegacy.{fluentName}\"] = \"{similar}\";");
                    }
                }
                
                foreach (var legacyName in missingInFluent)
                {
                    var similar = FindSimilarName(legacyName, fluentNames);
                    if (similar != null)
                    {
                        reportLines.Add($"  Maps[\"{machineName}\"][\"ToFluent.{legacyName}\"] = \"{similar}\";");
                    }
                }
            }
            
            report = string.Join(Environment.NewLine, reportLines);
            return !hasIssues;
        }

        /// <summary>
        /// Extension method to convert any enum to concrete trigger type
        /// </summary>
        public static object ToConcreteTrigger(this object value, StateMachineWrapperFactory.ApiType api, string machineName)
        {
            if (value == null) return null!;
            
            var registry = MachineRegistry.GetMachineInfo(machineName);
            if (registry == null)
                throw new InvalidOperationException($"Machine '{machineName}' not registered");
            
            if (api == StateMachineWrapperFactory.ApiType.Fluent)
            {
                if (value.GetType() == registry.FluentTriggerType)
                    return value;
                
                // Convert from Legacy to Fluent
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToFluent))!
                    .MakeGenericMethod(registry.FluentTriggerType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
            else
            {
                if (value.GetType() == registry.LegacyTriggerType)
                    return value;
                
                // Convert from Fluent to Legacy
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToLegacy))!
                    .MakeGenericMethod(registry.LegacyTriggerType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
        }

        /// <summary>
        /// Extension method to convert any enum to concrete state type
        /// </summary>
        public static object ToConcreteState(this object value, StateMachineWrapperFactory.ApiType api, string machineName)
        {
            if (value == null) return null!;
            
            var registry = MachineRegistry.GetMachineInfo(machineName);
            if (registry == null)
                throw new InvalidOperationException($"Machine '{machineName}' not registered");
            
            if (api == StateMachineWrapperFactory.ApiType.Fluent)
            {
                if (value.GetType() == registry.FluentStateType)
                    return value;
                
                // Convert from Legacy to Fluent
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToFluent))!
                    .MakeGenericMethod(registry.FluentStateType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
            else
            {
                if (value.GetType() == registry.LegacyStateType)
                    return value;
                
                // Convert from Fluent to Legacy
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToLegacy))!
                    .MakeGenericMethod(registry.LegacyStateType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
        }

        private static string MapName(string machineName, string direction, string sourceName, Type sourceType, Type targetType)
        {
            // Check manual mappings first
            if (Maps.TryGetValue(machineName, out var machineMap))
            {
                var key = $"{direction}.{sourceName}";
                if (machineMap.TryGetValue(key, out var mapped))
                    return mapped;
            }
            
            // Check for aliases on the target type
            var targetInfo = GetOrCreateTypeInfo(targetType);
            foreach (var kvp in targetInfo.Aliases)
            {
                if (kvp.Value.Contains(sourceName))
                    return kvp.Key;
            }
            
            // Default: use the same name
            return sourceName;
        }

        private static EnumTypeInfo GetOrCreateTypeInfo(Type enumType)
        {
            return _typeCache.GetOrAdd(enumType, type =>
            {
                var info = new EnumTypeInfo();
                
                foreach (var value in Enum.GetValues(type))
                {
                    var name = value.ToString()!;
                    info.ValuesByName[name] = value;
                    info.NamesByValue[value] = name;
                    
                    // Check for aliases
                    var field = type.GetField(name);
                    if (field != null)
                    {
                        var aliases = field.GetCustomAttributes<EnumAliasAttribute>()
                            .Select(a => a.Alias)
                            .ToList();
                        
                        if (aliases.Any())
                        {
                            info.Aliases[name] = aliases;
                            
                            // Also add reverse mappings
                            foreach (var alias in aliases)
                            {
                                info.ValuesByName[alias] = value;
                            }
                        }
                    }
                }
                
                return info;
            });
        }

        private static string? FindSimilarName(string name, IEnumerable<string> candidates)
        {
            // Simple similarity: case-insensitive match
            var lower = name.ToLowerInvariant();
            var exact = candidates.FirstOrDefault(c => c.ToLowerInvariant() == lower);
            if (exact != null) return exact;
            
            // Try removing underscores
            var withoutUnderscore = name.Replace("_", "");
            exact = candidates.FirstOrDefault(c => c.Replace("_", "").Equals(withoutUnderscore, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            
            // Try adding underscores (for hierarchical states)
            if (name.Contains("_"))
            {
                var parts = name.Split('_');
                if (parts.Length == 2)
                {
                    // Try without underscore
                    var combined = parts[0] + parts[1];
                    exact = candidates.FirstOrDefault(c => c.Equals(combined, StringComparison.OrdinalIgnoreCase));
                    if (exact != null) return exact;
                }
            }
            
            return null;
        }

        private static InvalidOperationException CreateMappingException(
            string machineName, string direction, Type sourceType, Type targetType, string valueName)
        {
            var targetInfo = GetOrCreateTypeInfo(targetType);
            var availableValues = string.Join(", ", targetInfo.ValuesByName.Keys.OrderBy(x => x));
            
            return new InvalidOperationException(
                $"Enum mapping failed (machine: {machineName}, direction: {direction}, " +
                $"sourceType: {sourceType.Name}, targetType: {targetType.Name}, value: {valueName}). " +
                $"Available target values: [{availableValues}]. " +
                $"Hint: Add to Maps[\"{machineName}\"][\"{direction}.{valueName}\"] = \"<TargetName>\" " +
                $"or add [EnumAlias(\"{valueName}\")] attribute on the target enum value.");
        }
    }
}