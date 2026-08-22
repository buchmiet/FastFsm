using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FastFsm.Observability;

public static class ObservabilityTelemetry
{
    public const string ActivitySourceName = "FastFsm";
    public const string MeterName = "FastFsm";

    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    public static Meter Meter { get; } = new(MeterName);

    public static Counter<long> Attempts { get; } =
        Meter.CreateCounter<long>("fastfsm.transition.attempts", description: "Transition attempts started.");

    public static Counter<long> Completed { get; } =
        Meter.CreateCounter<long>("fastfsm.transition.completed", description: "Transition attempts completed by outcome.");

    public static Histogram<double> Duration { get; } =
        Meter.CreateHistogram<double>("fastfsm.transition.duration", unit: "s", description: "Transition attempt duration.");

    public static Counter<long> Failures { get; } =
        Meter.CreateCounter<long>("fastfsm.transition.failures", description: "Transition attempts that faulted.");

    public static Counter<long> Cancellations { get; } =
        Meter.CreateCounter<long>("fastfsm.transition.cancellations", description: "Transition attempts that were canceled.");

    public static Counter<long> GuardRejected { get; } =
        Meter.CreateCounter<long>("fastfsm.transition.guard_rejected", description: "Transition attempts rejected by a guard.");

    public static Counter<long> Unhandled { get; } =
        Meter.CreateCounter<long>("fastfsm.transition.unhandled", description: "Transition attempts with an unhandled trigger.");
}
