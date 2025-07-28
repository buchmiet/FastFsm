using System.Collections.Generic;
using Abstractions.Attributes;
using static StateMachine.Tests.PayloadVariant.PayloadVariantTests;

namespace StateMachine.Tests.PayloadVariant
{
    [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
    [PayloadType(typeof(OrderData))]
    public partial class OrderStateMachine
    {
        public int LastProcessedOrderId { get; private set; }
        public decimal LastProcessedAmount { get; private set; }

        [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted,
            Action = nameof(ProcessSubmission))]
        private void Configure() { }

        private void ProcessSubmission(OrderData order)
        {
            LastProcessedOrderId = order.OrderId;
            LastProcessedAmount = order.Amount;
        }
    }

    // Single payload with guards
    [StateMachine(typeof(PaymentState), typeof(PaymentTrigger))]
    [PayloadType(typeof(PaymentData))]
    public partial class PaymentMachine
    {
        private const decimal ApprovalThreshold = 100;

        [Transition(PaymentState.Pending, PaymentTrigger.Process, PaymentState.Processed,
            Guard = nameof(CanProcessDirectly))]
        private void Configure() { }

        private bool CanProcessDirectly(PaymentData payment) => payment.Amount <= ApprovalThreshold;
    }

    // Single payload with action
    [StateMachine(typeof(NotificationState), typeof(NotificationTrigger))]
    [PayloadType(typeof(NotificationData))]
    public partial class NotificationMachine
    {
        public string LastSentMessage { get; private set; }
        public int RecipientCount { get; private set; }

        [Transition(NotificationState.Ready, NotificationTrigger.Send, NotificationState.Sent,
            Action = nameof(SendNotification))]
        private void Configure() { }

        private void SendNotification(NotificationData notification)
        {
            LastSentMessage = notification.Message;
            RecipientCount = notification.Recipients.Length;
        }
    }

    // OnEntry with payload
    [StateMachine(typeof(ProcessingState), typeof(ProcessingTrigger))]
    [PayloadType(typeof(ProcessConfig))]
    public partial class ProcessingMachine
    {
        public int ActiveThreads { get; private set; }
        public int Timeout { get; private set; }
        public bool IsInitialized { get; private set; }

        [State(ProcessingState.Running, OnEntry = nameof(InitializeProcessing))]
        private void ConfigureStates() { }

        [Transition(ProcessingState.Idle, ProcessingTrigger.Start, ProcessingState.Running)]
        private void Configure() { }

        private void InitializeProcessing(ProcessConfig config)
        {
            ActiveThreads = config.ThreadCount;
            Timeout = config.TimeoutSeconds;
            IsInitialized = true;
        }
    }

    // Multiple payload types
    [StateMachine(typeof(MultiState), typeof(MultiTrigger))]
    [PayloadType(MultiTrigger.Configure, typeof(ConfigPayload))]
    [PayloadType(MultiTrigger.Process, typeof(DataPayload))]
    [PayloadType(MultiTrigger.Error, typeof(ErrorPayload))]
    public partial class MultiPayloadMachine
    {
        public string CurrentSetting { get; private set; }
        public int ProcessedValue { get; private set; }
        public string LastErrorCode { get; private set; }

        [Transition(MultiState.Initial, MultiTrigger.Configure, MultiState.Configured,
            Action = nameof(ApplyConfiguration))]
        [Transition(MultiState.Configured, MultiTrigger.Process, MultiState.Processing,
            Action = nameof(ProcessData))]
        [Transition(MultiState.Processing, MultiTrigger.Error, MultiState.Failed,
            Action = nameof(HandleError))]
        private void Configure() { }

        private void ApplyConfiguration(ConfigPayload config)
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

    // Overloaded methods (both with and without payload)
    [StateMachine(typeof(OverloadState), typeof(OverloadTrigger))]
    [PayloadType(typeof(OverloadPayload))]
    public partial class OverloadedMachine
    {
        public List<string> CallLog { get; } = [];

        [State(OverloadState.B, OnEntry = nameof(OnEntryB))]
        private void ConfigureStates() { }

        [Transition(OverloadState.A, OverloadTrigger.Go, OverloadState.B,
            Guard = nameof(CanGo), Action = nameof(DoTransition))]
        private void Configure() { }

        // Parameterless versions
        private bool CanGo()
        {
            CallLog.Add("Guard()");
            return true;
        }

        private void DoTransition()
        {
            CallLog.Add("Action()");
        }

        private void OnEntryB()
        {
            CallLog.Add("OnEntry()");
        }

        // Payload versions
        private bool CanGo(OverloadPayload payload)
        {
            CallLog.Add("Guard(payload)");
            return true;
        }

        private void DoTransition(OverloadPayload payload)
        {
            CallLog.Add("Action(payload)");
        }

        private void OnEntryB(OverloadPayload payload)
        {
            CallLog.Add("OnEntry(payload)");
        }
    }

    // Internal transition with payload
    [StateMachine(typeof(InternalPayloadState), typeof(InternalPayloadTrigger))]
    [PayloadType(typeof(UpdatePayload))]
    public partial class InternalPayloadMachine
    {
        public int Counter { get; private set; }

        [InternalTransition(InternalPayloadState.Active, InternalPayloadTrigger.Update, nameof(UpdateCounter))]
        [Transition(InternalPayloadState.Active, InternalPayloadTrigger.Deactivate, InternalPayloadState.Inactive)]
        private void Configure() { }

        private void UpdateCounter(UpdatePayload update)
        {
            Counter += update.Increment;
        }
    }

    // Mixed payload types (default + specific)
    [StateMachine(typeof(MixedState), typeof(MixedTrigger))]
    [PayloadType(typeof(DefaultPayload))]
    [PayloadType(MixedTrigger.Special, typeof(SpecialPayload))]
    public partial class MixedPayloadMachine
    {
        public int LastDefaultId { get; private set; }
        public string LastSpecialValue { get; private set; }

        [Transition(MixedState.Start, MixedTrigger.Regular, MixedState.Middle,
            Action = nameof(ProcessDefault))]
        [Transition(MixedState.Middle, MixedTrigger.Special, MixedState.End,
            Action = nameof(ProcessSpecial))]
        private void Configure() { }

        private void ProcessDefault(DefaultPayload payload)
        {
            LastDefaultId = payload.Id;
        }

        private void ProcessSpecial(SpecialPayload payload)
        {
            LastSpecialValue = payload.SpecialValue;
        }
    }

    // Initial state OnEntry behavior
    [StateMachine(typeof(InitialPayloadState), typeof(InitialPayloadTrigger))]
    [PayloadType(typeof(OverloadPayload))]
    public partial class InitialPayloadMachine
    {
        public bool InitialEntryCalledParameterless { get; private set; }
        public bool InitialEntryCalledWithPayload { get; private set; }

        [State(InitialPayloadState.Start, OnEntry = nameof(OnEntryStart))]
        private void ConfigureStates() { }

        [Transition(InitialPayloadState.Start, InitialPayloadTrigger.Go, InitialPayloadState.Next)]
        private void Configure() { }

        private void OnEntryStart()
        {
            InitialEntryCalledParameterless = true;
        }

        private void OnEntryStart(OverloadPayload payload)
        {
            InitialEntryCalledWithPayload = true;
        }
    }

    // OnExit doesn't receive payload
    [StateMachine(typeof(ExitState), typeof(ExitTrigger))]
    [PayloadType(typeof(ExitPayload))]
    public partial class ExitCallbackMachine
    {
        public bool OnExitCalled { get; private set; }
        public string OnExitPayloadData { get; private set; }

        [State(ExitState.A, OnExit = nameof(OnExitA))]
        private void ConfigureStates() { }

        [Transition(ExitState.A, ExitTrigger.Go, ExitState.B)]
        private void Configure() { }

        private void OnExitA()
        {
            OnExitCalled = true;
            // OnExit cannot receive payload, so OnExitPayloadData remains null
        }
    }

    // Complex workflow
    [StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger))]
    [PayloadType(typeof(WorkflowPayload))]
    public partial class WorkflowMachine
    {
        public int Priority { get; private set; }
        public string ApprovedBy { get; private set; }
        public string Result { get; private set; }

        [State(WorkflowState.Initialized, OnEntry = nameof(OnInitialized))]
        [State(WorkflowState.Approved, OnEntry = nameof(OnApproved))]
        [State(WorkflowState.Completed, OnEntry = nameof(OnCompleted))]
        private void ConfigureStates() { }

        [Transition(WorkflowState.Created, WorkflowTrigger.Initialize, WorkflowState.Initialized)]
        [Transition(WorkflowState.Initialized, WorkflowTrigger.Submit, WorkflowState.Submitted)]
        [Transition(WorkflowState.Submitted, WorkflowTrigger.Approve, WorkflowState.Approved)]
        [Transition(WorkflowState.Approved, WorkflowTrigger.Complete, WorkflowState.Completed)]
        private void Configure() { }

        private void OnInitialized(WorkflowPayload payload)
        {
            Priority = payload.Priority;
        }

        private void OnApproved(WorkflowPayload payload)
        {
            ApprovedBy = payload.ApprovedBy;
        }

        private void OnCompleted(WorkflowPayload payload)
        {
            Result = payload.Result;
        }
    }

    // CanFire with payload
    [StateMachine(typeof(ConditionalState), typeof(ConditionalTrigger))]
    [PayloadType(typeof(ConditionalPayload))]
    public partial class ConditionalPayloadMachine
    {
        [Transition(ConditionalState.Ready, ConditionalTrigger.Execute, ConditionalState.Done,
            Guard = nameof(IsValid))]
        private void Configure() { }

        private bool IsValid(ConditionalPayload payload) => payload.IsValid;
    }

    // GetPermittedTriggers
    [StateMachine(typeof(PermittedState), typeof(PermittedTrigger))]
    [PayloadType(typeof(DefaultPayload))]
    public partial class PermittedTriggersMachine
    {
        [Transition(PermittedState.A, PermittedTrigger.Next, PermittedState.B)]
        [Transition(PermittedState.A, PermittedTrigger.Skip, PermittedState.C)]
        [Transition(PermittedState.B, PermittedTrigger.Next, PermittedState.C)]
        private void Configure() { }
    }

    // Strict multi-payload for error testing
    [StateMachine(typeof(StrictState), typeof(StrictTrigger))]
    [PayloadType(StrictTrigger.Process, typeof(ExpectedPayload))]
    public partial class StrictMultiPayloadMachine
    {
        [Transition(StrictState.Ready, StrictTrigger.Process, StrictState.Processing)]
        private void Configure() { }
    }
}
