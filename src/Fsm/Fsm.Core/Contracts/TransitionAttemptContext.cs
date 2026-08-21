using System;

namespace FastFsm.Contracts;

public readonly struct TransitionAttemptContext<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    public TransitionAttemptContext(
        Guid instanceId,
        long attemptId,
        TState sourceState,
        TTrigger trigger,
        object? payload,
        long startTimestamp)
    {
        InstanceId = instanceId;
        AttemptId = attemptId;
        SourceState = sourceState;
        Trigger = trigger;
        Payload = payload;
        StartTimestamp = startTimestamp;
    }

    public Guid InstanceId { get; }
    public long AttemptId { get; }
    public TState SourceState { get; }
    public TTrigger Trigger { get; }
    public object? Payload { get; }
    public long StartTimestamp { get; }
}