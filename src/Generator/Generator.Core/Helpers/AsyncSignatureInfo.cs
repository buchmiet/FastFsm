namespace Generator.Helpers;

/// <summary>
/// Result of analyzing a method signature for asynchrony and validity.
/// </summary>
internal struct AsyncSignatureInfo
{
    /// <summary>
    /// True when the method is asynchronous (returns Task/ValueTask).
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// True when the signature is void-equivalent (void, Task, ValueTask).
    /// Used for Action, OnEntry, and OnExit.
    /// </summary>
    public bool IsVoidEquivalent { get; set; }

    /// <summary>
    /// True when the signature is bool-equivalent (bool, ValueTask&lt;bool&gt;).
    /// Used for Guard.
    /// </summary>
    public bool IsBoolEquivalent { get; set; }

    /// <summary>
    /// True when an invalid <c>async void</c> signature was detected.
    /// </summary>
    public bool IsInvalidAsyncVoid { get; set; }

    /// <summary>
    /// True when an invalid <c>Task&lt;bool&gt;</c> guard signature was detected.
    /// </summary>
    public bool IsInvalidGuardTask { get; set; }
}
