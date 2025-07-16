namespace Generator.Rules.Contexts;

public class ForcedVariantValidationContext(
    string variant,  // "Pure", "WithPayload", "Full" etc.
    bool isForced,
    bool hasDefaultPayloadType,
    bool hasTriggerPayloadTypes,
    bool hasOnEntryExit,
    bool hasExtensions,
    bool generateExtensibleVersion)
{
    public string Variant { get; } = variant;
    public bool IsForced { get; } = isForced;
    public bool HasDefaultPayloadType { get; } = hasDefaultPayloadType;
    public bool HasTriggerPayloadTypes { get; } = hasTriggerPayloadTypes;
    public bool HasOnEntryExit { get; } = hasOnEntryExit;
    public bool HasExtensions { get; } = hasExtensions;
    public bool GenerateExtensibleVersion { get; } = generateExtensibleVersion;
}