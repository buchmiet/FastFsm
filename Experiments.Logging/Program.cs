using Abstractions.Attributes;
using Microsoft.Extensions.Logging;
using StateMachine.Contracts;
using StateMachine.Runtime;
using StateMachine.Runtime.Extensions;


var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
});
ILogger<ThrowingActionMachine> logger = loggerFactory.CreateLogger<ThrowingActionMachine>();
logger.LogDebug("Debug: start programu");
var ext = new ResultCapturingExtension();
var machine = new ThrowingActionMachine(TestState.A, [ext],logger);
var ok = machine.TryFire(TestTrigger.Go);
Console.WriteLine(ext.Results.Count);
machine.Fire(TestTrigger.Go);
logger.LogDebug("Debug: koniec programu");
//logger.LogInformation("Information: zaraz uruchomię maszynę stanów");
//var machine = new OrderMachine(OrderState.New,logger);
//machine.Fire(OrderTrigger.Submit);
//Console.WriteLine($"State: {machine.CurrentState}");

//[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
//public partial class OrderMachine
//{
//    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted)]
//    private void Configure() { }
//}

//public enum OrderState { New, Submitted, Shipped }
//public enum OrderTrigger { Submit, Ship }


//[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
//[GenerationMode(GenerationMode.WithExtensions, Force = true)]
//public partial class ExtensionsStateMachine
//{
//    public bool GuardResult { get; set; } = true;

//    [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
//        Guard = nameof(CanStart), Action = nameof(StartAction))]
//    [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
//    private void ConfigureStart() { }

//    private bool CanStart() => GuardResult;
//    private void StartAction() { }
//    private void OnProcessingEntry() { }
//}

//public enum TestState
//{
//    Initial,
//    Processing,
//    Completed,
//    Failed
//}

///// <summary>
///// Test triggers for all variants
///// </summary>
//public enum TestTrigger
//{
//    Start,
//    Process,
//    Complete,
//    Fail,
//    Reset
//}

//[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class ThrowingActionMachine
{
    //[Transition(TestState.A, TestTrigger.Go, TestState.B, Action = nameof(ThrowingAction))]
    private void Configure() { }

    public void ThrowingAction() => throw new InvalidOperationException("boom");
}

// enumy muszą być w namespace, żeby atrybuty widziały ich pełną nazwę
public enum TestState { A, B }
public enum TestTrigger { Go }


public class ResultCapturingExtension : IStateMachineExtension
{
    public List<bool> Results { get; } = [];

    public void OnAfterTransition<T>(T ctx, bool success) where T : IStateMachineContext
        => Results.Add(success);

    public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext { }
    public void OnGuardEvaluation<T>(T ctx, string g) where T : IStateMachineContext { }
    public void OnGuardEvaluated<T>(T ctx, string g, bool r) where T : IStateMachineContext { }
}

public interface IThrowingActionMachine : IExtensibleStateMachine<TestState, TestTrigger> { }
public partial class ThrowingActionMachine : StateMachineBase<TestState, TestTrigger>, IThrowingActionMachine
{
    private readonly List<IStateMachineExtension> _extensionsList;
    private readonly IReadOnlyList<IStateMachineExtension> _extensions;
    private readonly ExtensionRunner _extensionRunner;

    public IReadOnlyList<IStateMachineExtension> Extensions => _extensions;

    private readonly ILogger<ThrowingActionMachine>? _logger;
    private readonly string _instanceId = Guid.NewGuid().ToString();

    public ThrowingActionMachine(TestState initialState, IEnumerable<IStateMachineExtension>? extensions = null, ILogger<ThrowingActionMachine>? logger = null) : base(initialState)
    {
        _logger = logger;
        _extensionsList = extensions?.ToList() ?? new List<IStateMachineExtension>();
        _extensions = _extensionsList;
        _extensionRunner = new ExtensionRunner(_logger);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public override bool TryFire(TestTrigger trigger, object? payload = null)
    {
        var originalState = _currentState;
        bool success = false;

        switch (_currentState)
        {
            case TestState.A:
                {
                    switch (trigger)
                    {
                        case TestTrigger.Go:
                            {
                                var smCtx = new StateMachineContext<TestState, TestTrigger>(
                                    Guid.NewGuid().ToString(),
                                    _currentState,
                                    trigger,
                                    TestState.B,
                                    payload);

                                _extensionRunner.RunBeforeTransition(_extensions, smCtx);

                                try

                                {
                                    ThrowingAction();
                                }
                                catch (Exception ex)

                                {
                                    success = false;
                                    if (_logger?.IsEnabled(LogLevel.Warning) == true)
                                    {
                                        ThrowingActionMachineLog.TransitionFailed(_logger, _instanceId, "A", "Go");
                                    }
                                    goto END_TRY_FIRE;
                                }
                                if (_logger?.IsEnabled(LogLevel.Debug) == true)
                                {
                                    ThrowingActionMachineLog.ActionExecuted(_logger, _instanceId, "ThrowingAction", "A", "B", "Go");
                                }
                                _currentState = TestState.B;
                                if (_logger?.IsEnabled(LogLevel.Information) == true)
                                {
                                    ThrowingActionMachineLog.TransitionSucceeded(_logger, _instanceId, "A", "B", "Go");
                                }
                                success = true;
                                _extensionRunner.RunAfterTransition(_extensions, smCtx, true);
                                goto END_TRY_FIRE;
                            }
                        default: break;
                    }
                    break;
                }
            default: break;
        }

        if (!success)
        {
            var failCtx = new StateMachineContext<TestState, TestTrigger>(
                Guid.NewGuid().ToString(),
                originalState,
                trigger,
                originalState,
                payload);
            _extensionRunner.RunAfterTransition(_extensions, failCtx, false);
        }
    END_TRY_FIRE:;
        return success;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public override bool CanFire(TestTrigger trigger)
    {
        switch (_currentState)
        {
            case TestState.A:
                switch (trigger)
                {
                    case TestTrigger.Go: return true;
                    default: return false;
                }
            default: return false;
        }
    }

    public override System.Collections.Generic.IReadOnlyList<TestTrigger> GetPermittedTriggers()
    {
        switch (_currentState)
        {
            case TestState.A: return new TestTrigger[] { TestTrigger.Go };
            case TestState.B: return System.Array.Empty<TestTrigger>();
            default: return System.Array.Empty<TestTrigger>();
        }
    }

    public void AddExtension(IStateMachineExtension extension)
    {
        if (extension == null) throw new ArgumentNullException(nameof(extension));
        _extensionsList.Add(extension);
    }

    public bool RemoveExtension(IStateMachineExtension extension)
    {
        if (extension == null) return false;
        return _extensionsList.Remove(extension);
    }
}

[System.CodeDom.Compiler.GeneratedCode("StateMachineGenerator", "1.0.0")]
internal static class ThrowingActionMachineLog
{
    /// <summary>
    /// Logs successful state transition
    /// </summary>
    public static void TransitionSucceeded(this ILogger logger, string instanceId, string fromState, string toState, string trigger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.Log(
                LogLevel.Information,
                new EventId(1, nameof(TransitionSucceeded)),
                "State machine {InstanceId} transitioned from {FromState} to {ToState} on trigger {Trigger}",
                instanceId, fromState, toState, trigger);
        }
    }

    /// <summary>
    /// Logs when guard prevents transition
    /// </summary>
    public static void GuardFailed(this ILogger logger, string instanceId, string guardName, string fromState, string toState, string trigger)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                LogLevel.Warning,
                new EventId(2, nameof(GuardFailed)),
                "State machine {InstanceId} guard {GuardName} prevented transition from {FromState} to {ToState} on trigger {Trigger}",
                instanceId, guardName, fromState, toState, trigger);
        }
    }

    /// <summary>
    /// Logs when no valid transition found
    /// </summary>
    public static void TransitionFailed(this ILogger logger, string instanceId, string fromState, string trigger)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                LogLevel.Warning,
                new EventId(3, nameof(TransitionFailed)),
                "State machine {InstanceId} failed to transition from {FromState} on trigger {Trigger} - no valid transition found",
                instanceId, fromState, trigger);
        }
    }

    /// <summary>
    /// Logs OnEntry method execution
    /// </summary>
    public static void OnEntryExecuted(this ILogger logger, string instanceId, string methodName, string state)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                LogLevel.Debug,
                new EventId(4, nameof(OnEntryExecuted)),
                "State machine {InstanceId} executed OnEntry {MethodName} for state {State}",
                instanceId, methodName, state);
        }
    }

    /// <summary>
    /// Logs OnExit method execution
    /// </summary>
    public static void OnExitExecuted(this ILogger logger, string instanceId, string methodName, string state)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                LogLevel.Debug,
                new EventId(5, nameof(OnExitExecuted)),
                "State machine {InstanceId} executed OnExit {MethodName} for state {State}",
                instanceId, methodName, state);
        }
    }

    /// <summary>
    /// Logs action execution during transition
    /// </summary>
    public static void ActionExecuted(this ILogger logger, string instanceId, string actionName, string fromState, string toState, string trigger)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(
                LogLevel.Debug,
                new EventId(6, nameof(ActionExecuted)),
                "State machine {InstanceId} executed action {ActionName} during transition from {FromState} to {ToState} on trigger {Trigger}",
                instanceId, actionName, fromState, toState, trigger);
        }
    }

    /// <summary>
    /// Logs payload validation failure
    /// </summary>
    public static void PayloadValidationFailed(this ILogger logger, string instanceId, string trigger, string expectedType, string actualType)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(
                LogLevel.Warning,
                new EventId(7, nameof(PayloadValidationFailed)),
                "State machine {InstanceId} payload validation failed for trigger {Trigger} - expected {ExpectedType}, got {ActualType}",
                instanceId, trigger, expectedType, actualType);
        }
    }
}

