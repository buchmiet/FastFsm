namespace FastFsm.Tests.Features.Hsm.Common;

public enum HsmState
{
    // Root states
    Idle,
    Working,
    Completed,
    Error,

    // Working substates
    Working_Initializing,
    Working_Processing,
    Working_Validating,

    // For other HSM tests
    Parent,
    Child,
        
    // Menu hierarchy
    Outside,
    Menu,
    Menu_Main,
    Menu_Settings,

    // Parent_A/B hierarchy
    Parent_A,
    Parent_B,

    // Deep hierarchy for DeepHistory tests
    Out,
    Work,
    Work_S1,
    Work_S1_Loading,
    Work_S1_Calc
}

public enum HsmTrigger
{
    // Basic triggers
    Start,
    Process,
    Validate,
    Complete,
    Abort,
    Execute,

    // Navigation triggers
    Enter,
    Next,
    Back,
    Exit,
    Leave,

    // Parent/child triggers
    EnterParent,
    Switch,
    LeaveParent,
    EnterWork,
        
    // Internal transition triggers
    Refresh
}