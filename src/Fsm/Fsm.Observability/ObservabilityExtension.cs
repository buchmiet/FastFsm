using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FastFsm.Contracts;
using FastFsm.Exceptions;
using Microsoft.Extensions.Logging;

namespace FastFsm.Observability;

public sealed class ObservabilityExtension<TState, TTrigger> : IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    private readonly ConcurrentDictionary<AttemptKey, Activity> _activeActivities = new();
    private readonly FastFsmObservabilityOptions _options;
    private readonly ILogger<ObservabilityExtension<TState, TTrigger>>? _logger;
    private readonly IObservabilityEventSink? _eventSink;
    private readonly string _stateTypeName;
    private readonly string _triggerTypeName;

    public ObservabilityExtension(
        FastFsmObservabilityOptions options,
        ILogger<ObservabilityExtension<TState, TTrigger>>? logger = null,
        IObservabilityEventSink? eventSink = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _logger = options.Logging ? logger : null;
        _eventSink = options.EventStream ? eventSink : null;
        _stateTypeName = typeof(TState).Name;
        _triggerTypeName = typeof(TTrigger).Name;
        Hooks = ComputeHooks(options);
    }

    public ExtensionHooks Hooks { get; }

    public void OnAttemptStarting(in TransitionAttemptContext<TState, TTrigger> attempt)
    {
        if (_options.Metrics)
        {
            ObservabilityTelemetry.Attempts.Add(1);
        }

        if (_options.Tracing)
        {
            var activity = ObservabilityTelemetry.ActivitySource.StartActivity(
                "fsm.transition",
                ActivityKind.Internal);

            if (activity is not null)
            {
                activity.SetTag("fastfsm.state_type", _stateTypeName);
                activity.SetTag("fastfsm.trigger_type", _triggerTypeName);
                activity.SetTag("fastfsm.instance_id", attempt.InstanceId);
                activity.SetTag("fastfsm.attempt_id", attempt.AttemptId);
                activity.SetTag("fastfsm.source_state", FormatEnum(attempt.SourceState));
                activity.SetTag("fastfsm.trigger", FormatEnum(attempt.Trigger));
                _activeActivities[new AttemptKey(attempt.InstanceId, attempt.AttemptId)] = activity;
            }
        }

        EmitEvent(
            ObservabilityEventKind.AttemptStarting,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.AttemptStarting(
                _logger,
                FormatEnum(attempt.SourceState),
                FormatEnum(attempt.Trigger));
        }
    }

    public void OnTransitionMatched(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> matched)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.SetTag("fastfsm.handled_at_state", FormatEnum(matched.HandledAtState));
            activity.SetTag("fastfsm.transition.kind", matched.Kind.ToString());

            if (matched.DeclaredTarget is TState declaredTarget)
            {
                activity.SetTag("fastfsm.declared_target", FormatEnum(declaredTarget));
            }

            activity.AddEvent(new ActivityEvent("transition.matched"));
        }

        EmitEvent(
            ObservabilityEventKind.TransitionMatched,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            handledAtState: FormatEnum(matched.HandledAtState),
            declaredTarget: matched.DeclaredTarget is TState declared ? FormatEnum(declared) : null,
            transitionKind: matched.Kind.ToString(),
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.TransitionMatched(
                _logger,
                FormatEnum(attempt.SourceState),
                FormatEnum(attempt.Trigger),
                FormatEnum(matched.HandledAtState),
                matched.Kind.ToString());
        }
    }

    public void OnAttemptCompleted(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionResult<TState> result)
    {
        var elapsed = Stopwatch.GetElapsedTime(attempt.StartTimestamp);

        if (_options.Metrics)
        {
            RecordMetrics(in attempt, in result, elapsed);
        }

        if (_options.Tracing && TryRemoveActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            CompleteActivity(activity, in result);
        }

        EmitEvent(
            ObservabilityEventKind.AttemptCompleted,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            handledAtState: result.MatchedTransition is TransitionInfo<TState> matched
                ? FormatEnum(matched.HandledAtState)
                : null,
            declaredTarget: result.MatchedTransition is TransitionInfo<TState> matchedTransition
                && matchedTransition.DeclaredTarget is TState declaredTarget
                    ? FormatEnum(declaredTarget)
                    : null,
            resolvedTarget: result.ResolvedTarget is TState resolvedTarget
                ? FormatEnum(resolvedTarget)
                : null,
            finalState: FormatEnum(result.FinalState),
            transitionKind: result.MatchedTransition?.Kind.ToString(),
            outcome: result.Outcome.ToString(),
            stage: result.Stage?.ToString(),
            payload: FormatPayload(attempt.Payload),
            exception: result.Exception);

        if (_logger is not null)
        {
            ObservabilityLog.AttemptCompleted(
                _logger,
                FormatEnum(attempt.SourceState),
                FormatEnum(attempt.Trigger),
                FormatEnum(result.FinalState),
                result.Outcome.ToString(),
                elapsed.TotalMilliseconds);
        }
    }

    public void OnGuardEvaluating(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "guard.evaluating",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.guard.name", guardName }
                }));
        }

        EmitEvent(
            ObservabilityEventKind.GuardEvaluating,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            handledAtState: FormatEnum(candidate.HandledAtState),
            declaredTarget: candidate.DeclaredTarget is TState declared ? FormatEnum(declared) : null,
            transitionKind: candidate.Kind.ToString(),
            guardName: guardName,
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.GuardEvaluating(_logger, guardName, FormatEnum(attempt.SourceState));
        }
    }

    public void OnGuardEvaluated(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName,
        bool result)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "guard.evaluated",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.guard.name", guardName },
                    { "fastfsm.guard.result", result }
                }));
        }

        EmitEvent(
            ObservabilityEventKind.GuardEvaluated,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            handledAtState: FormatEnum(candidate.HandledAtState),
            declaredTarget: candidate.DeclaredTarget is TState declared ? FormatEnum(declared) : null,
            transitionKind: candidate.Kind.ToString(),
            guardName: guardName,
            guardResult: result,
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.GuardEvaluated(_logger, guardName, result, FormatEnum(attempt.SourceState));
        }
    }

    public void OnStateExiting(in TransitionAttemptContext<TState, TTrigger> attempt, TState state)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "state.exiting",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.state", FormatEnum(state) }
                }));
        }

        EmitEvent(
            ObservabilityEventKind.StateExiting,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            state: FormatEnum(state),
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.StateExiting(_logger, FormatEnum(state), FormatEnum(attempt.SourceState));
        }
    }

    public void OnStateEntered(in TransitionAttemptContext<TState, TTrigger> attempt, TState state)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "state.entered",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.state", FormatEnum(state) }
                }));
        }

        EmitEvent(
            ObservabilityEventKind.StateEntered,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            state: FormatEnum(state),
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.StateEntered(_logger, FormatEnum(state), FormatEnum(attempt.SourceState));
        }
    }

    public void OnCallbackExecuting(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TransitionStage stage,
        string callbackName)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "callback.executing",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.callback.stage", stage.ToString() },
                    { "fastfsm.callback.name", callbackName }
                }));
        }

        EmitEvent(
            ObservabilityEventKind.CallbackExecuting,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            stage: stage.ToString(),
            callbackName: callbackName,
            payload: FormatPayload(attempt.Payload));

        if (_logger is not null)
        {
            ObservabilityLog.CallbackExecuting(
                _logger,
                stage.ToString(),
                callbackName,
                FormatEnum(attempt.SourceState));
        }
    }

    public void OnCallbackFaulted(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TransitionStage stage,
        string callbackName,
        Exception exception)
    {
        if (_options.Tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "callback.faulted",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.callback.stage", stage.ToString() },
                    { "fastfsm.callback.name", callbackName },
                    { "fastfsm.exception.type", exception.GetType().Name }
                }));
        }

        EmitEvent(
            ObservabilityEventKind.CallbackFaulted,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            sourceState: FormatEnum(attempt.SourceState),
            trigger: FormatEnum(attempt.Trigger),
            stage: stage.ToString(),
            callbackName: callbackName,
            payload: FormatPayload(attempt.Payload),
            exception: exception);

        if (_logger is not null)
        {
            ObservabilityLog.CallbackFaulted(
                _logger,
                stage.ToString(),
                callbackName,
                FormatEnum(attempt.SourceState),
                exception);
        }
    }

    public void OnMachineStarted(Guid instanceId, TState initialState)
    {
        EmitEvent(
            ObservabilityEventKind.MachineStarted,
            instanceId,
            attemptId: 0,
            timestamp: Stopwatch.GetTimestamp(),
            finalState: FormatEnum(initialState));

        if (_logger is not null)
        {
            ObservabilityLog.MachineStarted(_logger, FormatEnum(initialState));
        }
    }

    private void RecordMetrics(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionResult<TState> result,
        TimeSpan elapsed)
    {
        TagList tags = default;
        tags.Add("outcome", result.Outcome.ToString());

        if (_options.IncludeStateTriggerMetricTags)
        {
            tags.Add("source_state", FormatEnum(attempt.SourceState));
            tags.Add("trigger", FormatEnum(attempt.Trigger));
        }

        if (result.MatchedTransition is TransitionInfo<TState> matched)
        {
            tags.Add("transition_kind", matched.Kind.ToString());
        }

        ObservabilityTelemetry.Completed.Add(1, in tags);
        ObservabilityTelemetry.Duration.Record(elapsed.TotalSeconds, in tags);

        switch (result.Outcome)
        {
            case TransitionOutcome.Faulted:
                ObservabilityTelemetry.Failures.Add(1);
                break;
            case TransitionOutcome.Canceled:
                ObservabilityTelemetry.Cancellations.Add(1);
                break;
            case TransitionOutcome.GuardRejected:
                ObservabilityTelemetry.GuardRejected.Add(1);
                break;
            case TransitionOutcome.UnhandledTrigger:
                ObservabilityTelemetry.Unhandled.Add(1);
                break;
        }
    }

    private static void CompleteActivity(
        Activity activity,
        in TransitionResult<TState> result)
    {
        activity.SetTag("fastfsm.final_state", FormatEnum(result.FinalState));
        activity.SetTag("fastfsm.outcome", result.Outcome.ToString());

        if (result.ResolvedTarget is TState resolvedTarget)
        {
            activity.SetTag("fastfsm.resolved_target", FormatEnum(resolvedTarget));
        }

        if (result.Stage is TransitionStage stage)
        {
            activity.SetTag("fastfsm.failure.stage", stage.ToString());
        }

        switch (result.Outcome)
        {
            case TransitionOutcome.Faulted:
                activity.SetStatus(ActivityStatusCode.Error, result.Exception?.Message);
                if (result.Exception is not null)
                {
                    activity.AddEvent(new ActivityEvent(
                        "exception",
                        tags: new ActivityTagsCollection
                        {
                            { "fastfsm.exception.type", result.Exception.GetType().Name }
                        }));
                }

                break;

            case TransitionOutcome.Canceled:
                activity.SetTag("fastfsm.canceled", true);
                activity.SetStatus(ActivityStatusCode.Ok);
                break;

            default:
                activity.SetStatus(ActivityStatusCode.Ok);
                break;
        }

        activity.Dispose();
    }

    private void EmitEvent(
        ObservabilityEventKind kind,
        Guid instanceId,
        long attemptId,
        long timestamp,
        string? sourceState = null,
        string? trigger = null,
        string? handledAtState = null,
        string? declaredTarget = null,
        string? resolvedTarget = null,
        string? finalState = null,
        string? state = null,
        string? transitionKind = null,
        string? outcome = null,
        string? stage = null,
        string? guardName = null,
        bool? guardResult = null,
        string? callbackName = null,
        string? payload = null,
        Exception? exception = null)
    {
        if (!_options.EventStream || _eventSink is null)
        {
            return;
        }

        _eventSink.OnEvent(new ObservabilityEvent
        {
            Kind = kind,
            InstanceId = instanceId,
            AttemptId = attemptId,
            Timestamp = timestamp,
            SourceState = sourceState,
            Trigger = trigger,
            HandledAtState = handledAtState,
            DeclaredTarget = declaredTarget,
            ResolvedTarget = resolvedTarget,
            FinalState = finalState,
            State = state,
            TransitionKind = transitionKind,
            Outcome = outcome,
            Stage = stage,
            GuardName = guardName,
            GuardResult = guardResult,
            CallbackName = callbackName,
            Payload = payload,
            Exception = exception
        });
    }

    private string? FormatPayload(object? payload)
    {
        if (!_options.CapturePayload || payload is null || _options.PayloadFormatter is null)
        {
            return null;
        }

        return _options.PayloadFormatter(payload);
    }

    private bool TryGetActivity(Guid instanceId, long attemptId, out Activity activity)
        => _activeActivities.TryGetValue(new AttemptKey(instanceId, attemptId), out activity!);

    private bool TryRemoveActivity(Guid instanceId, long attemptId, out Activity activity)
        => _activeActivities.TryRemove(new AttemptKey(instanceId, attemptId), out activity!);

    private static string FormatEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value.ToString();

    private static ExtensionHooks ComputeHooks(FastFsmObservabilityOptions options)
    {
        var hooks = ExtensionHooks.None;
        var wantsTransitions = options.Tracing || options.Metrics || options.EventStream || options.Logging;

        if (wantsTransitions)
        {
            hooks |= ExtensionHooks.Transitions;
        }

        if (options.IncludeGuardEvents && (options.Tracing || options.EventStream || options.Logging))
        {
            hooks |= ExtensionHooks.Guards;
        }

        if (options.IncludeStateEvents && (options.Tracing || options.EventStream || options.Logging))
        {
            hooks |= ExtensionHooks.States;
        }

        if (options.IncludeCallbackEvents && (options.Tracing || options.EventStream || options.Logging))
        {
            hooks |= ExtensionHooks.Callbacks;
        }

        if (options.EventStream || options.Logging)
        {
            hooks |= ExtensionHooks.Lifecycle;
        }

        return hooks;
    }

    private readonly struct AttemptKey(Guid instanceId, long attemptId) : IEquatable<AttemptKey>
    {
        public Guid InstanceId { get; } = instanceId;

        public long AttemptId { get; } = attemptId;

        public bool Equals(AttemptKey other)
            => InstanceId.Equals(other.InstanceId) && AttemptId == other.AttemptId;

        public override bool Equals(object? obj) => obj is AttemptKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(InstanceId, AttemptId);
    }
}
