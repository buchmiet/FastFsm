namespace Generator.ModernGeneration.Hooks;

public enum HookSlot
{
    // M3
    PayloadValidation,

    // M4
    BeforeTransition,
    GuardEvaluationStarted,
    GuardEvaluated,
    AfterActionExecuted,
    AfterTransition,
    TransitionFailed,

    // M6 – pełne pokrycie cyklu życia
    AfterOnEntryExecuted,      // natychmiast po metodzie OnEntry(state, payload)
    AfterOnExitExecuted,       // po OnExit(state, payload)
    ActionExecutionFailed,     // wyjątek w DoAction/DoProcess
    OnEntryExecutionFailed,    // wyjątek w OnEntry
    OnExitExecutionFailed      // wyjątek w OnExit
}