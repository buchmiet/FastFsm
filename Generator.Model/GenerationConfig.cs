namespace Generator.Model;

public class GenerationConfig
{
    public GenerationVariant Variant { get; set; } // Pure lub Basic
    public bool HasOnEntryExit { get; set; }      // Kluczowe dla rozróżnienia Pure/Basic
    public bool IsForced { get; set; }

    public bool HasPayload { get; set; }
    public bool HasExtensions { get; set; }
    public bool IsAsync { get; set; }
    /// <summary>
    /// Whether to treat OperationCanceledException as a failure (true) or as a cancellation (false).
    /// Default: false - cancellation is not treated as failure.
    /// </summary>
    public bool TreatCancellationAsFailure { get; set; } = false;
}
