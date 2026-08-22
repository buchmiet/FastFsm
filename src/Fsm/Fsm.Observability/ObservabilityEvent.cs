using System;

namespace FastFsm.Observability;

public readonly struct ObservabilityEvent
{
    public ObservabilityEventKind Kind { get; init; }

    public Guid InstanceId { get; init; }

    public long AttemptId { get; init; }

    /// <summary>Monotonic timestamp when this event was emitted.</summary>
    public long Timestamp { get; init; }

    /// <summary>Attempt start timestamp for correlating events within one attempt.</summary>
    public long AttemptStartTimestamp { get; init; }

    public string? SourceState { get; init; }

    public string? Trigger { get; init; }

    public string? HandledAtState { get; init; }

    public string? DeclaredTarget { get; init; }

    public string? ResolvedTarget { get; init; }

    public string? FinalState { get; init; }

    public string? State { get; init; }

    public string? TransitionKind { get; init; }

    public string? Outcome { get; init; }

    public string? Stage { get; init; }

    public string? GuardName { get; init; }

    public bool? GuardResult { get; init; }

    public string? CallbackName { get; init; }

    public string? Payload { get; init; }

    public Exception? Exception { get; init; }
}
