
namespace FastFsm.Runtime;

/// <summary>
/// Result of a transition attempt (for internal use)
/// </summary>
public readonly struct TransitionResult
{
    public readonly bool Success;
    public readonly string? FailureReason;
    
    private TransitionResult(bool success, string? reason = null)
    {
        Success = success;
        FailureReason = reason;
    }
    
    public static TransitionResult Succeeded() => new(true);
    public static TransitionResult Failed(string reason) => new(false, reason);
    
    public static readonly TransitionResult NoTransition = new(false, "No matching transition");
    public static readonly TransitionResult GuardFailed = new(false, "Guard condition failed");
}
