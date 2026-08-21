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
    private readonly bool _metrics;
    private readonly bool _tracing;
    private readonly bool _eventStream;
    private readonly bool _logging;
    private readonly bool _includeStateTriggerMetricTags;
    private readonly bool _capturePayload;
    private readonly Func<object?, string?>? _payloadFormatter;
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

        _metrics = options.Metrics;
        _tracing = options.Tracing;
        _eventStream = options.EventStream;
        _logging = options.Logging;
        _includeStateTriggerMetricTags = options.IncludeStateTriggerMetricTags;
        _capturePayload = options.CapturePayload;
        _payloadFormatter = options.PayloadFormatter;
        _logger = _logging ? logger : null;
        _eventSink = _eventStream ? eventSink : null;
        _stateTypeName = typeof(TState).Name;
        _triggerTypeName = typeof(TTrigger).Name;
        Hooks = ComputeHooks(options);
    }

    public ExtensionHooks Hooks { get; }

    public void OnAttemptStarting(in TransitionAttemptContext<TState, TTrigger> attempt)
    {
        if (_metrics)
        {
            ObservabilityTelemetry.Attempts.Add(1);
        }

        if (_tracing)
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

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.AttemptStarting,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                Payload = FormatPayload(attempt.Payload)
            });
        }

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
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.SetTag("fastfsm.handled_at_state", FormatEnum(matched.HandledAtState));
            activity.SetTag("fastfsm.transition.kind", KindName(matched.Kind));

            if (matched.DeclaredTarget is TState declaredTarget)
            {
                activity.SetTag("fastfsm.declared_target", FormatEnum(declaredTarget));
            }

            activity.AddEvent(new ActivityEvent("transition.matched"));
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.TransitionMatched,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                HandledAtState = FormatEnum(matched.HandledAtState),
                DeclaredTarget = matched.DeclaredTarget is TState declared ? FormatEnum(declared) : null,
                TransitionKind = KindName(matched.Kind),
                Payload = FormatPayload(attempt.Payload)
            });
        }

        if (_logger is not null)
        {
            ObservabilityLog.TransitionMatched(
                _logger,
                FormatEnum(attempt.SourceState),
                FormatEnum(attempt.Trigger),
                FormatEnum(matched.HandledAtState),
                KindName(matched.Kind));
        }
    }

    public void OnAttemptCompleted(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionResult<TState> result)
    {
        if (_metrics)
        {
            RecordMetrics(in attempt, in result, Stopwatch.GetElapsedTime(attempt.StartTimestamp));
        }

        if (_tracing && TryRemoveActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            CompleteActivity(activity, in result);
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.AttemptCompleted,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                HandledAtState = result.MatchedTransition is TransitionInfo<TState> matched
                    ? FormatEnum(matched.HandledAtState)
                    : null,
                DeclaredTarget = result.MatchedTransition is TransitionInfo<TState> matchedTransition
                    && matchedTransition.DeclaredTarget is TState declaredTarget
                        ? FormatEnum(declaredTarget)
                        : null,
                ResolvedTarget = result.ResolvedTarget is TState resolvedTarget
                    ? FormatEnum(resolvedTarget)
                    : null,
                FinalState = FormatEnum(result.FinalState),
                TransitionKind = result.MatchedTransition is TransitionInfo<TState> info
                    ? KindName(info.Kind)
                    : null,
                Outcome = OutcomeName(result.Outcome),
                Stage = result.Stage?.ToString(),
                Payload = FormatPayload(attempt.Payload),
                Exception = result.Exception
            });
        }

        if (_logger is not null)
        {
            ObservabilityLog.AttemptCompleted(
                _logger,
                FormatEnum(attempt.SourceState),
                FormatEnum(attempt.Trigger),
                FormatEnum(result.FinalState),
                OutcomeName(result.Outcome),
                Stopwatch.GetElapsedTime(attempt.StartTimestamp).TotalMilliseconds);
        }
    }

    public void OnGuardEvaluating(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName)
    {
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "guard.evaluating",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.guard.name", guardName }
                }));
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.GuardEvaluating,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                HandledAtState = FormatEnum(candidate.HandledAtState),
                DeclaredTarget = candidate.DeclaredTarget is TState declared ? FormatEnum(declared) : null,
                TransitionKind = KindName(candidate.Kind),
                GuardName = guardName,
                Payload = FormatPayload(attempt.Payload)
            });
        }

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
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "guard.evaluated",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.guard.name", guardName },
                    { "fastfsm.guard.result", result }
                }));
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.GuardEvaluated,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                HandledAtState = FormatEnum(candidate.HandledAtState),
                DeclaredTarget = candidate.DeclaredTarget is TState declared ? FormatEnum(declared) : null,
                TransitionKind = KindName(candidate.Kind),
                GuardName = guardName,
                GuardResult = result,
                Payload = FormatPayload(attempt.Payload)
            });
        }

        if (_logger is not null)
        {
            ObservabilityLog.GuardEvaluated(_logger, guardName, result, FormatEnum(attempt.SourceState));
        }
    }

    public void OnStateExiting(in TransitionAttemptContext<TState, TTrigger> attempt, TState state)
    {
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "state.exiting",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.state", FormatEnum(state) }
                }));
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.StateExiting,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                State = FormatEnum(state),
                Payload = FormatPayload(attempt.Payload)
            });
        }

        if (_logger is not null)
        {
            ObservabilityLog.StateExiting(_logger, FormatEnum(state), FormatEnum(attempt.SourceState));
        }
    }

    public void OnStateEntered(in TransitionAttemptContext<TState, TTrigger> attempt, TState state)
    {
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "state.entered",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.state", FormatEnum(state) }
                }));
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.StateEntered,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                State = FormatEnum(state),
                Payload = FormatPayload(attempt.Payload)
            });
        }

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
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
        {
            activity.AddEvent(new ActivityEvent(
                "callback.executing",
                tags: new ActivityTagsCollection
                {
                    { "fastfsm.callback.stage", stage.ToString() },
                    { "fastfsm.callback.name", callbackName }
                }));
        }

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.CallbackExecuting,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                Stage = stage.ToString(),
                CallbackName = callbackName,
                Payload = FormatPayload(attempt.Payload)
            });
        }

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
        if (_tracing && TryGetActivity(attempt.InstanceId, attempt.AttemptId, out var activity))
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

        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.CallbackFaulted,
                InstanceId = attempt.InstanceId,
                AttemptId = attempt.AttemptId,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = attempt.StartTimestamp,
                SourceState = FormatEnum(attempt.SourceState),
                Trigger = FormatEnum(attempt.Trigger),
                Stage = stage.ToString(),
                CallbackName = callbackName,
                Payload = FormatPayload(attempt.Payload),
                Exception = exception
            });
        }

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
        if (_eventStream && _eventSink is not null)
        {
            _eventSink.OnEvent(new ObservabilityEvent
            {
                Kind = ObservabilityEventKind.MachineStarted,
                InstanceId = instanceId,
                AttemptId = 0,
                Timestamp = Stopwatch.GetTimestamp(),
                AttemptStartTimestamp = 0,
                FinalState = FormatEnum(initialState)
            });
        }

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
        tags.Add("outcome", OutcomeName(result.Outcome));

        if (_includeStateTriggerMetricTags)
        {
            tags.Add("source_state", FormatEnum(attempt.SourceState));
            tags.Add("trigger", FormatEnum(attempt.Trigger));
        }

        if (result.MatchedTransition is TransitionInfo<TState> matched)
        {
            tags.Add("transition_kind", KindName(matched.Kind));
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
        activity.SetTag("fastfsm.outcome", OutcomeName(result.Outcome));

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

    private string? FormatPayload(object? payload)
    {
        if (!_capturePayload || payload is null || _payloadFormatter is null)
        {
            return null;
        }

        return _payloadFormatter(payload);
    }

    private bool TryGetActivity(Guid instanceId, long attemptId, out Activity activity)
        => _activeActivities.TryGetValue(new AttemptKey(instanceId, attemptId), out activity!);

    private bool TryRemoveActivity(Guid instanceId, long attemptId, out Activity activity)
        => _activeActivities.TryRemove(new AttemptKey(instanceId, attemptId), out activity!);

    private static string FormatEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value.ToString();

    private static string OutcomeName(TransitionOutcome outcome) => outcome switch
    {
        TransitionOutcome.Succeeded => nameof(TransitionOutcome.Succeeded),
        TransitionOutcome.GuardRejected => nameof(TransitionOutcome.GuardRejected),
        TransitionOutcome.UnhandledTrigger => nameof(TransitionOutcome.UnhandledTrigger),
        TransitionOutcome.InvalidPayload => nameof(TransitionOutcome.InvalidPayload),
        TransitionOutcome.Canceled => nameof(TransitionOutcome.Canceled),
        TransitionOutcome.Faulted => nameof(TransitionOutcome.Faulted),
        _ => outcome.ToString()
    };

    private static string KindName(TransitionKind kind) => kind switch
    {
        TransitionKind.External => nameof(TransitionKind.External),
        TransitionKind.Internal => nameof(TransitionKind.Internal),
        _ => kind.ToString()
    };

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
