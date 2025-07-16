using System;
using Xunit;
using Xunit.Abstractions;


namespace StateMachine.Tests.PayloadVariant;




public class PayloadVariantTests(ITestOutputHelper output)
{
    [Fact]
    public void SinglePayloadType_BasicTransition_PassesPayloadCorrectly()
    {
        // Arrange
        var machine = new OrderStateMachine(OrderState.New);
        var order = new OrderData { OrderId = 123, Amount = 100, Customer = "Test" };

        // Act
        var result = machine.TryFire(OrderTrigger.Submit, order);

        // Assert
        Assert.True(result);
        Assert.Equal(OrderState.Submitted, machine.CurrentState);
        Assert.Equal(123, machine.LastProcessedOrderId);
        Assert.Equal(100, machine.LastProcessedAmount);
    }

    [Fact]
    public void SinglePayloadType_GuardWithPayload_EvaluatesCorrectly()
    {
        // Arrange
        var machine = new PaymentMachine(PaymentState.Pending);
        var smallPayment = new PaymentData { Amount = 50 };
        var largePayment = new PaymentData { Amount = 150 };

        // Act & Assert - Small payment
        var result = machine.TryFire(PaymentTrigger.Process, smallPayment);
        Assert.True(result);
        Assert.Equal(PaymentState.Processed, machine.CurrentState);

        // Reset
        machine = new PaymentMachine(PaymentState.Pending);

        // Act & Assert - Large payment needs approval
        result = machine.TryFire(PaymentTrigger.Process, largePayment);
        Assert.False(result);
        Assert.Equal(PaymentState.Pending, machine.CurrentState);
    }

    [Fact]
    public void SinglePayloadType_ActionWithPayload_ReceivesData()
    {
        // Arrange
        var machine = new NotificationMachine(NotificationState.Ready);
        var notification = new NotificationData
        {
            Message = "Test Message",
            Recipients = ["user1", "user2"]
        };

        // Act
        machine.Fire(NotificationTrigger.Send, notification);

        // Assert
        Assert.Equal(NotificationState.Sent, machine.CurrentState);
        Assert.Equal("Test Message", machine.LastSentMessage);
        Assert.Equal(2, machine.RecipientCount);
    }

    [Fact]
    public void SinglePayloadType_OnEntryWithPayload_ReceivesTransitionData()
    {
        // Arrange
        var machine = new ProcessingMachine(ProcessingState.Idle);
        var config = new ProcessConfig { ThreadCount = 4, TimeoutSeconds = 30 };

        // Act
        machine.Fire(ProcessingTrigger.Start, config);

        // Assert
        Assert.Equal(ProcessingState.Running, machine.CurrentState);
        Assert.Equal(4, machine.ActiveThreads);
        Assert.Equal(30, machine.Timeout);
        Assert.True(machine.IsInitialized);
    }

    [Fact]
    public void MultiPayloadType_DifferentTriggersWithDifferentPayloads_WorkCorrectly()
    {
        // Arrange
        var machine = new MultiPayloadMachine(MultiState.Initial);
        var config = new ConfigPayload { Setting = "Debug" };
        var data = new DataPayload { Value = 42 };
        var error = new ErrorPayload { Code = "ERR001", Message = "Test error" };

        // Act - Configure
        machine.Fire(MultiTrigger.Configure, config);
        Assert.Equal(MultiState.Configured, machine.CurrentState);
        Assert.Equal("Debug", machine.CurrentSetting);

        // Act - Process
        machine.Fire(MultiTrigger.Process, data);
        Assert.Equal(MultiState.Processing, machine.CurrentState);
        Assert.Equal(42, machine.ProcessedValue);

        // Act - Error
        machine.Fire(MultiTrigger.Error, error);
        Assert.Equal(MultiState.Failed, machine.CurrentState);
        Assert.Equal("ERR001", machine.LastErrorCode);
    }

    [Fact]
    public void MultiPayloadType_WrongPayloadType_FailsGracefully()
    {
        // Arrange
        var machine = new MultiPayloadMachine(MultiState.Configured);
        var wrongPayload = new ConfigPayload { Setting = "Wrong" };

        // Act - Try to use ConfigPayload where DataPayload is expected
        var result = machine.TryFire(MultiTrigger.Process, wrongPayload);

        // Assert
        Assert.False(result);
        Assert.Equal(MultiState.Configured, machine.CurrentState);
    }

    [Fact]
    public void PayloadOverloading_BothParameterlessAndPayloadMethods_CalledCorrectly()
    {
        // Arrange
        var machine = new OverloadedMachine(OverloadState.A);
        var payload = new OverloadPayload { Data = "test" };

        // Act - Fire without payload
        machine.CallLog.Clear();
        machine.Fire(OverloadTrigger.Go);

        // Assert
        Assert.Contains("Guard()", machine.CallLog);
        Assert.Contains("Action()", machine.CallLog);
        Assert.Contains("OnEntry()", machine.CallLog);
        Assert.Equal(OverloadState.B, machine.CurrentState);

        // Act - Fire with payload
        machine = new OverloadedMachine(OverloadState.A);
        machine.CallLog.Clear();
        machine.Fire(OverloadTrigger.Go, payload);

        // Assert
        Assert.Contains("Guard(payload)", machine.CallLog);
        Assert.Contains("Action(payload)", machine.CallLog);
        Assert.Contains("OnEntry(payload)", machine.CallLog);
        Assert.Equal(OverloadState.B, machine.CurrentState);
    }

    [Fact]
    public void InternalTransition_WithPayload_DoesNotChangeState()
    {
        // Arrange
        var machine = new InternalPayloadMachine(InternalPayloadState.Active);
        var update = new UpdatePayload { Increment = 5 };

        // Act
        machine.Fire(InternalPayloadTrigger.Update, update);

        // Assert
        Assert.Equal(InternalPayloadState.Active, machine.CurrentState);
        Assert.Equal(5, machine.Counter);

        // Act again
        machine.Fire(InternalPayloadTrigger.Update, update);
        Assert.Equal(10, machine.Counter);
        Assert.Equal(InternalPayloadState.Active, machine.CurrentState);
    }

    [Fact]
    public void MixedPayloadTypes_DefaultAndSpecific_WorkTogether()
    {
        // Arrange
        var machine = new MixedPayloadMachine(MixedState.Start);
        var defaultData = new DefaultPayload { Id = 1 };
        var specialData = new SpecialPayload { SpecialValue = "Special" };

        // Act - Use default payload
        machine.Fire(MixedTrigger.Regular, defaultData);
        Assert.Equal(MixedState.Middle, machine.CurrentState);
        Assert.Equal(1, machine.LastDefaultId);

        // Act - Use special payload
        machine.Fire(MixedTrigger.Special, specialData);
        Assert.Equal(MixedState.End, machine.CurrentState);
        Assert.Equal("Special", machine.LastSpecialValue);
    }

    [Fact]
    public void InitialStateOnEntry_WithPayload_UsesParameterlessVersion()
    {
        // Arrange & Act
        var machine = new InitialPayloadMachine(InitialPayloadState.Start);

        // Assert - Initial OnEntry should use parameterless version
        Assert.True(machine.InitialEntryCalledParameterless);
        Assert.False(machine.InitialEntryCalledWithPayload);
    }

    [Fact]
    public void OnExitCallback_NeverReceivesPayload()
    {
        // Arrange
        var machine = new ExitCallbackMachine(ExitState.A);
        var payload = new ExitPayload { Data = "test" };

        // Act
        machine.Fire(ExitTrigger.Go, payload);

        // Assert
        Assert.Equal(ExitState.B, machine.CurrentState);
        Assert.True(machine.OnExitCalled);
        Assert.Null(machine.OnExitPayloadData); // OnExit doesn't receive payload
    }

    [Fact]
    public void ComplexScenario_ChainedTransitionsWithPayloads()
    {
        // Arrange
        var machine = new WorkflowMachine(WorkflowState.Created);
        var initData = new WorkflowPayload { WorkflowId = "WF001", Priority = 1 };
        var approvalData = new WorkflowPayload { WorkflowId = "WF001", ApprovedBy = "Manager" };
        var completeData = new WorkflowPayload { WorkflowId = "WF001", Result = "Success" };

        // Act - Full workflow
        machine.Fire(WorkflowTrigger.Initialize, initData);
        Assert.Equal(WorkflowState.Initialized, machine.CurrentState);
        Assert.Equal(1, machine.Priority);

        machine.Fire(WorkflowTrigger.Submit, initData);
        Assert.Equal(WorkflowState.Submitted, machine.CurrentState);

        machine.Fire(WorkflowTrigger.Approve, approvalData);
        Assert.Equal(WorkflowState.Approved, machine.CurrentState);
        Assert.Equal("Manager", machine.ApprovedBy);

        machine.Fire(WorkflowTrigger.Complete, completeData);
        Assert.Equal(WorkflowState.Completed, machine.CurrentState);
        Assert.Equal("Success", machine.Result);
    }

    [Fact]
    public void CanFire_WithPayload_EvaluatesGuards_Correctly()
    {
        // Arrange
        var machine = new ConditionalPayloadMachine(ConditionalState.Ready);
        var validPayload = new ConditionalPayload { IsValid = true };
        var invalidPayload = new ConditionalPayload { IsValid = false };

        // Act & Assert ----------------------------------------------------------
        // 1) Guard przechodzi → true
        Assert.True(machine.CanFire(ConditionalTrigger.Execute, validPayload));

        // 2) Guard nie przechodzi → false
        Assert.False(machine.CanFire(ConditionalTrigger.Execute, invalidPayload));

        // 3) Wywołanie bez payloadu, guard oczekuje danych → false
        Assert.False(machine.CanFire(ConditionalTrigger.Execute));
    }


    [Fact]
    public void GetPermittedTriggers_WithPayloadMachine_WorksCorrectly()
    {
        // Arrange
        var machine = new PermittedTriggersMachine(PermittedState.A);

        // Act
        var triggers = machine.GetPermittedTriggers();

        // Assert
        Assert.Contains(PermittedTrigger.Next, triggers);
        Assert.Contains(PermittedTrigger.Skip, triggers);
        Assert.Equal(2, triggers.Count);
    }

    [Fact]
    public void FireMethod_WithWrongPayloadType_ThrowsInMultiPayloadVariant()
    {
        // Arrange
        var machine = new StrictMultiPayloadMachine(StrictState.Ready);
        var wrongPayload = new WrongPayload();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            machine.Fire(StrictTrigger.Process, wrongPayload));
    }

    public class PayloadVariantMissingPayloadTests
    {
        [Fact]
        public void SinglePayloadType_WithoutPayload_TransitionsButNoAction()
        {
            // Arrange
            var machine = new OrderStateMachine(OrderState.New);

            // Act
            var result = machine.TryFire(OrderTrigger.Submit);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderState.Submitted, machine.CurrentState);
            // Akcja nie wykonała się, bo nie było payloadu
            Assert.Equal(0, machine.LastProcessedOrderId);
            Assert.Equal(default(decimal), machine.LastProcessedAmount);
        }

        [Fact]
        public void MultiPayloadType_WithoutPayload_FailsAndStateUnchanged()
        {
            // Arrange
            var machine = new MultiPayloadMachine(MultiState.Initial);

            // Act
            var result1 = machine.TryFire(MultiTrigger.Configure);
            var result2 = machine.TryFire(MultiTrigger.Process, null);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
            Assert.Equal(MultiState.Initial, machine.CurrentState);
        }

        [Fact]
        public void Fire_MultiPayloadWithoutPayload_ThrowsInvalidOperation()
        {
            // Arrange
            var machine = new MultiPayloadMachine(MultiState.Initial);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                machine.Fire(MultiTrigger.Configure));
        }
    }


    // Test Data Classes
    public class OrderData
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Customer { get; set; }
    }

    public class PaymentData
    {
        public decimal Amount { get; set; }
    }

    public class NotificationData
    {
        public string Message { get; set; }
        public string[] Recipients { get; set; }
    }

    public class ProcessConfig
    {
        public int ThreadCount { get; set; }
        public int TimeoutSeconds { get; set; }
    }

    public class ConfigPayload
    {
        public string Setting { get; set; }
    }

    public class DataPayload
    {
        public int Value { get; set; }
    }

    public class ErrorPayload
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class OverloadPayload
    {
        public string Data { get; set; }
    }

    public class UpdatePayload
    {
        public int Increment { get; set; }
    }

    public class DefaultPayload
    {
        public int Id { get; set; }
    }

    public class SpecialPayload
    {
        public string SpecialValue { get; set; }
    }

    public class ExitPayload
    {
        public string Data { get; set; }
    }

    public class WorkflowPayload
    {
        public string WorkflowId { get; set; }
        public int Priority { get; set; }
        public string ApprovedBy { get; set; }
        public string Result { get; set; }
    }

    public class ConditionalPayload
    {
        public bool IsValid { get; set; }
    }

    public class ExpectedPayload
    {
        public string Data { get; set; }
    }

    public class WrongPayload
    {
        public string Wrong { get; set; }
    }

    // Test State and Trigger Enums
    public enum OrderState { New, Submitted, Processing, Completed,Paid, Cancelled, Shipped }

    public enum OrderTrigger
    {
        Submit, Process, Complete,Pay
        , Cancel, Ship
    }

    public enum PaymentState { Pending, Processed, Failed }
    public enum PaymentTrigger { Process, Retry, Cancel }

    public enum NotificationState { Ready, Sent, Failed }
    public enum NotificationTrigger { Send, Retry }

    public enum ProcessingState { Idle, Running, Completed }
    public enum ProcessingTrigger { Start, Stop }

    public enum MultiState { Initial, Configured, Processing, Failed }
    public enum MultiTrigger { Configure, Process, Error }

    public enum OverloadState { A, B }
    public enum OverloadTrigger { Go }

    public enum InternalPayloadState { Active, Inactive }
    public enum InternalPayloadTrigger { Update, Deactivate }

    public enum MixedState { Start, Middle, End }
    public enum MixedTrigger { Regular, Special }

    public enum InitialPayloadState { Start, Next }
    public enum InitialPayloadTrigger { Go }

    public enum ExitState { A, B }
    public enum ExitTrigger { Go }

    public enum WorkflowState { Created, Initialized, Submitted, Approved, Completed }
    public enum WorkflowTrigger { Initialize, Submit, Approve, Complete }

    public enum ConditionalState { Ready, Done }
    public enum ConditionalTrigger { Execute }

    public enum PermittedState { A, B, C }
    public enum PermittedTrigger { Next, Skip }

    public enum StrictState { Ready, Processing }
    public enum StrictTrigger { Process }
}