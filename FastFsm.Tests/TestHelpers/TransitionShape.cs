using System;
using System.Collections.Generic;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Describes the shape/characteristics of a state transition
    /// </summary>
    public class TransitionShape
    {
        /// <summary>
        /// Whether the transition uses the machine's default payload type
        /// </summary>
        public bool UsesDefaultPayload { get; set; }
        
        /// <summary>
        /// The machine's default payload type (from StateMachine attribute)
        /// </summary>
        public Type? DefaultPayloadType { get; set; }
        
        /// <summary>
        /// Explicit payload type from .Payload<T>() on this specific transition
        /// </summary>
        public Type? ExplicitPayloadType { get; set; }
        
        /// <summary>
        /// Whether this is an internal transition (stays in same state)
        /// </summary>
        public bool IsInternal { get; set; }
        
        /// <summary>
        /// Whether this transition has async handlers (OnEntryAsync, GuardAsync, etc.)
        /// </summary>
        public bool IsAsync { get; set; }
        
        /// <summary>
        /// The actual payload type expected (ExplicitPayloadType ?? DefaultPayloadType)
        /// </summary>
        public Type? ExpectedPayloadType => ExplicitPayloadType ?? (UsesDefaultPayload ? DefaultPayloadType : null);
        
        /// <summary>
        /// Whether this transition requires a payload
        /// </summary>
        public bool RequiresPayload => ExpectedPayloadType != null;
        
        /// <summary>
        /// Source state of the transition
        /// </summary>
        public string? SourceState { get; set; }
        
        /// <summary>
        /// Target state of the transition (same as SourceState for internal)
        /// </summary>
        public string? TargetState { get; set; }
        
        /// <summary>
        /// Trigger that causes this transition
        /// </summary>
        public string? Trigger { get; set; }
        
        /// <summary>
        /// Machine name for diagnostics
        /// </summary>
        public string? MachineName { get; set; }
        
        public override string ToString()
        {
            var flags = new List<string>();
            if (IsAsync) flags.Add("Async");
            if (IsInternal) flags.Add("Internal");
            if (UsesDefaultPayload) flags.Add("Default");
            if (ExplicitPayloadType != null) flags.Add($"Explicit<{ExplicitPayloadType.Name}>");
            
            var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
            return $"{MachineName}.{SourceState} --{Trigger}--> {TargetState}{flagStr}";
        }
    }
}