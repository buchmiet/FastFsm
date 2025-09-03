namespace FastFsm.Tests.Features.Hsm.CompileTime
{
    public enum HsmState
    {
        Working,
        Completed,
        Error,

        Working_Processing,
        Working_Processing_Computing,

        Working_Processing_Computing_Loading,
        Working_Processing_Computing_Calculating,
        Working_Processing_Computing_Storing,
    }

    public enum HsmTrigger
    {
        Process,
        Complete,
        Finish,
        Abort
    }
}