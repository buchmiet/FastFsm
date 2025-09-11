using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Enhanced enum converter with bidirectional mapping, auto-aliasing, and normalization support
    /// </summary>
    public static class EnumConverterV2
    {
        private static readonly ConcurrentDictionary<Type, EnumTypeInfo> _typeCache = new();
        private static readonly ConcurrentDictionary<(string machine, Type from, Type to), Dictionary<string, string>> _autoMaps = new();
        
        /// <summary>
        /// Manual mapping overrides for specific machine types
        /// Key: "MachineName" -> Dictionary of mappings
        /// Inner dictionary: "Direction.SourceValue" -> "TargetValue" or just "SourceValue" -> "TargetValue"
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, string>> Maps = new()
        {
            // Add manual overrides only where auto-aliasing fails
            ["GuardPermitted"] = new()
            {
                // Both use same enum, no mapping needed
            },
            ["InternalTransition"] = new()
            {
                // Both use same enum, no mapping needed
            },
            ["PayloadStateMachine"] = new()
            {
                // Both use same enum, no mapping needed
            },
            ["FullMultiPayload"] = new()
            {
                // Both use same enum, no mapping needed
            },
            ["ExceptionCallback"] = new()
            {
                // Both use same enum, no mapping needed
            }
        };

        private class EnumTypeInfo
        {
            public Dictionary<string, object> ValuesByName { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, List<string>> Aliases { get; } = new(StringComparer.Ordinal);
            public Dictionary<object, string> NamesByValue { get; } = new();
        }

        /// <summary>
        /// Normalizes enum names for matching (removes non-alphanumeric, converts to uppercase)
        /// </summary>
        private static string Normalize(string name)
        {
            return new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        /// <summary>
        /// Builds automatic mapping between enum types based on normalized names
        /// </summary>
        private static Dictionary<string, string> BuildAutoMap(Type fromEnum, Type toEnum)
        {
            var fromNames = Enum.GetNames(fromEnum);
            var toNames = Enum.GetNames(toEnum);

            // Group target names by normalized form
            var toByNorm = toNames
                .GroupBy(Normalize)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var fromName in fromNames)
            {
                var normalizedKey = Normalize(fromName);
                if (toByNorm.TryGetValue(normalizedKey, out var candidates))
                {
                    // Prefer exact case match, otherwise first candidate
                    var exactMatch = candidates.FirstOrDefault(t => 
                        string.Equals(t, fromName, StringComparison.Ordinal));
                    map[fromName] = exactMatch ?? candidates[0];
                }
            }

            return map;
        }

        /// <summary>
        /// Core conversion logic with auto-aliasing and normalization
        /// </summary>
        private static string ConvertCore(string machineName, Type fromEnum, Type toEnum, string sourceName, string direction)
        {
            // 1) If same enum type, return as-is
            if (fromEnum == toEnum)
                return sourceName;

            // 2) Direct exact name match in target enum
            var targetNames = Enum.GetNames(toEnum);
            if (targetNames.Contains(sourceName, StringComparer.Ordinal))
                return sourceName;

            // 3) Check manual mappings
            if (Maps.TryGetValue(machineName, out var machineMap))
            {
                // Try with direction prefix
                var keyWithDirection = $"{direction}.{sourceName}";
                if (machineMap.TryGetValue(keyWithDirection, out var mapped))
                    return mapped;
                
                // Try without direction (bidirectional mapping)
                if (machineMap.TryGetValue(sourceName, out mapped))
                    return mapped;
            }

            // 4) Use auto-generated mapping
            var autoMap = _autoMaps.GetOrAdd(
                (machineName, fromEnum, toEnum),
                _ => BuildAutoMap(fromEnum, toEnum));
            
            if (autoMap.TryGetValue(sourceName, out var autoMapped))
                return autoMapped;

            // 5) Try normalized single name matching
            var normalized = Normalize(sourceName);
            var toByNorm = targetNames.ToLookup(Normalize, StringComparer.Ordinal);
            var normalizedMatch = toByNorm[normalized].FirstOrDefault();
            if (normalizedMatch != null)
                return normalizedMatch;

            // 6) Check for aliases on target type
            var targetInfo = GetOrCreateTypeInfo(toEnum);
            foreach (var kvp in targetInfo.Aliases)
            {
                if (kvp.Value.Contains(sourceName))
                    return kvp.Key;
            }

            // 7) Fail with detailed error
            var availableValues = string.Join(", ", targetNames);
            throw new InvalidOperationException(
                $"Enum mapping failed (machine: {machineName}, direction: {direction}, " +
                $"from: {fromEnum.Name}, to: {toEnum.Name}, value: {sourceName}). " +
                $"Available target values: [{availableValues}]. " +
                $"Hint: Add mapping to EnumConverterV2.Maps[\"{machineName}\"][\"{direction}.{sourceName}\"] = \"<TargetName>\" " +
                $"or ensure consistent naming between enums.");
        }

        /// <summary>
        /// Converts a Legacy enum value to Fluent
        /// </summary>
        public static TFluent ToFluent<TFluent>(object legacyValue, string machineName) 
            where TFluent : struct, Enum
        {
            if (legacyValue == null)
                throw new ArgumentNullException(nameof(legacyValue));
                
            // SHORT-CIRCUIT: If already the target type, no conversion needed
            if (legacyValue is TFluent fluentTyped)
                return fluentTyped;
                
            // SHORT-CIRCUIT: If same type but different instance
            if (legacyValue.GetType() == typeof(TFluent))
                return (TFluent)legacyValue;

            var sourceName = legacyValue.ToString()!;
            var targetName = ConvertCore(machineName, legacyValue.GetType(), typeof(TFluent), sourceName, "ToFluent");
            
            var info = GetOrCreateTypeInfo(typeof(TFluent));
            if (info.ValuesByName.TryGetValue(targetName, out var result))
                return (TFluent)result;

            // This shouldn't happen if ConvertCore succeeded
            throw new InvalidOperationException(
                $"Internal error: ConvertCore returned '{targetName}' but it's not in target enum {typeof(TFluent).Name}");
        }

        /// <summary>
        /// Converts a Fluent enum value to Legacy
        /// </summary>
        public static TLegacy ToLegacy<TLegacy>(object fluentValue, string machineName) 
            where TLegacy : struct, Enum
        {
            if (fluentValue == null)
                throw new ArgumentNullException(nameof(fluentValue));
                
            // SHORT-CIRCUIT: If already the target type, no conversion needed
            if (fluentValue is TLegacy legacyTyped)
                return legacyTyped;
                
            // SHORT-CIRCUIT: If same type but different instance
            if (fluentValue.GetType() == typeof(TLegacy))
                return (TLegacy)fluentValue;

            var sourceName = fluentValue.ToString()!;
            var targetName = ConvertCore(machineName, fluentValue.GetType(), typeof(TLegacy), sourceName, "ToLegacy");
            
            var info = GetOrCreateTypeInfo(typeof(TLegacy));
            if (info.ValuesByName.TryGetValue(targetName, out var result))
                return (TLegacy)result;

            // This shouldn't happen if ConvertCore succeeded
            throw new InvalidOperationException(
                $"Internal error: ConvertCore returned '{targetName}' but it's not in target enum {typeof(TLegacy).Name}");
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
        public static (bool isValid, List<string> errors) ValidateEnumParity<TFluent, TLegacy>(string machineName)
            where TFluent : struct, Enum
            where TLegacy : struct, Enum
        {
            var errors = new List<string>();
            
            // SHORT-CIRCUIT: If same type, they're already in parity
            if (typeof(TFluent) == typeof(TLegacy))
            {
                return (true, errors); // No conversion needed, perfect parity
            }
            
            var fluentNames = Enum.GetNames(typeof(TFluent));
            var legacyNames = Enum.GetNames(typeof(TLegacy));

            // Check if all Fluent values can map to Legacy
            foreach (var fluentName in fluentNames)
            {
                try
                {
                    var fluentValue = Enum.Parse<TFluent>(fluentName);
                    var _ = ToLegacy<TLegacy>(fluentValue, machineName);
                }
                catch (Exception ex)
                {
                    errors.Add($"Fluent -> Legacy: {fluentName} failed: {ex.Message}");
                }
            }

            // Check if all Legacy values can map to Fluent
            foreach (var legacyName in legacyNames)
            {
                try
                {
                    var legacyValue = Enum.Parse<TLegacy>(legacyName);
                    var _ = ToFluent<TFluent>(legacyValue, machineName);
                }
                catch (Exception ex)
                {
                    errors.Add($"Legacy -> Fluent: {legacyName} failed: {ex.Message}");
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates enum parity between Fluent and Legacy types (out parameter version for reflection)
        /// </summary>
        public static bool ValidateEnumParity<TFluent, TLegacy>(string machineName, out string report)
            where TFluent : struct, Enum
            where TLegacy : struct, Enum
        {
            var result = ValidateEnumParity<TFluent, TLegacy>(machineName);
            
            if (result.errors != null && result.errors.Count > 0)
            {
                report = $"Enum parity issues for {machineName}:\n" + string.Join("\n", result.errors);
            }
            else
            {
                report = $"✓ Enum parity OK for {machineName}";
            }
            
            return result.isValid;
        }

        /// <summary>
        /// Extension method to convert any enum to concrete trigger type
        /// </summary>
        public static object ConvertTrigger(
            object value,
            string machineName,
            StateMachineWrapperFactory.ApiType api)
        {
            if (value == null) return null!;

            // Resolve target enum type from MachineTypeRegistry (single source of truth)
            var targetApi = api == StateMachineWrapperFactory.ApiType.Fluent ? Api.Fluent : Api.Legacy;
            var targetType = MachineTypeRegistry.GetTriggerType(machineName, targetApi);

            // Pass-through if already of the correct type
            if (value.GetType() == targetType)
                return value;

            // Convert using strong generic path
            if (targetApi == Api.Fluent)
            {
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToFluent))!
                    .MakeGenericMethod(targetType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
            else
            {
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToLegacy))!
                    .MakeGenericMethod(targetType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
        }

        /// <summary>
        /// Extension method to convert any enum to concrete state type
        /// </summary>
        public static object ConvertState(
            object value,
            string machineName,
            StateMachineWrapperFactory.ApiType api)
        {
            if (value == null) return null!;

            // Resolve target enum type from MachineTypeRegistry (single source of truth)
            var targetApi = api == StateMachineWrapperFactory.ApiType.Fluent ? Api.Fluent : Api.Legacy;
            var targetType = MachineTypeRegistry.GetStateType(machineName, targetApi);

            // Pass-through if already of the correct type
            if (value.GetType() == targetType)
                return value;

            // Convert using strong generic path
            if (targetApi == Api.Fluent)
            {
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToFluent))!
                    .MakeGenericMethod(targetType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
            else
            {
                var method = typeof(EnumConverterV2).GetMethod(nameof(ToLegacy))!
                    .MakeGenericMethod(targetType);
                return method.Invoke(null, new[] { value, machineName })!;
            }
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
                    
                    // Check for aliases via attributes
                    var field = type.GetField(name);
                    if (field != null)
                    {
                        var aliases = field.GetCustomAttributes<EnumAliasAttribute>()
                            .Select(a => a.Alias)
                            .ToList();
                        
                        if (aliases.Any())
                        {
                            info.Aliases[name] = aliases;
                            
                            // Add reverse mappings
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

        /// <summary>
        /// Gets auto-generated mapping information for diagnostics
        /// </summary>
        public static string GetAutoMapDiagnostics(string machineName, Type fromEnum, Type toEnum)
        {
            var autoMap = _autoMaps.GetOrAdd(
                (machineName, fromEnum, toEnum),
                _ => BuildAutoMap(fromEnum, toEnum));

            var lines = new List<string>
            {
                $"Auto-map for {machineName} ({fromEnum.Name} -> {toEnum.Name}):",
                $"  Total mappings: {autoMap.Count}"
            };

            foreach (var kvp in autoMap.OrderBy(x => x.Key))
            {
                lines.Add($"  {kvp.Key} -> {kvp.Value}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
