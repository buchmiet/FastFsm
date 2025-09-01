using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
[PayloadType(MultiTrigger.Configure, typeof(ConfigPayload))]
[PayloadType(MultiTrigger.Process, typeof(DataPayload))]
[PayloadType(MultiTrigger.Error, typeof(ErrorPayload))]
public partial class MultiPayloadStateMachine
{
    public enum MultiState { Initial, Configured, Processing, Failed }
    public enum MultiTrigger { Configure, Process, Error }

    [Transition(MultiState.Initial, MultiTrigger.Configure, MultiState.Configured, Action = nameof(ApplyConfiguration))]
    [Transition(MultiState.Configured, MultiTrigger.Process, MultiState.Processing, Action = nameof(ProcessData))]
    [Transition(MultiState.Processing, MultiTrigger.Error, MultiState.Failed, Action = nameof(HandleError))]
    private void Configure() { }

    private void ApplyConfiguration(ConfigPayload config) { }
    private void ProcessData(DataPayload data) { }
    private void HandleError(ErrorPayload error) { }

    public sealed class ConfigPayload { public required string Setting { get; init; } }
    public sealed class DataPayload { public int Value { get; init; } }
    public sealed class ErrorPayload { public required string Code { get; init; } }
}

