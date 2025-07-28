namespace Generator.Rules.Contexts;

public class MissingPayloadTypeContext(
    string variant,
    bool hasDefaultPayloadType,
    bool hasTriggerPayloadTypes,
    bool isForced)
{
    public string Variant { get; } = variant;
    public bool HasDefaultPayloadType { get; } = hasDefaultPayloadType;
    public bool HasTriggerPayloadTypes { get; } = hasTriggerPayloadTypes;
    public bool IsForced { get; } = isForced;
}