namespace Generator.Model;

/// <summary>
/// Model representing single transition in state machine.
/// </summary>
public sealed class TransitionModel
{
    #region Core Properties

    /// <summary>
    /// Source state of transition
    /// </summary>
    public string FromState { get; set; } = "";

    /// <summary>
    /// Target state of transition
    /// </summary>
    public string ToState { get; set; } = "";

    /// <summary>
    /// Transition trigger
    /// </summary>
    public string Trigger { get; set; } = "";

    /// <summary>
    /// Whether transition is internal (does not change state)
    /// </summary>
    // Set explicitly only for [InternalTransition]. Self-transitions [Transition(A, T, A)] are NOT internal.
    public bool IsInternal { get; set; }

    /// <summary>
    /// Transition priority (for HSM)
    /// </summary>
    public int Priority { get; set; } = 0;

    #endregion

    #region Callback Methods

    /// <summary>
    /// Name of guard method (transition condition)
    /// </summary>
    public string? GuardMethod { get; set; }

    /// <summary>
    /// Name of action method executed during transition
    /// </summary>
    public string? ActionMethod { get; set; }

    #endregion

    #region Signature Information

    /// <summary>
    /// Complete signature information for the guard method
    /// </summary>
    public CallbackSignatureInfo GuardSignature { get; set; } = CallbackSignatureInfo.Empty;

    /// <summary>
    /// Complete signature information for the action method
    /// </summary>
    public CallbackSignatureInfo ActionSignature { get; set; } = CallbackSignatureInfo.Empty;

    #endregion

    #region Payload Information

    /// <summary>
    /// Expected payload type for this transition (null if no payload expected)
    /// </summary>
    public string? ExpectedPayloadType { get; set; }

    #endregion

    #region Convenience Properties (Derived from Signatures)

    /// <summary>
    /// Whether the guard method is async
    /// </summary>
    public bool GuardIsAsync
    {
        get => GuardSignature.IsAsync;
        set
        {
            var sig = GuardSignature;
            sig.IsAsync = value;
            GuardSignature = sig;
        }
    }

    /// <summary>
    /// Whether the action method is async
    /// </summary>
    public bool ActionIsAsync
    {
        get => ActionSignature.IsAsync;
        set
        {
            var sig = ActionSignature;
            sig.IsAsync = value;
            ActionSignature = sig;
        }
    }

    /// <summary>
    /// Whether the guard expects a payload parameter
    /// </summary>
    public bool GuardExpectsPayload
    {
        get => GuardSignature.HasPayloadOnly || GuardSignature.HasPayloadAndToken;
        set
        {
            var sig = GuardSignature;
            sig.HasPayloadOnly = value;
            GuardSignature = sig;
        }
    }

    /// <summary>
    /// Whether the guard has a parameterless overload
    /// </summary>
    public bool GuardHasParameterlessOverload
    {
        get => GuardSignature.HasParameterless;
        set
        {
            var sig = GuardSignature;
            sig.HasParameterless = value;
            GuardSignature = sig;
        }
    }

    /// <summary>
    /// Whether the action expects a payload parameter
    /// </summary>
    public bool ActionExpectsPayload
    {
        get => ActionSignature.HasPayloadOnly || ActionSignature.HasPayloadAndToken;
        set
        {
            var sig = ActionSignature;
            sig.HasPayloadOnly = value;
            ActionSignature = sig;
        }
    }

    /// <summary>
    /// Whether the action has a parameterless overload
    /// </summary>
    public bool ActionHasParameterlessOverload
    {
        get => ActionSignature.HasParameterless;
        set
        {
            var sig = ActionSignature;
            sig.HasParameterless = value;
            ActionSignature = sig;
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a transition model with all required properties
    /// </summary>
    public static TransitionModel Create(
        string fromState,
        string toState,
        string trigger,
        string? guardMethod = null,
        string? actionMethod = null,
        string? expectedPayloadType = null)
    {
        return new TransitionModel
        {
            FromState = fromState,
            ToState = toState,
            Trigger = trigger,
            GuardMethod = guardMethod,
            ActionMethod = actionMethod,
            ExpectedPayloadType = expectedPayloadType
        };
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString() => $"{FromState} --{Trigger}--> {ToState}";
}