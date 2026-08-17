namespace Generator.Model;

/// <summary>
/// Comprehensive description of a callback method signature,
/// including all possible overload combinations with payload and cancellation token.
/// </summary>
public struct CallbackSignatureInfo
{
    /// <summary>
    /// Whether the callback is async (returns Task/ValueTask).
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Whether the signature is void-equivalent (void, Task, ValueTask).
    /// Used for Action, OnEntry, OnExit callbacks.
    /// </summary>
    public bool IsVoidEquivalent { get; set; }

    /// <summary>
    /// Whether the signature is bool-equivalent (bool, ValueTask<bool>).
    /// Used for Guard callbacks.
    /// </summary>
    public bool IsBoolEquivalent { get; set; }

    /// <summary>
    /// Whether a parameterless overload exists: ()
    /// </summary>
    public bool HasParameterless { get; set; }

    /// <summary>
    /// Whether a payload-only overload exists: (T)
    /// </summary>
    public bool HasPayloadOnly { get; set; }

    /// <summary>
    /// Whether a token-only overload exists: (CancellationToken)
    /// </summary>
    public bool HasTokenOnly { get; set; }

    /// <summary>
    /// Whether a payload+token overload exists: (T, CancellationToken)
    /// </summary>
    public bool HasPayloadAndToken { get; set; }

    /// <summary>
    /// The fully qualified type name of the payload parameter (for T and T,CT overloads).
    /// Null if no payload overloads exist.
    /// </summary>
    public string? PayloadTypeFullName { get; set; }

    /// <summary>
    /// Determines the best overload to call based on available parameters.
    /// </summary>
    /// <param name="hasPayload">Whether a typed payload is available</param>
    /// <param name="hasToken">Whether a cancellation token is available</param>
    /// <returns>The overload type that should be called</returns>
    public OverloadType GetBestOverload(bool hasPayload, bool hasToken)
    {
        // Priority: payload+token → payload → token → parameterless
        if (hasPayload && hasToken && HasPayloadAndToken)
            return OverloadType.PayloadAndToken;

        if (hasPayload && HasPayloadOnly)
            return OverloadType.PayloadOnly;

        if (hasToken && HasTokenOnly)
            return OverloadType.TokenOnly;

        if (HasParameterless)
            return OverloadType.Parameterless;

        return OverloadType.None;
    }

    /// <summary>
    /// Creates an empty signature info (no overloads available).
    /// </summary>
    public static CallbackSignatureInfo Empty => new();
}

/// <summary>
/// Types of callback overloads.
/// </summary>
public enum OverloadType
{
    /// <summary>No matching overload</summary>
    None,
    /// <summary>Parameterless: ()</summary>
    Parameterless,
    /// <summary>Payload only: (T)</summary>
    PayloadOnly,
    /// <summary>Token only: (CancellationToken)</summary>
    TokenOnly,
    /// <summary>Payload and token: (T, CancellationToken)</summary>
    PayloadAndToken
}