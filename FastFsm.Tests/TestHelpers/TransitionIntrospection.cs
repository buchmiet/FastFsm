using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.Text.Json;
using Abstractions.Attributes;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Introspection utilities for determining transition shapes and payload requirements
    /// </summary>
    public static class TransitionIntrospection
    {
        // Cache for machine metadata
        private static readonly ConcurrentDictionary<Type, MachineMetadata> _machineCache = new();
        
        // Manual transition shape mappings for machines without exposed metadata
        // TODO: Replace with generator metadata when exposed
        private static readonly Dictionary<string, Dictionary<string, TransitionShape>> _manualMappings = new()
        {
            ["PayloadStateMachine"] = new()
            {
                ["Start"] = new TransitionShape 
                { 
                    MachineName = "PayloadStateMachine",
                    SourceState = "Initial", 
                    TargetState = "Processing",
                    Trigger = "Start",
                    ExplicitPayloadType = typeof(Features.Payload.OrderData),
                    IsAsync = false
                },
                ["Process"] = new TransitionShape
                {
                    MachineName = "PayloadStateMachine",
                    SourceState = "Processing",
                    TargetState = "Completed", 
                    Trigger = "Process",
                    ExplicitPayloadType = typeof(Features.Payload.ProcessConfig),
                    IsAsync = false
                }
            },
            ["FullMultiPayloadMachine"] = new()
            {
                ["Configure"] = new TransitionShape
                {
                    MachineName = "FullMultiPayloadMachine",
                    SourceState = "Initial",
                    TargetState = "Configured",
                    Trigger = "Configure",
                    ExplicitPayloadType = typeof(Features.Payload.ConfigPayload),
                    IsAsync = false
                },
                ["Process"] = new TransitionShape
                {
                    MachineName = "FullMultiPayloadMachine",
                    SourceState = "Configured",
                    TargetState = "Processing",
                    Trigger = "Process",
                    ExplicitPayloadType = typeof(Features.Payload.DataPayload),
                    IsAsync = false
                },
                ["Error"] = new TransitionShape
                {
                    MachineName = "FullMultiPayloadMachine",
                    SourceState = "Processing",
                    TargetState = "Failed",
                    Trigger = "Error",
                    ExplicitPayloadType = typeof(Features.Payload.ErrorPayload),
                    IsAsync = false
                }
            },
            ["ExceptionCallbackMachine"] = new()
            {
                ["Go"] = new TransitionShape
                {
                    MachineName = "ExceptionCallbackMachine",
                    SourceState = "A",
                    TargetState = "B",
                    Trigger = "Go",
                    IsAsync = true, // Has async action that throws
                    UsesDefaultPayload = false
                }
            },
            ["InternalOnlyMachine"] = new()
            {
                ["Action"] = new TransitionShape
                {
                    MachineName = "InternalOnlyMachine",
                    SourceState = "Static",
                    TargetState = "Static",
                    Trigger = "Action",
                    IsInternal = true,
                    IsAsync = false
                }
            }
        };
        
        // Payload adapters for test coercion
        private static readonly Dictionary<(Type from, Type to), Func<object, object>> _adapters = new();
        
        public class MachineMetadata
        {
            public Type? DefaultPayloadType { get; set; }
            public bool HasAsyncHandlers { get; set; }
            public Dictionary<string, TransitionShape> Transitions { get; } = new();
        }
        
        /// <summary>
        /// Get the transition shape for a specific machine and trigger
        /// </summary>
        public static TransitionShape? GetTransitionShape(string machineName, string trigger, string? currentState = null)
        {
            // First check manual mappings
            if (_manualMappings.TryGetValue(machineName, out var transitions))
            {
                if (transitions.TryGetValue(trigger, out var shape))
                {
                    return shape;
                }
            }
            
            // Try to introspect from machine type
            var machineInfo = MachineRegistry.GetMachineInfo(machineName);
            if (machineInfo == null) return null;
            
            // For now, return a basic shape based on machine capabilities
            return new TransitionShape
            {
                MachineName = machineName,
                Trigger = trigger,
                SourceState = currentState,
                // These would be determined from actual machine metadata
                UsesDefaultPayload = false,
                IsAsync = false,
                IsInternal = false
            };
        }
        
        /// <summary>
        /// Get machine metadata including default payload type
        /// </summary>
        public static MachineMetadata GetMachineMetadata(Type machineType)
        {
            return _machineCache.GetOrAdd(machineType, type =>
            {
                var metadata = new MachineMetadata();
                
                // Check for StateMachine attribute
                var stateMachineAttr = type.GetCustomAttribute<StateMachineAttribute>();
                if (stateMachineAttr != null)
                {
                    // DefaultPayloadType would be a property on the attribute
                    // For now we check PayloadType attributes
                }
                
                // Check for PayloadType attribute (indicates default payload)
                var payloadTypeAttr = type.GetCustomAttribute<PayloadTypeAttribute>();
                if (payloadTypeAttr != null)
                {
                    metadata.DefaultPayloadType = payloadTypeAttr.PayloadType;
                }
                
                // Check for async methods (simplified check)
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                metadata.HasAsyncHandlers = methods.Any(m => 
                    m.Name.EndsWith("Async") && 
                    (m.Name.Contains("OnEntry") || m.Name.Contains("OnExit") || 
                     m.Name.Contains("Guard") || m.Name.Contains("Action")));
                
                return metadata;
            });
        }
        
        /// <summary>
        /// Coerce a payload to the expected type
        /// </summary>
        public static object? CoercePayload(object? payload, TransitionShape shape)
        {
            // Null handling
            if (payload == null)
            {
                if (shape.RequiresPayload)
                {
                    throw new InvalidOperationException(
                        $"Transition requires payload of type {shape.ExpectedPayloadType?.Name} " +
                        $"(machine: {shape.MachineName}, trigger: {shape.Trigger})");
                }
                return null;
            }
            
            var expectedType = shape.ExpectedPayloadType;
            if (expectedType == null)
            {
                // No payload expected, but one was provided - let it through for now
                return payload;
            }
            
            var payloadType = payload.GetType();
            
            // Already correct type
            if (expectedType.IsAssignableFrom(payloadType))
            {
                return payload;
            }
            
            // Try registered adapters
            if (_adapters.TryGetValue((payloadType, expectedType), out var adapter))
            {
                return adapter(payload);
            }
            
            // Try dictionary/JSON coercion for test scenarios
            if (payload is IDictionary<string, object> dict)
            {
                return CoerceFromDictionary(dict, expectedType);
            }
            
            if (payload is JsonElement json)
            {
                return CoerceFromJson(json, expectedType);
            }
            
            // Cannot coerce
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payloadType.Name} to {expectedType.Name} " +
                $"(machine: {shape.MachineName}, trigger: {shape.Trigger}). " +
                $"If this is intentional, add a test adapter via TransitionIntrospection.RegisterAdapter<{payloadType.Name},{expectedType.Name}>(...)");
        }
        
        /// <summary>
        /// Register a payload adapter for testing
        /// </summary>
        public static void RegisterAdapter<TFrom, TTo>(Func<TFrom, TTo> adapter) where TTo : notnull
        {
            _adapters[(typeof(TFrom), typeof(TTo))] = obj => adapter((TFrom)obj)!;
        }
        
        private static object CoerceFromDictionary(IDictionary<string, object> dict, Type targetType)
        {
            var instance = Activator.CreateInstance(targetType);
            if (instance == null) throw new InvalidOperationException($"Cannot create instance of {targetType.Name}");
            
            foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanWrite && dict.TryGetValue(prop.Name, out var value))
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                        prop.SetValue(instance, convertedValue);
                    }
                    catch
                    {
                        // Ignore conversion errors in test scenarios
                    }
                }
            }
            
            return instance;
        }
        
        private static object CoerceFromJson(JsonElement json, Type targetType)
        {
            var jsonString = json.GetRawText();
            var instance = JsonSerializer.Deserialize(jsonString, targetType);
            if (instance == null) throw new InvalidOperationException($"Cannot deserialize JSON to {targetType.Name}");
            return instance;
        }
        
        /// <summary>
        /// Determine API capabilities for a machine
        /// </summary>
        public static ApiCapabilities DetermineCapabilities(string machineName, Type machineType)
        {
            var caps = ApiCapabilities.None;
            var metadata = GetMachineMetadata(machineType);
            
            if (metadata.DefaultPayloadType != null)
            {
                caps |= ApiCapabilities.HasDefaultPayload;
            }
            
            if (metadata.HasAsyncHandlers)
            {
                caps |= ApiCapabilities.HasAsync | ApiCapabilities.RequiresAsyncPath;
            }
            
            // Check for multi-payload support (simplified)
            if (_manualMappings.ContainsKey(machineName))
            {
                var shapes = _manualMappings[machineName].Values;
                if (shapes.Any(s => s.ExplicitPayloadType != null))
                {
                    caps |= ApiCapabilities.HasMultiPayloads;
                }
                
                if (shapes.Any(s => s.IsInternal))
                {
                    caps |= ApiCapabilities.HasInternalTransitions;
                }
                
                if (shapes.Any(s => s.IsAsync))
                {
                    caps |= ApiCapabilities.RequiresAsyncPath;
                }
            }
            
            return caps;
        }
    }
}