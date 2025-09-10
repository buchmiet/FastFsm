using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Diagnostics and logging for state machine wrappers
    /// </summary>
    public static class WrapperDiagnostics
    {
        private static ITestOutputHelper? _output;
        private static readonly List<string> _log = new();
        
        /// <summary>
        /// Set the test output helper for current test
        /// </summary>
        public static void SetOutput(ITestOutputHelper? output)
        {
            _output = output;
            if (output == null)
            {
                _log.Clear();
            }
        }
        
        /// <summary>
        /// Log transition shape information
        /// </summary>
        public static void LogShape(string machine, string trigger, TransitionShape shape)
        {
            var flags = new List<string>();
            
            if (shape.IsAsync) flags.Add("Async");
            if (shape.IsInternal) flags.Add("Internal");
            if (shape.UsesDefaultPayload) flags.Add($"Default<{shape.DefaultPayloadType?.Name ?? "?"}>");
            if (shape.ExplicitPayloadType != null) flags.Add($"Explicit<{shape.ExplicitPayloadType.Name}>");
            
            var message = $"[SHAPE] {machine}.{trigger}: {shape.SourceState} -> {shape.TargetState}";
            if (flags.Count > 0)
            {
                message += $" [{string.Join(", ", flags)}]";
            }
            
            Log(message);
        }
        
        /// <summary>
        /// Log payload coercion attempt
        /// </summary>
        public static void LogCoercion(Type? fromType, Type? toType, bool success)
        {
            var from = fromType?.Name ?? "null";
            var to = toType?.Name ?? "null";
            var status = success ? "OK" : "FAIL";
            
            Log($"[COERCE] {from} -> {to}: {status}");
        }
        
        /// <summary>
        /// Log async path enforcement
        /// </summary>
        public static void LogAsyncEnforcement(string machine, string trigger, string reason)
        {
            Log($"[ASYNC] {machine}.{trigger}: Async path required - {reason}");
        }
        
        /// <summary>
        /// Log wrapper capabilities
        /// </summary>
        public static void LogCapabilities(string machine, ApiCapabilities caps)
        {
            var capsList = new List<string>();
            
            if (caps.Has(ApiCapabilities.HasAsync)) capsList.Add("Async");
            if (caps.Has(ApiCapabilities.HasDefaultPayload)) capsList.Add("DefaultPayload");
            if (caps.Has(ApiCapabilities.HasMultiPayloads)) capsList.Add("MultiPayload");
            if (caps.Has(ApiCapabilities.HasInternalTransitions)) capsList.Add("Internal");
            if (caps.Has(ApiCapabilities.IsHierarchical)) capsList.Add("HSM");
            if (caps.Has(ApiCapabilities.RequiresAsyncPath)) capsList.Add("RequiresAsync");
            
            Log($"[CAPS] {machine}: {string.Join(", ", capsList)}");
        }
        
        /// <summary>
        /// Get diagnostics summary
        /// </summary>
        public static string GetSummary()
        {
            if (_log.Count == 0) return "No diagnostics logged";
            
            var sb = new StringBuilder();
            sb.AppendLine("=== Wrapper Diagnostics ===");
            foreach (var line in _log)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Clear diagnostics log
        /// </summary>
        public static void Clear()
        {
            _log.Clear();
        }
        
        private static void Log(string message)
        {
            _log.Add($"{DateTime.UtcNow:HH:mm:ss.fff} {message}");
            _output?.WriteLine(message);
        }
    }
}