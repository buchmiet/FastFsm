using System;
using System.Collections.Generic;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Utility for converting between Fluent and Legacy enum values by name
    /// </summary>
    public static class EnumConverter
    {
        private static readonly Dictionary<Type, Dictionary<string, object>> _enumCache = new();
        
        /// <summary>
        /// Converts an enum value to another enum type by matching names
        /// </summary>
        public static TTarget ConvertEnum<TTarget>(object sourceValue) where TTarget : Enum
        {
            if (sourceValue == null)
                throw new ArgumentNullException(nameof(sourceValue));
                
            string name = sourceValue.ToString()!;
            return (TTarget)ConvertEnum(typeof(TTarget), name);
        }
        
        /// <summary>
        /// Converts an enum name to the specified enum type
        /// </summary>
        public static object ConvertEnum(Type targetType, string name)
        {
            if (!targetType.IsEnum)
                throw new ArgumentException($"Type {targetType.Name} is not an enum", nameof(targetType));
                
            // Check cache first
            if (!_enumCache.TryGetValue(targetType, out var enumDict))
            {
                enumDict = new Dictionary<string, object>();
                foreach (var value in Enum.GetValues(targetType))
                {
                    enumDict[value.ToString()!] = value;
                }
                _enumCache[targetType] = enumDict;
            }
            
            if (enumDict.TryGetValue(name, out var result))
                return result;
                
            throw new InvalidOperationException(
                $"Cannot convert enum value '{name}' to type {targetType.Name}. " +
                $"Available values: {string.Join(", ", enumDict.Keys)}");
        }
        
        /// <summary>
        /// Tries to convert an enum value to another enum type by matching names
        /// </summary>
        public static bool TryConvertEnum<TTarget>(object sourceValue, out TTarget result) where TTarget : Enum
        {
            result = default!;
            
            if (sourceValue == null)
                return false;
                
            try
            {
                result = ConvertEnum<TTarget>(sourceValue);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Converts a list of enum values to another enum type
        /// </summary>
        public static IReadOnlyList<TTarget> ConvertEnumList<TTarget>(IEnumerable<object> sourceValues) where TTarget : Enum
        {
            var result = new List<TTarget>();
            foreach (var value in sourceValues)
            {
                result.Add(ConvertEnum<TTarget>(value));
            }
            return result;
        }
    }
}