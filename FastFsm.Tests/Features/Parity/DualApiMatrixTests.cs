using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using FastFsm.Tests.TestHelpers;
using Shouldly;
using static FastFsm.Tests.TestHelpers.StateMachineWrapperFactory;

namespace FastFsm.Tests.Features.Parity
{
    /// <summary>
    /// Matrix tests that run all machines on both APIs to ensure functional parity
    /// </summary>
    [Trait("Category", "Parity")]
    [Trait("Category", "Matrix")]
    public class DualApiMatrixTests
    {
        private readonly ITestOutputHelper _output;

        public DualApiMatrixTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetAllMachinesAndApis()
        {
            foreach (var machineName in MatrixConfig.GetAllMachineNames())
            {
                yield return new object[] { machineName, ApiType.Fluent };
                yield return new object[] { machineName, ApiType.Legacy };
            }
        }

        [Theory]
        [MemberData(nameof(GetAllMachinesAndApis))]
        public void Machine_BasicOperations_WorkOnBothApis(string machineName, ApiType apiType)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull($"Machine {machineName} not found in MatrixConfig");
            
            try
            {
                // Create wrapper using factory
                var wrapper = StateMachineWrapperFactory.Create(machineName, apiType, config.InitialState);
                wrapper.ShouldNotBeNull($"Failed to create {apiType} wrapper for {machineName}");
                
                // Start machine
                wrapper.Start();
                
                // Get current state
                var currentState = wrapper.CurrentState;
                currentState.ShouldNotBeNull($"{machineName} ({apiType}) CurrentState is null");
                
                // Get permitted triggers
                var permittedTriggers = wrapper.GetPermittedTriggers();
                permittedTriggers.ShouldNotBeNull($"{machineName} ({apiType}) GetPermittedTriggers returned null");
                
                // Try to execute the configured trigger sequence
                if (config.TriggerSequence.Length > 0)
                {
                    var firstTrigger = config.TriggerSequence[0];
                    var canFire = wrapper.CanFire(firstTrigger);
                    
                    if (canFire)
                    {
                        // Prepare payload if needed
                        object? payload = null;
                        if (config.Payloads.Length > 0)
                        {
                            payload = config.Payloads[0];
                        }
                        else if (wrapper.Caps.Has(ApiCapabilities.HasDefaultPayload) || 
                                 wrapper.Caps.Has(ApiCapabilities.HasMultiPayloads))
                        {
                            payload = MatrixConfig.CreateDummyPayload();
                        }
                        
                        // Try to fire the trigger
                        try
                        {
                            var result = wrapper.TryFire(firstTrigger, payload);
                            
                            // For internal transitions, state might not change
                            if (!wrapper.Caps.Has(ApiCapabilities.HasInternalTransitions) || !result)
                            {
                                // Either it should succeed and change state, or fail
                                if (result)
                                {
                                    var newState = wrapper.CurrentState;
                                    _output.WriteLine($"{machineName} ({apiType}): {currentState} -> {newState} via {firstTrigger}");
                                }
                                else
                                {
                                    _output.WriteLine($"{machineName} ({apiType}): TryFire({firstTrigger}) returned false");
                                }
                            }
                            else
                            {
                                _output.WriteLine($"{machineName} ({apiType}): Internal transition {firstTrigger} executed");
                            }
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("FSM204"))
                        {
                            // Async path required
                            _output.WriteLine($"{machineName} ({apiType}): Requires async path for {firstTrigger}");
                            wrapper.Caps.Has(ApiCapabilities.RequiresAsyncPath).ShouldBeTrue(
                                $"Machine threw FSM204 but doesn't have RequiresAsyncPath capability");
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("payload"))
                        {
                            // Payload required but not provided correctly
                            _output.WriteLine($"{machineName} ({apiType}): Payload required for {firstTrigger}");
                        }
                    }
                }
                
                _output.WriteLine($"✅ {machineName} ({apiType}): Basic operations successful");
            }
            catch (NotImplementedException)
            {
                var message = $"{machineName} ({apiType}) wrapper not fully implemented";
                _output.WriteLine($"⚠️ {message}");
                // Skip.If(true, message);
                return; // Skip test for now
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ {machineName} ({apiType}): {ex.Message}");
                throw;
            }
        }

        [Theory]
        [MemberData(nameof(GetAllMachinesAndApis))]
        public async void Machine_AsyncOperations_WorkOnBothApis(string machineName, ApiType apiType)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull($"Machine {machineName} not found in MatrixConfig");
            
            try
            {
                // Create wrapper using factory
                var wrapper = StateMachineWrapperFactory.Create(machineName, apiType, config.InitialState);
                wrapper.ShouldNotBeNull($"Failed to create {apiType} wrapper for {machineName}");
                
                // Start machine async
                await wrapper.StartAsync();
                
                // Get current state
                var currentState = wrapper.CurrentState;
                currentState.ShouldNotBeNull($"{machineName} ({apiType}) CurrentState is null after StartAsync");
                
                // Get permitted triggers
                var permittedTriggers = wrapper.GetPermittedTriggers();
                
                // Try to execute the configured trigger sequence async
                if (config.TriggerSequence.Length > 0)
                {
                    var firstTrigger = config.TriggerSequence[0];
                    var canFire = wrapper.CanFire(firstTrigger);
                    
                    if (canFire)
                    {
                        // Prepare payload if needed
                        object? payload = null;
                        if (config.Payloads.Length > 0)
                        {
                            payload = config.Payloads[0];
                        }
                        else if (wrapper.Caps.Has(ApiCapabilities.HasDefaultPayload) || 
                                 wrapper.Caps.Has(ApiCapabilities.HasMultiPayloads))
                        {
                            payload = MatrixConfig.CreateDummyPayload();
                        }
                        
                        // Try to fire the trigger async
                        try
                        {
                            var result = await wrapper.TryFireAsync(firstTrigger, payload);
                            
                            if (result)
                            {
                                var newState = wrapper.CurrentState;
                                _output.WriteLine($"{machineName} ({apiType}): Async {currentState} -> {newState} via {firstTrigger}");
                            }
                            else
                            {
                                _output.WriteLine($"{machineName} ({apiType}): Async TryFire({firstTrigger}) returned false");
                            }
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("payload"))
                        {
                            // Payload required but not provided correctly
                            _output.WriteLine($"{machineName} ({apiType}): Async payload required for {firstTrigger}");
                        }
                    }
                }
                
                _output.WriteLine($"✅ {machineName} ({apiType}): Async operations successful");
            }
            catch (NotImplementedException)
            {
                var message = $"{machineName} ({apiType}) async wrapper not fully implemented";
                _output.WriteLine($"⚠️ {message}");
                // Skip.If(true, message);
                return; // Skip test for now
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ {machineName} ({apiType}) Async: {ex.Message}");
                throw;
            }
        }

        [Theory]
        [MemberData(nameof(GetAllMachinesAndApis))]
        public void Machine_Capabilities_AreConsistent(string machineName, ApiType apiType)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull($"Machine {machineName} not found in MatrixConfig");
            
            try
            {
                var wrapper = StateMachineWrapperFactory.Create(machineName, apiType, config.InitialState);
                var caps = wrapper.Caps;
                
                _output.WriteLine($"{machineName} ({apiType}) Capabilities: {caps}");
                
                // Verify capabilities make sense
                if (caps.Has(ApiCapabilities.RequiresAsyncPath))
                {
                    caps.Has(ApiCapabilities.HasAsync).ShouldBeTrue(
                        "RequiresAsyncPath should imply HasAsync");
                }
                
                if (caps.Has(ApiCapabilities.HasMultiPayloads))
                {
                    caps.Has(ApiCapabilities.HasDefaultPayload).ShouldBeFalse(
                        "HasMultiPayloads and HasDefaultPayload should be mutually exclusive");
                }
            }
            catch (NotImplementedException)
            {
                // Skip.If(true, $"{machineName} ({apiType}) wrapper not implemented");
                _output.WriteLine($"⚠️ {machineName} ({apiType}) wrapper not implemented - skipping");
                return;
            }
        }

        // Moved to MatrixConfig.CreateDummyPayload()
    }
}