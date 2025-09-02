using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Tests
{
    /// <summary>
    /// Tests specific to FluentAPI functionality
    /// </summary>
    public class FluentAPISpecificTests
    {
        #region Test Machines

        [StateMachine(typeof(FluentTestState), typeof(FluentTestTrigger))]
        public partial class SimpleFluentMachine
        {
            public enum FluentTestState { Idle, Active, Done }
            public enum FluentTestTrigger { Start, Stop, Reset }

            public int TransitionCount { get; private set; }

            private static void Configure() => FSM
                .State(FluentTestState.Idle)
                    .On(FluentTestTrigger.Start)
                        .Action(nameof(IncrementCounter))
                        .GoTo(FluentTestState.Active)
                .State(FluentTestState.Active)
                    .On(FluentTestTrigger.Stop)
                        .Action(nameof(IncrementCounter))
                        .GoTo(FluentTestState.Done)
                    .On(FluentTestTrigger.Reset)
                        .GoTo(FluentTestState.Idle)
                .State(FluentTestState.Done)
                    .On(FluentTestTrigger.Reset)
                        .GoTo(FluentTestState.Idle);

            private void IncrementCounter() => TransitionCount++;
        }

        [StateMachine(typeof(PayloadState), typeof(PayloadTrigger))]
        public partial class FluentPayloadMachine
        {
            public enum PayloadState { Ready, Processing, Complete }
            public enum PayloadTrigger { Submit, Process, Finish }

            public sealed class SubmitData 
            { 
                public required string Id { get; init; }
                public int Priority { get; init; }
            }

            public sealed class ProcessData 
            { 
                public int ItemCount { get; init; } 
            }

            public string? LastSubmitId { get; private set; }
            public int ProcessedItems { get; private set; }

            private static void Configure() => FSM
                .State(PayloadState.Ready)
                    .On(PayloadTrigger.Submit)
                        .Payload<SubmitData>()
                        .Guard(nameof(ValidateSubmit))
                        .Action(nameof(HandleSubmit))
                        .GoTo(PayloadState.Processing)
                .State(PayloadState.Processing)
                    .On(PayloadTrigger.Process)
                        .Payload<ProcessData>()
                        .Action(nameof(ProcessItems))
                        .GoTo(PayloadState.Processing)  // Self-transition
                    .On(PayloadTrigger.Finish)
                        .GoTo(PayloadState.Complete)
                .State(PayloadState.Complete);

            private bool ValidateSubmit(SubmitData data) => !string.IsNullOrEmpty(data.Id);
            private void HandleSubmit(SubmitData data) => LastSubmitId = data.Id;
            private void ProcessItems(ProcessData data) => ProcessedItems += data.ItemCount;
        }

        [StateMachine(typeof(AsyncState), typeof(AsyncTrigger))]
        public partial class FluentAsyncMachine
        {
            public enum AsyncState { Disconnected, Connecting, Connected }
            public enum AsyncTrigger { Connect, Connected, Disconnect }

            public bool IsConnecting { get; private set; }
            public bool IsConnected { get; private set; }
            public int ConnectionAttempts { get; private set; }

            private static void Configure() => FSM
                .State(AsyncState.Disconnected)
                    .OnEntryAsync(nameof(OnDisconnectedEntryAsync))
                    .On(AsyncTrigger.Connect)
                        .GuardAsync(nameof(CanConnectAsync))
                        .ActionAsync(nameof(StartConnectionAsync))
                        .GoTo(AsyncState.Connecting)
                .State(AsyncState.Connecting)
                    .On(AsyncTrigger.Connected)
                        .ActionAsync(nameof(OnConnectedAsync))
                        .GoTo(AsyncState.Connected)
                .State(AsyncState.Connected)
                    .OnExitAsync(nameof(OnConnectedExitAsync))
                    .On(AsyncTrigger.Disconnect)
                        .ActionAsync(nameof(DisconnectAsync))
                        .GoTo(AsyncState.Disconnected);

            private async Task OnDisconnectedEntryAsync(CancellationToken ct)
            {
                IsConnected = false;
                await Task.Delay(10, ct);
            }

            private async ValueTask<bool> CanConnectAsync(CancellationToken ct)
            {
                await Task.Delay(10, ct);
                return ConnectionAttempts < 3;
            }

            private async Task StartConnectionAsync(CancellationToken ct)
            {
                IsConnecting = true;
                ConnectionAttempts++;
                await Task.Delay(50, ct);
            }

            private async Task OnConnectedAsync(CancellationToken ct)
            {
                IsConnecting = false;
                IsConnected = true;
                await Task.Delay(10, ct);
            }

            private async ValueTask OnConnectedExitAsync()
            {
                await Task.Delay(10);
            }

            private async Task DisconnectAsync(CancellationToken ct)
            {
                IsConnected = false;
                await Task.Delay(10, ct);
            }
        }

        [StateMachine(typeof(InternalState), typeof(InternalTrigger))]
        public partial class FluentInternalTransitionMachine
        {
            public enum InternalState { Active, Inactive }
            public enum InternalTrigger { Update, Toggle }

            public sealed class UpdateData { public int Value { get; init; } }

            public int Counter { get; private set; }
            public int UpdateCount { get; private set; }

            private static void Configure() => FSM
                .State(InternalState.Active)
                    .OnInternal(InternalTrigger.Update)
                        .Payload<UpdateData>()
                        .Guard(nameof(ValidateUpdate))
                        .Action(nameof(ApplyUpdate))
                        .Internal()  // Marks as internal transition
                    .On(InternalTrigger.Toggle)
                        .GoTo(InternalState.Inactive)
                .State(InternalState.Inactive)
                    .On(InternalTrigger.Toggle)
                        .GoTo(InternalState.Active);

            private bool ValidateUpdate(UpdateData data) => data.Value > 0;
            public void ApplyUpdate(UpdateData data)
            {
                Counter += data.Value;
                UpdateCount++;
            }
        }

        #endregion

        [Fact]
        public void FluentAPI_SimpleTransitions_ShouldWork()
        {
            // Arrange
            var machine = new SimpleFluentMachine(FluentTestState.Idle);

            // Act & Assert
            machine.CurrentState.ShouldBe(FluentTestState.Idle);
            machine.TransitionCount.ShouldBe(0);

            machine.Fire(FluentTestTrigger.Start);
            machine.CurrentState.ShouldBe(FluentTestState.Active);
            machine.TransitionCount.ShouldBe(1);

            machine.Fire(FluentTestTrigger.Stop);
            machine.CurrentState.ShouldBe(FluentTestState.Done);
            machine.TransitionCount.ShouldBe(2);

            machine.Fire(FluentTestTrigger.Reset);
            machine.CurrentState.ShouldBe(FluentTestState.Idle);
            machine.TransitionCount.ShouldBe(2); // Reset has no action
        }

        [Fact]
        public void FluentAPI_PayloadSupport_ShouldWork()
        {
            // Arrange
            var machine = new FluentPayloadMachine(PayloadState.Ready);

            // Act & Assert - Submit with payload
            var submitData = new FluentPayloadMachine.SubmitData { Id = "TEST-001", Priority = 1 };
            machine.CanFire(PayloadTrigger.Submit, submitData).ShouldBeTrue();
            
            machine.Fire(PayloadTrigger.Submit, submitData);
            machine.CurrentState.ShouldBe(PayloadState.Processing);
            machine.LastSubmitId.ShouldBe("TEST-001");

            // Process with different payload type
            var processData = new FluentPayloadMachine.ProcessData { ItemCount = 5 };
            machine.Fire(FluentPayloadMachine.PayloadTrigger.Process, processData);
            machine.CurrentState.ShouldBe(PayloadState.Processing); // Self-transition
            machine.ProcessedItems.ShouldBe(5);

            // Process again
            machine.Fire(FluentPayloadMachine.PayloadTrigger.Process, processData);
            machine.ProcessedItems.ShouldBe(10);

            // Finish without payload
            machine.Fire(PayloadTrigger.Finish);
            machine.CurrentState.ShouldBe(PayloadState.Complete);
        }

        [Fact]
        public void FluentAPI_PayloadValidation_ShouldRejectInvalid()
        {
            // Arrange
            var machine = new FluentPayloadMachine(PayloadState.Ready);

            // Act & Assert - Invalid payload should fail guard
            var invalidData = new FluentPayloadMachine.SubmitData { Id = "", Priority = 1 };
            machine.CanFire(PayloadTrigger.Submit, invalidData).ShouldBeFalse();

            // Should not transition
            machine.TryFire(PayloadTrigger.Submit, invalidData).ShouldBeFalse();
            machine.CurrentState.ShouldBe(PayloadState.Ready);

            // Valid payload should work
            var validData = new FluentPayloadMachine.SubmitData { Id = "VALID-001", Priority = 1 };
            machine.TryFire(PayloadTrigger.Submit, validData).ShouldBeTrue();
            machine.CurrentState.ShouldBe(PayloadState.Processing);
        }

        [Fact]
        public async Task FluentAPI_AsyncSupport_ShouldWork()
        {
            // Arrange
            var machine = new FluentAsyncMachine(AsyncState.Disconnected);
            await machine.StartAsync();

            // Act & Assert - Async guard
            var canConnect = await machine.CanFireAsync(AsyncTrigger.Connect);
            canConnect.ShouldBeTrue();

            // Fire async transition
            await machine.FireAsync(AsyncTrigger.Connect);
            machine.CurrentState.ShouldBe(AsyncState.Connecting);
            machine.IsConnecting.ShouldBeTrue();
            machine.ConnectionAttempts.ShouldBe(1);

            // Complete connection
            await machine.FireAsync(AsyncTrigger.Connected);
            machine.CurrentState.ShouldBe(AsyncState.Connected);
            machine.IsConnected.ShouldBeTrue();
            machine.IsConnecting.ShouldBeFalse();

            // Disconnect
            await machine.FireAsync(AsyncTrigger.Disconnect);
            machine.CurrentState.ShouldBe(AsyncState.Disconnected);
            machine.IsConnected.ShouldBeFalse();
        }

        [Fact]
        public async Task FluentAPI_AsyncWithCancellation_ShouldRespectToken()
        {
            // Arrange
            var machine = new FluentAsyncMachine(AsyncState.Disconnected);
            await machine.StartAsync();

            using var cts = new CancellationTokenSource();

            // Act - Start connection
            await machine.FireAsync(AsyncTrigger.Connect, null, cts.Token);
            machine.CurrentState.ShouldBe(AsyncState.Connecting);

            // Cancel during next transition
            cts.Cancel();

            // Assert - Should throw on cancelled token
            await Should.ThrowAsync<OperationCanceledException>(
                machine.FireAsync(AsyncTrigger.Connected, null, cts.Token)
            );
        }

        [Fact]
        public void FluentAPI_InternalTransitions_ShouldNotChangeState()
        {
            // Arrange
            var machine = new FluentInternalTransitionMachine(InternalState.Active);

            // Act - Internal transition with payload
            var updateData = new FluentInternalTransitionMachine.UpdateData { Value = 10 };
            machine.Fire(InternalTrigger.Update, updateData);

            // Assert - State should not change
            machine.CurrentState.ShouldBe(InternalState.Active);
            machine.Counter.ShouldBe(10);
            machine.UpdateCount.ShouldBe(1);

            // Another internal transition
            machine.Fire(InternalTrigger.Update, updateData);
            machine.CurrentState.ShouldBe(InternalState.Active);
            machine.Counter.ShouldBe(20);
            machine.UpdateCount.ShouldBe(2);

            // External transition
            machine.Fire(InternalTrigger.Toggle);
            machine.CurrentState.ShouldBe(InternalState.Inactive);

            // Internal transitions should not work in Inactive state
            machine.CanFire(InternalTrigger.Update, updateData).ShouldBeFalse();
        }

        [Fact]
        public void FluentAPI_InternalTransitionGuard_ShouldBeRespected()
        {
            // Arrange
            var machine = new FluentInternalTransitionMachine(InternalState.Active);

            // Act & Assert - Invalid update should be rejected
            var invalidUpdate = new FluentInternalTransitionMachine.UpdateData { Value = -5 };
            machine.CanFire(InternalTrigger.Update, invalidUpdate).ShouldBeFalse();
            
            machine.TryFire(InternalTrigger.Update, invalidUpdate).ShouldBeFalse();
            machine.Counter.ShouldBe(0);
            machine.UpdateCount.ShouldBe(0);

            // Valid update should work
            var validUpdate = new FluentInternalTransitionMachine.UpdateData { Value = 15 };
            machine.TryFire(InternalTrigger.Update, validUpdate).ShouldBeTrue();
            machine.Counter.ShouldBe(15);
            machine.UpdateCount.ShouldBe(1);
        }

        [Fact]
        public void FluentAPI_ChainedConfiguration_ShouldMaintainContext()
        {
            // This test verifies that the fluent API maintains proper context through method chaining
            var machine = new SimpleFluentMachine(FluentTestState.Idle);

            // The configuration is already defined in Configure() method
            // This test verifies it was parsed correctly

            // Verify all states are configured
            machine.CanFire(FluentTestTrigger.Start).ShouldBeTrue();
            machine.Fire(FluentTestTrigger.Start);
            
            machine.CanFire(FluentTestTrigger.Stop).ShouldBeTrue();
            machine.CanFire(FluentTestTrigger.Reset).ShouldBeTrue();
            machine.Fire(FluentTestTrigger.Stop);
            
            machine.CanFire(FluentTestTrigger.Reset).ShouldBeTrue();
            machine.CanFire(FluentTestTrigger.Stop).ShouldBeFalse(); // Not available in Done state
        }
    }
}