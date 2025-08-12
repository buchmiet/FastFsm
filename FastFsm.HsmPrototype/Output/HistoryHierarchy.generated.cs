// Generated Hierarchical State Machine
namespace TestNamespace;

public partial class HistoryHsmMachine
{
    private MachineState _currentState;

    // Shallow history for On
    private MachineState? _history_On;
    // Deep history for On_Working
    private MachineState? _historyDeep_On_Working;

    private Action? OnOnEntry;
    private Action? OnOnExit;
    private Action? OnIdleEntry;
    private Action? OnWorkingEntry;
    private Action? OnWorkingExit;
    private Action? OnFastEntry;
    private Action? OnSlowEntry;

    public void Fire(MachineTrigger trigger)
    {
        switch (_currentState)
        {
            case MachineState.Off:
                if (trigger == MachineTrigger.PowerOn)
                {
                    // Priority: 0, Internal: False
                    // Transition to On with Shallow history
                    if (_history_On.HasValue)
                    {
                        // Restore shallow history
                        var targetState = _history_On.Value;
                        // Enter On
                        OnOnEntry?.Invoke();
                        // Enter historical child
                        switch (targetState)
                        {
                            case MachineState.On_Idle:
                                OnIdleEntry?.Invoke();
                                _currentState = MachineState.On_Idle;
                                break;
                            case MachineState.On_Working:
                                OnWorkingEntry?.Invoke();
                                _currentState = MachineState.On_Working;
                                break;
                        }
                    }
                    else
                    {
                        // No history, enter default
                        // Enter On
                        OnOnEntry?.Invoke();
                        // Enter On_Idle
                        OnIdleEntry?.Invoke();
                        _currentState = MachineState.On_Idle;
                    }
                    return;
                }
                break;
            case MachineState.On:
                if (trigger == MachineTrigger.PowerOff)
                {
                    // Priority: 0, Internal: False
                    // Transition: On -> Off (LCA: none)
                    // Exit On
                    OnOnExit?.Invoke();
                    _currentState = MachineState.Off;
                    return;
                }
                break;
            case MachineState.On_Idle:
                if (trigger == MachineTrigger.StartWork)
                {
                    // Priority: 0, Internal: False
                    // Transition to On_Working with Deep history
                    if (_historyDeep_On_Working.HasValue)
                    {
                        // Restore deep history
                        var targetState = _historyDeep_On_Working.Value;
                        // Enter all states from LCA to historical state
                        switch (targetState)
                        {
                            case MachineState.On_Working_Fast:
                                // Enter On_Working
                                OnWorkingEntry?.Invoke();
                                // Enter On_Working_Fast
                                OnFastEntry?.Invoke();
                                _currentState = MachineState.On_Working_Fast;
                                break;
                            case MachineState.On_Working_Slow:
                                // Enter On_Working
                                OnWorkingEntry?.Invoke();
                                // Enter On_Working_Slow
                                OnSlowEntry?.Invoke();
                                _currentState = MachineState.On_Working_Slow;
                                break;
                        }
                    }
                    else
                    {
                        // No history, enter default
                        // Enter On
                        OnOnEntry?.Invoke();
                        // Enter On_Working
                        OnWorkingEntry?.Invoke();
                        // Enter On_Working_Fast
                        OnFastEntry?.Invoke();
                        _currentState = MachineState.On_Working_Fast;
                    }
                    return;
                }
                // Fallthrough to parent
                goto case MachineState.On;
            case MachineState.On_Working:
                if (trigger == MachineTrigger.Stop)
                {
                    // Priority: 0, Internal: False
                    // Transition: On_Working -> On_Idle (LCA: On)
                    // Save deep history for On_Working
                    _historyDeep_On_Working = _currentState;
                    // Exit On_Working
                    OnWorkingExit?.Invoke();
                    // Enter On_Idle
                    OnIdleEntry?.Invoke();
                    _currentState = MachineState.On_Idle;
                    return;
                }
                // Fallthrough to parent
                goto case MachineState.On;
            case MachineState.On_Working_Fast:
                if (trigger == MachineTrigger.Slow)
                {
                    // Priority: 0, Internal: False
                    // Transition: On_Working_Fast -> On_Working_Slow (LCA: On_Working)
                    // Enter On_Working_Slow
                    OnSlowEntry?.Invoke();
                    _currentState = MachineState.On_Working_Slow;
                    return;
                }
                // Fallthrough to parent
                goto case MachineState.On_Working;
            case MachineState.On_Working_Slow:
                if (trigger == MachineTrigger.Fast)
                {
                    // Priority: 0, Internal: False
                    // Transition: On_Working_Slow -> On_Working_Fast (LCA: On_Working)
                    // Enter On_Working_Fast
                    OnFastEntry?.Invoke();
                    _currentState = MachineState.On_Working_Fast;
                    return;
                }
                // Fallthrough to parent
                goto case MachineState.On_Working;
        }
    }

    private void RestoreFromHistory(string targetState)
    {
        // This method handles runtime dispatch to historical states
        if (targetState == "On")
        {
            if (_history_On.HasValue)
            {
                var historicalState = _history_On.Value;
                // Restore to shallow history
                switch (historicalState)
                {
                    case MachineState.On_Idle:
                        _currentState = MachineState.On_Idle;
                        return;
                    case MachineState.On_Working:
                        _currentState = MachineState.On_Working;
                        return;
                }
            }
            // No history, use initial
            _currentState = MachineState.On_Idle;
        }
        if (targetState == "On_Working")
        {
            if (_historyDeep_On_Working.HasValue)
            {
                // Restore to deep history
                _currentState = _historyDeep_On_Working.Value;
                return;
            }
            // No history, use initial
            _currentState = MachineState.On_Working_Fast;
        }
    }
}
