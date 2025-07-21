namespace Generator.Model;

public class GenerationConfig
{
    public GenerationVariant Variant { get; set; } // Pure lub Basic
    public bool HasOnEntryExit { get; set; }      // Kluczowe dla rozróżnienia Pure/Basic
    public bool IsForced { get; set; }

    public bool HasPayload { get; set; }
    public bool HasExtensions { get; set; }
    public bool IsAsync { get; set; }

}
