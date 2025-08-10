// Generated Hierarchical State Machine
namespace TestNamespace;

public partial class SimpleHsmMachine
{
    private ProcessState _currentState;


    private Action? OnWorkEntry;
    private Action? OnWorkExit;
    private Action? OnIdleEntry;
    private Action? OnIdleExit;
    private Action? OnActiveEntry;
    private Action? OnActiveExit;

    public void Fire(ProcessTrigger trigger)
    {
        switch (_currentState)
        {
            case ProcessState.Pending:
                if (trigger == ProcessTrigger.Start)
                {
                    // Priority: 0, Internal: False
                    // Transition: Pending -> Work_Idle (LCA: none)
                    // Enter Work
                    OnWorkEntry?.Invoke();
                    // Enter Work_Idle
                    OnIdleEntry?.Invoke();
                    _currentState = ProcessState.Work_Idle;
                    return;
                }
                break;
            case ProcessState.Work:
                if (trigger == ProcessTrigger.Abort)
                {
                    // Priority: 50, Internal: False
                    // Transition: Work -> Done (LCA: none)
                    // Exit Work
                    OnWorkExit?.Invoke();
                    // Transition action
                    CleanupWork();
                    _currentState = ProcessState.Done;
                    return;
                }
                break;
            case ProcessState.Work_Active:
                if (trigger == ProcessTrigger.Emergency && IsEmergency())
                {
                    // Priority: 200, Internal: False
                    // Transition: Work_Active -> Done (LCA: none)
                    // Exit Work_Active
                    OnActiveExit?.Invoke();
                    // Exit Work
                    OnWorkExit?.Invoke();
                    _currentState = ProcessState.Done;
                    return;
                }
                if (trigger == ProcessTrigger.Tick)
                {
                    // Priority: 150, Internal: True
                    // Internal transition in Work_Active
                    UpdateProgress();
                    // State remains: Work_Active
                    return;
                }
                if (trigger == ProcessTrigger.Finish)
                {
                    // Priority: 100, Internal: False
                    // Transition: Work_Active -> Done (LCA: none)
                    // Exit Work_Active
                    OnActiveExit?.Invoke();
                    // Exit Work
                    OnWorkExit?.Invoke();
                    _currentState = ProcessState.Done;
                    return;
                }
                if (trigger == ProcessTrigger.Finish)
                {
                    // Priority: 80, Internal: False
                    // Transition: Work_Active -> Work_Idle (LCA: Work)
                    // Exit Work_Active
                    OnActiveExit?.Invoke();
                    // Enter Work_Idle
                    OnIdleEntry?.Invoke();
                    _currentState = ProcessState.Work_Idle;
                    return;
                }
                if (trigger == ProcessTrigger.Pause)
                {
                    // Priority: 0, Internal: False
                    // Transition: Work_Active -> Work_Idle (LCA: Work)
                    // Exit Work_Active
                    OnActiveExit?.Invoke();
                    // Enter Work_Idle
                    OnIdleEntry?.Invoke();
                    _currentState = ProcessState.Work_Idle;
                    return;
                }
                // Fallthrough to parent
                goto case ProcessState.Work;
            case ProcessState.Work_Idle:
                if (trigger == ProcessTrigger.Activate)
                {
                    // Priority: 0, Internal: False
                    // Transition: Work_Idle -> Work_Active (LCA: Work)
                    // Exit Work_Idle
                    OnIdleExit?.Invoke();
                    // Enter Work_Active
                    OnActiveEntry?.Invoke();
                    _currentState = ProcessState.Work_Active;
                    return;
                }
                // Fallthrough to parent
                goto case ProcessState.Work;
        }
    }

}
