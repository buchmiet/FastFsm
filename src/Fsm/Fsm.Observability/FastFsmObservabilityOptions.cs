using System;

namespace FastFsm.Observability;

public sealed class FastFsmObservabilityOptions
{
    public static FastFsmObservabilityOptions Default { get; } = new();

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
}
