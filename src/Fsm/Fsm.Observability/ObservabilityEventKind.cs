namespace FastFsm.Observability;

public enum ObservabilityEventKind
{
    MachineStarted,
    AttemptStarting,
    TransitionMatched,
    GuardEvaluating,
    GuardEvaluated,
    StateExiting,
    StateEntered,
    CallbackExecuting,
    CallbackFaulted,
    AttemptCompleted
}
