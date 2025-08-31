using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests
{
    /// <summary>
    /// Integration tests for complex logging scenarios
    /// </summary>
    public class LoggingIntegrationTests : LoggingTestBase
    {
        [Fact]
        public void ComplexTransition_VerifyLogOrder()
        {
            // Arrange
            var machine = new BasicStateMachine(TestState.Initial, GetLogger<BasicStateMachine>());
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert - Verify exact order of logs (now includes MachineStarted and TransitionStarted)
            // Debug: Print actual logs
            for (int i = 0; i < LoggedMessages.Count; i++)
            {
                Console.WriteLine($"[{i}] {LoggedMessages[i].EventId.Name}: {LoggedMessages[i].Message}");
            }
            
            LoggedMessages.Count.ShouldBe(6);

            // 1. MachineStarted
            LoggedMessages[0].EventId.Name.ShouldBe("MachineStarted");

            // 2. TransitionStarted
            LoggedMessages[1].EventId.Name.ShouldBe("TransitionStarted");

            // 3. OnExit
            LoggedMessages[2].EventId.Name.ShouldBe("OnExitExecuted");

            // 4. OnEntry
            LoggedMessages[3].EventId.Name.ShouldBe("OnEntryExecuted");

            // 5. Action
            LoggedMessages[4].EventId.Name.ShouldBe("ActionExecuted");

            // 6. TransitionSucceeded
            LoggedMessages[5].EventId.Name.ShouldBe("TransitionSucceeded");

        }

        [Fact]
        public void InitialStateOnEntry_LoggedDuringConstruction_Machine_with_Actions()
        {
            // Arrange
            LoggedMessages.Clear(); // Clear any previous logs

            // Act - OnEntry for initial state should be called in constructor
            var machine = new InitialOnEntryStateMachineActions(
                TestInitialState.Ready,
                GetLogger < InitialOnEntryStateMachineActions >());
            machine.Start();
            // Assert - OnEntry is called first (in OnInitialEntry), then MachineStarted
            VerifyLogCount(2);
            LoggedMessages[0].EventId.Name.ShouldBe("OnEntryExecuted");
            LoggedMessages[0].Message.ShouldContain("OnReadyEntry");
            LoggedMessages[0].Message.ShouldContain("Ready");
            LoggedMessages[1].EventId.Name.ShouldBe("MachineStarted");
        }

        [Fact]
        public void InitialStateOnEntry_LoggedDuringConstruction_Machine_with_Payload()
        {
            // Arrange
            LoggedMessages.Clear();

            // Act
            var machine = new FullMultiPayloadMachine(
                OrderStatePayload.New,
                null,
                GetLogger<FullMultiPayloadMachine>());
            machine.Start();
            // Assert - OnEntry is called first (in OnInitialEntry), then MachineStarted
            VerifyLogCount(2);
            LoggedMessages[0].EventId.Name.ShouldBe("OnEntryExecuted");
            LoggedMessages[0].Message.ShouldContain("OnNewEntry");
            LoggedMessages[0].Message.ShouldContain("New");
            LoggedMessages[1].EventId.Name.ShouldBe("MachineStarted");
        }

        [Fact]
        public void LoggerDisabled_NoLogsGenerated()
        {
            // Arrange
            LoggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(false);
            var machine = new BasicStateMachine(TestState.Initial, LoggerMock.Object as ILogger<BasicStateMachine>);
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing); // Transition should work
            VerifyNoLogs(); // But no logs should be generated
        }

        [Fact]
        public void MultipleTransitions_LogsAccumulate()
        {
            // Arrange
            var machine = new PureStateMachine(TestState.Initial, GetLogger<PureStateMachine>());
            machine.Start();
            // Act - Multiple transitions
            machine.TryFire(TestTrigger.Start);
            machine.TryFire(TestTrigger.Complete);
            machine.TryFire(TestTrigger.Reset); // This should fail

            // Assert - Now includes MachineStarted, TransitionStarted events and UnhandledTrigger
            machine.CurrentState.ShouldBe(TestState.Completed);
            LoggedMessages.Count.ShouldBe(7);

            // MachineStarted
            LoggedMessages[0].EventId.Name.ShouldBe("MachineStarted");

            // First transition
            LoggedMessages[1].EventId.Name.ShouldBe("TransitionStarted");
            LoggedMessages[2].EventId.Name.ShouldBe("TransitionSucceeded");
            LoggedMessages[2].Message.ShouldContain("Initial");
            LoggedMessages[2].Message.ShouldContain("Processing");

            // Second transition
            LoggedMessages[3].EventId.Name.ShouldBe("TransitionStarted");
            LoggedMessages[4].EventId.Name.ShouldBe("TransitionSucceeded");
            LoggedMessages[4].Message.ShouldContain("Processing");
            LoggedMessages[4].Message.ShouldContain("Completed");

            // Failed transition - UnhandledTrigger and TransitionFailed
            LoggedMessages[5].EventId.Name.ShouldBe("UnhandledTrigger");
            LoggedMessages[5].Message.ShouldContain("Reset");
            LoggedMessages[6].EventId.Name.ShouldBe("TransitionFailed");
            LoggedMessages[6].Message.ShouldContain("Completed");
            LoggedMessages[6].Message.ShouldContain("Reset");
        }

        [Fact]
        public void FullVariant_CompleteScenario_AllLogsPresent()
        {
            // Arrange
            var extensionBeforeCalled = false;
            var extensionAfterCalled = false;
            var extension = new TestExtension
            {
                BeforeTransitionCallback = _ => extensionBeforeCalled = true,
                AfterTransitionCallback = (_, success) =>
                {
                    extensionAfterCalled = true;
                    success.ShouldBeTrue();
                }
            };

            var machine = new FullStateMachine(
                TestState.Initial,
                new[] { extension },
                GetLogger < FullStateMachine >());
            machine.Start();
            var payload = new TestPayload { Id = 99, Data = "Integration Test" };

            // Act
            var result = machine.TryFire(TestTrigger.Start, payload);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TestState.Processing);
            machine.LastPayload.ShouldBe(payload);
            extensionBeforeCalled.ShouldBeTrue();
            extensionAfterCalled.ShouldBeTrue();

            // Verify logs
            var actionLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "ActionExecuted");
            actionLog.ShouldNotBe(default);
            actionLog.Message.ShouldContain("ProcessAction");

            var onEntryLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "OnEntryExecuted");
            onEntryLog.ShouldNotBe(default);
            onEntryLog.Message.ShouldContain("OnProcessingEntry");

            var transitionLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "TransitionSucceeded");
            transitionLog.ShouldNotBe(default);
            transitionLog.Message.ShouldContain("Initial");
            transitionLog.Message.ShouldContain("Processing");
        }

        [Fact]
        public void InstanceId_RemainsConsistentAcrossLogs()
        {
            // Arrange
            var machine = new BasicStateMachine(TestState.Initial, GetLogger<BasicStateMachine>());
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            // Extract instance IDs from all log messages
            var instanceIds = LoggedMessages
                .Select(log => ExtractInstanceId(log.Message))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            instanceIds.Count.ShouldBe(1); // All logs should have the same instance ID
            instanceIds[0].ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void DifferentLogLevels_OnlyEnabledLevelsLogged()
        {
            // -------------------------------------------------
            // Arrange
            // -------------------------------------------------
            // Wyłączamy poziom Debug, zostawiamy Info i Warning
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(false);
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

            var machine = new BasicStateMachine(
                TestState.Initial,
                GetLogger<BasicStateMachine>());   // logger z przekierowaniem do LoggerMock
            machine.Start();
            // -------------------------------------------------
            // Act
            // -------------------------------------------------
            machine.TryFire(TestTrigger.Start);

            // -------------------------------------------------
            // Assert
            // -------------------------------------------------
            // Powinny pojawić się tylko logi Information: MachineStarted i TransitionSucceeded
            // Logi Debug (TransitionStarted, OnExit, Action, OnEntry) powinny być odfiltrowane
            VerifyLogCount(2);
            LoggedMessages[0].EventId.Name.ShouldBe("MachineStarted");
            LoggedMessages[0].Level.ShouldBe(LogLevel.Information);
            LoggedMessages[1].EventId.Name.ShouldBe("TransitionSucceeded");
            LoggedMessages[1].Level.ShouldBe(LogLevel.Information);
        }


        private string ExtractInstanceId(string message)
        {
            // Simple extraction - assumes instance ID is a GUID in the message
            var parts = message.Split(' ');
            foreach (var part in parts)
            {
                if (Guid.TryParse(part.Trim(',', '.', '"'), out _))
                {
                    return part.Trim(',', '.', '"');
                }
            }
            return string.Empty;
        }
    }

    // Additional test state machine for initial OnEntry testing
    public enum TestInitialState { Ready, Working, Done }
    public enum TestInitialTrigger { Go, Stop }

    [StateMachine(typeof(TestInitialState), typeof(TestInitialTrigger))]
    public partial class InitialOnEntryStateMachineActions
    {
        [State(TestInitialState.Ready, OnEntry = nameof(OnReadyEntry))]
        private void ConfigureReady() { }

        private void OnReadyEntry() { }
    }

    public enum OrderStatePayload { New, Processing, Paid, Shipped, Delivered, Cancelled }
    public enum OrderTriggerPayload { Process, Pay, Ship, Deliver, Cancel, Refund }

    [StateMachine(typeof(OrderStatePayload), typeof(OrderTriggerPayload), GenerateExtensibleVersion = true)]
    [PayloadType(OrderTriggerPayload.Process, typeof(OrderPayload))]
    [PayloadType(OrderTriggerPayload.Pay, typeof(PaymentPayload))]
    [PayloadType(OrderTriggerPayload.Ship, typeof(ShippingPayload))]
    
    public partial class FullMultiPayloadMachine
    {
        // Konfiguracja stanu New z metodą OnEntry
        [State(OrderStatePayload.New, OnEntry = nameof(OnNewEntry))]
        private void ConfigureNew() { }

        // Definicje przejść
        [Transition(OrderStatePayload.New, OrderTriggerPayload.Process, OrderStatePayload.Processing, Action = nameof(HandleOrder))]
        [Transition(OrderStatePayload.Processing, OrderTriggerPayload.Pay, OrderStatePayload.Paid, Action = nameof(HandlePayment))]
        [Transition(OrderStatePayload.Paid, OrderTriggerPayload.Ship, OrderStatePayload.Shipped, Action = nameof(HandleShipping))]
        private void Configure() { }

        // Metoda OnEntry dla stanu New
        private void OnNewEntry() { }

        // Metody akcji
        private void HandleOrder(OrderPayload order) { }
        private void HandlePayment(PaymentPayload payment) { }
        private void HandleShipping(ShippingPayload shipping) { }
    }

    // Klasy payload
    public class OrderPayload
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? TrackingNumber { get; set; }
    }

    public class PaymentPayload : OrderPayload
    {
        public string PaymentMethod { get; set; } = "";
        public DateTime PaymentDate { get; set; }
    }

    public class ShippingPayload : OrderPayload
    {
        public string Carrier { get; set; } = "";
        public DateTime EstimatedDelivery { get; set; }
    }


}
