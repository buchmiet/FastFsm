using Abstractions.Attributes;

namespace FastFsm.Logging.Tests
{
    // Pure variant state machine for testing
    [StateMachine(typeof(TestState), typeof(TestTrigger))]
    public partial class PureStateMachine
    {
        [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing)]
        [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
        private void Configure() { }
    }

    // Basic variant with OnEntry/OnExit
    [StateMachine(typeof(TestState), typeof(TestTrigger))]
    public partial class BasicStateMachine
    {
        public int OnEntryCallCount { get; private set; }
        public int OnExitCallCount { get; private set; }
        public int ActionCallCount { get; private set; }
        public bool GuardResult { get; set; } = true;

        [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
            Guard = nameof(CanStart), Action = nameof(StartAction))]
        [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
        [State(TestState.Initial, OnExit = nameof(OnInitialExit))]
        private void ConfigureStart() { }

        private bool CanStart() => GuardResult;
        private void StartAction() => ActionCallCount++;
        private void OnProcessingEntry() => OnEntryCallCount++;
        private void OnInitialExit() => OnExitCallCount++;
    }

    // WithPayload variant
    [StateMachine(typeof(TestState), typeof(TestTrigger), DefaultPayloadType = typeof(TestPayload))]
    public partial class PayloadStateMachine
    {
        public TestPayload? LastPayload { get; private set; }
        public bool GuardResult { get; set; } = true;

        [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
            Guard = nameof(CanStart), Action = nameof(ProcessAction))]
        [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
        private void ConfigureWithPayload() { }

        [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
        [Transition(TestState.Processing, TestTrigger.Fail, TestState.Failed)]
        private void ConfigureOthers() { }

        [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
        [Transition(TestState.Processing, TestTrigger.Fail, TestState.Failed)]
        [Transition(TestState.Completed, TestTrigger.Reset, TestState.Initial)]
        [Transition(TestState.Failed, TestTrigger.Reset, TestState.Initial)]
        private void ConfigureComplete() { }

        // Guard with payload overload
        private bool CanStart(TestPayload payload)
        {
            LastPayload = payload;
            return GuardResult;
        }

        // Parameterless guard overload
        private bool CanStart() => GuardResult;

        // Action with payload
        private void ProcessAction(TestPayload payload)
        {
            LastPayload = payload;
        }

        // Parameterless action
        private void ProcessAction() { }

        // OnEntry with payload
        private void OnProcessingEntry(TestPayload payload)
        {
            LastPayload = payload;
        }

        // Parameterless OnEntry
        private void OnProcessingEntry() { }
    }

    // WithExtensions variant
    [StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
    public partial class ExtensionsStateMachine
    {
        public bool GuardResult { get; set; } = true;

        [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
            Guard = nameof(CanStart), Action = nameof(StartAction))]
        [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
        private void ConfigureStart() { }

        [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
        [Transition(TestState.Processing, TestTrigger.Fail, TestState.Failed)]
        private void ConfigureOthers() { }

        private bool CanStart() => GuardResult;
        private void StartAction() { }
        private void OnProcessingEntry() { }
    }

    // Full variant (Payload + Extensions)
    [StateMachine(typeof(TestState), typeof(TestTrigger),
        GenerateExtensibleVersion = true, DefaultPayloadType = typeof(TestPayload))]
    public partial class FullStateMachine
    {
        public TestPayload? LastPayload { get; private set; }
        public bool GuardResult { get; set; } = true;

        [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
            Guard = nameof(CanStart), Action = nameof(ProcessAction))]
        [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
        private void ConfigureWithPayload() { }

        // Guard with payload
        private bool CanStart(TestPayload payload)
        {
            LastPayload = payload;
            return GuardResult;
        }

        // Parameterless guard
        private bool CanStart() => GuardResult;

        // Action with payload
        private void ProcessAction(TestPayload payload)
        {
            LastPayload = payload;
        }

        // Parameterless action
        private void ProcessAction() { }

        // OnEntry with payload
        private void OnProcessingEntry(TestPayload payload)
        {
            LastPayload = payload;
        }

        // Parameterless OnEntry
        private void OnProcessingEntry() { }
    }

    // Multi-payload variant for testing payload validation
    [StateMachine(typeof(TestState), typeof(TestTrigger))]
    [PayloadType(TestTrigger.Start, typeof(TestPayload))]
    [PayloadType(TestTrigger.Process, typeof(string))]
    public partial class MultiPayloadStateMachine
    {
        [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing)]
        private void ConfigureWithTestPayload() { }

        [Transition(TestState.Initial, TestTrigger.Process, TestState.Processing)]
        private void ConfigureWithStringPayload() { }
    }
}
