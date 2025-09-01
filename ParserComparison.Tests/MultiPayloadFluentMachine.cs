using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
[PayloadType(MultiTrigger.Configure, typeof(ConfigPayload))]
[PayloadType(MultiTrigger.Process, typeof(DataPayload))]
[PayloadType(MultiTrigger.Error, typeof(ErrorPayload))]
public partial class MultiPayloadFluentMachine
{
    public enum MultiState { Initial, Configured, Processing, Failed }
    public enum MultiTrigger { Configure, Process, Error }

    private static void Configure() => FSM
        .State(MultiState.Initial)
            .On(MultiTrigger.Configure).GoTo(MultiState.Configured).Action(nameof(ApplyConfiguration))
        .State(MultiState.Configured)
            .On(MultiTrigger.Process).GoTo(MultiState.Processing).Action(nameof(ProcessData))
        .State(MultiState.Processing)
            .On(MultiTrigger.Error).GoTo(MultiState.Failed).Action(nameof(HandleError));

    private void ApplyConfiguration(ConfigPayload config) { }
    private void ProcessData(DataPayload data) { }
    private void HandleError(ErrorPayload error) { }

    public sealed class ConfigPayload { public required string Setting { get; init; } }
    public sealed class DataPayload { public int Value { get; init; } }
    public sealed class ErrorPayload { public required string Code { get; init; } }
}

