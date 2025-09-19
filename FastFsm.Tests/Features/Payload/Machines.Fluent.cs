using System.Collections.Generic;
using Abstractions.Fluent;

namespace FastFsm.Tests.Features.Payload;

// 1. Order State Machine - Single Payload
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderData))]
public partial class OrderStateMachineFluent
{
    public int LastProcessedOrderId { get; private set; }
    public decimal LastProcessedAmount { get; private set; }

    private static void Configure() => FSM
        .State(OrderState.New)
        .On(OrderTrigger.Submit).Action(nameof(ProcessOrder)).GoTo(OrderState.Submitted)
        .State(OrderState.Submitted)
        .On(OrderTrigger.Process).GoTo(OrderState.Processing)
        .On(OrderTrigger.Cancel).GoTo(OrderState.Cancelled)
        .State(OrderState.Processing)
        .On(OrderTrigger.Complete).GoTo(OrderState.Completed)
        .On(OrderTrigger.Cancel).GoTo(OrderState.Cancelled)
        .State(OrderState.Completed)
        .On(OrderTrigger.Ship).GoTo(OrderState.Shipped)
        .State(OrderState.Cancelled)
        .State(OrderState.Shipped);

    private void ProcessOrder(OrderData order)
    {
        LastProcessedOrderId = order.OrderId;
        LastProcessedAmount = order.Amount;
    }
}

// 2. Payment Machine - Guard with Payload
[StateMachine(typeof(PaymentState), typeof(PaymentTrigger))]
[PayloadType(typeof(PaymentData))]
public partial class PaymentMachineFluent
{
    private static void Configure() => FSM
        .State(PaymentState.Pending)
        .On(PaymentTrigger.Process).Guard(IsSmallAmount).GoTo(PaymentState.Processed)
        .On(PaymentTrigger.Cancel).GoTo(PaymentState.Failed)
        .State(PaymentState.Processed)
        .State(PaymentState.Failed)
        .On(PaymentTrigger.Retry).GoTo(PaymentState.Pending);

    private bool IsSmallAmount(PaymentData payment) => payment.Amount <= 100;
}

// 3. Notification Machine - Action with Payload
[StateMachine(typeof(NotificationState), typeof(NotificationTrigger))]
[PayloadType(typeof(NotificationData))]
public partial class NotificationMachineFluent
{
    public string LastSentMessage { get; private set; }
    public int RecipientCount { get; private set; }

    private static void Configure() => FSM
        .State(NotificationState.Ready)
        .On(NotificationTrigger.Send).Action(nameof(SendNotification)).GoTo(NotificationState.Sent)
        .State(NotificationState.Sent)
        .On(NotificationTrigger.Retry).GoTo(NotificationState.Ready)
        .State(NotificationState.Failed)
        .On(NotificationTrigger.Retry).GoTo(NotificationState.Ready);

    private void SendNotification(NotificationData notification)
    {
        LastSentMessage = notification.Message;
        RecipientCount = notification.Recipients?.Length ?? 0;
    }
}

// 4. Processing Machine - OnEntry with Payload
[StateMachine(typeof(ProcessingState), typeof(ProcessingTrigger))]
[PayloadType(typeof(ProcessConfig))]
public partial class ProcessingMachineFluent
{
    public int ActiveThreads { get; private set; }
    public int Timeout { get; private set; }
    public bool IsInitialized { get; private set; }

    private static void Configure() => FSM
        .State(ProcessingState.Idle)
        .On(ProcessingTrigger.Start).GoTo(ProcessingState.Running)
        .State(ProcessingState.Running)
        .OnEntry(nameof(Initialize))
        .On(ProcessingTrigger.Stop).GoTo(ProcessingState.Completed)
        .State(ProcessingState.Completed);

    private void Initialize(ProcessConfig config)
    {
        ActiveThreads = config.ThreadCount;
        Timeout = config.TimeoutSeconds;
        IsInitialized = true;
    }
}

// 5. Multi-Payload Machine - Different Triggers with Different Payloads
[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
[PayloadType(MultiTrigger.Configure, typeof(ConfigPayload))]
[PayloadType(MultiTrigger.Process, typeof(DataPayload))]
[PayloadType(MultiTrigger.Error, typeof(ErrorPayload))]
public partial class MultiPayloadMachineFluent
{
    public string CurrentSetting { get; private set; }
    public int ProcessedValue { get; private set; }
    public string LastErrorCode { get; private set; }

    private static void Configure() => FSM
        .State(MultiState.Initial)
        .On(MultiTrigger.Configure).Action(nameof(ApplyConfig)).GoTo(MultiState.Configured)
        .State(MultiState.Configured)
        .On(MultiTrigger.Process).Action(nameof(ProcessData)).GoTo(MultiState.Processing)
        .On(MultiTrigger.Error).Action(nameof(HandleError)).GoTo(MultiState.Failed)
        .State(MultiState.Processing)
        .On(MultiTrigger.Error).Action(nameof(HandleError)).GoTo(MultiState.Failed)
        .State(MultiState.Failed);

    private void ApplyConfig(ConfigPayload config)
    {
        CurrentSetting = config.Setting;
    }

    private void ProcessData(DataPayload data)
    {
        ProcessedValue = data.Value;
    }

    private void HandleError(ErrorPayload error)
    {
        LastErrorCode = error.Code;
    }
}

// 6. Overloaded Machine - Both Parameterless and Payload Methods
[StateMachine(typeof(OverloadState), typeof(OverloadTrigger))]
[PayloadType(typeof(OverloadPayload))]
public partial class OverloadedMachineFluent
{
    public List<string> CallLog { get; } = new();

    private static void Configure() => FSM
        .State(OverloadState.A)
        .On(OverloadTrigger.Go).Guard(Guard).Action(nameof(Action)).GoTo(OverloadState.B)
        .State(OverloadState.B)
        .OnEntry(nameof(OnEntry));

    private bool Guard()
    {
        CallLog.Add("Guard()");
        return true;
    }

    private bool Guard(OverloadPayload payload)
    {
        CallLog.Add("Guard(payload)");
        return true;
    }

    private void Action()
    {
        CallLog.Add("Action()");
    }

    private void Action(OverloadPayload payload)
    {
        CallLog.Add("Action(payload)");
    }

    private void OnEntry()
    {
        CallLog.Add("OnEntry()");
    }

    private void OnEntry(OverloadPayload payload)
    {
        CallLog.Add("OnEntry(payload)");
    }
}

// 7. Internal Payload Machine - Internal Transition with Payload
[StateMachine(typeof(InternalPayloadState), typeof(InternalPayloadTrigger))]
[PayloadType(typeof(UpdatePayload))]
public partial class InternalPayloadMachineFluent
{
    public int Counter { get; private set; }

    private static void Configure() => FSM
        .State(InternalPayloadState.Active)
        .OnInternal(InternalPayloadTrigger.Update).Action(nameof(UpdateCounter))
        .On(InternalPayloadTrigger.Deactivate).GoTo(InternalPayloadState.Inactive)
        .State(InternalPayloadState.Inactive);

    private void UpdateCounter(UpdatePayload update)
    {
        Counter += update.Increment;
    }
}

// 8. Mixed Payload Machine - Default and Specific Payloads
[StateMachine(typeof(MixedState), typeof(MixedTrigger))]
[PayloadType(typeof(DefaultPayload))]
[PayloadType(MixedTrigger.Special, typeof(SpecialPayload))]
public partial class MixedPayloadMachineFluent
{
    public int LastDefaultId { get; private set; }
    public string LastSpecialValue { get; private set; }

    private static void Configure() => FSM
        .State(MixedState.Start)
        .On(MixedTrigger.Regular).Action(nameof(ProcessDefault)).GoTo(MixedState.Middle)
        .State(MixedState.Middle)
        .On(MixedTrigger.Special).Action(nameof(ProcessSpecial)).GoTo(MixedState.End)
        .State(MixedState.End);

    private void ProcessDefault(DefaultPayload data)
    {
        LastDefaultId = data.Id;
    }

    private void ProcessSpecial(SpecialPayload data)
    {
        LastSpecialValue = data.SpecialValue;
    }
}

// 9. Initial Payload Machine - Initial State with Payload
[StateMachine(typeof(InitialPayloadState), typeof(InitialPayloadTrigger))]
[PayloadType(typeof(DefaultPayload))]
public partial class InitialPayloadMachineFluent
{
    public bool InitialEntryCalledParameterless { get; private set; }
    public bool InitialEntryCalledWithPayload { get; private set; }

    private static void Configure() => FSM
        .State(InitialPayloadState.Start)
        .OnEntry(nameof(OnStartEntry))
        .On(InitialPayloadTrigger.Go).GoTo(InitialPayloadState.Next)
        .State(InitialPayloadState.Next);

    private void OnStartEntry()
    {
        InitialEntryCalledParameterless = true;
    }

    private void OnStartEntry(DefaultPayload payload)
    {
        InitialEntryCalledWithPayload = true;
    }
}

// 10. Exit Callback Machine - OnExit Never Receives Payload
[StateMachine(typeof(ExitState), typeof(ExitTrigger))]
[PayloadType(typeof(ExitPayload))]
public partial class ExitCallbackMachineFluent
{
    public bool OnExitCalled { get; private set; }
    public string OnExitPayloadData { get; private set; }

    private static void Configure() => FSM
        .State(ExitState.A)
        .OnExit(nameof(OnExitA))
        .On(ExitTrigger.Go).GoTo(ExitState.B)
        .State(ExitState.B);

    private void OnExitA()
    {
        OnExitCalled = true;
    }
}

// 11. Workflow Machine - Chained Transitions with Payloads
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger))]
[PayloadType(typeof(WorkflowPayload))]
public partial class WorkflowMachineFluent
{
    public int Priority { get; private set; }
    public string ApprovedBy { get; private set; }
    public string Result { get; private set; }

    private static void Configure() => FSM
        .State(WorkflowState.Created)
        .On(WorkflowTrigger.Initialize).Action(nameof(Initialize)).GoTo(WorkflowState.Initialized)
        .State(WorkflowState.Initialized)
        .On(WorkflowTrigger.Submit).GoTo(WorkflowState.Submitted)
        .State(WorkflowState.Submitted)
        .On(WorkflowTrigger.Approve).Action(nameof(Approve)).GoTo(WorkflowState.Approved)
        .State(WorkflowState.Approved)
        .On(WorkflowTrigger.Complete).Action(nameof(Complete)).GoTo(WorkflowState.Completed)
        .State(WorkflowState.Completed);

    private void Initialize(WorkflowPayload data)
    {
        Priority = data.Priority;
    }

    private void Approve(WorkflowPayload data)
    {
        ApprovedBy = data.ApprovedBy;
    }

    private void Complete(WorkflowPayload data)
    {
        Result = data.Result;
    }
}

// 12. Conditional Payload Machine - CanFire with Payload
[StateMachine(typeof(ConditionalState), typeof(ConditionalTrigger))]
[PayloadType(typeof(ConditionalPayload))]
public partial class ConditionalPayloadMachineFluent
{
    private static void Configure() => FSM
        .State(ConditionalState.Ready)
        .On(ConditionalTrigger.Execute).Guard(IsValid).GoTo(ConditionalState.Done)
        .State(ConditionalState.Done);

    private bool IsValid(ConditionalPayload payload) => payload?.IsValid ?? false;
}

// 13. Permitted Triggers Machine - GetPermittedTriggers with Payload Machine
[StateMachine(typeof(PermittedState), typeof(PermittedTrigger))]
public partial class PermittedTriggersMachineFluent
{
    private static void Configure() => FSM
        .State(PermittedState.A)
        .On(PermittedTrigger.Next).GoTo(PermittedState.B)
        .On(PermittedTrigger.Skip).GoTo(PermittedState.C)
        .State(PermittedState.B)
        .On(PermittedTrigger.Next).GoTo(PermittedState.C)
        .State(PermittedState.C);
}

// 14. Strict Multi-Payload Machine - Fire with Wrong Payload Type Throws
[StateMachine(typeof(StrictState), typeof(StrictTrigger))]
[PayloadType(StrictTrigger.Process, typeof(ExpectedPayload))]
public partial class StrictMultiPayloadMachineFluent
{
    private static void Configure() => FSM
        .State(StrictState.Ready)
        .On(StrictTrigger.Process).GoTo(StrictState.Processing)
        .State(StrictState.Processing);
}