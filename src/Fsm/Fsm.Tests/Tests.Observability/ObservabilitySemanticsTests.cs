using System.Diagnostics;
using FastFsm.Contracts;
using FastFsm.Observability;

namespace Tests.Observability;

public sealed class ObservabilitySemanticsTests
{
    [Fact]
    public void Tracing_enabled_emits_one_completed_activity_per_attempt()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
        {
            options.Tracing = true;
            options.EventStream = true;
        });
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Go));
        Assert.False(machine.TryFire(ObservabilityFlatTrigger.Reject));
        Assert.False(machine.TryFire(ObservabilityFlatTrigger.Missing));

        Assert.Equal(3, harness.CompletedActivities.Count);
        Assert.Equal(3, harness.CompletedActivities.Select(activity => activity.GetTagItem("fastfsm.attempt_id")).Distinct().Count());
    }

    [Fact]
    public async Task Async_success_emits_one_completed_activity_per_attempt()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityAsyncFlatState, ObservabilityAsyncFlatTrigger>(options =>
        {
            options.Tracing = true;
            options.EventStream = true;
        });
        var machine = new ObservabilityAsyncFlatMachine(ObservabilityAsyncFlatState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(ObservabilityAsyncFlatTrigger.Go));

        var activity = Assert.Single(harness.CompletedActivities);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal("Succeeded", activity.GetTagItem("fastfsm.outcome") as string);
    }

    [Fact]
    public void Faulted_activity_has_error_status()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityOutcomeState, ObservabilityOutcomeTrigger>(options =>
        {
            options.Tracing = true;
            options.EventStream = true;
        });
        var machine = new ObservabilityOutcomeMachine(ObservabilityOutcomeState.A, [extension])
        {
            FailureStage = FastFsm.Exceptions.TransitionStage.Action
        };
        machine.Start();
        Assert.Throws<ObservabilityOutcomeException>(() => machine.Fire(ObservabilityOutcomeTrigger.Go));

        var activity = Assert.Single(harness.CompletedActivities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Null(activity.GetTagItem("fastfsm.canceled"));
    }

    [Fact]
    public void Canceled_activity_has_ok_status_with_canceled_tag()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityOutcomeState, ObservabilityOutcomeTrigger>(options =>
        {
            options.Tracing = true;
            options.EventStream = true;
        });
        var machine = new ObservabilityOutcomeMachine(ObservabilityOutcomeState.A, [extension])
        {
            CancellationStage = FastFsm.Exceptions.TransitionStage.Guard
        };
        machine.Start();
        Assert.Throws<OperationCanceledException>(() => machine.Fire(ObservabilityOutcomeTrigger.Go));

        var activity = Assert.Single(harness.CompletedActivities);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal(true, activity.GetTagItem("fastfsm.canceled"));
    }

    [Theory]
    [InlineData(nameof(FireGuardRejected))]
    [InlineData(nameof(FireUnhandled))]
    [InlineData(nameof(FireInvalidPayload))]
    public void Non_fault_outcomes_leave_activity_ok_without_canceled_tag(string fireMethod)
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
        {
            options.Tracing = true;
            options.EventStream = true;
        });

        InvokeFireMethod(fireMethod, harness, extension);

        var activity = Assert.Single(harness.CompletedActivities);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Null(activity.GetTagItem("fastfsm.canceled"));
    }

    [Fact]
    public void Completed_event_carries_v2_source_handled_declared_resolved_final_kind_and_outcome()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Go));

        var completed = Assert.Single(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
        Assert.Equal(nameof(ObservabilityFlatState.A), completed.SourceState);
        Assert.Equal(nameof(ObservabilityFlatTrigger.Go), completed.Trigger);
        Assert.Equal(nameof(ObservabilityFlatState.A), completed.HandledAtState);
        Assert.Equal(nameof(ObservabilityFlatState.B), completed.DeclaredTarget);
        Assert.Equal(nameof(ObservabilityFlatState.B), completed.ResolvedTarget);
        Assert.Equal(nameof(ObservabilityFlatState.B), completed.FinalState);
        Assert.Equal(nameof(TransitionKind.External), completed.TransitionKind);
        Assert.Equal(nameof(TransitionOutcome.Succeeded), completed.Outcome);
    }

    [Fact]
    public void Internal_transition_reports_null_declared_and_resolved_targets()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Internal));

        var completed = Assert.Single(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
        Assert.Equal(nameof(TransitionKind.Internal), completed.TransitionKind);
        Assert.Null(completed.DeclaredTarget);
        Assert.Null(completed.ResolvedTarget);
        Assert.Equal(nameof(ObservabilityFlatState.A), completed.FinalState);
    }

    [Fact]
    public void External_self_transition_reports_external_kind()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Self));

        var completed = Assert.Single(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
        Assert.Equal(nameof(TransitionKind.External), completed.TransitionKind);
        Assert.Equal(nameof(ObservabilityFlatState.A), completed.DeclaredTarget);
        Assert.Equal(nameof(ObservabilityFlatState.A), completed.ResolvedTarget);
    }

    [Fact]
    public void Hsm_ancestor_owned_transition_reports_source_and_handled_at_differently()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityHsmState, ObservabilityHsmTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityHsmSemanticsMachine(ObservabilityHsmState.Root, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityHsmTrigger.AncestorTransition));

        var completed = Assert.Single(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
        Assert.Equal(nameof(ObservabilityHsmState.LeftLeaf), completed.SourceState);
        Assert.Equal(nameof(ObservabilityHsmState.Root), completed.HandledAtState);
        Assert.Equal(nameof(ObservabilityHsmState.RightLeaf), completed.DeclaredTarget);
        Assert.Equal(nameof(ObservabilityHsmState.RightLeaf), completed.ResolvedTarget);
        Assert.Equal(nameof(ObservabilityHsmState.RightLeaf), completed.FinalState);
    }

    [Fact]
    public void Composite_entry_reports_declared_composite_and_resolved_initial_leaf()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityHsmState, ObservabilityHsmTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityHsmSemanticsMachine(ObservabilityHsmState.Outside, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityHsmTrigger.EnterComposite));

        var completed = Assert.Single(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
        Assert.Equal(nameof(ObservabilityHsmState.Outside), completed.SourceState);
        Assert.Equal(nameof(ObservabilityHsmState.Outside), completed.HandledAtState);
        Assert.Equal(nameof(ObservabilityHsmState.Root), completed.DeclaredTarget);
        Assert.Equal(nameof(ObservabilityHsmState.LeftLeaf), completed.ResolvedTarget);
        Assert.Equal(nameof(ObservabilityHsmState.LeftLeaf), completed.FinalState);
    }

    [Fact]
    public void Shallow_history_reports_restored_child_as_resolved_target()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityHistoryState, ObservabilityHistoryTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityShallowHistoryMachine(ObservabilityHistoryState.Outside, [extension]);
        machine.Start();
        machine.Fire(ObservabilityHistoryTrigger.Enter);
        machine.Fire(ObservabilityHistoryTrigger.Next);
        machine.Fire(ObservabilityHistoryTrigger.Exit);
        harness.EventSink.Events.Clear();

        Assert.True(machine.TryFire(ObservabilityHistoryTrigger.Enter));

        var completed = Assert.Single(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
        Assert.Equal(nameof(ObservabilityHistoryState.Outside), completed.SourceState);
        Assert.Equal(nameof(ObservabilityHistoryState.Outside), completed.HandledAtState);
        Assert.Equal(nameof(ObservabilityHistoryState.Composite), completed.DeclaredTarget);
        Assert.Equal(nameof(ObservabilityHistoryState.Second), completed.ResolvedTarget);
        Assert.Equal(nameof(ObservabilityHistoryState.Second), completed.FinalState);
    }

    [Fact]
    public void Metrics_record_duration_histogram_and_completed_counter_by_outcome()
    {
        using var harness = new ObservabilityTestHarness();
        harness.CaptureMetricBaseline();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.Metrics = true);
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Go));

        Assert.Equal(1, harness.GetCounterDelta(ObservabilityTelemetry.Attempts.Name));
        Assert.Equal(1, harness.GetCounterDelta(ObservabilityTelemetry.Completed.Name));
        Assert.Single(
            harness.GetHistogramMeasurements(),
            sample => sample.Name == ObservabilityTelemetry.Duration.Name
                && sample.Value >= 0
                && sample.Tags["outcome"] as string == nameof(TransitionOutcome.Succeeded));
    }

    [Fact]
    public void Metric_tags_do_not_include_instance_or_attempt_identifiers()
    {
        using var harness = new ObservabilityTestHarness();
        harness.CaptureMetricBaseline();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
        {
            options.Metrics = true;
            options.IncludeStateTriggerMetricTags = true;
        });
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Go));

        foreach (var sample in harness.GetHistogramMeasurements())
        {
            Assert.DoesNotContain(sample.Tags.Keys, key => key is "instance_id" or "attempt_id" or "fastfsm.instance_id" or "fastfsm.attempt_id");
        }
    }

    [Fact]
    public void Event_stream_payload_is_null_by_default()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityPayloadMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Payload, new ObservabilityPayload(42)));

        Assert.All(harness.EventSink.Events, evt => Assert.Null(evt.Payload));
    }

    [Fact]
    public void All_features_disabled_emit_no_activities_or_metric_increments()
    {
        using var harness = new ObservabilityTestHarness();
        harness.CaptureMetricBaseline();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>();
        Assert.Equal(ExtensionHooks.None, extension.Hooks);

        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();
        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Go));

        Assert.Empty(harness.CompletedActivities);
        Assert.Equal(0, harness.GetCounterDelta(ObservabilityTelemetry.Attempts.Name));
        Assert.Equal(0, harness.GetCounterDelta(ObservabilityTelemetry.Completed.Name));
        Assert.Empty(harness.GetHistogramMeasurements());
        Assert.Empty(harness.EventSink.Events);
    }

    [Fact]
    public void Metrics_only_extension_requests_transition_hooks()
    {
        using var harness = new ObservabilityTestHarness();
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.Metrics = true);
        Assert.Equal(ExtensionHooks.Transitions, extension.Hooks);
    }

    [Fact]
    public void Throwing_event_sink_does_not_break_fsm_transition()
    {
        using var harness = new ObservabilityTestHarness();
        harness.EventSink.ThrowOnEvent = true;
        var extension = harness.CreateExtension<ObservabilityFlatState, ObservabilityFlatTrigger>(options =>
            options.EventStream = true);
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(ObservabilityFlatTrigger.Go));
        Assert.Equal(ObservabilityFlatState.B, machine.CurrentState);
    }

    private static void InvokeFireMethod(
        string fireMethod,
        ObservabilityTestHarness harness,
        ObservabilityExtension<ObservabilityFlatState, ObservabilityFlatTrigger> extension)
    {
        switch (fireMethod)
        {
            case nameof(FireGuardRejected):
                FireGuardRejected(harness, extension);
                break;
            case nameof(FireUnhandled):
                FireUnhandled(harness, extension);
                break;
            case nameof(FireInvalidPayload):
                FireInvalidPayload(harness, extension);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fireMethod), fireMethod, null);
        }
    }

    private static void FireGuardRejected(
        ObservabilityTestHarness harness,
        ObservabilityExtension<ObservabilityFlatState, ObservabilityFlatTrigger> extension)
    {
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();
        Assert.False(machine.TryFire(ObservabilityFlatTrigger.Reject));
    }

    private static void FireUnhandled(
        ObservabilityTestHarness harness,
        ObservabilityExtension<ObservabilityFlatState, ObservabilityFlatTrigger> extension)
    {
        var machine = new ObservabilityFlatMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();
        Assert.False(machine.TryFire(ObservabilityFlatTrigger.Missing));
    }

    private static void FireInvalidPayload(
        ObservabilityTestHarness harness,
        ObservabilityExtension<ObservabilityFlatState, ObservabilityFlatTrigger> extension)
    {
        var machine = new ObservabilityPayloadMachine(ObservabilityFlatState.A, [extension]);
        machine.Start();
        Assert.False(machine.TryFire(ObservabilityFlatTrigger.AlternatePayload, new ObservabilityPayload(42)));
    }
}
