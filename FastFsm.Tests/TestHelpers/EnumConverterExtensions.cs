using System;
using System.Collections.Generic;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Extension methods for enum conversion
    /// </summary>
    public static class EnumConverterExtensions
    {
        /// <summary>
        /// Converts a trigger object to the concrete trigger type for the specified API and machine
        /// </summary>
        public static object ToConcreteTrigger(this object trigger, StateMachineWrapperFactory.ApiType apiType, string machineName)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            
            // If it's a string, we need to convert it to the appropriate enum
            if (trigger is string triggerName)
            {
                // Determine target enum type from MachineTypeRegistry
                var api = apiType == StateMachineWrapperFactory.ApiType.Fluent ? Api.Fluent : Api.Legacy;
                Type targetEnumType = MachineTypeRegistry.GetTriggerType(machineName, api);

                try
                {
                    // Parse into the exact target enum type
                    return Enum.Parse(targetEnumType, triggerName, ignoreCase: false);
                }
                catch
                {
                    return trigger; // return original string if parsing fails
                }
            }
            
            // If it's already an enum, return as-is
            if (trigger.GetType().IsEnum)
            {
                return trigger;
            }
            
            // Default: return as-is
            return trigger;
        }
        
        // Historical mapping removed in favor of MachineTypeRegistry
    }
}
