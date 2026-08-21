using System;

namespace FastFsm.Observability;

public sealed class FastFsmObservabilityOptions
{
    public static FastFsmObservabilityOptions Default { get; } = new();

    public bool Tracing { get; init; }

    public bool Metrics { get; init; }

    public bool EventStream { get; init; }

    public bool Logging { get; init; }

    public bool IncludeStateTriggerMetricTags { get; init; }

    public bool IncludeGuardEvents { get; init; }

    public bool IncludeStateEvents { get; init; }

    public bool IncludeCallbackEvents { get; init; }

    public bool CapturePayload { get; init; }

    public Func<object?, string?>? PayloadFormatter { get; init; }

    public static FastFsmObservabilityOptions Create(Action<Builder>? configure = null)
    {
        var builder = new Builder();
        configure?.Invoke(builder);
        return builder.Build();
    }

    public sealed class Builder
    {
        public bool Tracing { get; set; }

        public bool Metrics { get; set; }

        public bool EventStream { get; set; }

        public bool Logging { get; set; }

        public bool IncludeStateTriggerMetricTags { get; set; }

        public bool IncludeGuardEvents { get; set; }

        public bool IncludeStateEvents { get; set; }

        public bool IncludeCallbackEvents { get; set; }

        public bool CapturePayload { get; set; }

        public Func<object?, string?>? PayloadFormatter { get; set; }

        public FastFsmObservabilityOptions Build() => new()
        {
            Tracing = Tracing,
            Metrics = Metrics,
            EventStream = EventStream,
            Logging = Logging,
            IncludeStateTriggerMetricTags = IncludeStateTriggerMetricTags,
            IncludeGuardEvents = IncludeGuardEvents,
            IncludeStateEvents = IncludeStateEvents,
            IncludeCallbackEvents = IncludeCallbackEvents,
            CapturePayload = CapturePayload,
            PayloadFormatter = PayloadFormatter
        };
    }
}
