using Abstractions.Attributes;

namespace Tests.Logging;

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
